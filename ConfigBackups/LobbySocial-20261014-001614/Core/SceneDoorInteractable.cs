using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Lobby entrance that routes the generic interaction contract to a scene.</summary>
    public sealed class SceneDoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationName;

        public string DestinationScene { get => destinationScene; set => destinationScene = value; }
        public string DestinationName { get => destinationName; set => destinationName = value; }
        public string InteractionPrompt => string.IsNullOrEmpty(destinationName) ? "点击进入实验" : $"点击进入：{destinationName}";

        public void Interact(InteractionContext context)
        {
            if (string.IsNullOrWhiteSpace(destinationScene))
            {
                Debug.LogError("SceneDoorInteractable has no destination scene.", this);
                return;
            }
            SceneManager.LoadScene(destinationScene);
        }
    }
}
