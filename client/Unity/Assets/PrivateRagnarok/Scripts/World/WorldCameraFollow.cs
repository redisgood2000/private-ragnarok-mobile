using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Simple orbit camera for the prototype; mouse in editor, one-finger drag on mobile.</summary>
    public sealed class WorldCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float distance = 6f;
        [SerializeField, Min(0.1f)] private float minDistance = 3f;
        [SerializeField, Min(0.1f)] private float maxDistance = 10f;
        [SerializeField] private float lookSensitivity = 0.18f;
        [SerializeField] private float zoomSensitivity = 0.01f;
        [SerializeField] private float minPitch = 15f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.06f;

        private float yaw;
        private float pitch = 35f;
        private Vector3 smoothVelocity;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                transform.position = target.position + new Vector3(0f, 3f, -distance);
                transform.LookAt(target.position + Vector3.up * 1.2f);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            ReadOrbitInput();

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + Vector3.up * 1.25f;
            var desired = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, smoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(focus - transform.position), 1f - Mathf.Exp(-14f * Time.deltaTime));
        }

        private void ReadOrbitInput()
        {
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    yaw += touch.deltaPosition.x * lookSensitivity;
                    pitch = Mathf.Clamp(pitch - touch.deltaPosition.y * lookSensitivity, minPitch, maxPitch);
                }
            }
            else if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                var previousA = a.position - a.deltaPosition;
                var previousB = b.position - b.deltaPosition;
                var previousGap = Vector2.Distance(previousA, previousB);
                var currentGap = Vector2.Distance(a.position, b.position);
                distance = Mathf.Clamp(distance - (currentGap - previousGap) * zoomSensitivity,
                    minDistance, maxDistance);
            }
            else
            {
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxis("Mouse X") * 3f;
                    pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, minPitch, maxPitch);
                }
                distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.5f, minDistance, maxDistance);
            }
        }
    }
}
