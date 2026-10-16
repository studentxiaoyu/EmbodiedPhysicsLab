using UnityEngine;
using UnityEngine.Rendering;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Places only this avatar's head renderers on a layer omitted by its local first-person camera.</summary>
    public sealed class FirstPersonAvatarVisibility : MonoBehaviour
    {
        [SerializeField] private Renderer[] headRenderers;
        [SerializeField] private string localHeadLayerName = "LocalAvatarHead";
        private Camera localCamera;
        private int originalCullingMask;
        private bool cameraMaskCaptured;

        public Renderer[] HeadRenderers => headRenderers;
        public string LocalHeadLayerName => localHeadLayerName;

        public void Configure(Renderer[] renderers)
        {
            headRenderers = renderers;
            ApplyHeadLayer();
        }

        public void ConfigureHeadLayer(string layerName)
        {
            localHeadLayerName = layerName;
            ApplyHeadLayer();
        }

        private void OnEnable() => Apply();

        private void OnDisable()
        {
            if (cameraMaskCaptured && localCamera != null)
                localCamera.cullingMask = originalCullingMask;
            cameraMaskCaptured = false;
            localCamera = null;
        }

        private void Apply()
        {
            int layer = LayerMask.NameToLayer(localHeadLayerName);
            if (layer < 0)
            {
                Debug.LogError($"Avatar head layer '{localHeadLayerName}' is not defined.", this);
                return;
            }

            ApplyHeadLayer(layer);
            localCamera = FindLocalCamera();
            if (localCamera == null) return;

            originalCullingMask = localCamera.cullingMask;
            cameraMaskCaptured = true;
            localCamera.cullingMask &= ~(1 << layer);
        }

        private void ApplyHeadLayer()
        {
            int layer = LayerMask.NameToLayer(localHeadLayerName);
            if (layer >= 0) ApplyHeadLayer(layer);
        }

        private void ApplyHeadLayer(int layer)
        {
            if (headRenderers == null) return;
            foreach (var renderer in headRenderers)
            {
                if (renderer == null) continue;
                renderer.gameObject.layer = layer;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private Camera FindLocalCamera()
        {
            foreach (var camera in transform.root.GetComponentsInChildren<Camera>(true))
                if (camera != null && camera.CompareTag("MainCamera"))
                    return camera;
            return null;
        }
    }
}


