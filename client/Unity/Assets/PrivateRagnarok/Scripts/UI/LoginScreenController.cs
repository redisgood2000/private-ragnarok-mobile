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

        private bool demoEntered;
        private bool busy;
        private int lastManualTouchFrame = -1;

        private void Awake()
        {
            if (authClient == null) authClient = GetComponent<ApiAuthClient>();
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            if (loginButton != null) loginButton.onClick.AddListener(Login);
            if (registerButton != null) registerButton.onClick.AddListener(Register);
            if (demoButton != null) demoButton.onClick.AddListener(EnterDemo);
            SetBusy(false);
        }

        // Fallback for Android devices where Unity's StandaloneInputModule does not
        // deliver UI pointer events. Hit-test the actual button RectTransforms using
        // the raw touch coordinates, then invoke the same button action.
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (loginPanel == null || !loginPanel.activeInHierarchy || Input.touchCount == 0)
                return;

            foreach (var touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began || lastManualTouchFrame == Time.frameCount)
                    continue;

                if (TryManualButtonHit(demoButton, touch.position) ||
                    TryManualButtonHit(loginButton, touch.position) ||
                    TryManualButtonHit(registerButton, touch.position))
                {
                    lastManualTouchFrame = Time.frameCount;
                    break;
                }
            }
#endif
        }

        private static bool TryManualButtonHit(Button button, Vector2 screenPosition)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
                return false;

            var rect = button.transform as RectTransform;
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, screenPosition, null, out var localPoint) || !rect.rect.Contains(localPoint))
                return false;

            button.onClick.Invoke();
            return true;
        }

        // Offline prototype mode: no backend or server URL is required.
        private void EnterDemo()
        {
            if (demoEntered) return;
            demoEntered = true;
            if (loginPanel != null) loginPanel.SetActive(false);
            if (worldNetworkClient == null) worldNetworkClient = FindObjectOfType<WorldNetworkClient>();
            if (worldNetworkClient != null) worldNetworkClient.enabled = false;
            Debug.Log("Entered Private Ragnarok offline demo.");
        }

        private void Login()
        {
            if (busy || !ApplyServerUrl() || !ValidateInputs(false)) return;
            SetBusy(true);
            StartCoroutine(authClient.Login(usernameInput.text.Trim(), passwordInput.text,
                OnAuthenticated, OnFailed));
        }

        private void Register()
        {
            if (busy || !ApplyServerUrl() || !ValidateInputs(true)) return;
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
            if (usernameInput == null || passwordInput == null ||
                string.IsNullOrWhiteSpace(usernameInput.text) ||
                string.IsNullOrWhiteSpace(passwordInput.text))
            {
                Show("Enter your username and password.");
                return false;
            }
            if (needsInvite && (inviteCodeInput == null || string.IsNullOrWhiteSpace(inviteCodeInput.text)))
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

        private void SetBusy(bool value)
        {
            busy = value;
            if (loginButton != null) loginButton.interactable = !value;
            if (registerButton != null) registerButton.interactable = !value;
            if (demoButton != null) demoButton.interactable = !value;
            if (value) Show("Connecting to the game server...");
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