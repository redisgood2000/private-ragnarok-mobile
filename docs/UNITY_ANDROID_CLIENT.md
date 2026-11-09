# Unity Android client — first source milestone

Added a small Unity integration scaffold under `client/Unity`:

- `ApiAuthClient`: HTTPS JSON registration/login, response validation, memory-only access token.
- `LoginScreenController`: login/register UI wiring and safe user-facing errors.
- `ThirdPersonPrototypeController`: local CharacterController input prototype.

## Not yet implemented
This is not a complete Unity project, APK, or connected multiplayer game. It does not include a Unity scene, art, animation, character select, SignalR client, server snapshots, interpolation, combat, inventory, or map content.

## Backend contract
- Register: `POST /api/auth/register` with `username`, `password`, `inviteCode`.
- Login: `POST /api/auth/login` with `username`, `password`.
- Response: `accessToken`, `playerId`, `username`.
- Authenticated hub: `/hubs/world`.

## Backend integration caveat
The current backend uses EF Core `EnsureCreated` and in-memory SignalR presence. Before a persistent test deployment, use EF migrations, database-backed character state, rate limits, server-tick movement validation and reconnect/session handling. A max displacement per message alone is not anti-cheat.

## Unity setup
See `client/Unity/README.md` for editor version, scene wiring, Android build settings and SignalR package guidance.
