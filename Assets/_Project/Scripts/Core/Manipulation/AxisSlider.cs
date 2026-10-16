using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>One-axis travel in the parent's coordinates; outputs normalized and mapped values.</summary>
    [DisallowMultipleComponent]
    public sealed class AxisSlider : GrabbableBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.right;
        [SerializeField] private float minimumTravel = 0f, maximumTravel = 0.25f;
        [SerializeField] private float minimumValue = 0f, maximumValue = 1f;
        [SerializeField, Range(0f, 1f)] private float initialNormalizedValue;
        [SerializeField] private UnityEvent<float> normalizedChanged = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> valueChanged = new UnityEvent<float>();
        private bool initialized;
        private Transform frame;
        private Vector3 origin, initialGrabPoint;
        private float initialGrabTravel;

        public float NormalizedValue { get; private set; }
        public float Value => Mathf.Lerp(minimumValue, maximumValue, NormalizedValue);
        public UnityEvent<float> NormalizedChanged => normalizedChanged;
        public UnityEvent<float> ValueChanged => valueChanged;
        private Vector3 Axis => localAxis.sqrMagnitude > 0.00001f ? localAxis.normalized : Vector3.right;
        private Vector3 LocalPoint(Vector3 world) => frame != null ? frame.InverseTransformPoint(world) : world;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            frame = transform.parent;
            origin = LocalPoint(transform.position);
            NormalizedValue = Mathf.Clamp01(initialNormalizedValue);
            ApplyPosition();
        }

        public void SetNormalizedValue(float value)
        {
            Initialize();
            float next = Mathf.Clamp01(value);
            bool changed = !Mathf.Approximately(next, NormalizedValue);
            NormalizedValue = next;
            ApplyPosition();
            if (changed) { normalizedChanged.Invoke(next); valueChanged.Invoke(Value); }
        }

        private void ApplyPosition()
        {
            var point = origin + Axis * Mathf.Lerp(minimumTravel, maximumTravel, NormalizedValue);
            transform.position = frame != null ? frame.TransformPoint(point) : point;
        }

        protected override void OnGrabStarted(Pose pose)
        {
            Initialize();
            initialGrabPoint = LocalPoint(pose.position);
            initialGrabTravel = Mathf.Lerp(minimumTravel, maximumTravel, NormalizedValue);
        }

        protected override void OnGrabUpdated(Pose pose, float deltaTime)
        {
            float travel = initialGrabTravel + Vector3.Dot(LocalPoint(pose.position) - initialGrabPoint, Axis);
            SetNormalizedValue(Mathf.InverseLerp(minimumTravel, maximumTravel, travel));
        }
    }
}
