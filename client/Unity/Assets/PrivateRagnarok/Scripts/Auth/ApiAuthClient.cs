using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace PrivateRagnarok.Client
{
    [Serializable] public sealed class RegisterPayload
    {
        public string username;
        public string password;
        public string inviteCode;
    }

    [Serializable] public sealed class LoginPayload
    {
        public string username;
        public string password;
    }

    [Serializable] public sealed class AuthResponse
    {
        public string accessToken;
        public string playerId;
        public string username;
    }

    /// <summary>HTTPS JSON authentication client. Access token is memory-only.</summary>
    public sealed class ApiAuthClient : MonoBehaviour
    {
        [SerializeField] private string apiBaseUrl = "https://YOUR-API-HOST";
        [SerializeField, Min(3)] private int timeoutSeconds = 15;

        public string ApiBaseUrl => apiBaseUrl;
        public string AccessToken { get; private set; }
        public AuthResponse CurrentUser { get; private set; }

        public IEnumerator Register(string username, string password, string inviteCode,
            Action<AuthResponse> success, Action<string> failure)
        {
            var payload = new RegisterPayload {
                username = username, password = password, inviteCode = inviteCode
            };
            yield return Send("/api/auth/register", JsonUtility.ToJson(payload), success, failure);
        }

        public IEnumerator Login(string username, string password,
            Action<AuthResponse> success, Action<string> failure)
        {
            var payload = new LoginPayload { username = username, password = password };
            yield return Send("/api/auth/login", JsonUtility.ToJson(payload), success, failure);
        }

        private IEnumerator Send(string path, string json,
            Action<AuthResponse> success, Action<string> failure)
        {
            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps)
            {
                failure?.Invoke("Configure a valid HTTPS API URL.");
                yield break;
            }

            var url = new Uri(baseUri, path).ToString();
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Avoid showing server internals or credentials in the UI.
                    failure?.Invoke(request.responseCode == 401
                        ? "Invalid username or password."
                        : "Could not reach the game server. Please try again.");
                    yield break;
                }

                AuthResponse response;
                try { response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text); }
                catch (Exception) { failure?.Invoke("The server returned an invalid response."); yield break; }

                if (response == null || string.IsNullOrWhiteSpace(response.accessToken) ||
                    string.IsNullOrWhiteSpace(response.playerId))
                {
                    failure?.Invoke("The server response was incomplete.");
                    yield break;
                }

                AccessToken = response.accessToken;
                CurrentUser = response;
                success?.Invoke(response);
            }
        }

        public void Logout()
        {
            AccessToken = null;
            CurrentUser = null;
        }
    }
}
