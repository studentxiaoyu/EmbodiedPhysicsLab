using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Rotation constrained to one parent-local axis. Drag the operation point around its pivot.</summary>
    [DisallowMultipleComponent]
    public sealed class RotaryKnob : GrabbableBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [SerializeField] private float minimumAngle = -135f, maximumAngle = 135f, initialAngle;
        [SerializeField] private UnityEvent<float> angleChanged = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> normalizedChanged = new UnityEvent<float>();
        private bool initialized;
        private Transform frame;
        private Quaternion restRotation;
        [SerializeField, HideInInspector] private Quaternion referenceRotation = Quaternion.identity;
        [SerializeField, HideInInspector] private bool hasReferenceRotation;
        private Vector3 previousDirection;
        public float Angle { get; private set; }
        public float NormalizedValue => Mathf.InverseLerp(minimumAngle, maximumAngle, Angle);
        public UnityEvent<float> AngleChanged => angleChanged;
        public UnityEvent<float> NormalizedChanged => normalizedChanged;
        private Vector3 Axis => localAxis.sqrMagnitude > 0.00001f ? localAxis.normalized : Vector3.forward;
        private Vector3 WorldAxis => frame != null ? frame.TransformDirection(Axis) : Axis;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            frame = transform.parent;
            if (!hasReferenceRotation)
            {
                referenceRotation = frame != null ? Quaternion.Inverse(frame.rotation) * transform.rotation : transform.rotation;
                hasReferenceRotation = true;
            }
            restRotation = referenceRotation;
            Angle = Mathf.Clamp(initialAngle, minimumAngle, maximumAngle);
            ApplyRotation();
        }

        public void SetAngle(float degrees)
        {
            Initialize();
            float next = Mathf.Clamp(degrees, minimumAngle, maximumAngle);
            bool changed = !Mathf.Approximately(Angle, next);
            Angle = next;
            ApplyRotation();
            if (changed) { angleChanged.Invoke(Angle); normalizedChanged.Invoke(NormalizedValue); }
        }

        private void ApplyRotation() => transform.rotation = (frame != null ? frame.rotation : Quaternion.identity)
            * Quaternion.AngleAxis(Angle, Axis) * restRotation;
        private Vector3 Direction(Pose pose) => Vector3.ProjectOnPlane(pose.position - transform.position, WorldAxis);
        protected override void OnGrabStarted(Pose pose) { Initialize(); previousDirection = Direction(pose); }
        protected override void OnGrabUpdated(Pose pose, float deltaTime)
        {
            var direction = Direction(pose);
            if (previousDirection.sqrMagnitude > 0.00001f && direction.sqrMagnitude > 0.00001f)
                SetAngle(Angle + Vector3.SignedAngle(previousDirection, direction, WorldAxis));
            previousDirection = direction;
        }
    }
}
