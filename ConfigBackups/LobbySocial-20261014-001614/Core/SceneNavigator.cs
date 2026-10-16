using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Loads a destination scene when its UI control is activated.</summary>
    public sealed class SceneNavigator : MonoBehaviour
    {
        [SerializeField] private string targetScene;

        public string TargetScene
        {
            get => targetScene;
            set => targetScene = value;
        }

        public void Navigate()
        {
            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogError("SceneNavigator has no target scene.", this);
                return;
            }

            SceneManager.LoadScene(targetScene);
        }
    }
}
