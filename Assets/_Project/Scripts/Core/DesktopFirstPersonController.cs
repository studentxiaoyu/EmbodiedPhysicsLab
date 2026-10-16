using UnityEngine;
using UnityEngine.InputSystem;

namespace EmbodiedPhysicsLab.Core
{
    /// <summary>Desktop-only movement/look adapter for a CharacterController player.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopFirstPersonController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.2f;
        [SerializeField, Min(0.01f)] private float lookSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [SerializeField, Range(30f, 89f)] private float pitchLimit = 80f;

        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraPivot;
        private float yaw;
        private float pitch;
        private float verticalSpeed;
        private Vector3 eyeLocalPosition;
        [SerializeField, Range(0f, 0.25f)] private float downwardLeanDistance = 0.15f;

        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        public void Configure(CharacterController controller, Transform viewPivot)
        {
            characterController = controller;
            cameraPivot = viewPivot;
        }

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            if (cameraPivot != null)
            {
                eyeLocalPosition = cameraPivot.localPosition;
                pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
            }
            LockCursor();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || characterController == null || cameraPivot == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsCursorLocked) UnlockCursor();
                else LockCursor();
            }
            if (!IsCursorLocked) return;

            ReadLook();
            Move(keyboard);
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReadLook()
        {
            if (Mouse.current == null) return;
            var delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -pitchLimit, pitchLimit);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            // A small desktop neck lean avoids looking into the collar; the base view remains at the rig eyes.
            var lean = Mathf.SmoothStep(0f, downwardLeanDistance, Mathf.InverseLerp(15f, 65f, pitch));
            cameraPivot.localPosition = eyeLocalPosition + Vector3.forward * lean;
        }

        private void Move(Keyboard keyboard)
        {
            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            if (characterController.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed -= gravity * Time.deltaTime;
            var heading = Quaternion.Euler(0f, yaw, 0f);
            var planar = heading * new Vector3(input.x, 0f, input.y) * moveSpeed;
            characterController.Move((planar + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private void OnDisable()
        {
            UnlockCursor();
        }
    }
}


