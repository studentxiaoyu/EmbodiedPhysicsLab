using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Two or three detents, with an input-independent state setter and event.</summary>
    [DisallowMultipleComponent]
    public sealed class LeverSwitch : GrabbableBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.right;
        [SerializeField, Range(2, 3)] private int stateCount = 3;
        [SerializeField] private float minimumAngle = -35f, maximumAngle = 35f;
        [SerializeField, Range(0, 2)] private int initialState;
        [SerializeField] private UnityEvent<int> stateChanged = new UnityEvent<int>();
        private bool initialized;
        private Transform frame;
        private Quaternion restRotation;
        private Vector3 previousDirection;
        private float dragAngle;
        public int State { get; private set; }
        public int StateCount => Mathf.Clamp(stateCount, 2, 3);
        public UnityEvent<int> StateChanged => stateChanged;
        private Vector3 Axis => localAxis.sqrMagnitude > 0.00001f ? localAxis.normalized : Vector3.right;
        private Vector3 WorldAxis => frame != null ? frame.TransformDirection(Axis) : Axis;
        private float StateAngle => Mathf.Lerp(minimumAngle, maximumAngle, (float)State / (StateCount - 1));
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            frame = transform.parent;
            restRotation = frame != null ? Quaternion.Inverse(frame.rotation) * transform.rotation : transform.rotation;
            State = Mathf.Clamp(initialState, 0, StateCount - 1);
            ApplyRotation();
        }

        public void SetState(int value)
        {
            Initialize();
            int next = Mathf.Clamp(value, 0, StateCount - 1);
            bool changed = State != next;
            State = next;
            ApplyRotation();
            if (changed) stateChanged.Invoke(State);
        }

        private void ApplyRotation() => transform.rotation = (frame != null ? frame.rotation : Quaternion.identity)
            * Quaternion.AngleAxis(StateAngle, Axis) * restRotation;
        private Vector3 Direction(Pose pose) => Vector3.ProjectOnPlane(pose.position - transform.position, WorldAxis);
        protected override void OnGrabStarted(Pose pose)
        {
            Initialize();
            previousDirection = Direction(pose);
            dragAngle = StateAngle;
        }
        protected override void OnGrabUpdated(Pose pose, float deltaTime)
        {
            var direction = Direction(pose);
            if (previousDirection.sqrMagnitude > 0.00001f && direction.sqrMagnitude > 0.00001f)
            {
                dragAngle = Mathf.Clamp(dragAngle + Vector3.SignedAngle(previousDirection, direction, WorldAxis), minimumAngle, maximumAngle);
                SetState(Mathf.RoundToInt(Mathf.InverseLerp(minimumAngle, maximumAngle, dragAngle) * (StateCount - 1)));
            }
            previousDirection = direction;
        }
    }
}
