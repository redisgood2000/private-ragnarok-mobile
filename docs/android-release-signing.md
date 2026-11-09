# Android release signing

Private Ragnarok uses one long-lived Android release keystore for every distributable APK. Never commit the keystore or its passwords to Git.

## Existing test APK transition

The previously published APK (Unity Android build run #19) was signed with the generic Android Debug certificate, SHA-256:

89:C9:B4:13:6C:09:2F:CA:2D:DE:E2:CC:DC:03:7E:1B:C4:36:A7:B3:DF:55:5E:7B:08:FE:80:FD:CD:5B:87:06

The matching private debug key is not available in the repository or APK. Android cannot update that installation using a newly generated release key. The first release signed with the permanent keystore therefore requires uninstalling the old test build and installing the new signed APK once. Later updates will install over it and preserve app data, provided the package ID and keystore remain unchanged.

## Create the permanent keystore (Windows)

Use a trusted desktop. Install Android Studio (which includes a Java runtime), open PowerShell, and run:

```powershell
$keytool = "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe"
if (!(Test-Path $keytool)) { $keytool = "keytool" }

& $keytool -genkeypair -v -keystore .\privateragnarok-release.keystore `
  -storetype PKCS12 -alias privateragnarok -keyalg RSA -keysize 4096 -validity 10000
```

Choose a strong keystore password and store it in a password manager. Use alias privateragnarok. For simplicity, use the same strong password for the key password when prompted. Keep an offline backup. Losing the keystore makes future in-place updates impossible.

Encode the file for GitHub Actions:

```powershell
$bytes = [System.IO.File]::ReadAllBytes((Resolve-Path .\privateragnarok-release.keystore))
[Convert]::ToBase64String($bytes) | Set-Clipboard
```

## Add GitHub repository secrets

Open https://github.com/redisgood2000/private-ragnarok-mobile/settings/secrets/actions and create these four Repository secrets:

| Secret name | Value |
|---|---|
| ANDROID_KEYSTORE_BASE64 | Clipboard contents from the PowerShell command |
| ANDROID_KEYSTORE_PASS | Keystore password |
| ANDROID_KEYALIAS_NAME | privateragnarok |
| ANDROID_KEYALIAS_PASS | Key password (same as keystore password if chosen) |

Do not add the keystore file itself to GitHub, the repository, or a public release.

## Build verification

The Unity Android workflow requires all four secrets, signs each APK using the same keystore, verifies the APK signature, and prints the signer certificate SHA-256 fingerprint in the Actions log. Compare that fingerprint across releases; it must never change.

The package ID remains com.privateragnarok.mobile. Keep it unchanged for in-place updates.