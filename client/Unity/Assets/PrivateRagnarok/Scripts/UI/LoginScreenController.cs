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
        [SerializeField] private InputField serverUrlInput;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private InputField inviteCodeInput;
        [Header("Actions")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button demoButton;
        [Header("Feedback")]
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject loginPanel;

        private void Awake()
        {
            if (authClient == null) authClient = GetComponent<ApiAuthClient>();
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            if (loginButton != null) loginButton.onClick.AddListener(Login);
            if (registerButton != null) registerButton.onClick.AddListener(Register);
            if (demoButton != null) demoButton.onClick.AddListener(EnterDemo);
            SetBusy(false);
            
        }

        // Offline prototype mode: no backend or server URL is required.
        // The local WorldBootstrap already creates the town, player and camera.
        private void EnterDemo()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            if (worldNetworkClient != null) worldNetworkClient.enabled = false;
            Debug.Log("Entered Private Ragnarok offline demo.");
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
            if (loginPanel != null) loginPanel.SetActive(false);
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
            if (loginButton != null) loginButton.interactable = !busy;
            if (registerButton != null) registerButton.interactable = !busy;
            if (demoButton != null) demoButton.interactable = !busy;
            if (busy) Show("Connecting to the game server...");
        }

        private void Show(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void OnDestroy()
        {
            if (loginButton != null) loginButton.onClick.RemoveListener(Login);
            if (registerButton != null) registerButton.onClick.RemoveListener(Register);
            if (demoButton != null) demoButton.onClick.RemoveListener(EnterDemo);
            if (worldNetworkClient != null) worldNetworkClient.ConnectionStatusChanged -= OnWorldStatusChanged;
        }
    }
}
