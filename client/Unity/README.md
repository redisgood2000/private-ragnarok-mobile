# Unity Android client scaffold

This is the first Unity client source drop, not a complete Unity project export or APK yet.

## Target
- Unity 2022.3 LTS or newer
- Android, IL2CPP, ARM64
- Original/licensed 3D models, animations, maps, UI and audio only
- API base URL must be HTTPS outside local development

## Add to a Unity project
1. Create a Unity 3D (URP optional) project.
2. Copy `Assets/PrivateRagnarok` into the project's `Assets` folder.
3. Add TextMeshPro via Window > TextMeshPro > Import TMP Essential Resources.
4. Create a Canvas with TMP_InputField username/password/invite fields, Button login/register, TMP_Text status and token display optional; assign them to `LoginScreenController`.
5. Set API Base Url in the inspector to the backend URL.
6. For Android, set Player Settings > Other Settings > Scripting Backend to IL2CPP and Target Architectures to ARM64.

## SignalR package
The backend hub is `/hubs/world`. Add the Microsoft.AspNetCore.SignalR.Client NuGet package compatible with the Unity scripting runtime using NuGetForUnity (or a vetted Unity-compatible package workflow). Do not copy arbitrary DLLs from untrusted sources. The hub accepts the JWT as the `access_token` query parameter for WebSocket negotiation/connection.

The client source now includes authenticated SignalR connection lifecycle, online-player snapshots, join/leave events, server-authoritative movement input, remote-avatar interpolation, and a left-thumb mobile joystick. The server computes positions from normalized movement intent; the client does not submit world coordinates. Map collision, combat, inventory, character animation, token refresh, persistent world state, and multi-instance coordination are still outstanding.

## Security
- Never ship the invite code or JWT signing key in the app.
- Invite code is entered by the invited player and sent over HTTPS.
- Keep access tokens in memory for this prototype; implement refresh/revocation before production.
- Do not enable cleartext HTTP for production Android builds.

## Procedural town scene
See `docs/UNITY_WORLD_PROTOTYPE.md`. Copy the scripts into a Unity project, add an empty GameObject with `WorldBootstrap`, and press Play to generate the original low-poly town. This is a local visual prototype only; it is not yet connected to the multiplayer server.

## Multiplayer world scripts
- `WorldNetworkClient.cs` — connects to `/hubs/world` after login, sends movement intent at 10 Hz, and interpolates server snapshots.
- `MobileMovementInput.cs` — left-side touch joystick. The right side is reserved for camera orbit/pinch.
- `WorldCameraFollow.cs` — camera-relative movement and orbit controls.
- `WorldBootstrap.cs` — creates the original low-poly test town and wires the local avatar/network components.

The backend currently keeps online sessions and positions in process memory. Run a single API instance for multiplayer tests; restarting it clears presence, and multiple replicas will not share positions.
