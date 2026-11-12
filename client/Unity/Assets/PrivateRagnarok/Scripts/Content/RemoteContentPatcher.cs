using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    [Serializable]
    internal sealed class RemoteContentManifest
    {
        public bool patchEnabled;
        public string patchVersion;
        public string patchBundleUrl;
        public string patchBundleName;
        public string patchSha256;
        public string worldPrefab;
    }

    /// <summary>
    /// Downloads and verifies optional content bundles independently of the APK.
    /// Displays an on-screen progress panel so patch failures are never silent.
    /// </summary>
    public sealed class RemoteContentPatcher : MonoBehaviour
    {
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/redisgood2000/private-ragnarok-mobile/main/update/update.json";

        private AssetBundle loadedBundle;
        private string loadedVersion;
        private Canvas progressCanvas;
        private Text progressTitle;
        private Text progressMessage;
        private Text progressPercent;
        private Image progressFill;

        public IEnumerator PrepareWorldContent(Action<GameObject> completed, Action<string> status)
        {
            GameObject prefab = null;
            string failure = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            CreateProgressUi();
            SetProgress("Checking game resources", "Checking for content patch...", 0.02f);

            using (var request = UnityWebRequest.Get(ManifestUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
            {
                request.timeout = 20;
                request.SetRequestHeader("Cache-Control", "no-cache");
                request.SetRequestHeader("User-Agent", "PrivateRagnarok-ContentPatcher");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                    failure = "Could not check resources: " + request.error;
                else
                {
                    RemoteContentManifest manifest = null;
                    try { manifest = JsonUtility.FromJson<RemoteContentManifest>(request.downloadHandler.text); }
                    catch (Exception e) { failure = "Patch manifest could not be read: " + e.Message; }

                    if (manifest == null && failure == null)
                        failure = "Patch manifest is empty or invalid.";

                    if (manifest != null && failure == null)
                    {
                        if (!manifest.patchEnabled)
                        {
                            SetProgress("Resources up to date", "No content patch is currently enabled.", 1f);
                        }
                        else if (!IsValidManifest(manifest))
                            failure = "Patch manifest is incomplete or has an unsafe URL.";
                        else
                        {
                            var directory = Path.Combine(Application.persistentDataPath, "content-patches");
                            var bundlePath = Path.Combine(directory, manifest.patchBundleName);
                            try { Directory.CreateDirectory(directory); }
                            catch (Exception e) { failure = "Patch cache could not be created: " + e.Message; }

                            bool cached = failure == null && File.Exists(bundlePath) && HashMatches(bundlePath, manifest.patchSha256);
                            if (!cached && failure == null)
                            {
                                SetProgress("Downloading resources " + manifest.patchVersion,
                                    "Downloading game content. Please keep the game open...", 0f);
                                using (var bundleRequest = UnityWebRequest.Get(manifest.patchBundleUrl))
                                {
                                    bundleRequest.timeout = 180;
                                    bundleRequest.downloadHandler = new DownloadHandlerBuffer();
                                    var operation = bundleRequest.SendWebRequest();
                                    while (!operation.isDone)
                                    {
                                        SetProgress("Downloading resources " + manifest.patchVersion,
                                            "Downloading game content. Please keep the game open...",
                                            Mathf.Clamp01(bundleRequest.downloadProgress) * 0.88f);
                                        yield return null;
                                    }

                                    if (bundleRequest.result != UnityWebRequest.Result.Success)
                                        failure = "Patch download failed: " + bundleRequest.error;
                                    else
                                    {
                                        var bytes = bundleRequest.downloadHandler.data;
                                        SetProgress("Verifying resources", "Checking downloaded file integrity...", 0.92f);
                                        if (bytes == null || !HashMatches(bytes, manifest.patchSha256))
                                            failure = "Patch integrity check failed. The downloaded file did not match the published checksum.";
                                        else
                                        {
                                            try
                                            {
                                                var temporaryPath = bundlePath + ".download";
                                                File.WriteAllBytes(temporaryPath, bytes);
                                                if (File.Exists(bundlePath)) File.Delete(bundlePath);
                                                File.Move(temporaryPath, bundlePath);
                                            }
                                            catch (Exception e) { failure = "Patch could not be cached: " + e.Message; }
                                        }
                                    }
                                }
                            }
                            else if (cached)
                            {
                                SetProgress("Loading resources " + manifest.patchVersion,
                                    "Verified resource patch already exists on this device.", 0.90f);
                            }

                            if (failure == null)
                            {
                                SetProgress("Loading resources " + manifest.patchVersion,
                                    "Opening game content...", 0.96f);
                                if (loadedBundle != null) loadedBundle.Unload(true);
                                loadedBundle = AssetBundle.LoadFromFile(bundlePath);
                                if (loadedBundle == null)
                                    failure = "Downloaded patch is not a valid Unity AssetBundle.";
                                else
                                {
                                    prefab = loadedBundle.LoadAsset<GameObject>(manifest.worldPrefab);
                                    if (prefab == null)
                                        failure = "Patch is valid, but its configured world prefab '" + manifest.worldPrefab + "' was not found.";
                                    else
                                    {
                                        loadedVersion = manifest.patchVersion;
                                        SetProgress("Resources ready", "Content patch " + loadedVersion + " loaded successfully.", 1f);
                                        if (status != null) status("Content patch " + loadedVersion + " loaded.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
#else
            failure = "Remote content patches are loaded in the Android build.";
#endif
            if (!string.IsNullOrEmpty(failure))
            {
                SetProgress("Resource patch not applied", failure + " Using built-in game resources.", 1f);
                if (status != null) status(failure);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            yield return new WaitForSecondsRealtime(string.IsNullOrEmpty(failure) ? 1.5f : 4f);
            if (progressCanvas != null) Destroy(progressCanvas.gameObject);
#endif
            completed?.Invoke(prefab);
            yield break; // Keep this iterator valid when UNITY_ANDROID is not defined in Editor compilation.
        }

        public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            return loadedBundle == null || string.IsNullOrWhiteSpace(assetName)
                ? null
                : loadedBundle.LoadAsset<T>(assetName);
        }

        private void CreateProgressUi()
        {
            if (progressCanvas != null) return;
            var canvasObject = new GameObject("Resource Patch Progress UI");
            progressCanvas = canvasObject.AddComponent<Canvas>();
            progressCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            progressCanvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateUiObject("Panel", canvasObject.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(900, 300));
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.94f);

            progressTitle = CreateText("Title", panel.transform, "Checking game resources", 42,
                FontStyle.Bold, new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.92f), TextAnchor.MiddleLeft);
            progressTitle.color = new Color(0.95f, 0.82f, 0.48f);
            progressMessage = CreateText("Message", panel.transform, "Please wait...", 28,
                FontStyle.Normal, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.64f), TextAnchor.MiddleLeft);
            progressMessage.color = Color.white;

            var barBackground = CreateUiObject("Progress Background", panel.transform,
                new Vector2(0.06f, 0.20f), new Vector2(0.82f, 0.29f), new Vector2(0, 0));
            barBackground.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.29f, 1f);
            var fillObject = CreateUiObject("Progress Fill", barBackground.transform,
                Vector2.zero, Vector2.one, Vector2.zero);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            progressFill = fillObject.GetComponent<Image>();
            progressFill.color = new Color(0.20f, 0.72f, 0.55f, 1f);
            progressPercent = CreateText("Percent", panel.transform, "0%", 25,
                FontStyle.Bold, new Vector2(0.83f, 0.16f), new Vector2(0.94f, 0.33f), TextAnchor.MiddleRight);
            progressPercent.color = Color.white;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return go;
        }

        private static Text CreateText(string objectName, Transform parent, string value,
            int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private void SetProgress(string title, string message, float progress)
        {
            if (progressCanvas == null) return;
            if (progressTitle != null) progressTitle.text = title;
            if (progressMessage != null) progressMessage.text = message;
            var value = Mathf.Clamp01(progress);
            if (progressFill != null)
                progressFill.rectTransform.anchorMax = new Vector2(value, 1f);
            if (progressPercent != null)
                progressPercent.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private static bool IsValidManifest(RemoteContentManifest manifest)
        {
            Uri uri;
            return !string.IsNullOrWhiteSpace(manifest.patchVersion) &&
                !string.IsNullOrWhiteSpace(manifest.patchBundleName) &&
                !string.IsNullOrWhiteSpace(manifest.worldPrefab) &&
                !string.IsNullOrWhiteSpace(manifest.patchSha256) &&
                manifest.patchSha256.Length == 64 &&
                Uri.TryCreate(manifest.patchBundleUrl, UriKind.Absolute, out uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                (string.Equals(uri.Host, "raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) &&
                Path.GetFileName(manifest.patchBundleName) == manifest.patchBundleName;
        }

        private static bool HashMatches(string path, string expected)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Equals(ToHex(sha.ComputeHash(stream)), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HashMatches(byte[] bytes, string expected)
        {
            using (var sha = SHA256.Create())
                return string.Equals(ToHex(sha.ComputeHash(bytes)), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        private void OnDestroy()
        {
            if (loadedBundle != null) loadedBundle.Unload(false);
        }
    }
}