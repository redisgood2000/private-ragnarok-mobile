using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Fixed isometric follow camera for the mobile MMORPG prototype.
    /// Uses orthographic projection to preserve the classic angled-RPG presentation.
    /// Pinch and mouse wheel adjust zoom; camera rotation remains fixed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float followDistance = 18f;
        [SerializeField, Range(15f, 60f)] private float pitch = 35.264f;
        [SerializeField] private float yaw = 45f;
        // Tighter isometric framing makes the full-body player clearly readable on mobile.
        // Pinch zoom remains available for close inspection and wider exploration.
        [SerializeField, Min(1f)] private float zoom = 4.2f;
        [SerializeField, Min(2f)] private float minZoom = 2.8f;
        [SerializeField, Min(4f)] private float maxZoom = 10f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.10f;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.9f, 0f);

        private Vector3 smoothVelocity;
        private Camera controlledCamera;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
            controlledCamera = GetComponent<Camera>();
            if (controlledCamera == null) controlledCamera = Camera.main;
            ConfigureCamera();
            SnapToTarget();
        }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            if (controlledCamera == null) controlledCamera = Camera.main;
            ConfigureCamera();
        }

        private void ConfigureCamera()
        {
            if (controlledCamera == null) return;
            controlledCamera.orthographic = true;
            controlledCamera.orthographicSize = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            ReadZoomInput();

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + focusOffset;
            var desired = focus - rotation * Vector3.forward * followDistance;

            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref smoothVelocity, smoothTime);
            transform.rotation = rotation;
        }

        private void SnapToTarget()
        {
            if (target == null) return;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + focusOffset;
            transform.SetPositionAndRotation(
                focus - rotation * Vector3.forward * followDistance, rotation);
        }

        private void ReadZoomInput()
        {
            if (controlledCamera == null) return;

            if (Input.touchCount >= 2)
            {
                var first = Input.GetTouch(0);
                var second = Input.GetTouch(1);
                var previousFirst = first.position - first.deltaPosition;
                var previousSecond = second.position - second.deltaPosition;
                var previousGap = Vector2.Distance(previousFirst, previousSecond);
                var currentGap = Vector2.Distance(first.position, second.position);

                if (previousGap > 0.01f)
                    zoom = Mathf.Clamp(zoom * previousGap / Mathf.Max(currentGap, 0.01f),
                        minZoom, maxZoom);
            }
            else
            {
                var wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.001f)
                    zoom = Mathf.Clamp(zoom - wheel * 0.6f, minZoom, maxZoom);
            }

            controlledCamera.orthographicSize = Mathf.Lerp(
                controlledCamera.orthographicSize, zoom, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }
}
