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

The initial client source implements authentication and a basic local third-person movement prototype. It deliberately does not pretend local movement is authoritative multiplayer. The next client iteration must add SignalR connection lifecycle, server snapshots, interpolation, map collision, and server-tick movement validation.

## Security
- Never ship the invite code or JWT signing key in the app.
- Invite code is entered by the invited player and sent over HTTPS.
- Keep access tokens in memory for this prototype; implement refresh/revocation before production.
- Do not enable cleartext HTTP for production Android builds.
