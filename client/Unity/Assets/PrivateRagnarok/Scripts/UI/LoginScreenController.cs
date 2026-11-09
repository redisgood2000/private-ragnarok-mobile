using System.Collections;
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
        private bool demoStarting;
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
            if (demoEntered || demoStarting) return;
            StartCoroutine(EnterDemoRoutine());
        }

        private IEnumerator EnterDemoRoutine()
        {
            demoStarting = true;
            if (demoButton != null) demoButton.interactable = false;
            if (statusText != null) statusText.text = "Checking for world content updates...";

            GameObject remoteWorldPrefab = null;
            var patcher = FindObjectOfType<RemoteContentPatcher>();
            if (patcher != null)
                yield return patcher.PrepareWorldContent(
                    prefab => remoteWorldPrefab = prefab,
                    message => { if (statusText != null) statusText.text = message; });

            try
            {
                // Build only after the optional remote patch has been checked.
                var world = FindObjectOfType<WorldBootstrap>();
                if (world == null)
                    throw new System.Exception("WorldBootstrap is missing from the startup scene.");

                world.BuildWorld(remoteWorldPrefab);

                // Offline demo must not attempt a connection to the unconfigured API.
                var network = FindObjectOfType<WorldNetworkClient>();
                if (network != null)
                    network.enabled = false;

                // WorldNetworkClient.Initialize marks the player as server-controlled.
                // Offline demo must explicitly hand movement back to the local controller.
                var localMovement = FindObjectOfType<ThirdPersonPrototypeController>();
                if (localMovement != null)
                    localMovement.NetworkControlled = false;

                demoEntered = true;
                Debug.Log(remoteWorldPrefab != null
                    ? "PLAY DEMO succeeded. Remote world content is active."
                    : "PLAY DEMO succeeded. Built-in local prototype world is ready.");
                if (loginPanel != null) loginPanel.SetActive(false);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (statusText != null)
                    statusText.text = "Could not start demo: " + exception.Message;
                if (demoButton != null) demoButton.interactable = true;
            }
            demoStarting = false;
        }

        private void OnDestroy()
        {
            if (demoButton != null)
                demoButton.onClick.RemoveListener(EnterDemo);
        }
    }
}