using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>Original small meadow slime. Visual-only geometry; combat is added separately.</summary>
    public sealed class MeadowSlimeVisual : MonoBehaviour
    {
        private Transform body, leftEye, rightEye, mouth, leftFeel, rightFeel;
        private Vector3 basePosition;
        private float phase;
        private Material slimeMat, highlightMat, eyeMat, mouthMat, feelerMat;

        public void Initialize(Shader shader, Color bodyColor, float size = 1f)
        {
            if (body != null) return;
            if (shader == null) shader = Resources.Load<Shader>("PrivateRagnarok/World");
            if (shader == null) throw new System.InvalidOperationException("Slime visual shader is missing.");
            slimeMat = Mat(shader, "Slime body", bodyColor);
            highlightMat = Mat(shader, "Slime highlight", Color.Lerp(bodyColor, Color.white, 0.62f));
            eyeMat = Mat(shader, "Slime eyes", new Color(0.045f, 0.055f, 0.075f));
            mouthMat = Mat(shader, "Slime mouth", new Color(0.20f, 0.055f, 0.07f));
            feelerMat = Mat(shader, "Slime feelers", Color.Lerp(bodyColor, Color.black, 0.18f));

            var visual = new GameObject("Meadow Slime Visual").transform;
            visual.SetParent(transform, false);
            visual.localScale = Vector3.one * Mathf.Max(0.2f, size);
            body = Part("Soft jelly body", PrimitiveType.Sphere, new Vector3(0f, 0.48f, 0f),
                new Vector3(0.92f, 0.76f, 0.82f), slimeMat, visual).transform;
            Part("Top glimmer", PrimitiveType.Sphere, new Vector3(-0.20f, 0.72f, 0.30f),
                new Vector3(0.23f, 0.12f, 0.12f), highlightMat, visual);
            leftEye = Part("Left eye", PrimitiveType.Sphere, new Vector3(-0.19f, 0.53f, 0.365f),
                new Vector3(0.10f, 0.15f, 0.065f), eyeMat, visual).transform;
            rightEye = Part("Right eye", PrimitiveType.Sphere, new Vector3(0.19f, 0.53f, 0.365f),
                new Vector3(0.10f, 0.15f, 0.065f), eyeMat, visual).transform;
            mouth = Part("Tiny smile", PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0.405f),
                new Vector3(0.13f, 0.045f, 0.035f), mouthMat, visual).transform;
            leftFeel = Part("Left feeler", PrimitiveType.Sphere, new Vector3(-0.34f, 0.91f, 0.02f),
                new Vector3(0.12f, 0.18f, 0.12f), feelerMat, visual).transform;
            rightFeel = Part("Right feeler", PrimitiveType.Sphere, new Vector3(0.34f, 0.91f, 0.02f),
                new Vector3(0.12f, 0.18f, 0.12f), feelerMat, visual).transform;
            basePosition = visual.localPosition;
        }

        private static Material Mat(Shader shader, string name, Color color)
        {
            return new Material(shader) { name = name, color = color };
        }

        private static GameObject Part(string label, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return part;
        }

        private void Update()
        {
            if (body == null) return;
            phase += Time.deltaTime * 2.4f;
            var pulse = 1f + Mathf.Sin(phase) * 0.035f;
            body.localScale = new Vector3(0.92f * pulse, 0.76f / pulse, 0.82f * pulse);
            body.localPosition = new Vector3(0f, 0.48f + Mathf.Sin(phase * 2f) * 0.025f, 0f);
            leftFeel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 1.3f) * 13f);
            rightFeel.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(phase * 1.3f) * 13f);
            var blink = Mathf.Sin(phase * 0.7f) > 0.985f ? 0.22f : 1f;
            leftEye.localScale = new Vector3(0.10f, 0.15f * blink, 0.065f);
            rightEye.localScale = new Vector3(0.10f, 0.15f * blink, 0.065f);
        }

        private void OnDestroy()
        {
            if (slimeMat != null) Destroy(slimeMat);
            if (highlightMat != null) Destroy(highlightMat);
            if (eyeMat != null) Destroy(eyeMat);
            if (mouthMat != null) Destroy(mouthMat);
            if (feelerMat != null) Destroy(feelerMat);
        }
    }
}