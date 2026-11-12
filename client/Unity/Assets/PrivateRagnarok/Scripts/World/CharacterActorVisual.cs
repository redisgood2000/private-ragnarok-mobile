using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Original stylized novice swordsman. All geometry is generated locally from
    /// Unity primitives; visual parts deliberately have no colliders.
    /// </summary>
    public sealed class CharacterActorVisual : MonoBehaviour
    {
        private Transform visualRoot, leftLegPivot, rightLegPivot, leftArmPivot, rightArmPivot, cape;
        private Vector3 rootBase;
        private Vector3 previousPosition;
        private float phase;
        private Material skin, hair, tunic, tunicDark, gold, leather, pants, boots, steel, steelDark, eye, cloth;

        public void Initialize(Shader shader)
        {
            if (visualRoot != null) return;
            if (shader == null) shader = Resources.Load<Shader>("PrivateRagnarok/World");
            if (shader == null) throw new System.InvalidOperationException("Character visual shader is missing.");
            CreateMaterials(shader);
            previousPosition = transform.position;
            visualRoot = new GameObject("Original Novice Swordsman").transform;
            visualRoot.SetParent(transform, false);
            rootBase = visualRoot.localPosition;
            BuildModel();
        }

        private void CreateMaterials(Shader shader)
        {
            skin = Mat(shader, "Swordsman skin", new Color(0.91f, 0.67f, 0.49f));
            hair = Mat(shader, "Swordsman chestnut hair", new Color(0.20f, 0.075f, 0.035f));
            tunic = Mat(shader, "Novice blue cloth", new Color(0.12f, 0.34f, 0.68f));
            tunicDark = Mat(shader, "Tunic shadow blue", new Color(0.055f, 0.16f, 0.34f));
            gold = Mat(shader, "Warm gold trim", new Color(0.95f, 0.66f, 0.19f));
            leather = Mat(shader, "Oiled brown leather", new Color(0.25f, 0.12f, 0.055f));
            pants = Mat(shader, "Charcoal trousers", new Color(0.12f, 0.15f, 0.21f));
            boots = Mat(shader, "Dark leather boots", new Color(0.16f, 0.075f, 0.035f));
            steel = Mat(shader, "Polished novice steel", new Color(0.72f, 0.79f, 0.84f));
            steelDark = Mat(shader, "Steel bevel shadow", new Color(0.30f, 0.39f, 0.47f));
            eye = Mat(shader, "Dark eyes", new Color(0.075f, 0.045f, 0.03f));
            cloth = Mat(shader, "Ivory undershirt", new Color(0.88f, 0.83f, 0.69f));
        }

        private static Material Mat(Shader shader, string name, Color color)
        {
            return new Material(shader) { name = name, color = color };
        }

        private void BuildModel()
        {
            // Legs use separate pivots so the boot, greave and leg swing as one unit.
            leftLegPivot = Group("Left leg pivot", new Vector3(-0.17f, 0.48f, 0f), visualRoot);
            rightLegPivot = Group("Right leg pivot", new Vector3(0.17f, 0.48f, 0f), visualRoot);
            Part("Left trouser", PrimitiveType.Capsule, new Vector3(0f, -0.06f, 0f), new Vector3(0.22f, 0.34f, 0.23f), pants, leftLegPivot);
            Part("Right trouser", PrimitiveType.Capsule, new Vector3(0f, -0.06f, 0f), new Vector3(0.22f, 0.34f, 0.23f), pants, rightLegPivot);
            Part("Left boot", PrimitiveType.Cube, new Vector3(0f, -0.34f, 0.09f), new Vector3(0.25f, 0.20f, 0.39f), boots, leftLegPivot);
            Part("Right boot", PrimitiveType.Cube, new Vector3(0f, -0.34f, 0.09f), new Vector3(0.25f, 0.20f, 0.39f), boots, rightLegPivot);
            Part("Left shin guard", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(0.17f, 0.22f, 0.035f), steel, leftLegPivot);
            Part("Right shin guard", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(0.17f, 0.22f, 0.035f), steel, rightLegPivot);

            // Layered tunic, collar, belt and small utility pouches.
            Part("Tunic body", PrimitiveType.Capsule, new Vector3(0f, 0.96f, 0f), new Vector3(0.69f, 0.50f, 0.43f), tunic, visualRoot);
            Part("Dark tunic side panel", PrimitiveType.Cube, new Vector3(0f, 0.70f, -0.015f), new Vector3(0.59f, 0.22f, 0.40f), tunicDark, visualRoot);
            Part("Gold tunic hem", PrimitiveType.Cube, new Vector3(0f, 0.60f, 0f), new Vector3(0.63f, 0.075f, 0.42f), gold, visualRoot);
            Part("Ivory collar", PrimitiveType.Cylinder, new Vector3(0f, 1.22f, 0f), new Vector3(0.28f, 0.07f, 0.28f), cloth, visualRoot);
            Part("Leather belt", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.015f), new Vector3(0.68f, 0.105f, 0.45f), leather, visualRoot);
            Part("Gold belt buckle", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.248f), new Vector3(0.15f, 0.13f, 0.035f), gold, visualRoot);
            Part("Left belt pouch", PrimitiveType.Cube, new Vector3(-0.29f, 0.68f, 0.04f), new Vector3(0.16f, 0.20f, 0.18f), leather, visualRoot);
            Part("Right belt pouch", PrimitiveType.Cube, new Vector3(0.29f, 0.68f, 0.04f), new Vector3(0.16f, 0.20f, 0.18f), leather, visualRoot);

            // Head, hair fringe, ears and readable face details.
            Part("Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.28f, 0f), new Vector3(0.19f, 0.13f, 0.19f), skin, visualRoot);
            Part("Face", PrimitiveType.Sphere, new Vector3(0f, 1.54f, 0.015f), new Vector3(0.56f, 0.60f, 0.51f), skin, visualRoot);
            Part("Hair cap", PrimitiveType.Sphere, new Vector3(0f, 1.77f, -0.02f), new Vector3(0.61f, 0.34f, 0.56f), hair, visualRoot);
            Part("Hair fringe", PrimitiveType.Sphere, new Vector3(0f, 1.66f, 0.225f), new Vector3(0.46f, 0.19f, 0.20f), hair, visualRoot);
            Part("Left hair lock", PrimitiveType.Capsule, new Vector3(-0.245f, 1.56f, 0.04f), new Vector3(0.13f, 0.25f, 0.14f), hair, visualRoot);
            Part("Right hair lock", PrimitiveType.Capsule, new Vector3(0.245f, 1.56f, 0.04f), new Vector3(0.13f, 0.25f, 0.14f), hair, visualRoot);
            Part("Left ear", PrimitiveType.Sphere, new Vector3(-0.285f, 1.53f, 0.015f), new Vector3(0.12f, 0.17f, 0.13f), skin, visualRoot);
            Part("Right ear", PrimitiveType.Sphere, new Vector3(0.285f, 1.53f, 0.015f), new Vector3(0.12f, 0.17f, 0.13f), skin, visualRoot);
            Part("Left eye", PrimitiveType.Sphere, new Vector3(-0.115f, 1.55f, 0.254f), new Vector3(0.055f, 0.075f, 0.035f), eye, visualRoot);
            Part("Right eye", PrimitiveType.Sphere, new Vector3(0.115f, 1.55f, 0.254f), new Vector3(0.055f, 0.075f, 0.035f), eye, visualRoot);
            Part("Nose", PrimitiveType.Sphere, new Vector3(0f, 1.46f, 0.27f), new Vector3(0.065f, 0.07f, 0.06f), skin, visualRoot);

            // Arm pivots and articulated leather/steel bracers.
            leftArmPivot = Group("Left arm pivot", new Vector3(-0.43f, 1.12f, 0f), visualRoot);
            rightArmPivot = Group("Right arm pivot", new Vector3(0.43f, 1.12f, 0f), visualRoot);
            BuildArm(leftArmPivot);
            BuildArm(rightArmPivot);
            Part("Left steel pauldron", PrimitiveType.Sphere, new Vector3(-0.43f, 1.18f, 0f), new Vector3(0.30f, 0.22f, 0.31f), steel, visualRoot);
            Part("Right steel pauldron", PrimitiveType.Sphere, new Vector3(0.43f, 1.18f, 0f), new Vector3(0.30f, 0.22f, 0.31f), steel, visualRoot);
            Part("Left pauldron crest", PrimitiveType.Sphere, new Vector3(-0.43f, 1.20f, 0.16f), new Vector3(0.12f, 0.10f, 0.06f), gold, visualRoot);
            Part("Right pauldron crest", PrimitiveType.Sphere, new Vector3(0.43f, 1.20f, 0.16f), new Vector3(0.12f, 0.10f, 0.06f), gold, visualRoot);

            // A compact round shield on the left and a recognizable short sword on the right.
            var shield = Group("Novice shield", new Vector3(-0.60f, 0.68f, 0.20f), visualRoot);
            Part("Shield body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.46f, 0.12f, 0.46f), leather, shield).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("Shield steel rim", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.012f), new Vector3(0.49f, 0.075f, 0.49f), gold, shield).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("Shield boss", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.085f), new Vector3(0.16f, 0.09f, 0.16f), steel, shield);
            Part("Shield emblem", PrimitiveType.Cube, new Vector3(0f, 0f, 0.09f), new Vector3(0.055f, 0.23f, 0.035f), gold, shield);

            var sword = Group("Novice sword", new Vector3(0.57f, 0.57f, 0.22f), visualRoot);
            sword.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Part("Sword grip", PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.085f, 0.20f, 0.085f), leather, sword);
            Part("Sword pommel", PrimitiveType.Sphere, new Vector3(0f, -0.19f, 0f), new Vector3(0.12f, 0.10f, 0.12f), gold, sword);
            Part("Sword crossguard", PrimitiveType.Cube, new Vector3(0f, 0.045f, 0f), new Vector3(0.36f, 0.07f, 0.11f), gold, sword);
            Part("Sword blade", PrimitiveType.Cube, new Vector3(0f, 0.39f, 0f), new Vector3(0.15f, 0.66f, 0.055f), steel, sword);
            Part("Blade fuller", PrimitiveType.Cube, new Vector3(0f, 0.39f, 0.031f), new Vector3(0.035f, 0.55f, 0.012f), steelDark, sword);

            cape = Part("Short blue cape", PrimitiveType.Cube, new Vector3(0f, 0.93f, -0.27f), new Vector3(0.56f, 0.72f, 0.10f), tunicDark, visualRoot).transform;
            cape.localRotation = Quaternion.Euler(8f, 0f, 0f);
            Part("Cape clasp", PrimitiveType.Sphere, new Vector3(0f, 1.19f, -0.23f), new Vector3(0.15f, 0.13f, 0.08f), gold, visualRoot);
            Part("Chest crest", PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.218f), new Vector3(0.14f, 0.14f, 0.045f), gold, visualRoot);
        }

        private void BuildArm(Transform pivot)
        {
            Part("Sleeve", PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.21f, 0.34f, 0.22f), tunic, pivot);
            Part("Leather forearm", PrimitiveType.Capsule, new Vector3(0f, -0.39f, 0.02f), new Vector3(0.17f, 0.23f, 0.18f), leather, pivot);
            Part("Steel bracer", PrimitiveType.Cube, new Vector3(0f, -0.40f, 0.12f), new Vector3(0.20f, 0.17f, 0.055f), steel, pivot);
            Part("Glove", PrimitiveType.Sphere, new Vector3(0f, -0.57f, 0.035f), new Vector3(0.18f, 0.16f, 0.18f), boots, pivot);
        }

        private static Transform Group(string label, Vector3 position, Transform parent)
        {
            var group = new GameObject(label).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
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
            if (visualRoot == null) return;
            var delta = transform.position - previousPosition;
            delta.y = 0f;
            var moving = delta.sqrMagnitude > 0.00001f;
            previousPosition = transform.position;
            phase += Time.deltaTime * (moving ? 10f : 2f);
            var swing = moving ? Mathf.Sin(phase) * 30f : Mathf.Sin(phase) * 1.5f;
            leftLegPivot.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            leftArmPivot.localRotation = Quaternion.Euler(-swing * 0.65f, 0f, 0f);
            rightArmPivot.localRotation = Quaternion.Euler(swing * 0.65f, 0f, 0f);
            visualRoot.localPosition = rootBase + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.035f : Mathf.Sin(phase) * 0.012f);
            cape.localRotation = Quaternion.Euler(8f + Mathf.Sin(phase) * (moving ? 7f : 2f), 0f, 0f);
        }

        private void OnDestroy()
        {
            DestroyMaterials(skin, hair, tunic, tunicDark, gold, leather, pants, boots, steel, steelDark, eye, cloth);
        }

        private static void DestroyMaterials(params Material[] materials)
        {
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}