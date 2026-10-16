using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Free-moving item with smooth holding and optional proximity snap on release.</summary>
    [DisallowMultipleComponent]
    public sealed class SnapObject : GrabbableBehaviour
    {
        [SerializeField] private string snapKey = "";
        [SerializeField, Min(0.01f)] private float followSpeed = 12f;
        [SerializeField] private bool autoSnapWhenNearby = true;
        [SerializeField] private UnityEvent<SnapSocket> socketChanged = new UnityEvent<SnapSocket>();
        private Rigidbody body;
        private bool bodyStateSaved, originalKinematic, originalGravity;
        private Quaternion rotationOffset;

        public string SnapKey => snapKey;
        public SnapSocket Socket { get; private set; }
        public UnityEvent<SnapSocket> SocketChanged => socketChanged;

        protected override void OnGrabStarted(Pose pose)
        {
            if (Socket != null) Socket.Unsnap(this);
            FreezeBody();
            rotationOffset = Quaternion.Inverse(pose.rotation) * transform.rotation;
        }

        protected override void OnGrabUpdated(Pose pose, float deltaTime)
        {
            float fraction = 1f - Mathf.Exp(-followSpeed * deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, pose.position, fraction),
                Quaternion.Slerp(transform.rotation, pose.rotation * rotationOffset, fraction));
        }

        protected override void OnGrabEnded()
        {
            if (!isActiveAndEnabled || !TrySnapNearby()) RestoreBody();
        }

        private void LateUpdate()
        {
            if (IsGrabbed) return;
            if (Socket != null) transform.SetPositionAndRotation(Socket.SnapPoint.position, Socket.SnapPoint.rotation);
            else if (autoSnapWhenNearby) TrySnapNearby();
        }

        public bool TrySnapNearby()
        {
            if (IsGrabbed || Socket != null) return false;
            var socket = SnapSocket.FindNearest(this);
            return socket != null && socket.TrySnap(this);
        }

        internal void Attach(SnapSocket socket)
        {
            FreezeBody();
            Socket = socket;
            transform.SetPositionAndRotation(socket.SnapPoint.position, socket.SnapPoint.rotation);
            socketChanged.Invoke(socket);
        }

        internal void Detach(SnapSocket socket)
        {
            if (Socket != socket) return;
            Socket = null;
            if (!IsGrabbed) RestoreBody();
            socketChanged.Invoke(null);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Socket != null) Socket.Unsnap(this);
            RestoreBody();
        }

        private void FreezeBody()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (body == null || bodyStateSaved) return;
            originalKinematic = body.isKinematic;
            originalGravity = body.useGravity;
            bodyStateSaved = true;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void RestoreBody()
        {
            if (body == null || !bodyStateSaved) return;
            body.isKinematic = originalKinematic;
            body.useGravity = originalGravity;
            bodyStateSaved = false;
        }
    }
}
