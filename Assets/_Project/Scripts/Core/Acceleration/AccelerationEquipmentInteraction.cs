using System.Collections.Generic;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using TMPro;
using UnityEngine;

namespace EmbodiedPhysicsLab.Core.Acceleration
{
    /// <summary>Maps physical socket occupancy and handle events to the existing authoritative controller.</summary>
    public sealed class AccelerationEquipmentInteraction : MonoBehaviour
    {
        [SerializeField] private AccelerationExperimentController experiment;
        [SerializeField] private SnapObject[] cartItems, bucketItems;
        [SerializeField] private SnapSocket[] cartHomes, bucketHomes, cartSlots, bucketSlots;
        [SerializeField] private AxisSlider angleSlider;
        [SerializeField] private RotaryKnob frictionKnob;
        [SerializeField] private TMP_Text angleReadout, frictionReadout;
        private bool subscribed, synchronizing;

        public IReadOnlyList<SnapObject> CartItems => cartItems;
        public IReadOnlyList<SnapObject> BucketItems => bucketItems;
        public IReadOnlyList<SnapSocket> CartSlots => cartSlots;
        public IReadOnlyList<SnapSocket> BucketSlots => bucketSlots;
        public IReadOnlyList<SnapSocket> CartHomes => cartHomes;
        public IReadOnlyList<SnapSocket> BucketHomes => bucketHomes;
        public AxisSlider AngleSlider => angleSlider;
        public RotaryKnob FrictionKnob => frictionKnob;

        public void Configure(AccelerationExperimentController controller, SnapObject[] carts, SnapObject[] buckets,
            SnapSocket[] cartStorage, SnapSocket[] bucketStorage, SnapSocket[] carSockets, SnapSocket[] bucketSockets,
            AxisSlider slider, RotaryKnob knob, TMP_Text angleText, TMP_Text frictionText)
        {
            Unsubscribe();
            experiment = controller; cartItems = carts; bucketItems = buckets;
            cartHomes = cartStorage; bucketHomes = bucketStorage; cartSlots = carSockets; bucketSlots = bucketSockets;
            angleSlider = slider; frictionKnob = knob; angleReadout = angleText; frictionReadout = frictionText;
            Subscribe();
            ResetEquipment();
        }

        private void OnEnable()
        {
            if (experiment == null) return;
            Subscribe();
            ResetEquipment();
        }
        private void OnDisable() => Unsubscribe();
        private IEnumerable<SnapObject> AllItems => cartItems.Concat(bucketItems);
        private IEnumerable<SnapSocket> AllSockets => cartHomes.Concat(bucketHomes).Concat(cartSlots).Concat(bucketSlots);

        private void Subscribe()
        {
            if (subscribed || experiment == null) return;
            subscribed = true;
            experiment.Changed += Refresh;
            experiment.ResetPerformed += ResetEquipment;
            experiment.StartAllowed = CanStart;
            foreach (var socket in cartSlots.Concat(bucketSlots))
            {
                socket.Snapped.AddListener(OnWeightChanged);
                socket.Unsnapped.AddListener(OnWeightChanged);
            }
            angleSlider.NormalizedChanged.AddListener(OnAngleChanged);
            frictionKnob.NormalizedChanged.AddListener(OnFrictionChanged);
        }
        private void Unsubscribe()
        {
            if (!subscribed || experiment == null) return;
            subscribed = false;
            experiment.Changed -= Refresh;
            experiment.ResetPerformed -= ResetEquipment;
            if (experiment.StartAllowed == CanStart) experiment.StartAllowed = null;
            foreach (var socket in cartSlots.Concat(bucketSlots))
            {
                if (socket == null) continue;
                socket.Snapped.RemoveListener(OnWeightChanged);
                socket.Unsnapped.RemoveListener(OnWeightChanged);
            }
            angleSlider.NormalizedChanged.RemoveListener(OnAngleChanged);
            frictionKnob.NormalizedChanged.RemoveListener(OnFrictionChanged);
        }

        private bool CanStart() => !angleSlider.IsGrabbed && !frictionKnob.IsGrabbed && AllItems.All(x => !x.IsGrabbed);
        private void OnAngleChanged(float value)
        {
            if (synchronizing) return;
            var options = experiment.Settings.anglesDegrees;
            experiment.SetAngleDegrees(options.Min() + (options.Max() - options.Min()) * value);
        }
        private void OnFrictionChanged(float value)
        {
            if (synchronizing) return;
            var options = experiment.Settings.frictionCoefficients;
            experiment.SetFrictionCoefficient(options.Min() + (options.Max() - options.Min()) * value);
        }
        private void OnWeightChanged(SnapObject item)
        {
            if (synchronizing || experiment.State.Phase != AccelerationPhase.Ready) return;
            // A mass dropped through the bucket's mouth settles on the lowest free level.
            var installed = bucketSlots.Where(x => x.Occupant != null).Select(x => x.Occupant).ToArray();
            if (installed.Where((weight, index) => bucketSlots[index].Occupant != weight).Any())
            {
                synchronizing = true;
                foreach (var socket in bucketSlots) if (socket.Occupant != null) socket.Unsnap(socket.Occupant);
                for (int i = 0; i < installed.Length; i++)
                {
                    installed[i].transform.SetPositionAndRotation(bucketSlots[i].SnapPoint.position, bucketSlots[i].SnapPoint.rotation);
                    bucketSlots[i].TrySnap(installed[i]);
                }
                synchronizing = false;
            }
            int carts = cartSlots.Count(x => x.Occupant != null), buckets = bucketSlots.Count(x => x.Occupant != null);
            if (carts != experiment.State.CartWeights) experiment.ChangeCartWeights(carts - experiment.State.CartWeights);
            if (buckets != experiment.State.BucketWeights) experiment.ChangeBucketWeights(buckets - experiment.State.BucketWeights);
        }

        private void Refresh(AccelerationExperimentState state)
        {
            if (synchronizing) return;
            synchronizing = true;
            bool editable = state.Phase == AccelerationPhase.Ready;
            foreach (var socket in AllSockets) socket.SetSnappingEnabled(editable);
            foreach (var item in AllItems) item.SetInteractionEnabled(editable);
            angleSlider.SetInteractionEnabled(editable);
            frictionKnob.SetInteractionEnabled(editable);
            var angles = experiment.Settings.anglesDegrees;
            var frictions = experiment.Settings.frictionCoefficients;
            angleSlider.SetNormalizedValue(Mathf.InverseLerp((float)angles.Min(), (float)angles.Max(), (float)state.Angle));
            float friction = Mathf.InverseLerp((float)frictions.Min(), (float)frictions.Max(), (float)state.Friction);
            frictionKnob.SetAngle(friction * 270f);
            if (angleReadout != null) angleReadout.text = $"木板倾角 θ = {state.Angle:F2}°\n拖动滑块连续调节";
            if (frictionReadout != null) frictionReadout.text = $"摩擦 μ = {state.Friction:F3}\n转动旋钮调节";
            foreach (var socket in cartSlots.Concat(bucketSlots))
            {
                var marker = socket.GetComponentInChildren<Renderer>(true);
                if (marker != null) marker.enabled = socket.Occupant == null;
            }
            synchronizing = false;
        }

        private void ResetEquipment()
        {
            if (experiment == null) return;
            synchronizing = true;
            foreach (var socket in AllSockets) socket.SetSnappingEnabled(false);
            foreach (var item in AllItems) item.SetInteractionEnabled(false);
            foreach (var socket in AllSockets) if (socket.Occupant != null) socket.Unsnap(socket.Occupant);
            RestoreItems(cartItems, cartHomes, cartSlots, experiment.State.CartWeights);
            RestoreItems(bucketItems, bucketHomes, bucketSlots, experiment.State.BucketWeights);
            synchronizing = false;
            Refresh(experiment.State);
        }
        private static void RestoreItems(SnapObject[] items, SnapSocket[] homes, SnapSocket[] slots, int installedCount)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var socket = i < installedCount ? slots[i] : homes[i];
                items[i].transform.SetPositionAndRotation(socket.SnapPoint.position, socket.SnapPoint.rotation);
                socket.SetSnappingEnabled(true);
                socket.TrySnap(items[i]);
                socket.SetSnappingEnabled(false);
            }
        }
    }
}
