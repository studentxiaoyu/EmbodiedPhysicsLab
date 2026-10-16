using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Shared ownership, lifecycle and compatibility with the existing interaction ray.</summary>
    public abstract class GrabbableBehaviour : MonoBehaviour, IGrabbable, IInteractable
    {
        [SerializeField] private string interactionPrompt = "抓取 / 操作";
        [SerializeField] private string lockedPrompt = "实验参数已锁定，请先 Reset";
        [SerializeField] private UnityEvent grabbed = new UnityEvent();
        [SerializeField] private UnityEvent released = new UnityEvent();

        public bool IsGrabbed { get; private set; }
        public bool InteractionEnabled { get; private set; } = true;
        public Transform GrabOwner { get; private set; }
        public string InteractionPrompt => !InteractionEnabled ? lockedPrompt : IsGrabbed ? "再次点击释放" : interactionPrompt;
        public UnityEvent Grabbed => grabbed;
        public UnityEvent Released => released;

        public void SetInteractionEnabled(bool value)
        {
            InteractionEnabled = value;
            if (!value) EndGrab();
        }

        public void Interact(InteractionContext context)
        {
            if (context.Actor != null)
                context.Actor.GetComponent<IGrabInteractor>()?.TryGrab(this, context.Origin);
        }

        public bool TryBeginGrab(Transform actor, Pose pose)
        {
            if (!isActiveAndEnabled || !InteractionEnabled || IsGrabbed || actor == null) return false;
            IsGrabbed = true;
            GrabOwner = actor;
            OnGrabStarted(pose);
            grabbed.Invoke();
            return true;
        }

        public void UpdateGrab(Pose pose, float deltaTime)
        {
            if (IsGrabbed && isActiveAndEnabled) OnGrabUpdated(pose, Mathf.Max(0f, deltaTime));
        }

        public void EndGrab()
        {
            if (!IsGrabbed) return;
            IsGrabbed = false;
            GrabOwner = null;
            OnGrabEnded();
            released.Invoke();
        }

        protected virtual void OnDisable() => EndGrab();
        protected abstract void OnGrabStarted(Pose pose);
        protected abstract void OnGrabUpdated(Pose pose, float deltaTime);
        protected virtual void OnGrabEnded() { }
    }
}
