# Private Ragnarok Mobile — Railway deployment

The backend is an ASP.NET Core 8 API with PostgreSQL persistence, invite-code registration, JWT authentication, and a SignalR world hub.

## Railway project

- Project: `Private Ragnarok Mobile`
- Project ID: `3237f5fe-6bf8-4519-83d1-afd706f59e77`
- Environment: `production`
- Current state at the time this file was added: no services or databases provisioned.

The repository is configured to build the API from the repository root using
`src/PrivateRagnarok.Api/Dockerfile`. The root context is required because the
Dockerfile copies the project from `src/PrivateRagnarok.Api`.

## Required service variables

Configure these on the API service in Railway:

| Variable | Value |
|---|---|
| `ConnectionStrings__GameDb` | `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true` |
| `Auth__JwtKey` | A unique random secret of at least 32 bytes |
| `Game__InviteCode` | A private invite code shared only with approved testers |
| `Auth__Issuer` | `PrivateRagnarok` |
| `Auth__Audience` | `PrivateRagnarokClient` |

The PostgreSQL service must be named `Postgres` for the reference-variable
expression above. Keep the database on Railway's private network; do not expose
its TCP port publicly for the Unity client.

## API endpoints

- `GET /health` — Railway health check
- `POST /api/demo/guest` — rate-limited ephemeral guest token
- `POST /api/auth/register` — invite-code registration
- `POST /api/auth/login` — account login
- `GET /api/me` — authenticated account check
- `/hubs/world` — authenticated SignalR world hub

The Unity client should connect to the API's public HTTPS domain. PostgreSQL
should only be reachable from the API over Railway's private network.

## Deployment safety

Creating a PostgreSQL service and running an API service can consume Railway
plan resources or incur charges. This repository configuration and CI build do
not provision resources. Provision the database/API only after confirming the
current Railway plan and intended spend. Do not place JWT keys, invite codes,
database passwords, or Railway tokens in Git.
