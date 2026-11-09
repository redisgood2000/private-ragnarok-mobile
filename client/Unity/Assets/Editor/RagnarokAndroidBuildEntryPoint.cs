#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public static class RagnarokAndroidBuildEntryPoint
{
    private const string ScenePath = "Assets/PrivateRagnarok/Scenes/Prototype.unity";
    private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Private Ragnarok/Generate Prototype Scene")]
    public static void GenerateScene()
    {
        Directory.CreateDirectory("Assets/PrivateRagnarok/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var font = UiFont;
        if (font == null)
            throw new System.Exception("Unity built-in UI font could not be loaded.");

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var world = new GameObject("WorldBootstrap");
        var worldBootstrap = world.AddComponent<PrivateRagnarok.Client.WorldBootstrap>();
        var worldShader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/PrivateRagnarok/World.shader");
        if (worldShader == null)
            throw new System.Exception("Required world shader asset could not be imported at Assets/Resources/PrivateRagnarok/World.shader.");
        var worldSerialized = new SerializedObject(worldBootstrap);
        worldSerialized.FindProperty("worldShader").objectReferenceValue = worldShader;
        worldSerialized.FindProperty("buildOnStart").boolValue = false;
        worldSerialized.ApplyModifiedPropertiesWithoutUndo();

        var auth = new GameObject("Authentication");
        var authClient = auth.AddComponent<PrivateRagnarok.Client.ApiAuthClient>();

        var canvasObject = new GameObject("LoginCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = Rect("LoginPanel", canvasObject.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 760));
        panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.94f);

        var heading = MakeText("PRIVATE RAGNAROK", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -105), new Vector2(680, 100), 52, font);
        heading.alignment = TextAnchor.MiddleCenter;
        heading.color = new Color(0.95f, 0.78f, 0.38f);

        var demoButton = MakeButton("PLAY DEMO", panel, -340, new Color(0.16f, 0.42f, 0.28f), font);
        demoButton.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 112);
        var demoLabel = demoButton.GetComponentInChildren<Text>();
        demoLabel.fontSize = 36;

        var status = MakeText("Tap PLAY DEMO to enter the world", panel,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 35),
            new Vector2(680, 58), 19, font);
        status.alignment = TextAnchor.MiddleCenter;
        status.color = Color.white;

        // Modal update prompt: the updater displays it only after finding a newer APK.
        var prompt = Rect("UpdatePrompt", canvasObject.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
        prompt.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.98f);
        var promptHeading = MakeText("GAME UPDATE AVAILABLE", prompt,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70),
            new Vector2(680, 80), 34, font);
        promptHeading.alignment = TextAnchor.MiddleCenter;
        promptHeading.color = new Color(0.95f, 0.78f, 0.38f);
        var updateDetails = MakeText("", prompt, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(660, 220), 18, font);
        updateDetails.alignment = TextAnchor.MiddleCenter;
        updateDetails.color = Color.white;
        var updateButton = MakeButton("YES - UPDATE NOW", prompt, -360,
            new Color(0.12f, 0.48f, 0.30f), font);
        updateButton.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 82);
        updateButton.GetComponentInChildren<Text>().fontSize = 27;
        var laterButton = MakeButton("NO - LATER", prompt, -460,
            new Color(0.25f, 0.28f, 0.32f), font);
        laterButton.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 72);
        laterButton.GetComponentInChildren<Text>().fontSize = 25;
        prompt.gameObject.SetActive(false);

        var appServices = new GameObject("AppServices");
        appServices.AddComponent<PrivateRagnarok.Client.RemoteContentPatcher>();
        var updater = appServices.AddComponent<PrivateRagnarok.Client.AndroidUpdateController>();
        var updaterSerialized = new SerializedObject(updater);
        updaterSerialized.FindProperty("statusText").objectReferenceValue = status;
        updaterSerialized.FindProperty("updateDetailsText").objectReferenceValue = updateDetails;
        updaterSerialized.FindProperty("updateButton").objectReferenceValue = updateButton;
        updaterSerialized.FindProperty("laterButton").objectReferenceValue = laterButton;
        updaterSerialized.FindProperty("updatePromptPanel").objectReferenceValue = prompt.gameObject;
        updaterSerialized.ApplyModifiedPropertiesWithoutUndo();

        var controller = appServices.AddComponent<PrivateRagnarok.Client.LoginScreenController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("loginPanel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("demoButton").objectReferenceValue = demoButton;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Prototype scene generated at " + ScenePath);
    }

    public static void Build()
    {
        GenerateScene();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        PlayerSettings.productName = "Private Ragnarok";
        PlayerSettings.companyName = "PrivateRagnarok";
        PlayerSettings.applicationIdentifier = "com.privateragnarok.mobile";
        var buildNumberFile = Path.Combine(Application.dataPath, "Editor", "BuildNumber.txt");
        if (!File.Exists(buildNumberFile))
            throw new FileNotFoundException("Build number file was not generated by the GitHub Actions workflow.", buildNumberFile);
        var runNumberText = File.ReadAllText(buildNumberFile).Trim();
        if (!int.TryParse(runNumberText, out var buildNumber) || buildNumber < 1)
            throw new Exception("BuildNumber.txt is missing or invalid; refusing to build a non-upgradable APK.");
        PlayerSettings.bundleVersion = "0.1." + buildNumber;
        PlayerSettings.Android.bundleVersionCode = buildNumber;

        // GameCI supplies these values to the Unity process, but this custom
        // BuildPlayer entry point must explicitly apply them to PlayerSettings.
        // Otherwise Unity silently falls back to its debug signing certificate.
        var keystoreFile = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_NAME");
        var keystorePassword = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        var keyAlias = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
        var keyAliasPassword = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
        if (string.IsNullOrWhiteSpace(keystoreFile) ||
            string.IsNullOrWhiteSpace(keystorePassword) ||
            string.IsNullOrWhiteSpace(keyAlias) ||
            string.IsNullOrWhiteSpace(keyAliasPassword))
            throw new Exception("Release signing configuration is incomplete.");

        var keystoreCandidates = new[]
        {
            Path.GetFullPath(keystoreFile),
            Path.GetFullPath(Path.Combine("client/Unity", keystoreFile)),
            Path.GetFullPath(Path.Combine("..", "..", keystoreFile))
        };
        var resolvedKeystore = Array.Find(keystoreCandidates, File.Exists);
        if (string.IsNullOrEmpty(resolvedKeystore))
            throw new FileNotFoundException("Configured Android release keystore was not found.", keystoreFile);

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = resolvedKeystore;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyAliasPassword;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/PrivateRagnarok.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Android build failed: " + report.summary.result);
        Debug.Log("APK created: " + report.summary.outputPath);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text MakeText(string value, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, int fontSize, Font font)
    {
        var rect = Rect(value, parent, anchorMin, anchorMax, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static InputField MakeInput(string placeholder, Transform parent, float y, Font font)
    {
        var rect = Rect(placeholder, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, y), new Vector2(520, 66));
        var background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.12f, 0.15f, 0.19f, 1f);

        var text = MakeText("", rect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-28, -16), 25, font);
        text.color = Color.white;
        var hint = MakeText(placeholder, rect, Vector2.zero, Vector2.one, Vector2.zero,
            new Vector2(-28, -16), 23, font);
        hint.fontStyle = FontStyle.Italic;
        hint.color = new Color(0.65f, 0.68f, 0.72f);

        var field = rect.gameObject.AddComponent<InputField>();
        field.textComponent = text;
        field.placeholder = hint;
        field.targetGraphic = background;
        field.lineType = InputField.LineType.SingleLine;
        return field;
    }

    private static Button MakeButton(string label, Transform parent, float y, Color color, Font font)
    {
        var rect = Rect(label, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, y), new Vector2(245, 58));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var text = MakeText(label, rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 23, font);
        text.alignment = TextAnchor.MiddleCenter;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
#endif
