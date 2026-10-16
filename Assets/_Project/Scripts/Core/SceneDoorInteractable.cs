using UnityEngine;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Input-independent entrance; scene loading is delegated to the shared navigator.</summary>
    [RequireComponent(typeof(SceneNavigator))]
    public sealed class SceneDoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationName;

        public string DestinationScene { get => destinationScene; set => destinationScene = value; }
        public string DestinationName { get => destinationName; set => destinationName = value; }
        public string InteractionPrompt => string.IsNullOrEmpty(destinationName) ? "点击进入实验" : $"点击进入：{destinationName}";

        public void Interact(InteractionContext context)
        {
            var navigator = GetComponent<SceneNavigator>();
            navigator.TargetScene = destinationScene;
            navigator.Navigate();
        }
    }
}
