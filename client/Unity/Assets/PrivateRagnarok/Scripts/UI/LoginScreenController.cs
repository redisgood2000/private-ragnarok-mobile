using UnityEngine;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Minimal offline-first launcher. The prototype world is built before the
    /// menu is dismissed, so PLAY DEMO has one deterministic job.
    /// </summary>
    public sealed class LoginScreenController : MonoBehaviour
    {
        [SerializeField] private Button demoButton;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject loginPanel;

        private bool demoEntered;
        private int lastManualTouchFrame = -1;

        private void Awake()
        {
            if (demoButton != null)
                demoButton.onClick.AddListener(EnterDemo);
            if (statusText != null)
                statusText.text = "Tap PLAY DEMO to enter the world";
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Fallback to raw legacy touch input if Unity's UI EventSystem
            // does not dispatch pointer clicks on a particular Android device.
            if (demoEntered || loginPanel == null || !loginPanel.activeInHierarchy ||
                demoButton == null || Input.touchCount == 0 ||
                lastManualTouchFrame == Time.frameCount)
                return;

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                var rect = demoButton.transform as RectTransform;
                if (rect != null && demoButton.isActiveAndEnabled && demoButton.interactable &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, touch.position, null))
                {
                    lastManualTouchFrame = Time.frameCount;
                    Debug.Log("PLAY DEMO raw touch accepted at " + touch.position);
                    demoButton.onClick.Invoke();
                    return;
                }
            }
#endif
        }

        private void EnterDemo()
        {
            if (demoEntered)
                return;

            try
            {
                // Ensure the local town exists even if Unity Start ordering changes.
                var world = FindObjectOfType<WorldBootstrap>();
                if (world == null)
                    throw new System.Exception("WorldBootstrap is missing from the startup scene.");

                world.BuildWorld();

                // Offline demo must not attempt a connection to the unconfigured API.
                var network = FindObjectOfType<WorldNetworkClient>();
                if (network != null)
                    network.enabled = false;

                demoEntered = true;
                Debug.Log("PLAY DEMO succeeded. Local prototype world is ready.");
                if (loginPanel != null)
                    loginPanel.SetActive(false);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (statusText != null)
                    statusText.text = "Could not start demo: " + exception.Message;
            }
        }

        private void OnDestroy()
        {
            if (demoButton != null)
                demoButton.onClick.RemoveListener(EnterDemo);
        }
    }
}