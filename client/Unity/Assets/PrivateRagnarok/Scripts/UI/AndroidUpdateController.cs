using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    // This file is hosted separately from the APK. Editing it does not rebuild the app.
    [Serializable] internal sealed class RemoteUpdateManifest
    {
        public bool enabled = true;
        public string latestVersion;
        public string apkUrl;
        public string releaseNotes;
    }

    public sealed class AndroidUpdateController : MonoBehaviour
    {
        private const string ApkName = "PrivateRagnarok.apk";
        private const string ManifestUrl = "https://raw.githubusercontent.com/redisgood2000/private-ragnarok-mobile/main/update/update.json";
        [SerializeField] private Text statusText;
        [SerializeField] private Text updateDetailsText;
        [SerializeField] private Button updateButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private GameObject updatePromptPanel;
        private string downloadUrl;
        private string latestVersion;
        private bool checking;
        private bool downloading;

        private void Awake()
        {
            // These references are serialized into the generated scene. Do not initialize
            // from the Editor scene generator: editor-time listeners/coroutines are not
            // persisted into the installed Android player.
            if (updatePromptPanel != null) updatePromptPanel.SetActive(false);
            if (updateButton != null)
            {
                updateButton.onClick.RemoveListener(DownloadAndInstall);
                updateButton.onClick.AddListener(DownloadAndInstall);
            }
            if (laterButton != null)
            {
                laterButton.onClick.RemoveListener(HidePrompt);
                laterButton.onClick.AddListener(HidePrompt);
            }
        }

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(CheckForUpdate());
#endif
        }

        public IEnumerator CheckForUpdate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (checking) yield break;
            checking = true;
            var manifestUrl = ManifestUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using (var request = UnityWebRequest.Get(manifestUrl))
            {
                request.timeout = 15;
                request.SetRequestHeader("Cache-Control", "no-cache");
                request.SetRequestHeader("User-Agent", "PrivateRagnarok-Android-Updater");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus("Update check unavailable. You can still play.");
                    checking = false; yield break;
                }

                RemoteUpdateManifest manifest = null;
                try { manifest = JsonUtility.FromJson<RemoteUpdateManifest>(request.downloadHandler.text); }
                catch (Exception) { }
                if (manifest == null || !manifest.enabled ||
                    string.IsNullOrWhiteSpace(manifest.latestVersion) ||
                    string.IsNullOrWhiteSpace(manifest.apkUrl))
                {
                    SetStatus("You're using the latest available version.");
                    checking = false; yield break;
                }

                Version remote, local;
                var remoteTag = manifest.latestVersion.Trim().TrimStart('v', 'V');
                if (!Version.TryParse(remoteTag, out remote) ||
                    !Version.TryParse(Application.version, out local))
                {
                    SetStatus("Update information is invalid. You can still play.");
                    checking = false; yield break;
                }
                if (remote <= local)
                {
                    SetStatus("You're using the latest version (" + Application.version + ").");
                    checking = false; yield break;
                }

                Uri apkUri;
                if (!Uri.TryCreate(manifest.apkUrl, UriKind.Absolute, out apkUri) ||
                    apkUri.Scheme != Uri.UriSchemeHttps ||
                    !string.Equals(apkUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Update link is invalid. You can still play.");
                    checking = false; yield break;
                }

                latestVersion = manifest.latestVersion;
                downloadUrl = manifest.apkUrl;
                if (updateDetailsText != null)
                    updateDetailsText.text = "Version " + latestVersion + " is available.\n\n" +
                        (string.IsNullOrWhiteSpace(manifest.releaseNotes)
                            ? "Bug fixes and improvements."
                            : manifest.releaseNotes);
                if (updateButton != null) updateButton.gameObject.SetActive(true);
                if (laterButton != null) laterButton.gameObject.SetActive(true);
                if (updatePromptPanel != null) updatePromptPanel.SetActive(true);
                SetStatus("A game update is available.");
            }
            checking = false;
#else
            SetStatus("In-app updates are available in the Android build.");
            yield break;
#endif
        }

        private void DownloadAndInstall()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!downloading && !string.IsNullOrWhiteSpace(downloadUrl)) StartCoroutine(DownloadWithAndroidManager());
#endif
        }

        private IEnumerator DownloadWithAndroidManager()
        {
            downloading = true;
            if (updateButton != null) updateButton.interactable = false;
            SetStatus("Preparing secure APK download...");
            long downloadId = -1;
            AndroidJavaObject activity = null, manager = null;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using (var context = new AndroidJavaClass("android.content.Context"))
                    manager = activity.Call<AndroidJavaObject>("getSystemService", context.GetStatic<string>("DOWNLOAD_SERVICE"));
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                {
                    var uri = uriClass.CallStatic<AndroidJavaObject>("parse", downloadUrl);
                    var request = new AndroidJavaObject("android.app.DownloadManager$Request", uri);
                    request.Call<AndroidJavaObject>("setTitle", "Private Ragnarok " + latestVersion);
                    request.Call<AndroidJavaObject>("setDescription", "Downloading game update");
                    request.Call<AndroidJavaObject>("setMimeType", "application/vnd.android.package-archive");
                    request.Call<AndroidJavaObject>("setNotificationVisibility", 1);
                    request.Call<AndroidJavaObject>("setAllowedOverMetered", true);
                    request.Call<AndroidJavaObject>("setDestinationInExternalFilesDir", activity, null, ApkName);
                    downloadId = manager.Call<long>("enqueue", request);
                    request.Dispose(); uri.Dispose();
                }
            }
            catch (Exception e)
            {
                SetStatus("Could not start the download: " + e.Message);
                downloading = false; if (updateButton != null) updateButton.interactable = true; yield break;
            }

            bool complete = false, failed = false;
            while (!complete && !failed)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                AndroidJavaObject query = null, cursor = null;
                try
                {
                    query = new AndroidJavaObject("android.app.DownloadManager$Query");
                    query.Call<AndroidJavaObject>("setFilterById", new long[] { downloadId });
                    cursor = manager.Call<AndroidJavaObject>("query", query);
                    if (cursor == null || !cursor.Call<bool>("moveToFirst")) { failed = true; }
                    else
                    {
                        using (var dm = new AndroidJavaClass("android.app.DownloadManager"))
                        {
                            int statusCol = cursor.Call<int>("getColumnIndex", dm.GetStatic<string>("COLUMN_STATUS"));
                            int bytesCol = cursor.Call<int>("getColumnIndex", dm.GetStatic<string>("COLUMN_BYTES_DOWNLOADED_SO_FAR"));
                            int totalCol = cursor.Call<int>("getColumnIndex", dm.GetStatic<string>("COLUMN_TOTAL_SIZE_BYTES"));
                            int state = cursor.Call<int>("getInt", statusCol);
                            long bytes = cursor.Call<long>("getLong", bytesCol);
                            long total = cursor.Call<long>("getLong", totalCol);
                            if (total > 0) SetStatus("Downloading update: " + (bytes * 100 / total) + "%");
                            complete = state == 8;
                            failed = state == 16;
                        }
                    }
                }
                catch (Exception) { failed = true; }
                finally { if (cursor != null) cursor.Dispose(); if (query != null) query.Dispose(); }
            }
            if (failed)
            {
                SetStatus("Update download failed. Please try again.");
                downloading = false; if (updateButton != null) updateButton.interactable = true; yield break;
            }

            SetStatus("Download complete. Preparing Android installer...");
            try
            {
                using (var uri = manager.Call<AndroidJavaObject>("getUriForDownloadedFile", downloadId))
                using (var build = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdk = build.GetStatic<int>("SDK_INT");
                    if (sdk >= 26)
                    {
                        AndroidJavaObject packageManager = null;
                        bool canInstall = false;
                        try
                        {
                            packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
                            canInstall = packageManager.Call<bool>("canRequestPackageInstalls");
                        }
                        finally
                        {
                            if (packageManager != null) packageManager.Dispose();
                        }
                        if (!canInstall)
                    {
                        using (var settings = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES"))
                        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                        {
                            var appUri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + activity.Call<string>("getPackageName"));
                            settings.Call<AndroidJavaObject>("setData", appUri);
                            settings.Call<AndroidJavaObject>("addFlags", 0x10000000);
                            activity.Call("startActivity", settings);
                            appUri.Dispose();
                        }
                        SetStatus("Allow installs for Private Ragnarok, return here, then tap Update again.");
                        downloading = false; if (updateButton != null) updateButton.interactable = true; yield break;
                    }
                    using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.INSTALL_PACKAGE"))
                    {
                        intent.Call<AndroidJavaObject>("setData", uri);
                        intent.Call<AndroidJavaObject>("addFlags", 0x00000001 | 0x10000000);
                        activity.Call("startActivity", intent);
                    }
                }
                SetStatus("Follow the Android prompt to install the update.");
            }
            catch (Exception e)
            {
                SetStatus("Could not open Android installer: " + e.Message);
                if (updateButton != null) updateButton.interactable = true;
            }
            downloading = false;
        }

        private void HidePrompt()
        {
            if (updatePromptPanel != null) updatePromptPanel.SetActive(false);
            if (updateButton != null) updateButton.gameObject.SetActive(false);
            if (laterButton != null) laterButton.gameObject.SetActive(false);
            if (updateDetailsText != null) updateDetailsText.text = string.Empty;
            SetStatus("You can update later from the game launcher.");
        }
        private void SetStatus(string value) { if (statusText != null) statusText.text = value; }
        private void OnDestroy()
        {
            if (updateButton != null) updateButton.onClick.RemoveListener(DownloadAndInstall);
            if (laterButton != null) laterButton.onClick.RemoveListener(HidePrompt);
        }
    }
}
