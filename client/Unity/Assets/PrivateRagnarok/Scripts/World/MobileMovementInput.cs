using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Small left-thumb virtual joystick. Touch coordinates are never sent to the server.</summary>
    public sealed class MobileMovementInput : MonoBehaviour
    {
        [SerializeField, Min(40f)] private float radiusPixels = 68f;
        [SerializeField] private Vector2 centerFromBottomLeft = new Vector2(112f, 126f);
        private int activeFingerId = -1;
        private Vector2 direction;
        private Vector2 activeCenter;
        private Texture2D whiteTexture;

        public Vector2 Direction => direction;

        private void Update()
        {
            if (Input.touchCount == 0)
            {
                activeFingerId = -1;
                direction = Vector2.zero;
                return;
            }

            var found = false;
            foreach (var touch in Input.touches)
            {
                if (touch.fingerId == activeFingerId)
                {
                    found = true;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        activeFingerId = -1;
                        direction = Vector2.zero;
                    }
                    else
                    {
                        var delta = (touch.position - activeCenter) / radiusPixels;
                        direction = Vector2.ClampMagnitude(delta, 1f);
                    }
                    break;
                }
            }

            if (found || activeFingerId != -1) return;
            foreach (var touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began || touch.position.x > Screen.width * 0.5f)
                    continue;
                activeFingerId = touch.fingerId;
                activeCenter = touch.position;
                direction = Vector2.zero;
                break;
            }
        }

        private void OnGUI()
        {
            if (whiteTexture == null)
            {
                const int size = 64;
                whiteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var alpha = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 16f);
                    whiteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                whiteTexture.Apply();
            }

            var center = activeFingerId >= 0
                ? new Vector2(activeCenter.x, Screen.height - activeCenter.y)
                : new Vector2(centerFromBottomLeft.x, Screen.height - centerFromBottomLeft.y);
            var outer = new Rect(center.x - radiusPixels, center.y - radiusPixels,
                radiusPixels * 2f, radiusPixels * 2f);
            var knobCenter = center + new Vector2(direction.x, -direction.y) * radiusPixels * 0.58f;
            var innerRadius = radiusPixels * 0.34f;
            var inner = new Rect(knobCenter.x - innerRadius, knobCenter.y - innerRadius,
                innerRadius * 2f, innerRadius * 2f);

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(outer, whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, 0.42f);
            GUI.DrawTexture(inner, whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void OnDestroy()
        {
            if (whiteTexture != null) Destroy(whiteTexture);
        }
    }
}
