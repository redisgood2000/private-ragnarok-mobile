using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Builds an original, lightweight chibi adventurer from primitive meshes.
    /// Visual-only children have no colliders; the root CharacterController owns movement.
    /// </summary>
    public sealed class CharacterActorVisual : MonoBehaviour
    {
        private Transform bodyRoot;
        private Transform leftArm, rightArm, leftLeg, rightLeg, cape;
        private Vector3 bodyBasePosition;
        private float phase;
        private Material skin, hair, tunic, trim, leather, pants, boots, metal, eye;

        public void Initialize()
        {
            if (bodyRoot != null) return;
            BuildMaterials();
            bodyRoot = new GameObject("Adventurer Visual").transform;
            bodyRoot.SetParent(transform, false);
            bodyBasePosition = bodyRoot.localPosition;
            BuildBody();
        }

        private void BuildMaterials()
        {
            skin = Mat("Warm skin", new Color(0.91f, 0.66f, 0.48f));
            hair = Mat("Chestnut hair", new Color(0.24f, 0.105f, 0.055f));
            tunic = Mat("Royal blue tunic", new Color(0.12f, 0.32f, 0.62f));
            trim = Mat("Gold trim", new Color(0.92f, 0.66f, 0.20f));
            leather = Mat("Brown leather", new Color(0.28f, 0.15f, 0.08f));
            pants = Mat("Dark trousers", new Color(0.16f, 0.19f, 0.27f));
            boots = Mat("Leather boots", new Color(0.20f, 0.105f, 0.055f));
            metal = Mat("Brushed steel", new Color(0.62f, 0.68f, 0.72f));
            eye = Mat("Eyes", new Color(0.12f, 0.075f, 0.045f));
        }

        private static Material Mat(string label, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = label, color = color };
            return material;
        }

        private void BuildBody()
        {
            // Compact boots and legs; pivots sit at the hips for simple walk-cycle motion.
            leftLeg = Part("Left leg", PrimitiveType.Capsule, new Vector3(-0.17f, 0.43f, 0f),
                new Vector3(0.22f, 0.42f, 0.23f), pants, bodyRoot).transform;
            rightLeg = Part("Right leg", PrimitiveType.Capsule, new Vector3(0.17f, 0.43f, 0f),
                new Vector3(0.22f, 0.42f, 0.23f), pants, bodyRoot).transform;
            Part("Left boot", PrimitiveType.Cube, new Vector3(-0.17f, 0.13f, 0.09f),
                new Vector3(0.25f, 0.20f, 0.39f), boots, bodyRoot);
            Part("Right boot", PrimitiveType.Cube, new Vector3(0.17f, 0.13f, 0.09f),
                new Vector3(0.25f, 0.20f, 0.39f), boots, bodyRoot);

            Part("Tunic torso", PrimitiveType.Capsule, new Vector3(0f, 0.94f, 0f),
                new Vector3(0.68f, 0.49f, 0.40f), tunic, bodyRoot);
            Part("Tunic hem", PrimitiveType.Cube, new Vector3(0f, 0.66f, 0f),
                new Vector3(0.62f, 0.12f, 0.40f), trim, bodyRoot);
            Part("Leather belt", PrimitiveType.Cube, new Vector3(0f, 0.76f, 0.015f),
                new Vector3(0.65f, 0.09f, 0.43f), leather, bodyRoot);
            Part("Belt buckle", PrimitiveType.Cube, new Vector3(0f, 0.76f, 0.24f),
                new Vector3(0.13f, 0.12f, 0.035f), trim, bodyRoot);

            // Head and hair silhouette.
            Part("Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.26f, 0f),
                new Vector3(0.19f, 0.13f, 0.19f), skin, bodyRoot);
            Part("Head", PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0.015f),
                new Vector3(0.57f, 0.61f, 0.52f), skin, bodyRoot);
            Part("Hair cap", PrimitiveType.Sphere, new Vector3(0f, 1.75f, -0.015f),
                new Vector3(0.60f, 0.34f, 0.55f), hair, bodyRoot);
            Part("Hair fringe", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 0.235f),
                new Vector3(0.43f, 0.19f, 0.20f), hair, bodyRoot);
            Part("Left eye", PrimitiveType.Sphere, new Vector3(-0.115f, 1.53f, 0.263f),
                new Vector3(0.055f, 0.075f, 0.035f), eye, bodyRoot);
            Part("Right eye", PrimitiveType.Sphere, new Vector3(0.115f, 1.53f, 0.263f),
                new Vector3(0.055f, 0.075f, 0.035f), eye, bodyRoot);
            Part("Nose", PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0.275f),
                new Vector3(0.07f, 0.07f, 0.06f), skin, bodyRoot);

            leftArm = Part("Left arm", PrimitiveType.Capsule, new Vector3(-0.43f, 0.98f, 0f),
                new Vector3(0.20f, 0.37f, 0.21f), tunic, bodyRoot).transform;
            rightArm = Part("Right arm", PrimitiveType.Capsule, new Vector3(0.43f, 0.98f, 0f),
                new Vector3(0.20f, 0.37f, 0.21f), tunic, bodyRoot).transform;
            Part("Left glove", PrimitiveType.Sphere, new Vector3(-0.45f, 0.60f, 0.02f),
                new Vector3(0.19f, 0.17f, 0.19f), leather, bodyRoot);
            Part("Right glove", PrimitiveType.Sphere, new Vector3(0.45f, 0.60f, 0.02f),
                new Vector3(0.19f, 0.17f, 0.19f), leather, bodyRoot);
            Part("Left shoulder guard", PrimitiveType.Sphere, new Vector3(-0.42f, 1.18f, 0f),
                new Vector3(0.28f, 0.20f, 0.28f), metal, bodyRoot);
            Part("Right shoulder guard", PrimitiveType.Sphere, new Vector3(0.42f, 1.18f, 0f),
                new Vector3(0.28f, 0.20f, 0.28f), metal, bodyRoot);

            cape = Part("Short adventurer cape", PrimitiveType.Cube, new Vector3(0f, 0.93f, -0.25f),
                new Vector3(0.53f, 0.72f, 0.10f), leather, bodyRoot).transform;
            cape.localRotation = Quaternion.Euler(8f, 0f, 0f);
            // Small gold crest makes the front readable at the normal camera distance.
            Part("Chest crest", PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0.205f),
                new Vector3(0.13f, 0.13f, 0.035f), trim, bodyRoot);
        }

        private static GameObject Part(string label, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, Transform parent)
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
            if (bodyRoot == null) return;
            var velocity = new Vector3(transform.position.x, 0f, transform.position.z);
            var moving = false;
            var input = GetComponent<MobileMovementInput>();
            if (input != null) moving = input.Direction.sqrMagnitude > 0.025f;
            phase += Time.deltaTime * (moving ? 10f : 2.2f);
            var swing = moving ? Mathf.Sin(phase) * 28f : Mathf.Sin(phase) * 2f;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.72f, 0f, 0f);
            rightArm.localRotation = Quaternion.Euler(swing * 0.72f, 0f, 0f);
            bodyRoot.localPosition = bodyBasePosition + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.035f : 0f);
            cape.localRotation = Quaternion.Euler(8f + Mathf.Sin(phase) * (moving ? 5f : 1.5f), 0f, 0f);
        }

        private void OnDestroy()
        {
            DestroyMaterials(skin, hair, tunic, trim, leather, pants, boots, metal, eye);
        }

        private static void DestroyMaterials(params Material[] materials)
        {
            foreach (var material in materials)
                if (material != null) Destroy(material);
        }
    }
}