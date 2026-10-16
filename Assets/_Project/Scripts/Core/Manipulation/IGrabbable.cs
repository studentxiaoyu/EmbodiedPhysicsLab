using UnityEngine;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Input-independent manipulation contract. Poses are expressed in world space.</summary>
    public interface IGrabbable
    {
        bool IsGrabbed { get; }
        bool TryBeginGrab(Transform actor, Pose pose);
        void UpdateGrab(Pose pose, float deltaTime);
        void EndGrab();
    }

    /// <summary>Desktop and future XR adapters share this entry from IInteractable.</summary>
    public interface IGrabInteractor
    {
        IGrabbable HeldObject { get; }
        bool TryGrab(IGrabbable target, Transform origin);
        bool Release();
    }
}
