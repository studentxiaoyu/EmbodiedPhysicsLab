using UnityEditor;

namespace EmbodiedPhysicsLab.Editor
{
    /// <summary>Keep the existing menu/CLI entry and validate the physical apparatus workflow.</summary>
    public static class TransformerPlayVerification
    {
        [MenuItem("Embodied Physics Lab/Run Transformer Play Verification")]
        public static void Run() => TransformerPhysicalVerification.Run();
    }
}
