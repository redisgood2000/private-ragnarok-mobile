using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// SignalR world client. Movement is input intent; the server returns authoritative positions.
    /// SignalR callbacks are marshalled onto Unity's main thread before touching GameObjects.
    /// </summary>
    public sealed class WorldNetworkClient : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float inputSendInterval = 0.1f;
        [SerializeField, Min(0.1f)] private float remoteInterpolation = 12f;
        [SerializeField] private bool connectOnConfigure = true;

        private readonly ConcurrentQueue<Action> mainThreadActions = new();
        private readonly Dictionary<string, RemoteAvatar> remotePlayers = new();
        private HubConnection connection;
        private string playerId;
        private string username;
        private string token;
        private string apiBaseUrl;
        private Transform localPlayer;
        private ThirdPersonPrototypeController localMovement;
        private Vector3 localTarget;
        private float localTargetYaw;
        private bool hasLocalSnapshot;
        private float nextInputAt;
        private long inputSequence;
        private bool connectStarted;

        public bool IsConnected => connection?.State == HubConnectionState.Connected;
        public event Action<string> ConnectionStatusChanged;

        public void Initialize(Transform player, ThirdPersonPrototypeController movement)
        {
            localPlayer = player;
            localMovement = movement;
            if (localMovement != null) localMovement.NetworkControlled = true;
        }

        public void ConfigureAndConnect(string accessToken, string id, string displayName, string baseUrl)
        {
            token = accessToken;
            playerId = id;
            username = displayName;
            apiBaseUrl = baseUrl;
            if (connectOnConfigure) _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (connectStarted || string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(apiBaseUrl))
                return;

            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps)
            {
                ConnectionStatusChanged?.Invoke("Invalid HTTPS game server URL.");
                return;
            }

            connectStarted = true;
            ConnectionStatusChanged?.Invoke("Connecting to world...");
            try
            {
                var hubUri = new Uri(baseUri, "/hubs/world");
                connection = new HubConnectionBuilder()
                    .WithUrl(hubUri, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                connection.On<PlayerSnapshot>("PlayerSpawned", snapshot =>
                    mainThreadActions.Enqueue(() => ApplySnapshot(snapshot, true)));
                connection.On<List<PlayerSnapshot>>("OnlinePlayers", snapshots =>
                    mainThreadActions.Enqueue(() =>
                    {
                        foreach (var snapshot in snapshots) ApplySnapshot(snapshot, false);
                    }));
                connection.On<PlayerSnapshot>("PlayerJoined", snapshot =>
                    mainThreadActions.Enqueue(() => ApplySnapshot(snapshot, false)));
                connection.On<PlayerSnapshot>("PlayerMoved", snapshot =>
                    mainThreadActions.Enqueue(() => ApplySnapshot(snapshot, snapshot.PlayerId == playerId)));
                connection.On<string>("PlayerLeft", id =>
                    mainThreadActions.Enqueue(() => RemoveRemote(id)));

                connection.Reconnecting += error =>
                {
                    mainThreadActions.Enqueue(() => ConnectionStatusChanged?.Invoke("Reconnecting..."));
                    return Task.CompletedTask;
                };
                connection.Reconnected += id =>
                {
                    mainThreadActions.Enqueue(() => ConnectionStatusChanged?.Invoke("Connected to world"));
                    return Task.CompletedTask;
                };
                connection.Closed += error =>
                {
                    mainThreadActions.Enqueue(() => ConnectionStatusChanged?.Invoke("Disconnected from world"));
                    return Task.CompletedTask;
                };

                await connection.StartAsync();
                mainThreadActions.Enqueue(() => ConnectionStatusChanged?.Invoke("Connected to world"));
            }
            catch (Exception ex)
            {
                connectStarted = false;
                mainThreadActions.Enqueue(() =>
                    ConnectionStatusChanged?.Invoke("Could not connect to the world server."));
                Debug.LogWarning("World connection failed: " + ex.GetType().Name);
            }
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out var action)) action();

            if (localPlayer != null && hasLocalSnapshot)
            {
                localPlayer.position = Vector3.Lerp(localPlayer.position, localTarget,
                    1f - Mathf.Exp(-18f * Time.deltaTime));
                localPlayer.rotation = Quaternion.RotateTowards(localPlayer.rotation,
                    Quaternion.Euler(0f, localTargetYaw, 0f), 720f * Time.deltaTime);
            }

            foreach (var avatar in remotePlayers.Values)
            {
                avatar.Root.position = Vector3.Lerp(avatar.Root.position, avatar.TargetPosition,
                    1f - Mathf.Exp(-remoteInterpolation * Time.deltaTime));
                avatar.Root.rotation = Quaternion.RotateTowards(avatar.Root.rotation,
                    Quaternion.Euler(0f, avatar.TargetYaw, 0f), 540f * Time.deltaTime);
            }

            if (IsConnected && Time.unscaledTime >= nextInputAt)
            {
                nextInputAt = Time.unscaledTime + inputSendInterval;
                var x = Input.GetAxisRaw("Horizontal");
                var z = Input.GetAxisRaw("Vertical");
                _ = SendInputAsync(x, z, ++inputSequence);
            }
        }

        private async Task SendInputAsync(float x, float z, long sequence)
        {
            try
            {
                await connection.InvokeAsync("MoveInput", new MoveInputRequest
                {
                    X = x, Z = z, Sequence = sequence
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Movement update failed: " + ex.GetType().Name);
            }
        }

        private void ApplySnapshot(PlayerSnapshot snapshot, bool forceLocal)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.PlayerId)) return;
            if (snapshot.PlayerId == playerId)
            {
                localTarget = new Vector3(snapshot.X, snapshot.Y, snapshot.Z);
                localTargetYaw = snapshot.Yaw;
                if (forceLocal && localPlayer != null) localPlayer.position = localTarget;
                hasLocalSnapshot = true;
                return;
            }

            if (!remotePlayers.TryGetValue(snapshot.PlayerId, out var avatar))
            {
                avatar = CreateRemoteAvatar(snapshot);
                remotePlayers.Add(snapshot.PlayerId, avatar);
            }
            avatar.TargetPosition = new Vector3(snapshot.X, snapshot.Y, snapshot.Z);
            avatar.TargetYaw = snapshot.Yaw;
            avatar.NameLabel.text = snapshot.Username;
        }

        private RemoteAvatar CreateRemoteAvatar(PlayerSnapshot snapshot)
        {
            var root = new GameObject("Remote_" + snapshot.PlayerId);
            root.transform.position = new Vector3(snapshot.X, snapshot.Y, snapshot.Z);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Remote player body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.transform.localScale = Vector3.one;
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.78f, 0.55f, 0.22f);

            var labelObject = new GameObject("Player name");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 48;
            label.color = Color.white;
            label.text = snapshot.Username;
            return new RemoteAvatar(root.transform, label,
                new Vector3(snapshot.X, snapshot.Y, snapshot.Z), snapshot.Yaw);
        }

        private void RemoveRemote(string id)
        {
            if (remotePlayers.TryGetValue(id, out var avatar))
            {
                remotePlayers.Remove(id);
                if (avatar.Root != null) Destroy(avatar.Root.gameObject);
            }
        }

        private async void OnDestroy()
        {
            if (connection != null)
            {
                try { await connection.StopAsync(); }
                catch (Exception) { /* teardown */ }
                await connection.DisposeAsync();
            }
        }

        [Serializable]
        private sealed class MoveInputRequest
        {
            public float X { get; set; }
            public float Z { get; set; }
            public long Sequence { get; set; }
        }

        [Serializable]
        public sealed class PlayerSnapshot
        {
            public string PlayerId { get; set; }
            public string Username { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Yaw { get; set; }
        }

        private sealed class RemoteAvatar
        {
            public Transform Root { get; }
            public TextMesh NameLabel { get; }
            public Vector3 TargetPosition;
            public float TargetYaw;
            public RemoteAvatar(Transform root, TextMesh label, Vector3 position, float yaw)
            {
                Root = root; NameLabel = label; TargetPosition = position; TargetYaw = yaw;
            }
        }
    }
}
