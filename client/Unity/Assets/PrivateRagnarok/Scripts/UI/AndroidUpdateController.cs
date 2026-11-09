using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    [Serializable] internal sealed class GitHubRelease
    {
        public string tag_name;
        public string body;
        public bool draft;
        public bool prerelease;
        public GitHubReleaseAsset[] assets;
    }
    [Serializable] internal sealed class GitHubReleaseAsset
    {
        public string name;
        public string browser_download_url;
        public long size;
    }

    public sealed class AndroidUpdateController : MonoBehaviour
    {
        private const string ReleasesApi = "https://api.github.com/repos/redisgood2000/private-ragnarok-mobile/releases/latest";
        private const string ApkName = "PrivateRagnarok.apk";
        [SerializeField] private Text statusText;
        [SerializeField] private Text updateDetailsText;
        [SerializeField] private Button updateButton;
        [SerializeField] private Button laterButton;
        private string downloadUrl;
        private string latestVersion;
        private bool checking;
        private bool downloading;

        public void Initialize(Text status, Text details, Button update, Button later)
        {
            statusText = status; updateDetailsText = details;
            updateButton = update; laterButton = later;
            if (updateButton != null) updateButton.onClick.AddListener(DownloadAndInstall);
            if (laterButton != null) laterButton.onClick.AddListener(HidePrompt);
            if (updateButton != null) updateButton.gameObject.SetActive(false);
            if (laterButton != null) laterButton.gameObject.SetActive(false);
            StartCoroutine(CheckForUpdate());
        }

        public IEnumerator CheckForUpdate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (checking) yield break;
            checking = true;
            using (var request = UnityWebRequest.Get(ReleasesApi))
            {
                request.timeout = 15;
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("User-Agent", "PrivateRagnarok-Android-Updater");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus("Update check unavailable. You can still play.");
                    checking = false; yield break;
                }
                GitHubRelease release = null;
                try { release = JsonUtility.FromJson<GitHubRelease>(request.downloadHandler.text); }
                catch (Exception) { }
                if (release == null || release.draft || release.prerelease || string.IsNullOrWhiteSpace(release.tag_name))
                {
                    SetStatus("You're using the latest available version.");
                    checking = false; yield break;
                }
                var tag = release.tag_name.TrimStart('v', 'V');
                Version remote, local;
                if (!Version.TryParse(tag, out remote) || !Version.TryParse(Application.version, out local) || remote <= local)
                {
                    SetStatus("You're using the latest version (" + Application.version + ").");
                    checking = false; yield break;
                }
                GitHubReleaseAsset apk = null;
                if (release.assets != null)
                    foreach (var asset in release.assets)
                        if (asset != null && string.Equals(asset.name, ApkName, StringComparison.OrdinalIgnoreCase)) { apk = asset; break; }
                if (apk == null || string.IsNullOrWhiteSpace(apk.browser_download_url))
                {
                    SetStatus("Update " + release.tag_name + " is published but its APK is not available yet.");
                    checking = false; yield break;
                }
                latestVersion = release.tag_name;
                downloadUrl = apk.browser_download_url;
                if (updateDetailsText != null)
                    updateDetailsText.text = "Version " + latestVersion + " is available.\n\n" +
                        (string.IsNullOrWhiteSpace(release.body) ? "Bug fixes and improvements." : release.body);
                if (updateButton != null) updateButton.gameObject.SetActive(true);
                if (laterButton != null) laterButton.gameObject.SetActive(true);
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
                    if (sdk >= 26 && !activity.Call<bool>("canRequestPackageInstalls"))
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
