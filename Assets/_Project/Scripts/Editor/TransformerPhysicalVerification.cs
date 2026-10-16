using System;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Transformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    [InitializeOnLoad]
    public static class TransformerPhysicalVerification
    {
        private const string RunningKey = "TransformerPhysicalVerification.Running";
        private static TransformerExperimentController experiment;
        private static TransformerEquipmentInteraction equipment;
        private static GameObject player;
        private static Camera camera;
        private static DesktopGrabInteractor grab;
        private static int initialFrame;
        static TransformerPhysicalVerification() { EditorApplication.playModeStateChanged += OnMode; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(TransformerLabBuilder.Lab, OpenSceneMode.Single);
            SessionState.SetBool(RunningKey, true); EditorApplication.EnterPlaymode();
        }
        private static void OnMode(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false))
            {
                initialFrame = Time.frameCount;
                EditorApplication.update += WaitForStartup;
            }
        }
        private static void WaitForStartup()
        {
            if (Time.frameCount < initialFrame + 2) return;
            EditorApplication.update -= WaitForStartup;
            Verify();
        }
        private static void Verify()
        {
            int result = 0;
            try
            {
                experiment = Object.FindFirstObjectByType<TransformerExperimentController>();
                equipment = experiment.GetComponent<TransformerEquipmentInteraction>();
                player = GameObject.Find("DesktopPlayer"); camera = player.GetComponentInChildren<Camera>(); grab = player.GetComponent<DesktopGrabInteractor>();
                Require(equipment != null && grab != null, "Physical Transformer apparatus is missing.");
                Require(experiment.State.AssemblyReady && equipment.YokeSocket.Occupant == equipment.UpperYoke, "Initial assembly did not restore after all scene objects enabled.");
                Require(!Object.FindObjectsByType<TransformerInteractable>(FindObjectsSortMode.None).Any(x => x.Command != TransformerCommand.Record && x.Command != TransformerCommand.Reset), "Legacy parameter buttons remain in TransformerLab.");
                Near(experiment.State.SelectedInputVoltage, 9); Near(equipment.VoltageKnob.Angle, 135);
                Require(Quaternion.Angle(equipment.VoltageKnob.transform.rotation, Quaternion.AngleAxis(135, Vector3.forward)) < .05f, "Saved default voltage applied knob rotation twice.");
                Require(!experiment.Record() && experiment.State.OutputVoltage == 0, "Source-off record guard failed.");
                var initialCoil = equipment.PrimaryModules[1];
                Require(!initialCoil.TryBeginGrab(player.transform, new Pose()), "Closed upper yoke allowed coil removal.");
                DragVoltage(67.5f); Near(experiment.State.SelectedInputVoltage, 10.5, 1e-4);
                Power(true); Require(experiment.State.PowerOn, "Physical lever did not energize the source.");
                Near(experiment.State.OutputVoltage, 10.5, 1e-4);
                Require(!equipment.UpperYoke.TryBeginGrab(player.transform, OperationPose()) && !initialCoil.TryBeginGrab(player.transform, OperationPose()), "Live parts remained grabbable.");
                Record();

                OpenCore();
                Require(!experiment.State.CoreClosed && experiment.State.OutputVoltage == 0 && !experiment.Record(), "Open-core state allowed voltage or recording.");
                Power(true); Require(!experiment.State.PowerOn && equipment.PowerLever.State == 0, "Incomplete assembly accepted the ON detent.");
                ReplaceCoil(false, 3, true);
                CloseCore(); Power(true); VerifyRatio(); Record();
                Near(experiment.State.OutputVoltage, 21, 1e-4);

                OpenCore(); ReplaceCoil(true, 0, false); CloseCore(); Power(true); VerifyRatio(); Record();
                Near(experiment.State.OutputVoltage, 42, 1e-4);
                OpenCore(); ReplaceCoil(false, 0, false); CloseCore(); Power(true); VerifyRatio(); Record();
                Require(experiment.Recorder.Points.Count == 4, "Four actual turn combinations were not recorded.");
                foreach (var row in experiment.Recorder.Points) Require(row.Error < 1e-9, "Recorded ideal transformer ratio is incorrect.");
                var graph = GameObject.Find("MeasuredRatioCurve").GetComponent<LineRenderer>(); Require(graph.positionCount == 4, "Ratio graph did not update.");
                DragVoltage(67.5f); Near(experiment.State.SelectedInputVoltage, 12, 1e-4); Near(experiment.State.OutputVoltage, 12, 1e-4);
                Require(!experiment.Record() && experiment.Recorder.Points.Count == 4, "Duplicate turn pair was incorrectly recorded.");
                experiment.ResetExperiment();
                VerifyReset(); Require(graph.positionCount == 0, "Reset did not clear the ratio graph.");
                OpenCore(); ReplaceCoil(false, 2, false); CloseCore(); Power(true); Record();
                Require(experiment.Recorder.Points.Count == 1 && experiment.State.SecondaryTurns == 300, "Second round required reloading the scene.");
                experiment.ResetExperiment(); VerifyReset();
                Debug.Log("TRANSFORMER_PHYSICAL_VERIFIED: default saved knob pose, ray pickup/placement of same coil and upper yoke, free-release Rigidbody defaults, continuous voltage drag, lever ON/OFF, assembly/live locks, 4 actual coil combinations, unchanged ideal ratios, graph, reset and second round.");
            }
            catch (Exception error) { Debug.LogException(error); result = 1; }
            SessionState.SetBool(RunningKey, false); EditorApplication.ExitPlaymode(); int code = result;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
        private static void VerifyReset()
        {
            Require(!experiment.State.PowerOn && experiment.Recorder.Points.Count == 0, "Reset did not clear power/data.");
            Require(equipment.PrimarySocket.Occupant == equipment.PrimaryModules[1] && equipment.SecondarySocket.Occupant == equipment.SecondaryModules[1] && equipment.YokeSocket.Occupant == equipment.UpperYoke, "Reset did not physically reassemble default modules.");
            Near(experiment.State.SelectedInputVoltage, 9); Near(equipment.VoltageKnob.Angle, 135);
            Require(equipment.PowerLever.State == 0, "Reset lever remained on.");
            Require(Object.FindObjectsByType<SnapObject>(FindObjectsSortMode.None).Length == 9, "Physical parts were duplicated or removed.");
        }
        private static void VerifyRatio()
        {
            var state = experiment.State;
            Near(state.OutputVoltage, TransformerPhysicsModel.SecondaryVoltage(state.InputVoltage, state.PrimaryTurns, state.SecondaryTurns, true), 1e-9);
            Near(state.VoltageRatio, state.TurnsRatio, 1e-9);
        }
        private static void Record()
        {
            var before = experiment.Recorder.Points.Count;
            var target = Object.FindObjectsByType<TransformerInteractable>(FindObjectsSortMode.None).First(x => x.Command == TransformerCommand.Record);
            Aim(target.transform, Vector3.up * .05f);
            Require(player.GetComponent<PlayerInteractor>().TryInteract() && experiment.Recorder.Points.Count == before + 1, "Physical record button did not save a measurement.");
        }
        private static void OpenCore()
        {
            Power(false);
            Grab(equipment.UpperYoke.transform, Vector3.up * .07f); Place(equipment.UpperYoke, equipment.YokeHome);
            Require(equipment.YokeHome.Occupant == equipment.UpperYoke, "Upper yoke did not reach its storage position.");
        }
        private static void CloseCore() { Grab(equipment.UpperYoke.transform, Vector3.up * .07f); Place(equipment.UpperYoke, equipment.YokeSocket); }
        private static void ReplaceCoil(bool primary, int option, bool dropTest)
        {
            var modules = primary ? equipment.PrimaryModules : equipment.SecondaryModules;
            var homes = primary ? equipment.PrimaryHomes : equipment.SecondaryHomes;
            var mount = primary ? equipment.PrimarySocket : equipment.SecondarySocket;
            var current = mount.Occupant;
            int currentOption = Enumerable.Range(0, modules.Count).First(i => modules[i] == current);
            Grab(current.transform, Vector3.zero); Place(current, homes[currentOption]);
            Require(!(primary ? experiment.State.PrimaryInstalled : experiment.State.SecondaryInstalled), "Removing a coil did not mark it absent.");
            var next = modules[option]; var original = next.transform.position;
            Grab(next.transform, Vector3.zero);
            Require(Vector3.Distance(original, next.transform.position) < .0001f, "Coil pickup teleported the real module.");
            if (dropTest)
            {
                next.UpdateGrab(new Pose(new Vector3(0, 2.2f, 1.8f), Quaternion.identity), .5f); grab.Release();
                var body = next.GetComponent<Rigidbody>(); Require(!body.isKinematic && body.useGravity, "Free coil release did not restore gravity.");
                Grab(next.transform, Vector3.zero);
            }
            Place(next, mount);
            Require(mount.Occupant == next, "The same real module did not occupy its mount.");
            Require((primary ? experiment.State.PrimaryTurns : experiment.State.SecondaryTurns) == experiment.Settings.TurnsAt(option), "Actual installed module did not determine turn count.");
        }
        private static void Power(bool on)
        {
            if (equipment.PowerLever.State == (on ? 1 : 0)) return;
            var lever = equipment.PowerLever;
            Grab(lever.transform, Vector3.up * .13f);
            var pose = OperationPose(); var axis = lever.transform.parent.TransformDirection(Vector3.right);
            var delta = on ? 50f : -50f;
            var vector = pose.position - lever.transform.position;
            lever.UpdateGrab(new Pose(lever.transform.position + Quaternion.AngleAxis(delta, axis) * vector, pose.rotation), .02f);
            grab.Release();
        }
        private static void DragVoltage(float degrees)
        {
            var knob = equipment.VoltageKnob; Grab(knob.transform, Vector3.zero);
            var pose = OperationPose(); var vector = pose.position - knob.transform.position;
            knob.UpdateGrab(new Pose(knob.transform.position + Quaternion.AngleAxis(degrees, Vector3.forward) * vector, pose.rotation), .02f);
            grab.Release();
        }
        private static Pose OperationPose() => new Pose(camera.transform.TransformPoint(new Vector3(.25f, -.23f, .95f)), camera.transform.rotation);
        private static void Aim(Transform target, Vector3 offset)
        {
            player.transform.rotation = Quaternion.identity;
            player.transform.position = new Vector3(target.position.x, .04f, target.position.z - 1.25f);
            camera.transform.rotation = Quaternion.LookRotation(target.position + offset - camera.transform.position);
            Physics.SyncTransforms();
        }
        private static void Grab(Transform target, Vector3 offset)
        {
            Aim(target, offset);
            Require(player.GetComponent<PlayerInteractor>().TryInteract() && grab.HeldObject != null, "Ray did not grab " + target.name);
            Require(ReferenceEquals(grab.HeldObject, target.GetComponent<IGrabbable>()), "Another physical part obstructed the ray to " + target.name);
        }
        private static void Place(SnapObject item, SnapSocket socket)
        {
            for (int i = 0; i < 30; i++) item.UpdateGrab(new Pose(socket.SnapPoint.position, socket.SnapPoint.rotation), .05f);
            Require(grab.Release() && item.Socket == socket, "Release did not snap " + item.name + " to " + socket.name);
        }
        private static void Near(double value, double expected, double tolerance = 1e-8) => Require(Math.Abs(value - expected) < tolerance, $"Expected {expected}, got {value}.");
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
