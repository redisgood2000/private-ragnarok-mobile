#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
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
