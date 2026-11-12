using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Local isometric RPG movement. The left side remains reserved for the virtual
    /// joystick; tapping the right side sets a ground-plane destination.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPrototypeController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 540f;
        [SerializeField, Min(0.05f)] private float destinationStopDistance = 0.18f;
        [SerializeField] private float groundPlaneHeight = 0.05f;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private MobileMovementInput mobileInput;

        private CharacterController controller;
        private Vector3 moveDestination;
        private bool hasMoveDestination;

        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
        public bool NetworkControlled { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mobileInput = GetComponent<MobileMovementInput>();
        }

        private void Update()
        {
            if (NetworkControlled || controller == null || !controller.enabled) return;

            if (mobileInput == null)
                mobileInput = GetComponent<MobileMovementInput>();

            ReadDestinationInput();

            // The virtual joystick (or keyboard in the Editor) takes priority over a
            // previously selected destination and cancels it immediately.
            var input = mobileInput != null
                ? mobileInput.Direction
                : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 direction;
            var movingToDestination = false;
            if (input.sqrMagnitude > 0.0025f)
            {
                hasMoveDestination = false;
                var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                direction = forward * input.y + right * input.x;
            }
            else if (hasMoveDestination)
            {
                var offset = moveDestination - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= destinationStopDistance * destinationStopDistance)
                {
                    hasMoveDestination = false;
                    direction = Vector3.zero;
                }
                else
                {
                    direction = offset.normalized;
                    movingToDestination = true;
                }
            }
            else
            {
                direction = Vector3.zero;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, rotationSpeed * Time.deltaTime);

                var step = moveSpeed * Time.deltaTime;
                if (movingToDestination)
                {
                    var remaining = Vector3.Distance(
                        new Vector3(transform.position.x, 0f, transform.position.z),
                        new Vector3(moveDestination.x, 0f, moveDestination.z));
                    step = Mathf.Min(step, remaining);
                }
                controller.Move(direction.normalized * step);
            }

            // Keep the controller grounded without accumulating downward velocity.
            if (controller.isGrounded)
                controller.Move(Vector3.down * 0.05f);
        }

        private void ReadDestinationInput()
        {
            if (cameraTransform == null)
                return;

            // Android: right-half taps select a destination. The left half belongs to
            // MobileMovementInput, so joystick touches never create accidental targets.
            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began || touch.position.x < Screen.width * 0.5f)
                        continue;

                    TrySetDestination(touch.position);
                    break;
                }
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            else if (Input.GetMouseButtonDown(0))
            {
                TrySetDestination(Input.mousePosition);
            }
#endif
        }

        private void TrySetDestination(Vector2 screenPosition)
        {
            var ray = cameraTransform.GetComponent<Camera>();
            if (ray == null)
                ray = Camera.main;
            if (ray == null)
                return;

            // The current prototype town uses a flat walkable ground plane. Using a
            // plane intersection avoids selecting building walls/roofs as destinations.
            var screenRay = ray.ScreenPointToRay(screenPosition);
            var ground = new Plane(Vector3.up, new Vector3(0f, groundPlaneHeight, 0f));
            if (!ground.Raycast(screenRay, out var distance) || distance < 0f)
                return;

            moveDestination = screenRay.GetPoint(distance);
            moveDestination.y = transform.position.y;
            hasMoveDestination = true;
        }
    }
}
