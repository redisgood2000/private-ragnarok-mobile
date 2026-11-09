using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Creates an original low-poly medieval town from Unity primitives at runtime.
    /// This is a local visual prototype, not a replica of any copyrighted Ragnarok map.
    /// </summary>
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Header("Prototype")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Vector3 playerSpawn = new Vector3(0f, 0.15f, -7f);
        [SerializeField] private string prototypePlayerName = "Adventurer";
        // Assigned by the editor scene generator as a direct serialized asset reference.
        [SerializeField] private Shader worldShader;

        private Material groundMat, pathMat, wallMat, roofMat, woodMat, stoneMat, accentMat, playerMat;

        private void Start()
        {
            if (buildOnStart) BuildWorld();
        }

        [ContextMenu("Build Prototype World")]
        public void BuildWorld()
        {
            if (transform.childCount > 0) return;
            CreateMaterials();
            BuildGroundAndPaths();
            BuildTownBuildings();
            BuildDecorations();
            BuildLighting();
            BuildPlayer();
            BuildHudAnchor();
        }

        private void CreateMaterials()
        {
            groundMat = MakeMaterial("Meadow", new Color(0.30f, 0.48f, 0.28f));
            pathMat = MakeMaterial("Warm cobblestone", new Color(0.58f, 0.49f, 0.37f));
            wallMat = MakeMaterial("Limestone plaster", new Color(0.78f, 0.69f, 0.52f));
            roofMat = MakeMaterial("Terracotta roof", new Color(0.48f, 0.18f, 0.13f));
            woodMat = MakeMaterial("Dark timber", new Color(0.25f, 0.15f, 0.09f));
            stoneMat = MakeMaterial("Stone", new Color(0.38f, 0.39f, 0.36f));
            accentMat = MakeMaterial("Banner blue", new Color(0.12f, 0.30f, 0.52f));
            playerMat = MakeMaterial("Player tunic", new Color(0.18f, 0.42f, 0.68f));
        }

        private Material MakeMaterial(string materialName, Color color)
        {
            // A direct scene reference avoids runtime name lookup and shader stripping.
            var shader = worldShader;
            if (shader == null)
                shader = Resources.Load<Shader>("PrivateRagnarok/World");
            if (shader == null)
                throw new System.InvalidOperationException(
                    "World shader reference is missing. Rebuild the startup scene so WorldBootstrap.worldShader references Assets/Resources/PrivateRagnarok/World.shader.");
            return new Material(shader) { name = materialName, color = color };
        }

        private void BuildGroundAndPaths()
        {
            CreatePrimitive(PrimitiveType.Cube, "Meadow", new Vector3(0f, -0.35f, 0f),
                new Vector3(64f, 0.6f, 64f), groundMat, transform);
            CreatePrimitive(PrimitiveType.Cube, "Main avenue", new Vector3(0f, -0.02f, 0f),
                new Vector3(7f, 0.12f, 48f), pathMat, transform);
            CreatePrimitive(PrimitiveType.Cube, "Market square", new Vector3(0f, 0.015f, 7f),
                new Vector3(22f, 0.14f, 15f), pathMat, transform);
            CreatePrimitive(PrimitiveType.Cube, "Cross street", new Vector3(0f, 0.02f, 7f),
                new Vector3(44f, 0.12f, 5f), pathMat, transform);
            CreatePrimitive(PrimitiveType.Cube, "North path", new Vector3(0f, 0.02f, 23f),
                new Vector3(7f, 0.12f, 18f), pathMat, transform);
        }

        private void BuildTownBuildings()
        {
            House("West inn", new Vector3(-12f, 0f, 7f), new Vector3(7f, 5f, 8f));
            House("East item shop", new Vector3(12f, 0f, 7f), new Vector3(7f, 5f, 8f));
            House("West residence", new Vector3(-13f, 0f, -7f), new Vector3(8f, 4.5f, 7f));
            House("East residence", new Vector3(13f, 0f, -7f), new Vector3(8f, 4.5f, 7f));
            House("North hall", new Vector3(0f, 0f, 25f), new Vector3(12f, 7f, 8f));
            House("South guardhouse", new Vector3(0f, 0f, -23f), new Vector3(10f, 5f, 7f));
        }

        private void House(string houseName, Vector3 position, Vector3 size)
        {
            var root = new GameObject(houseName);
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            CreatePrimitive(PrimitiveType.Cube, "Plaster walls", position + Vector3.up * (size.y * 0.5f),
                new Vector3(size.x, size.y, size.z), wallMat, root.transform);
            var roof = CreatePrimitive(PrimitiveType.Cube, "Pitched roof block",
                position + Vector3.up * (size.y + 0.55f),
                new Vector3(size.x + 1.2f, 1.1f, size.z + 1.2f), roofMat, root.transform);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, "Timber door",
                position + new Vector3(0f, 1.1f, -size.z * 0.5f - 0.055f),
                new Vector3(1.15f, 2.2f, 0.12f), woodMat, root.transform);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(PrimitiveType.Cube, "Window",
                    position + new Vector3(side * size.x * 0.28f, size.y * 0.55f, -size.z * 0.5f - 0.07f),
                    new Vector3(1.15f, 1.1f, 0.12f), accentMat, root.transform);
            }
        }

        private void BuildDecorations()
        {
            // Fountain and low stone rim in the center of the square.
            CreatePrimitive(PrimitiveType.Cylinder, "Fountain basin",
                new Vector3(0f, 0.25f, 7f), new Vector3(3.2f, 0.5f, 3.2f), stoneMat, transform);
            CreatePrimitive(PrimitiveType.Cylinder, "Fountain water",
                new Vector3(0f, 0.52f, 7f), new Vector3(2.35f, 0.08f, 2.35f), accentMat, transform);
            CreatePrimitive(PrimitiveType.Cylinder, "Fountain pillar",
                new Vector3(0f, 1.15f, 7f), new Vector3(0.55f, 1.3f, 0.55f), stoneMat, transform);

            for (var i = -2; i <= 2; i++)
            {
                var z = -17f + i * 8f;
                CreatePrimitive(PrimitiveType.Cylinder, "Street tree trunk",
                    new Vector3(-19f, 1f, z), new Vector3(0.65f, 2f, 0.65f), woodMat, transform);
                CreatePrimitive(PrimitiveType.Sphere, "Street tree canopy",
                    new Vector3(-19f, 3f, z), new Vector3(3f, 3f, 3f), groundMat, transform);
                CreatePrimitive(PrimitiveType.Cylinder, "Street tree trunk",
                    new Vector3(19f, 1f, z), new Vector3(0.65f, 2f, 0.65f), woodMat, transform);
                CreatePrimitive(PrimitiveType.Sphere, "Street tree canopy",
                    new Vector3(19f, 3f, z), new Vector3(3f, 3f, 3f), groundMat, transform);
            }
        }

        private void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(0.62f, 0.63f, 0.68f);
            var sunObject = new GameObject("Prototype sun");
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
        }

        private void BuildPlayer()
        {
            var player = new GameObject(prototypePlayerName);
            player.transform.SetParent(transform, false);
            player.transform.position = playerSpawn;
            var capsule = CreatePrimitive(PrimitiveType.Capsule, "Adventurer body",
                playerSpawn + Vector3.up, Vector3.one, playerMat, player.transform);
            Destroy(capsule.GetComponent<Collider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            var movement = player.AddComponent<ThirdPersonPrototypeController>();
            var touchInput = player.AddComponent<MobileMovementInput>();
            var network = player.AddComponent<WorldNetworkClient>();
            network.Initialize(player.transform, movement, touchInput);

            var cameraObject = new GameObject("World camera");
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<WorldCameraFollow>();
            follow.Initialize(player.transform);
            movement.CameraTransform = cameraObject.transform;
            network.SetMovementCamera(cameraObject.transform);
        }

        private void BuildHudAnchor()
        {
            var labelObject = new GameObject("Prototype status label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.position = new Vector3(0f, 0.05f, 15f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "TOWN SQUARE  |  LOCAL PROTOTYPE";
            label.characterSize = 0.25f;
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string objectName,
            Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = objectName;
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return instance;
        }
    }
}
