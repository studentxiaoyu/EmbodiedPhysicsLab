using UnityEngine;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Pose adapter only. DesktopInteractionInput routes the existing left-click to release.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class DesktopGrabInteractor : MonoBehaviour, IGrabInteractor
    {
        [Tooltip("Operation point beside and below the view, in camera-local metres.")]
        [SerializeField] private Vector3 operationOffset = new Vector3(0.25f, -0.2f, 1.25f);
        private Transform view;
        private IGrabbable held;
        private DesktopFirstPersonController desktopPlayer;

        public IGrabbable HeldObject => IsHeldAlive ? held : null;
        private bool IsHeldAlive => held != null && (!(held is Object unityObject) || unityObject != null);

        private void Awake() => desktopPlayer = GetComponent<DesktopFirstPersonController>();

        public bool TryGrab(IGrabbable target, Transform origin)
        {
            if (!isActiveAndEnabled || target == null || origin == null || HeldObject != null) return false;
            view = origin;
            if (!target.TryBeginGrab(transform, TargetPose())) return false;
            held = target;
            return true;
        }

        public bool Release()
        {
            if (!IsHeldAlive) { held = null; return false; }
            var target = held;
            held = null;
            view = null;
            target.EndGrab();
            return true;
        }

        private Pose TargetPose() => new Pose(view.TransformPoint(operationOffset), view.rotation);

        private void LateUpdate()
        {
            if (!IsHeldAlive) { held = null; return; }
            if (!held.IsGrabbed) { held = null; view = null; return; }
            if (view == null || (desktopPlayer != null && !desktopPlayer.IsCursorLocked)) { Release(); return; }
            held.UpdateGrab(TargetPose(), Time.deltaTime);
        }

        private void OnDisable() => Release();
    }
}
