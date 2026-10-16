using TMPro;
using UnityEngine;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Camera-centered target selection. Input adapters call TryInteract().</summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float interactionDistance = 3.5f;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private TMP_Text promptText;
        private IInteractable currentTarget;

        public void Configure(Camera camera, TMP_Text prompt, float distance = 3.5f)
        {
            viewCamera = camera;
            promptText = prompt;
            interactionDistance = distance;
        }

        private void Awake()
        {
            if (viewCamera == null && Camera.main != null) viewCamera = Camera.main;
        }

        private void Update()
        {
            currentTarget = FindTarget();
            if (promptText == null) return;
            promptText.enabled = currentTarget != null;
            if (currentTarget != null) promptText.text = $"{currentTarget.InteractionPrompt}  [鼠标左键]";
        }

        public bool TryInteract()
        {
            currentTarget = FindTarget();
            if (currentTarget == null || viewCamera == null) return false;
            currentTarget.Interact(new InteractionContext(transform, viewCamera.transform));
            return true;
        }

        private IInteractable FindTarget()
        {
            if (viewCamera == null) return null;
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return null;
            return hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
