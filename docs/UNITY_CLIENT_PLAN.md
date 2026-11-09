# Unity Android client plan

The first backend slice is API-first. Create a Unity 2022.3 LTS or newer 3D project and add the client modules below.

- AuthClient: HTTPS login/register; keep access token in memory, not PlayerPrefs.
- WorldConnection: SignalR client with reconnect and bearer-token provider.
- PlayerController: local input/animation prediction only; server coordinates are authoritative.
- RemotePlayerRegistry: handle PlayerJoined, OnlinePlayers, PlayerMoved, and PlayerLeft.
- SceneBootstrap: load a small original 3D test zone after authentication.

Movement currently has a coarse 8-unit-per-command and world-bound guard. This is not complete anti-cheat. Production movement needs server-time speed limits, acceleration checks, collision/navmesh validation, sequence numbers, per-player rate limits, and persisted position.

Use HTTPS/WSS and normal certificate validation. Never ship database credentials, invite codes, or JWT signing keys in the APK. Use original or properly licensed assets.
