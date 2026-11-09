# Unity world prototype milestone

This milestone adds a procedural, original low-poly medieval town assembled from Unity primitives. It is deliberately an original layout and does not include proprietary Ragnarok/Gravity map data, models, textures, music, or other client assets.

## Files
- `WorldBootstrap.cs`: creates the meadow, stone avenues, market square, six simple buildings, fountain, trees, directional light, player capsule, camera, and a small world-space status label.
- `WorldCameraFollow.cs`: smoothed third-person orbit camera. Right mouse drag + wheel in the editor; one-finger orbit and two-finger pinch on touch devices.
- `ThirdPersonPrototypeController.cs`: local prototype movement only.

## Setup
1. Open a Unity 2022.3 LTS (or newer) 3D project.
2. Copy `client/Unity/Assets/PrivateRagnarok` into the Unity project's `Assets` directory.
3. Create an empty GameObject named `WorldBootstrap` in a new scene.
4. Attach `PrivateRagnarok.Client.WorldBootstrap`.
5. Press Play. The town is generated at runtime.
6. Set the project's Active Input Handling to **Both** or **Input Manager (Old)** because this prototype reads the legacy `Horizontal`, `Vertical`, mouse, and touch input APIs.
7. For mobile, use landscape orientation and test touch controls on a physical Android device.

## Controls
- Move: WASD / arrow keys or left virtual axes (the virtual joystick UI is not part of this milestone).
- Orbit: hold right mouse button and drag; on Android drag with one finger.
- Zoom: mouse wheel; on Android pinch with two fingers.

## Important limitations
- This is a scene bootstrap script, not a serialized Unity scene or APK. Unity Editor is required to generate the actual scene and build an Android package.
- The player is a capsule with no rig, animations, equipment, collision navigation, NPCs, quests, combat, or inventory.
- Movement is entirely local and is not sent to the backend. It is not multiplayer and must not be trusted by a server.
- Primitive geometry is intentionally used as placeholder art. Replace it with original or properly licensed assets before public distribution.
- Procedural objects use primitive colliders; optimize and author proper colliders/navmesh when the map grows.
- The script avoids destroying/rebuilding an already populated bootstrap object; remove generated children before using the context menu to regenerate.
