# Private Ragnarok Mobile

Friends-only original-content mobile MMORPG. Separate from the existing rAthena/FluxCP project.

## Stack
- Unity 3D / C# Android client (integration plan in docs)
- ASP.NET Core 8 API
- SignalR realtime world hub
- PostgreSQL with EF Core
- Server-authoritative player state

## Local run
Requirements: .NET 8 SDK and Docker Compose.

1. Copy .env.example to .env and replace the JWT key and invite code.
2. Start PostgreSQL: docker compose up -d db
3. Run: dotnet run --project src/PrivateRagnarok.Api/PrivateRagnarok.Api.csproj
4. Check /health.

Container run: docker compose up --build.

## Configuration
ConnectionStrings__GameDb, Auth__JwtKey (minimum 32 bytes), Auth__Issuer, Auth__Audience, and Game__InviteCode are environment-overridable. Never commit real secrets. Use HTTPS outside local development.

## API
- GET /health
- POST /api/auth/register
- POST /api/auth/login
- GET /api/me (JWT required)
- /hubs/world (JWT required; Move validates requested coordinates)

This is an initial development foundation, not production-ready. It does not yet include combat, inventory, world partitioning, anti-cheat telemetry, account lockout, refresh tokens, migrations, or admin tools.

Use original or properly licensed assets. Do not bundle proprietary Ragnarok client files, GRFs, sprites, or music without permission.
