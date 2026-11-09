using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Local third-person movement. Offline demo reads the virtual joystick; editor/PC also supports keyboard axes.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPrototypeController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 540f;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private MobileMovementInput mobileInput;
        private CharacterController controller;

        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
        public bool NetworkControlled { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (mobileInput == null) mobileInput = GetComponent<MobileMovementInput>();
        }

        private void Update()
        {
            if (NetworkControlled || controller == null || !controller.enabled) return;

            // The previous controller only read keyboard axes. Android has no physical
            // WASD input, so consume the on-screen joystick direction on touch devices.
            var input = mobileInput != null && Input.touchCount > 0
                ? mobileInput.Direction
                : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            var direction = forward * input.y + right * input.x;
            if (direction.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, rotationSpeed * Time.deltaTime);
                controller.Move(direction.normalized * (moveSpeed * Time.deltaTime));
            }

            // Keep the controller grounded without accumulating downward velocity.
            if (controller.isGrounded)
                controller.Move(Vector3.down * 0.05f);
        }
    }
}
