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
        world.AddComponent<PrivateRagnarok.Client.WorldBootstrap>();

        var auth = new GameObject("Authentication");
        var authClient = auth.AddComponent<PrivateRagnarok.Client.ApiAuthClient>();

        var canvasObject = new GameObject("LoginCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = Rect("LoginPanel", canvasObject.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 760));
        panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.94f);

        var heading = MakeText("PRIVATE RAGNAROK", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -65), new Vector2(560, 70), 42, font);
        heading.alignment = TextAnchor.MiddleCenter;
        heading.color = new Color(0.95f, 0.78f, 0.38f);

        var subtitle = MakeText("Enter the world with your party", panel, new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(560, 42), 22, font);
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = Color.white;

        var serverUrl = MakeInput("Game server HTTPS URL", panel, -170, font);
        var username = MakeInput("Username", panel, -245, font);
        var password = MakeInput("Password (12+ characters)", panel, -320, font);
        password.contentType = InputField.ContentType.Password;
        var invite = MakeInput("Invite code (registration only)", panel, -395, font);

        var loginButton = MakeButton("LOGIN", panel, -475, new Color(0.16f, 0.36f, 0.57f), font);
        var registerButton = MakeButton("REGISTER", panel, -545, new Color(0.34f, 0.25f, 0.12f), font);
        var demoButton = MakeButton("PLAY DEMO", panel, -615, new Color(0.16f, 0.42f, 0.28f), font);
        var status = MakeText("Enter server URL. Demo mode needs no account.", panel,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(570, 54), 17, font);
        status.alignment = TextAnchor.MiddleCenter;
        status.color = Color.white;

        var updateDetails = MakeText("", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(570, 100), 16, font);
        updateDetails.alignment = TextAnchor.MiddleCenter;
        updateDetails.color = new Color(0.95f, 0.82f, 0.52f);
        var updateButton = MakeButton("UPDATE GAME", panel, 205, new Color(0.12f, 0.48f, 0.30f), font);
        updateButton.gameObject.SetActive(false);
        var laterButton = MakeButton("LATER", panel, 145, new Color(0.25f, 0.28f, 0.32f), font);
        laterButton.gameObject.SetActive(false);
        var updater = auth.AddComponent<PrivateRagnarok.Client.AndroidUpdateController>();
        updater.Initialize(status, updateDetails, updateButton, laterButton);

        var controller = auth.AddComponent<PrivateRagnarok.Client.LoginScreenController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("authClient").objectReferenceValue = authClient;
        serialized.FindProperty("loginPanel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("serverUrlInput").objectReferenceValue = serverUrl;
        serialized.FindProperty("usernameInput").objectReferenceValue = username;
        serialized.FindProperty("passwordInput").objectReferenceValue = password;
        serialized.FindProperty("inviteCodeInput").objectReferenceValue = invite;
        serialized.FindProperty("loginButton").objectReferenceValue = loginButton;
        serialized.FindProperty("registerButton").objectReferenceValue = registerButton;
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
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
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
