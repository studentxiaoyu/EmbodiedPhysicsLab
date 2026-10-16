namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Input-independent contract for objects that can be activated by a player, XR hand, or other interactor.</summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(InteractionContext context);
    }
}
