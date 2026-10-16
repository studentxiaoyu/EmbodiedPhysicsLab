using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    [DisallowMultipleComponent]
    public sealed class SnapSocket : MonoBehaviour
    {
        private static readonly HashSet<SnapSocket> Active = new HashSet<SnapSocket>();
        [SerializeField] private Transform snapPoint;
        [SerializeField, Min(0.01f)] private float snapDistance = 0.2f;
        [Tooltip("Empty accepts any SnapObject; otherwise the keys must match.")]
        [SerializeField] private string acceptedKey = "";
        [SerializeField] private bool allowSnapping = true;
        [SerializeField] private UnityEvent<SnapObject> snapped = new UnityEvent<SnapObject>();
        [SerializeField] private UnityEvent<SnapObject> unsnapped = new UnityEvent<SnapObject>();

        public SnapObject Occupant { get; private set; }
        public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
        public UnityEvent<SnapObject> Snapped => snapped;
        public UnityEvent<SnapObject> Unsnapped => unsnapped;
        public void SetSnappingEnabled(bool value) => allowSnapping = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();
        private void OnEnable() => Active.Add(this);
        private void OnDisable()
        {
            Active.Remove(this);
            if (Occupant != null) Unsnap(Occupant);
        }

        public bool CanAccept(SnapObject item)
        {
            return isActiveAndEnabled && allowSnapping && item != null && item.isActiveAndEnabled && !item.IsGrabbed
                && Occupant == null && item.Socket == null
                && (string.IsNullOrEmpty(acceptedKey) || acceptedKey == item.SnapKey)
                && Vector3.Distance(item.transform.position, SnapPoint.position) <= snapDistance;
        }

        public bool TrySnap(SnapObject item)
        {
            if (!CanAccept(item)) return false;
            Occupant = item;
            item.Attach(this);
            snapped.Invoke(item);
            return true;
        }

        public bool Unsnap(SnapObject item)
        {
            if (item == null || Occupant != item) return false;
            Occupant = null;
            item.Detach(this);
            unsnapped.Invoke(item);
            return true;
        }

        public static SnapSocket FindNearest(SnapObject item)
        {
            SnapSocket nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var socket in Active)
            {
                if (socket == null || !socket.CanAccept(item)) continue;
                float candidate = (socket.SnapPoint.position - item.transform.position).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                nearest = socket;
            }
            return nearest;
        }
    }
}
