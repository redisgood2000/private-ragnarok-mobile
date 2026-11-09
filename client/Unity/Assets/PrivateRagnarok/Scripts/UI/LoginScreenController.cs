using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    public sealed class LoginScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ApiAuthClient authClient;
        [SerializeField] private WorldNetworkClient worldNetworkClient;
        [Header("Inputs")]
        [SerializeField] private TMP_InputField serverUrlInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField inviteCodeInput;
        [Header("Actions")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject authenticatedPanel;

        private void Awake()
        {
            if (authClient == null) authClient = GetComponent<ApiAuthClient>();
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            loginButton.onClick.AddListener(Login);
            registerButton.onClick.AddListener(Register);
            SetBusy(false);
            if (authenticatedPanel != null) authenticatedPanel.SetActive(false);
        }

        private void Login()
        {
            if (!ApplyServerUrl() || !ValidateInputs(false)) return;
            SetBusy(true);
            StartCoroutine(authClient.Login(usernameInput.text.Trim(), passwordInput.text,
                OnAuthenticated, OnFailed));
        }

        private void Register()
        {
            if (!ApplyServerUrl() || !ValidateInputs(true)) return;
            SetBusy(true);
            StartCoroutine(authClient.Register(usernameInput.text.Trim(), passwordInput.text,
                inviteCodeInput.text.Trim(), OnAuthenticated, OnFailed));
        }

        private bool ApplyServerUrl()
        {
            if (serverUrlInput == null || authClient == null || !authClient.SetApiBaseUrl(serverUrlInput.text.Trim()))
            {
                Show("Enter your game server HTTPS address first.");
                return false;
            }
            return true;
        }

        private bool ValidateInputs(bool needsInvite)
        {
            if (string.IsNullOrWhiteSpace(usernameInput.text) ||
                string.IsNullOrWhiteSpace(passwordInput.text))
            {
                Show("Enter your username and password.");
                return false;
            }
            if (needsInvite && string.IsNullOrWhiteSpace(inviteCodeInput.text))
            {
                Show("An invitation code is required.");
                return false;
            }
            if (passwordInput.text.Length < 12)
            {
                Show("Password must be at least 12 characters.");
                return false;
            }
            return true;
        }

        private void OnAuthenticated(AuthResponse response)
        {
            SetBusy(false);
            Show("Welcome, " + response.username + "!");
            if (authenticatedPanel != null) authenticatedPanel.SetActive(true);
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            if (worldNetworkClient != null && authClient != null)
            {
                worldNetworkClient.ConnectionStatusChanged -= OnWorldStatusChanged;
                worldNetworkClient.ConnectionStatusChanged += OnWorldStatusChanged;
                worldNetworkClient.ConfigureAndConnect(authClient.AccessToken, response.playerId,
                    response.username, authClient.ApiBaseUrl);
            }
        }

        private void OnWorldStatusChanged(string message) => Show(message);

        private void OnFailed(string message)
        {
            SetBusy(false);
            Show(message);
        }

        private void SetBusy(bool busy)
        {
            loginButton.interactable = !busy;
            registerButton.interactable = !busy;
            if (busy) Show("Connecting to the world...");
        }

        private void Show(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void OnDestroy()
        {
            if (loginButton != null) loginButton.onClick.RemoveListener(Login);
            if (registerButton != null) registerButton.onClick.RemoveListener(Register);
            if (worldNetworkClient != null) worldNetworkClient.ConnectionStatusChanged -= OnWorldStatusChanged;
        }
    }
}
