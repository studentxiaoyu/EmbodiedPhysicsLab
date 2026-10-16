using UnityEngine;
using UnityEngine.InputSystem;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Desktop mouse adapter; XR adapters can call PlayerInteractor.TryInteract() directly.</summary>
    public sealed class DesktopInteractionInput : MonoBehaviour
    {
        [SerializeField] private DesktopFirstPersonController playerController;
        [SerializeField] private PlayerInteractor interactor;

        public void Configure(DesktopFirstPersonController controller, PlayerInteractor playerInteractor)
        {
            playerController = controller;
            interactor = playerInteractor;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (playerController == null || interactor == null) return;

            if (!playerController.IsCursorLocked)
            {
                playerController.LockCursor();
                return;
            }
            interactor.TryInteract();
        }
    }
}
