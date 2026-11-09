# Unity Android client

This folder is a Unity 2022.3.62f1 project scaffold. The editor build bootstrap generates the prototype scene and login UI, then builds an Android ARM64 APK.

## Included prototype
- Original procedural medieval town generated from Unity primitives.
- Third-person camera, left-thumb joystick, and right-side camera orbit/pinch.
- Invite-code registration and login over HTTPS.
- Authenticated SignalR world connection, authoritative movement input, remote-player interpolation.
- Editor menu: **Private Ragnarok → Generate Prototype Scene**.
- Automated Android build method: `PrivateRagnarok.Editor.AndroidBuildBootstrap.Build`.

## Open in Unity
1. Install Unity **2022.3.62f1** with Android Build Support, Android SDK & NDK tools, and OpenJDK.
2. Open this `client/Unity` folder in Unity Hub.
3. Allow Package Manager to resolve TextMeshPro, UGUI and NuGetForUnity.
4. Import TMP Essential Resources if prompted.
5. In NuGetForUnity, restore `Assets/packages.config` so `Microsoft.AspNetCore.SignalR.Client 8.0.7` and dependencies are installed.
6. Open **Private Ragnarok → Generate Prototype Scene** and press Play.

## Configure a reachable backend
Enter the public HTTPS API base URL in the login screen before logging in. The backend must already be deployed and reachable from the phone. A localhost address on the developer PC will not work from an Android phone. The current server stores multiplayer presence in process memory, so use one API instance for the prototype.

## Build APK locally
Run the editor build method `PrivateRagnarok.Editor.AndroidBuildBootstrap.Build`. Output: `Build/PrivateRagnarok.apk`.

## GitHub Actions APK build
The workflow `.github/workflows/unity-android.yml` is manual, restores NuGet dependencies in CI, and requires a valid Unity license file in repository secret `UNITY_LICENSE`. Add `UNITY_EMAIL` and `UNITY_PASSWORD` only if required by your license activation method. Then run **Actions → Unity Android APK → Run workflow**. The APK is uploaded as the `Private-Ragnarok-Android-APK` artifact.

## Important limitations
This is a first playable-prototype target, not a finished MMORPG. Placeholder capsule characters and primitive buildings are used. There is no combat, skills, quests, NPC logic, inventory, character selection, persistent world state, reconnect token refresh, collision navigation, or production anti-cheat. The Unity client must pass an actual editor/Android build and device test before it can be called playable. Do not ship proprietary Ragnarok/Gravity assets unless you have the necessary rights.
