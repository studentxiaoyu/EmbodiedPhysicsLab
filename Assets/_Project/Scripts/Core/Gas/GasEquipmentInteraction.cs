using TMPro;
using UnityEngine;

namespace EmbodiedPhysicsLab.Core.Gas
{
    /// <summary>Adapts input-independent apparatus controls to the existing gas controller.</summary>
    public sealed class GasEquipmentInteraction : MonoBehaviour
    {
        [SerializeField] private GasExperimentController controller;
        [SerializeField] private AxisSlider pistonSlider;
        [SerializeField] private LeverSwitch thermalLever;
        [SerializeField] private TMP_Text volumeReadout, thermalReadout;
        [SerializeField] private Renderer thermalIndicator;
        private bool synchronizing, started;
        private MaterialPropertyBlock indicatorProperties;
        public AxisSlider PistonSlider => pistonSlider;
        public LeverSwitch ThermalLever => thermalLever;

        public void Configure(GasExperimentController owner, AxisSlider piston, LeverSwitch lever,
            TMP_Text volumeText, TMP_Text thermalText, Renderer indicator)
        {
            Unsubscribe();
            controller = owner; pistonSlider = piston; thermalLever = lever;
            volumeReadout = volumeText; thermalReadout = thermalText; thermalIndicator = indicator;
            Subscribe(); Refresh(controller.State);
        }
        private void OnEnable() { Subscribe(); if (started) Refresh(controller.State); }
        private void Start() { started = true; Refresh(controller.State); }
        private void OnDisable() => Unsubscribe();
        private void Subscribe()
        {
            Unsubscribe();
            if (controller == null || pistonSlider == null || thermalLever == null) return;
            controller.Changed += Refresh; controller.ResetPerformed += ResetEquipment;
            pistonSlider.NormalizedChanged.AddListener(VolumeDragged);
            thermalLever.StateChanged.AddListener(ModeSelected);
        }
        private void Unsubscribe()
        {
            if (controller != null) { controller.Changed -= Refresh; controller.ResetPerformed -= ResetEquipment; }
            if (pistonSlider != null) pistonSlider.NormalizedChanged.RemoveListener(VolumeDragged);
            if (thermalLever != null) thermalLever.StateChanged.RemoveListener(ModeSelected);
        }
        private float VolumeNormalized(int index)
        {
            var s = controller.Settings;
            return Mathf.InverseLerp((float)s.VolumeM3(0), (float)s.VolumeM3(s.VolumeCount - 1), (float)s.VolumeM3(index));
        }
        private void VolumeDragged(float value)
        {
            if (synchronizing) return;
            int nearest = 0; float distance = float.MaxValue;
            for (int i = 0; i < controller.Settings.VolumeCount; i++)
            {
                float d = Mathf.Abs(value - VolumeNormalized(i));
                if (d < distance) { nearest = i; distance = d; }
            }
            controller.SetVolumeIndex(nearest);
            // The physical piston has detents at the configured volume marks.
            Refresh(controller.State);
        }
        private void ModeSelected(int state)
        {
            if (!synchronizing) controller.SetMode(state == 0 ? ThermalMode.Cooling : state == 2 ? ThermalMode.Heating : ThermalMode.HoldConstant);
        }
        private void ResetEquipment()
        {
            if (Application.isPlaying && !started) return;
            var grab = FindFirstObjectByType<DesktopGrabInteractor>();
            if (grab != null && (ReferenceEquals(grab.HeldObject, pistonSlider) || ReferenceEquals(grab.HeldObject, thermalLever))) grab.Release();
            Refresh(controller.State);
        }
        private void Refresh(GasExperimentState state)
        {
            if ((Application.isPlaying && !started) || synchronizing || pistonSlider == null || thermalLever == null) return;
            synchronizing = true;
            try
            {
                pistonSlider.SetNormalizedValue(VolumeNormalized(state.VolumeIndex));
                thermalLever.SetState(state.Mode == ThermalMode.Cooling ? 0 : state.Mode == ThermalMode.Heating ? 2 : 1);
                if (volumeReadout != null) volumeReadout.text = $"V = {state.VolumeM3 * 1000:F1} L\nP = {state.PressurePa / 1000:F3} kPa";
                if (thermalReadout != null) thermalReadout.text = $"{(state.Mode == ThermalMode.Heating ? "加热" : state.Mode == ThermalMode.Cooling ? "冷却" : "恒温维持")}\nT = {state.TemperatureK:F1} K\n目标 {state.TargetTemperatureK:F0} K";
                if (thermalIndicator != null)
                {
                    indicatorProperties ??= new MaterialPropertyBlock();
                    var color = state.Mode == ThermalMode.Heating ? new Color(.95f,.32f,.08f) : state.Mode == ThermalMode.Cooling ? new Color(.12f,.65f,.9f) : new Color(.15f,.72f,.4f);
                    thermalIndicator.GetPropertyBlock(indicatorProperties);
                    indicatorProperties.SetColor("_BaseColor", color); thermalIndicator.SetPropertyBlock(indicatorProperties);
                }
            }
            finally { synchronizing = false; }
        }
    }
}
