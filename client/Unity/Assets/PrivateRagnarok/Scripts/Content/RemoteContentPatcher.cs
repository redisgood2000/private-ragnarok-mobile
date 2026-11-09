using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

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
    /// The app falls back to its built-in prototype when no valid patch is available.
    /// </summary>
    public sealed class RemoteContentPatcher : MonoBehaviour
    {
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/redisgood2000/private-ragnarok-mobile/main/update/update.json";

        private AssetBundle loadedBundle;
        private string loadedVersion;

        public IEnumerator PrepareWorldContent(Action<GameObject> completed, Action<string> status)
        {
            GameObject prefab = null;
            string failure = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var request = UnityWebRequest.Get(ManifestUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
            {
                request.timeout = 15;
                request.SetRequestHeader("Cache-Control", "no-cache");
                request.SetRequestHeader("User-Agent", "PrivateRagnarok-ContentPatcher");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    failure = "Content server unavailable; using built-in world.";
                }
                else
                {
                    RemoteContentManifest manifest = null;
                    try { manifest = JsonUtility.FromJson<RemoteContentManifest>(request.downloadHandler.text); }
                    catch (Exception e) { failure = "Patch manifest could not be read: " + e.Message; }

                    if (manifest != null && manifest.patchEnabled)
                    {
                        if (!IsValidManifest(manifest))
                        {
                            failure = "Patch manifest is incomplete or has an unsafe URL.";
                        }
                        else
                        {
                            var directory = Path.Combine(Application.persistentDataPath, "content-patches");
                            var bundlePath = Path.Combine(directory, Path.GetFileName(manifest.patchBundleName));
                            try
                            {
                                Directory.CreateDirectory(directory);
                                if (!File.Exists(bundlePath) || !HashMatches(bundlePath, manifest.patchSha256))
                                {
                                    using (var bundleRequest = UnityWebRequest.Get(manifest.patchBundleUrl))
                                    {
                                        bundleRequest.timeout = 90;
                                        bundleRequest.downloadHandler = new DownloadHandlerBuffer();
                                        yield return bundleRequest.SendWebRequest();
                                        if (bundleRequest.result != UnityWebRequest.Result.Success)
                                            failure = "Patch download failed: " + bundleRequest.error;
                                        else
                                        {
                                            var bytes = bundleRequest.downloadHandler.data;
                                            if (bytes == null || !HashMatches(bytes, manifest.patchSha256))
                                                failure = "Patch integrity check failed; built-in world will be used.";
                                            else
                                            {
                                                var temporaryPath = bundlePath + ".download";
                                                File.WriteAllBytes(temporaryPath, bytes);
                                                if (File.Exists(bundlePath)) File.Delete(bundlePath);
                                                File.Move(temporaryPath, bundlePath);
                                            }
                                        }
                                    }
                                }

                                if (failure == null)
                                {
                                    if (loadedBundle != null) loadedBundle.Unload(true);
                                    loadedBundle = AssetBundle.LoadFromFile(bundlePath);
                                    if (loadedBundle == null)
                                        failure = "Downloaded patch is not a valid Unity AssetBundle.";
                                    else
                                    {
                                        prefab = loadedBundle.LoadAsset<GameObject>(manifest.worldPrefab);
                                        if (prefab == null)
                                            failure = "Patch is valid but its configured world prefab was not found.";
                                        else
                                        {
                                            loadedVersion = manifest.patchVersion;
                                            if (status != null) status("Content patch " + loadedVersion + " loaded.");
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                failure = "Patch could not be installed: " + e.Message;
                            }
                        }
                    }
                }
            }
#else
            failure = "Remote content patches are loaded in the Android build.";
#endif

            if (!string.IsNullOrEmpty(failure) && status != null) status(failure);
            completed?.Invoke(prefab);
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
                string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
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
