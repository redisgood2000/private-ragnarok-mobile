#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PrivateRagnarok.Editor
{
    /// <summary>Creates a deterministic prototype scene and builds an ARM64 Android APK.</summary>
    public static class AndroidBuildBootstrap
    {
        private const string ScenePath = "Assets/PrivateRagnarok/Scenes/Prototype.unity";

        [MenuItem("Private Ragnarok/Generate Prototype Scene")]
        public static void GenerateScene()
        {
            Directory.CreateDirectory("Assets/PrivateRagnarok/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
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
                new Vector2(0.5f, 0.5f), new Vector2(620, 690));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.055f, 0.085f, 0.94f);

            var heading = Text("PRIVATE RAGNAROK", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -65), new Vector2(560, 70), 42);
            heading.alignment = TextAlignmentOptions.Center;
            heading.color = new Color(0.95f, 0.78f, 0.38f);

            var subtitle = Text("Enter the world with your party", panel, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(560, 42), 22);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = Color.white;

            var serverUrl = Input("Game server HTTPS URL", panel, -170);
            var username = Input("Username", panel, -245);
            var password = Input("Password (12+ characters)", panel, -320);
            password.contentType = TMP_InputField.ContentType.Password;
            var invite = Input("Invite code (registration only)", panel, -395);

            var loginButton = Button("LOGIN", panel, -475, new Color(0.16f, 0.36f, 0.57f));
            var registerButton = Button("REGISTER", panel, -545, new Color(0.34f, 0.25f, 0.12f));
            var status = Text("Enter your public HTTPS game server address above.", panel,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(570, 54), 17);
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            var controller = auth.AddComponent<PrivateRagnarok.Client.LoginScreenController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("authClient").objectReferenceValue = authClient;
            serialized.FindProperty("serverUrlInput").objectReferenceValue = serverUrl;
            serialized.FindProperty("usernameInput").objectReferenceValue = username;
            serialized.FindProperty("passwordInput").objectReferenceValue = password;
            serialized.FindProperty("inviteCodeInput").objectReferenceValue = invite;
            serialized.FindProperty("loginButton").objectReferenceValue = loginButton;
            serialized.FindProperty("registerButton").objectReferenceValue = registerButton;
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
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Text(string value, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(value, parent, anchorMin, anchorMax, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = fontSize; text.color = Color.white;
            text.font = TMP_Settings.defaultFontAsset;
            text.enableWordWrapping = true;
            return text;
        }

        private static TMP_InputField Input(string placeholder, Transform parent, float y)
        {
            var rect = Rect(placeholder, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, y), new Vector2(520, 66));
            rect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.19f, 1f);
            var textRect = Rect("Text", rect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-28, -16));
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 25; text.color = Color.white;
            text.font = TMP_Settings.defaultFontAsset;
            var placeholderRect = Rect("Placeholder", rect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-28, -16));
            var placeholderText = placeholderRect.gameObject.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder; placeholderText.fontSize = 23;
            placeholderText.font = TMP_Settings.defaultFontAsset;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = new Color(0.65f, 0.68f, 0.72f);
            var field = rect.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = rect;
            field.textComponent = text;
            field.placeholder = placeholderText;
            return field;
        }

        private static Button Button(string label, Transform parent, float y, Color color)
        {
            var rect = Rect(label, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, y), new Vector2(245, 58));
            rect.gameObject.AddComponent<Image>().color = color;
            var text = Text(label, rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 23);
            text.alignment = TextAlignmentOptions.Center;
            return rect.gameObject.AddComponent<Button>();
        }
    }
}
#endif
