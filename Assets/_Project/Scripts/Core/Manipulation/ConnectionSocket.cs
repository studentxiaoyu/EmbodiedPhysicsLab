using UnityEngine;
using UnityEngine.Events;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Logical one-to-one port connection; does not simulate electricity or create wires.</summary>
    [DisallowMultipleComponent]
    public sealed class ConnectionSocket : MonoBehaviour
    {
        [SerializeField] private string connectionType = "Electrical";
        [SerializeField] private UnityEvent<ConnectionSocket> connected = new UnityEvent<ConnectionSocket>();
        [SerializeField] private UnityEvent<ConnectionSocket> disconnected = new UnityEvent<ConnectionSocket>();
        public ConnectionSocket ConnectedTo { get; private set; }
        public UnityEvent<ConnectionSocket> Connected => connected;
        public UnityEvent<ConnectionSocket> Disconnected => disconnected;

        public bool CanConnect(ConnectionSocket other) => isActiveAndEnabled && other != null && other != this
            && other.isActiveAndEnabled && ConnectedTo == null && other.ConnectedTo == null
            && connectionType == other.connectionType;

        public bool TryConnect(ConnectionSocket other)
        {
            if (!CanConnect(other)) return false;
            ConnectedTo = other;
            other.ConnectedTo = this;
            connected.Invoke(other);
            other.connected.Invoke(this);
            return true;
        }

        public void Disconnect()
        {
            var other = ConnectedTo;
            if (other == null) { ConnectedTo = null; return; }
            ConnectedTo = null;
            if (other.ConnectedTo == this) other.ConnectedTo = null;
            disconnected.Invoke(other);
            other.disconnected.Invoke(this);
        }

        private void OnDisable() => Disconnect();
    }
}
