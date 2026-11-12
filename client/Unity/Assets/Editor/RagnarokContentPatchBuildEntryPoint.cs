#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RagnarokContentPatchBuildEntryPoint
{
    private const string ContentRoot = "Assets/RemoteContent";
    private const string BundleName = "worldcontent";
    private const string OutputRoot = "Build/ContentPatch";

    /// <summary>
    /// Builds the assets placed under Assets/RemoteContent into a downloadable
    /// Android AssetBundle. At least one prefab named WorldRoot must be included.
    /// </summary>
    public static void BuildAndroidPatch()
    {
        if (!Directory.Exists(ContentRoot))
            Directory.CreateDirectory(ContentRoot);
        AssetDatabase.Refresh();
        GenerateCharacterActorPrefab();

        // The town is generated procedurally by the client. Bake that same original
        // environment into a prefab automatically so a patch can be published even
        // before custom art assets are added to Assets/RemoteContent.
        var worldRootPath = ContentRoot + "/WorldRoot.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(worldRootPath) == null)
            GenerateWorldRootPrefab(worldRootPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var assetPaths = AssetDatabase.GetAllAssetPaths();
        var bundleAssets = new System.Collections.Generic.List<string>();
        var hasWorldRoot = false;
        foreach (var path in assetPaths)
        {
            if (!path.StartsWith(ContentRoot + "/", StringComparison.Ordinal) ||
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                continue;

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && string.Equals(prefab.name, "WorldRoot", StringComparison.Ordinal))
                hasWorldRoot = true;

            importer.assetBundleName = BundleName;
            importer.assetBundleVariant = string.Empty;
            importer.SaveAndReimport();
            bundleAssets.Add(path);
        }

        if (bundleAssets.Count == 0)
            throw new InvalidOperationException("Assets/RemoteContent contains no importable assets.");
        if (!hasWorldRoot)
            throw new InvalidOperationException("Add a prefab named exactly WorldRoot under Assets/RemoteContent before building a patch.");

        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
        if (Directory.Exists(output)) Directory.Delete(output, true);
        Directory.CreateDirectory(output);

        var build = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = bundleAssets.ToArray()
        };
        var manifest = BuildPipeline.BuildAssetBundles(
            output,
            new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.Android);

        if (manifest == null)
            throw new InvalidOperationException("Unity failed to build the Android content AssetBundle.");

        var bundlePath = Path.Combine(output, BundleName);
        if (!File.Exists(bundlePath))
            throw new FileNotFoundException("Expected content bundle was not produced.", bundlePath);

        var hash = ComputeSha256(bundlePath);
        File.WriteAllText(Path.Combine(output, "patch-sha256.txt"), hash + Environment.NewLine);
        File.WriteAllText(Path.Combine(output, "patch-bundle-name.txt"), BundleName + Environment.NewLine);
        Debug.Log("Android content patch built: " + bundlePath + " SHA256=" + hash);
    }


    /// <summary>
    /// Converts the original chibi FBX in RemoteContent into the CharacterActor
    /// prefab consumed by WorldBootstrap. Keeps the source FBX and animation clip
    /// inside the same AssetBundle so the prefab has no external dependencies.
    /// </summary>
    private static void GenerateCharacterActorPrefab()
    {
        const string characterFolder = ContentRoot + "/Characters";
        const string modelPath = characterFolder + "/Cute_Ragnarok_Inspired_Chibi_Swordsman.fbx";
        const string prefabPath = characterFolder + "/CharacterActor.prefab";
        const string controllerPath = characterFolder + "/CharacterActor.controller";

        var modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (modelImporter == null)
        {
            Debug.LogWarning("No chibi swordsman FBX found at " + modelPath + "; keeping current player visual.");
            return;
        }

        modelImporter.animationType = ModelImporterAnimationType.Generic;
        modelImporter.importAnimation = true;
        modelImporter.SaveAndReimport();
        AssetDatabase.Refresh();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
            throw new InvalidOperationException("Chibi swordsman FBX could not be imported: " + modelPath);

        var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && !clip.name.StartsWith("__", StringComparison.Ordinal))
            .ToArray();

        RuntimeAnimatorController runtimeController = null;
        if (clips.Length > 0)
        {
            var oldController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (oldController != null) AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.AddState("Idle");
            state.motion = clips[0];
            controller.layers[0].stateMachine.defaultState = state;
            runtimeController = controller;
        }

        var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("Unity could not instantiate the chibi swordsman FBX.");
        try
        {
            instance.name = "CharacterActor";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            // The Blender source is approximately 3 m tall; scale it to fit the
            // existing 1.8 m player CharacterController.
            instance.transform.localScale = Vector3.one * 0.65f;
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.applyRootMotion = false;
            if (runtimeController != null) animator.runtimeAnimatorController = runtimeController;
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Generated patched CharacterActor prefab: " + prefabPath +
                  " (animation clips: " + clips.Length + ")");
    }

    private static void GenerateWorldRootPrefab(string prefabPath)
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/PrivateRagnarok/World.shader");
        if (shader == null)
            throw new InvalidOperationException("The original world shader is missing.");

        var root = new GameObject("WorldRoot");
        try
        {
            var bootstrap = root.AddComponent<PrivateRagnarok.Client.WorldBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("worldShader").objectReferenceValue = shader;
            serialized.FindProperty("buildOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            bootstrap.BuildEnvironmentForContentPatch();
            UnityEngine.Object.DestroyImmediate(bootstrap);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("Generated remote WorldRoot prefab: " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static string ComputeSha256(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
    }
}
#endif
