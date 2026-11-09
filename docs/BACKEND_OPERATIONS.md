# Backend development and deployment notes

## CI checks

Every push to `main` or `feature/**`, pull request targeting `main`, and manual workflow dispatch runs:

1. .NET 8 dependency restore
2. Release configuration compile
3. Docker image build

A green workflow verifies compilation and container packaging. It does **not** prove database connectivity, authentication behavior, SignalR gameplay correctness, or production readiness.

## Local verification

Prerequisites: .NET 8 SDK and Docker.

```bash
dotnet restore src/PrivateRagnarok.Api/PrivateRagnarok.Api.csproj
dotnet build src/PrivateRagnarok.Api/PrivateRagnarok.Api.csproj --configuration Release
docker compose up --build
```

Then request `GET /health`. The API currently creates its initial EF Core schema with `EnsureCreated`; this is acceptable only for disposable early development databases. Before any persistent shared/test/prod deployment, replace it with reviewed EF Core migrations and a controlled migration step.

## Railway deployment boundary

Do not connect or deploy this service to Railway until the project has available capacity and the user explicitly approves provisioning. When approved, deploy the API from this repository and provision PostgreSQL separately. Configure secrets as Railway variables, never in Git:

- `ConnectionStrings__GameDb`
- `Auth__JwtKey` (at least 32 bytes of high-entropy secret)
- `Auth__Issuer`
- `Auth__Audience`
- `Game__InviteCode`
- `ASPNETCORE_URLS=http://0.0.0.0:8080`

Set the API healthcheck path to `/health`. Keep the database private to the project. Use HTTPS for public client traffic.

## Known gaps before friends can play

- EF Core migrations and backup/restore procedure
- Automated API and SignalR integration tests
- Account rate limiting, lockout, and refresh-token/session revocation
- Persistent character profiles and world positions
- Authoritative tick loop, movement speed limits over time, map bounds/collision and server-side combat
- Per-player connection ownership (one active session policy) and presence cleanup
- Unity Android client, TLS configuration, and reconnect handling

Movement's current maximum-distance-per-call check is a prototype guard, not a complete speed/teleport anti-cheat system. The client must never be trusted to decide combat, item ownership, currency, or character stats.
