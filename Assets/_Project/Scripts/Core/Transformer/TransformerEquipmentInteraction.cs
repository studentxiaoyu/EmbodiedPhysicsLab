using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace EmbodiedPhysicsLab.Core.Transformer
{
    /// <summary>Physical assembly/input adapter; the experiment controller remains the source of electrical state.</summary>
    public sealed class TransformerEquipmentInteraction : MonoBehaviour
    {
        [SerializeField] private TransformerExperimentController experiment;
        [SerializeField] private TransformerExperimentVisual visual;
        [SerializeField] private SnapObject[] primaryModules, secondaryModules;
        [SerializeField] private SnapSocket[] primaryHomes, secondaryHomes;
        [SerializeField] private SnapSocket primarySocket, secondarySocket, yokeSocket, yokeHome;
        [SerializeField] private SnapObject upperYoke;
        [SerializeField] private RotaryKnob voltageKnob;
        [SerializeField] private LeverSwitch powerLever;
        [SerializeField] private TMP_Text assemblyReadout;
        private bool synchronizing, subscribed, started;

        public IReadOnlyList<SnapObject> PrimaryModules => primaryModules;
        public IReadOnlyList<SnapObject> SecondaryModules => secondaryModules;
        public IReadOnlyList<SnapSocket> PrimaryHomes => primaryHomes;
        public IReadOnlyList<SnapSocket> SecondaryHomes => secondaryHomes;
        public SnapSocket PrimarySocket => primarySocket;
        public SnapSocket SecondarySocket => secondarySocket;
        public SnapSocket YokeSocket => yokeSocket;
        public SnapSocket YokeHome => yokeHome;
        public SnapObject UpperYoke => upperYoke;
        public RotaryKnob VoltageKnob => voltageKnob;
        public LeverSwitch PowerLever => powerLever;
        private IEnumerable<SnapObject> AllModules => primaryModules.Concat(secondaryModules);
        private IEnumerable<SnapSocket> AllSockets => primaryHomes.Concat(secondaryHomes).Concat(new[] { primarySocket, secondarySocket, yokeSocket, yokeHome });

        public void Configure(TransformerExperimentController controller, TransformerExperimentVisual display,
            SnapObject[] primary, SnapObject[] secondary, SnapSocket[] primaryStorage, SnapSocket[] secondaryStorage,
            SnapSocket primaryMount, SnapSocket secondaryMount, SnapObject yoke, SnapSocket yokeMount, SnapSocket yokeStorage,
            RotaryKnob voltage, LeverSwitch power, TMP_Text readout)
        {
            Unsubscribe(); experiment = controller; visual = display;
            primaryModules = primary; secondaryModules = secondary; primaryHomes = primaryStorage; secondaryHomes = secondaryStorage;
            primarySocket = primaryMount; secondarySocket = secondaryMount; upperYoke = yoke; yokeSocket = yokeMount; yokeHome = yokeStorage;
            voltageKnob = voltage; powerLever = power; assemblyReadout = readout;
            Subscribe(); ResetEquipment();
        }
        private void OnEnable()
        {
            if (experiment == null) return;
            Subscribe();
            if (started) ResetEquipment();
        }
        // All scene roots/sockets must be enabled before rebuilding the physical assembly.
        private void Start() { started = true; ResetEquipment(); }
        private void OnDisable() => Unsubscribe();
        private void Subscribe()
        {
            if (subscribed || experiment == null) return;
            subscribed = true;
            experiment.Changed += Refresh; experiment.ResetPerformed += ResetEquipment;
            foreach (var socket in new[] { primarySocket, secondarySocket, yokeSocket })
            {
                socket.Snapped.AddListener(OnAssemblyChanged); socket.Unsnapped.AddListener(OnAssemblyChanged);
            }
            voltageKnob.NormalizedChanged.AddListener(OnVoltageChanged);
            powerLever.StateChanged.AddListener(OnPowerChanged);
        }
        private void Unsubscribe()
        {
            if (!subscribed || experiment == null) return;
            subscribed = false;
            experiment.Changed -= Refresh; experiment.ResetPerformed -= ResetEquipment;
            foreach (var socket in new[] { primarySocket, secondarySocket, yokeSocket })
            {
                if (socket == null) continue;
                socket.Snapped.RemoveListener(OnAssemblyChanged); socket.Unsnapped.RemoveListener(OnAssemblyChanged);
            }
            if (voltageKnob != null) voltageKnob.NormalizedChanged.RemoveListener(OnVoltageChanged);
            if (powerLever != null) powerLever.StateChanged.RemoveListener(OnPowerChanged);
        }
        private void OnVoltageChanged(float normalized)
        {
            if (synchronizing) return;
            var options = experiment.Settings.inputVoltageOptions;
            experiment.SetInputVoltage(options.Min() + (options.Max() - options.Min()) * normalized);
        }
        private void OnPowerChanged(int state) { if (!synchronizing) experiment.SetPower(state == 1); }
        private void OnAssemblyChanged(SnapObject item)
        {
            if (synchronizing) return;
            experiment.SetAssembly(Array.IndexOf(primaryModules, primarySocket.Occupant), Array.IndexOf(secondaryModules, secondarySocket.Occupant), yokeSocket.Occupant == upperYoke);
        }

        private void Refresh(TransformerExperimentState state)
        {
            if (Application.isPlaying && !started) return;
            if (synchronizing) return;
            synchronizing = true;
            bool off = !state.PowerOn, open = yokeSocket.Occupant == null;
            foreach (var home in primaryHomes.Concat(secondaryHomes)) home.SetSnappingEnabled(off);
            yokeHome.SetSnappingEnabled(off);
            primarySocket.SetSnappingEnabled(off && open); secondarySocket.SetSnappingEnabled(off && open);
            yokeSocket.SetSnappingEnabled(off && primarySocket.Occupant != null && secondarySocket.Occupant != null);
            foreach (var module in AllModules)
            {
                bool installed = module.Socket == primarySocket || module.Socket == secondarySocket;
                module.SetInteractionEnabled(off && (!installed || open));
            }
            upperYoke.SetInteractionEnabled(off);
            var options = experiment.Settings.inputVoltageOptions;
            voltageKnob.SetAngle(Mathf.InverseLerp(options.Min(), options.Max(), (float)state.SelectedInputVoltage) * 270f);
            powerLever.SetState(state.PowerOn ? 1 : 0);
            var primary = primarySocket.Occupant;
            var secondary = secondarySocket.Occupant;
            visual.SetActiveCoils(CopperRenderers(primary), CopperRenderers(secondary));
            if (assemblyReadout != null)
                assemblyReadout.text = $"原线圈 {(state.PrimaryInstalled ? state.PrimaryTurns + " 匝" : "未安装")}  |  副线圈 {(state.SecondaryInstalled ? state.SecondaryTurns + " 匝" : "未安装")}\n{(state.PowerOn ? "通电测量中，请先断电再换装" : state.CoreClosed ? "上铁轭已合上；换线圈前先移开" : "上铁轭已移开，可更换线圈模块")}";
            synchronizing = false;
        }
        private static Renderer[] CopperRenderers(SnapObject module) => module == null ? Array.Empty<Renderer>()
            : module.GetComponentsInChildren<Renderer>().Where(x => x.name.StartsWith("Copper")).ToArray();

        private void ResetEquipment()
        {
            if (experiment == null) return;
            if (Application.isPlaying && !started) return;
            synchronizing = true;
            foreach (var socket in AllSockets) socket.SetSnappingEnabled(false);
            foreach (var item in AllModules) item.SetInteractionEnabled(false);
            upperYoke.SetInteractionEnabled(false);
            foreach (var socket in AllSockets) if (socket.Occupant != null) socket.Unsnap(socket.Occupant);
            for (int i = 0; i < primaryModules.Length; i++)
                Place(primaryModules[i], i == experiment.Settings.defaultPrimaryTurnsIndex ? primarySocket : primaryHomes[i]);
            for (int i = 0; i < secondaryModules.Length; i++)
                Place(secondaryModules[i], i == experiment.Settings.defaultSecondaryTurnsIndex ? secondarySocket : secondaryHomes[i]);
            Place(upperYoke, yokeSocket);
            synchronizing = false;
            OnAssemblyChanged(null);
            Refresh(experiment.State);
        }
        private static void Place(SnapObject item, SnapSocket socket)
        {
            item.transform.SetPositionAndRotation(socket.SnapPoint.position, socket.SnapPoint.rotation);
            socket.SetSnappingEnabled(true); socket.TrySnap(item); socket.SetSnappingEnabled(false);
        }
    }
}
