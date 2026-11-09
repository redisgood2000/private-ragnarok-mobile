# Unity world prototype milestone

This milestone adds a procedural, original low-poly medieval town assembled from Unity primitives. It is deliberately an original layout and does not include proprietary Ragnarok/Gravity map data, models, textures, music, or other client assets.

## Files
- `WorldBootstrap.cs`: creates the meadow, stone avenues, market square, six simple buildings, fountain, trees, directional light, player capsule, camera, and a small world-space status label.
- `WorldCameraFollow.cs`: smoothed third-person orbit camera. Right mouse drag + wheel in the editor; one-finger orbit and two-finger pinch on touch devices.
- `ThirdPersonPrototypeController.cs`: local movement is disabled once network control is enabled.\n- `WorldNetworkClient.cs`: authenticated SignalR connection, presence list, join/leave events, server snapshot handling, and remote avatar interpolation.\n- `MobileMovementInput.cs`: left-thumb virtual joystick; camera orbit remains on the right half of the screen.

## Setup
1. Open a Unity 2022.3 LTS (or newer) 3D project.
2. Copy `client/Unity/Assets/PrivateRagnarok` into the Unity project's `Assets` directory.
3. Create an empty GameObject named `WorldBootstrap` in a new scene.
4. Attach `PrivateRagnarok.Client.WorldBootstrap`.
5. Press Play. The town is generated at runtime.
6. Set the project's Active Input Handling to **Both** or **Input Manager (Old)** because this prototype reads the legacy `Horizontal`, `Vertical`, mouse, and touch input APIs.
7. For mobile, use landscape orientation and test touch controls on a physical Android device.

## Controls
- Move: WASD / arrow keys in the editor; on touch devices use the left-side virtual joystick.
- Orbit: hold right mouse button and drag; on Android drag with one finger.
- Zoom: mouse wheel; on Android pinch with two fingers.

## Multiplayer setup\n1. Add a Unity-compatible `Microsoft.AspNetCore.SignalR.Client` package using NuGetForUnity. Use a version whose assemblies target a Unity-supported .NET Standard profile; verify the package in the actual Unity editor before Android export.\n2. Add the login UI and `ApiAuthClient` as described in `client/Unity/README.md`. The login controller discovers the generated `WorldNetworkClient` after successful authentication.\n3. Configure the backend API base URL as HTTPS. The client connects to `/hubs/world` with the JWT supplied through SignalR's access-token provider.\n4. Run two authenticated clients against the same API instance to verify join, movement and leave replication.\n\n## Server movement contract\n- Client sends `MoveInput(X, Z, Sequence)` at about 10 updates/second. X/Z are direction intent, not coordinates.\n- Server clamps input, normalizes diagonal movement, caps elapsed simulation time, applies a fixed walk speed, and clamps the prototype map bounds. Y is server-owned.\n- Server broadcasts accepted `PlayerMoved` snapshots to all connected clients. Clients interpolate remote avatars.\n- Current presence and positions are in process memory. A restart clears them; multiple server replicas will not share state. Do not horizontally scale this prototype yet.\n\n## Important limitations
- This is a scene bootstrap script, not a serialized Unity scene or APK. Unity Editor is required to generate the actual scene and build an Android package.
- The player is a capsule with no rig, animations, equipment, collision navigation, NPCs, quests, combat, or inventory.
- The movement protocol is now wired through SignalR, but the client has not been compiled or device-tested in Unity. Server presence is in-memory and single-instance only.
- Primitive geometry is intentionally used as placeholder art. Replace it with original or properly licensed assets before public distribution.
- Procedural objects use primitive colliders; optimize and author proper colliders/navmesh when the map grows.
- The script avoids destroying/rebuilding an already populated bootstrap object; remove generated children before using the context menu to regenerate.
