using UnityEngine;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Context supplied by any interaction input source.</summary>
    public readonly struct InteractionContext
    {
        public Transform Actor { get; }
        public Transform Origin { get; }

        public InteractionContext(Transform actor, Transform origin)
        {
            Actor = actor;
            Origin = origin;
        }
    }
}
