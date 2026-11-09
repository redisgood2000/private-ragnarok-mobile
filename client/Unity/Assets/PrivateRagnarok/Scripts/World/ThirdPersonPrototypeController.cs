using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Local input/animation prototype only; server must own validated world position.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPrototypeController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 540f;
        [SerializeField] private Transform cameraTransform;
        private CharacterController controller;

        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            var direction = forward * input.y + right * input.x;
            if (direction.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, rotationSpeed * Time.deltaTime);
                controller.Move(direction.normalized * (moveSpeed * Time.deltaTime));
            }
            if (controller.isGrounded && controller.velocity.y < 0f)
                controller.Move(Vector3.down * 0.05f);
        }
    }
}
