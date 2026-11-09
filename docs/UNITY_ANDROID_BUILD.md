# Android APK build setup

The Unity APK workflow is in `.github/workflows/unity-android.yml`. It is manual so it does not repeatedly fail before Unity licensing is configured.

## One-time Unity Personal license setup

1. Open the repository's **Actions** tab.
2. Select **Unity Personal License Activation File** and click **Run workflow**.
3. When it completes, download the `Unity-Personal-Activation-Request` artifact and extract the `.alf` file.
4. Upload that `.alf` file to Unity's manual license activation page: https://license.unity3d.com/manual
5. Complete the Personal license activation flow and download the resulting `.ulf` file.
6. In GitHub, open **Settings → Secrets and variables → Actions → New repository secret**.
7. Add:
   - `UNITY_LICENSE`: the entire text contents of the downloaded `.ulf` file
   - `UNITY_EMAIL`: the email for the Unity account
   - `UNITY_PASSWORD`: the Unity account password

Do not commit the `.ulf` file, account password, or license material into the repository. If Unity no longer offers Personal activation in the manual portal, follow the current GameCI activation troubleshooting guidance; do not attempt to bypass Unity licensing.

## Build the APK

1. Open **Actions → Unity Android APK**.
2. Click **Run workflow** on the `main` branch.
3. Wait for dependency restore, Unity compilation and Android IL2CPP build.
4. Download the `Private-Ragnarok-Android-APK` artifact from the successful run.

The build uses Unity 2022.3.62f1 and produces an ARM64 APK at `client/Unity/Build/PrivateRagnarok.apk`.

## What the APK still needs to connect

The app asks for a public HTTPS API URL on its login screen. The API must be deployed and reachable from the phone. The current Private Ragnarok Mobile Railway project was not provisioned as part of this work, and existing Ragnarok Online services are intentionally left untouched. The prototype multiplayer server uses in-memory presence and must run as a single API instance.

## First device test

Install the APK on an Android device, enter the HTTPS API URL, register using a valid invite code, and log in. To verify multiplayer, use two separate authenticated devices/instances against the same API service. Confirm both players spawn, move, and see each other leave/join. This is a test prototype, not a complete MMORPG.
