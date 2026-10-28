# ADNoirBase

[![CI](https://github.com/alidmrky/ADNoirBase/actions/workflows/ci.yml/badge.svg)](https://github.com/alidmrky/ADNoirBase/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16.4-black?logo=nextdotjs)](https://nextjs.org/)
[![PRs welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

A production-shaped starting point for small to mid-size projects: a **.NET 9
modular monolith** with a **Next.js 16 frontend**, wired together with JWT auth,
a consistent response envelope, and a database layer that works against both
PostgreSQL and MongoDB.

It is deliberately **not** a framework. Clone it, delete the parts you do not
want, and start building.

> **Status:** pre-1.0. The architecture is stable; expect small breaking changes
> until a `v1.0.0` tag exists. No independent security audit has been performed —
> read [`SECURITY.md`](SECURITY.md) and [`docs/SECURITY_ISSUES.md`](docs/SECURITY_ISSUES.md)
> before deploying.

---

## Table of Contents

- [Why this exists](#why-this-exists)
- [What's included](#whats-included)
- [Architecture](#architecture)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [API surface](#api-surface)
- [Core conventions](#core-conventions)
- [Adding a module](#adding-a-module)
- [Project layout](#project-layout)
- [Testing and CI](#testing-and-ci)
- [Contributing](#contributing)
- [License](#license)

---

## Why this exists

Most .NET templates give you either a single-project CRUD sample that falls
apart at the third feature, or a full enterprise Clean Architecture setup with
CQRS, MediatR, and six layers of indirection for a `GET /users`.

ADNoirBase sits in between:

- **Modules, not layers-only.** Each feature is a self-contained vertical slice
  (`Domain`, `Application`, `Infrastructure`) that could later be lifted into
  its own service. You still get Clean Architecture boundaries, scoped to a module.
- **No CQRS, no MediatR.** Application logic lives in plain services with
  constructor injection. You can read a request end to end by following three
  method calls.
- **One response shape.** Every endpoint returns `BaseResponse` or
  `BaseResponse<T>`, so the frontend has exactly one contract to handle.
- **Both databases, one interface.** `IRepository<TEntity, TId>` covers
  PostgreSQL and MongoDB, so a module can pick whichever fits its data.

## What's included

| Area | Choice |
| --- | --- |
| Runtime | .NET 9, C# 13 |
| API | ASP.NET Core Web API, controller-based |
| Relational | PostgreSQL via EF Core 9 (Npgsql) |
| Document | MongoDB via the official 3.x driver |
| Auth | JWT access tokens + rotating refresh tokens, BCrypt hashing |
| Validation | FluentValidation |
| Logging | Serilog, console + rolling file, correlation IDs |
| Docs | Swagger / OpenAPI (development only) |
| Health | `/health/live` and `/health/ready` |
| Hardening | Per-client rate limiting, security headers, HSTS |
| Migrations | Dedicated `DbMigrator` console project |
| Frontend | Next.js 16 App Router, React 19, TypeScript |
| UI | shadcn/ui, Radix primitives, Tailwind CSS v4 |
| Frontend auth | Auth.js v5 (NextAuth) credentials provider |
| Frontend validation | Zod |
| CI | GitHub Actions, Dependabot |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      ui/  (Next.js 16)                          │
│   App Router · Server Components · Auth.js v5 · Tailwind v4     │
└───────────────────────────────┬─────────────────────────────────┘
                                │  JSON over HTTPS, Bearer JWT
┌───────────────────────────────▼─────────────────────────────────┐
│                      ADNoirBase.API                             │
│   Controllers · Exception + CorrelationId middleware · Swagger  │
└───────────────────────────────┬─────────────────────────────────┘
                                │
          ┌─────────────────────┴──────────────────────┐
          │                                            │
┌─────────▼──────────┐                      ┌──────────▼─────────┐
│  Identity module   │                      │   Your next module │
│  ────────────────  │                      │   ──────────────── │
│  Identity.Domain   │                      │   Order.Domain     │
│  Identity.Application                     │   Order.Application│
│  Identity.Infrastructure                  │   Order.Infrastructure
└─────────┬──────────┘                      └──────────┬─────────┘
          │                                            │
          └─────────────────────┬──────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│  Shared.Infrastructure  JWT · AuditInterceptor · CurrentUser    │
│  Shared.Application     Service interfaces · PagedRequest       │
│  Shared.Domain          Entity hierarchy · IRepository          │
│  Shared.Contracts       BaseResponse · MessageCodes             │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
         ┌──────▼──────┐                 ┌──────▼──────┐
         │ PostgreSQL  │                 │   MongoDB   │
         └─────────────┘                 └─────────────┘
```

Dependencies point inward and never backward. An `Application` project never
references an `Infrastructure` project — when it needs infrastructure
behaviour, it declares an interface and `Infrastructure` implements it.

### Entity hierarchy

```
BaseEntity<TId>                         Id
  └── AuditableEntity<TId>              + CreatedAt, CreatedBy
        └── FullAuditableEntity<TId>    + UpdatedAt, UpdatedBy
              └── SoftDeletableEntity<TId>  + IsDeleted, DeletedAt, DeletedBy
```

`TId` can be `Guid`, `int`, or `string`. Aliases exist so the common case stays
short: `BaseEntity` (Guid), `IntEntity` (int), `StringEntity` (string, for
MongoDB documents).

Audit fields are populated automatically by an EF Core
`SaveChangesInterceptor`, and soft-deleted rows are hidden by a global query
filter. You do not set these by hand.

## Quick start

**Requirements:** .NET SDK 9.0 (pinned in `global.json`), Docker, Node.js 22+.

> Setting the project up on a new machine? [`docs/FIRST_RUN.md`](docs/FIRST_RUN.md)
> (Turkish) walks through it step by step, lists everything that has to change
> per machine, and collects the errors people hit on a fresh clone.

### 1. Clone and configure

```bash
git clone https://github.com/alidmrky/ADNoirBase.git
cd ADNoirBase
cp .env.example .env
```

Open `.env` and fill in the empty values. Nothing ships with a default
credential, and the application refuses to start without them:

```bash
# Generate a database password and a JWT signing key
openssl rand -base64 24   # -> POSTGRES_PASSWORD
openssl rand -base64 48   # -> Jwt__SecretKey  (min. 32 characters)
```

### 2. Start the databases

```bash
docker compose up -d postgres mongodb
```

### 3. Migrate and seed

```bash
docker compose run --rm migrator
```

This applies EF Core migrations, seeds the `ADMIN` and `USER` roles, creates
the admin account from `ADMIN_EMAIL` and `ADMIN_PASSWORD`, and sets up the
MongoDB indexes and default notification templates. Change the admin password
after your first login.

Running it with `dotnet run` instead works too, but `dotnet run` does not read
`.env` — load the variables into your shell first, as described in
[`docs/FIRST_RUN.md`](docs/FIRST_RUN.md).

### 4. Run the API

```bash
dotnet run --project src/ADNoirBase.API
```

The API listens on <http://localhost:5197>. Swagger is served at `/swagger` in
the Development environment only. Store local secrets with
`dotnet user-secrets` rather than `.env`.

### 5. Run the frontend

```bash
cd ui
cp .env.local.example .env.local   # then fill it in
npm install
npm run dev
```

The UI is at <http://localhost:3000>.

### Or: everything in Docker

```bash
docker compose up --build
```

Compose reads `.env` and will refuse to start if a required variable is
missing, rather than falling back to a weak default.

## Configuration

### Notifications

The Notification module supports email, SMS (log provider), and in-app delivery.
Admins use `/notification-admin` for templates, sandboxed previews, self-addressed
test sends, attachments, manual To/Cc/Bcc delivery, campaigns and delivery logs.
Users have `/notifications`, `/notification-preferences` and a live SignalR bell;
unsubscribe links open the public `/unsubscribe` confirmation page.

Registration and password changes enqueue transactional notifications. Marketing
campaigns respect preferences for recipients resolved from the user directory.
Admin APIs are under `/api/admin/notifications`; user APIs are under
`/api/notifications`. The authenticated hub is `/hubs/notifications`.

Local email/SMS providers default to `Log`. Configure `Notification__Email__Provider`
as `Pickup` for `.eml` files or `Smtp` for delivery. Set
`Notification__UnsubscribeSecret` and `Notification__AppUrl` before marketing sends.
Optional local SMTP capture: `docker compose --profile tools up -d mailpit`.
See [first-run instructions](docs/FIRST_RUN.md) for host/container SMTP settings,
limits and the local smoke test; [notification status](docs/NOTIFICATION_STATUS.md)
records verified behavior and remaining environment checks.

Configuration follows the standard ASP.NET Core precedence: `appsettings.json`,
then `appsettings.{Environment}.json`, then environment variables. **Every
committed config file ships with empty secret values.** Use environment
variables in production and `dotnet user-secrets` locally.

Double underscores map to nested JSON sections, so `Jwt__SecretKey` sets
`{ "Jwt": { "SecretKey": ... } }`.

| Variable | Required | Notes |
| --- | --- | --- |
| `ConnectionStrings__PostgreSQL` | Yes | Npgsql connection string |
| `ConnectionStrings__MongoDB` | Yes | Only if a module uses MongoDB |
| `Jwt__SecretKey` | Yes | Minimum 32 characters; startup fails below that |
| `Jwt__Issuer` / `Jwt__Audience` | Yes | Default `ADNoirBase` |
| `Cors__AllowedOrigins__0`, `__1`, … | Yes | `*` is rejected outside Development |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Migrator only | No fallback; password min. 12 characters |
| `RateLimiting__*` | No | Defaults to 100/min globally, 5/min on auth |
| `ForwardedHeaders__*` | Behind a proxy | Required for correct client IPs |
| `Bcrypt__WorkFactor` | No | Default 12, allowed 10–16; older hashes are upgraded on next login |
| `MongoDb__DatabaseName` | No | Default `adnoirbase` |

See [`.env.example`](.env.example) for the full annotated list.

### Fail-fast behaviour

The application deliberately refuses to start — rather than running insecurely —
when the JWT key is missing or shorter than 32 characters, when `Cors:AllowedOrigins`
is `*` outside Development, or when the migrator is run without `ADMIN_PASSWORD`.

## API surface

All endpoints return a `BaseResponse` envelope. `BaseApiController` maps
`ResponseStatus` to the HTTP status code, so the body and the status line never
disagree.

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | Anonymous | Issue access + refresh tokens |
| `POST` | `/api/auth/register` | Anonymous | Create an account |
| `POST` | `/api/auth/refresh-token` | Anonymous | Rotate the refresh token; reusing a rotated token revokes all of the user's sessions |
| `POST` | `/api/auth/logout` | Bearer | Revoke the refresh token |
| `GET` | `/api/user/me` | Bearer | Current user profile |
| `PUT` | `/api/user/me` | Bearer | Update own profile |
| `PUT` | `/api/user/me/change-password` | Bearer | Change own password |
| `GET` | `/api/admin/users` | `ADMIN` | Paged user list |
| `GET` | `/api/admin/users/{userId}` | `ADMIN` | User detail |
| `POST` | `/api/admin/users` | `ADMIN` | Create a user |
| `PUT` | `/api/admin/users/{userId}` | `ADMIN` | Update a user |
| `POST` | `/api/admin/users/{userId}/activate` | `ADMIN` | Activate |
| `POST` | `/api/admin/users/{userId}/deactivate` | `ADMIN` | Deactivate |
| `POST` | `/api/admin/users/{userId}/roles/{roleId}` | `ADMIN` | Assign a role |
| `DELETE` | `/api/admin/users/{userId}/roles/{roleId}` | `ADMIN` | Remove a role |
| `GET` | `/health/live` | Anonymous | Liveness; touches no dependency |
| `GET` | `/health/ready` | Bearer | Readiness; checks Postgres and Mongo |

Every `/api/auth/*` endpoint is rate limited to 5 requests per minute per
client, and every other endpoint to 100. Exceeding either returns `429` with a
`Retry-After` header and the same `BaseResponse` envelope. Tune the limits
under the `RateLimiting` configuration section, or set `RateLimiting:Enabled`
to `false` in integration tests.

If you run behind a reverse proxy, set `ForwardedHeaders:Enabled` to `true`
and list your proxies under `ForwardedHeaders:KnownProxies` or
`KnownNetworks`. Rate limiting partitions anonymous traffic by client IP, so
without this every user shares one bucket. The application refuses to start if
you enable forwarded headers without naming the trusted proxies, since that
would let any client spoof its IP and bypass the limits.

`/health/ready` reports database names and failure details, so it requires
authentication. `/health/live` returns a bare `Healthy` for load balancers that
cannot authenticate.

### Response shape

```jsonc
{
  "isSuccess": true,
  "status": "Success",
  "messageCode": "identity.auth.login_success",
  "data": { "accessToken": "…", "refreshToken": "…" },
  "errors": [],
  "warnings": [],
  "correlationId": "4f1c…",
  "timestampUtc": "2026-10-27T12:34:56Z"
}
```

`isSuccess` is derived from `status` rather than stored, so a response cannot
claim success while carrying a failure status.

## Core conventions

These are enforced in review and in `.cursor/rules/`. Full detail is in
[`CONTRIBUTING.md`](CONTRIBUTING.md).

**One type per file.** A file holds exactly one top-level type and is named
after it. Generic types use an `OfT` suffix (`BaseResponseOfT.cs`). Nested
classes bundled into one file are not accepted.

**Every method returns `BaseResponse`.** No `ProblemDetails`, no bare DTOs.

**No literal message strings.** Messages are constants in a `MessageCodes`
catalog, named `{module}.{entity}.{state}`:

```csharp
return BaseResponse<UserDto>.NotFound(IdentityMessageCodes.User.NotFound);
```

Shared codes live in `Shared.Contracts/Messages/MessageCodes.cs`; module codes
live in `[Module].Application/Messages/[Module]MessageCodes.cs`.

**Warning-free builds.** `Directory.Build.props` sets
`TreatWarningsAsErrors=true` for every project. Disable a genuinely wrong
analyzer rule in `.editorconfig` with a comment, not with scattered `#pragma`s.

**Central package versions.** Versions live in `Directory.Packages.props`;
`.csproj` files reference packages without a `Version` attribute.

## Adding a module

Say you want an `Order` module.

1. Create three projects under `src/Modules/Order/`: `Order.Domain`,
   `Order.Application`, `Order.Infrastructure`. Wire references so dependencies
   point inward.
2. Add `Order.Application/Messages/OrderMessageCodes.cs`.
3. Implement `IModuleRegistration` in `Order.Application/DependencyInjection.cs`.
   Modules are discovered by reflection, so this is what makes it load.
4. Add `OrderDbContext` in `Order.Infrastructure` with its own Postgres schema,
   or use MongoDB if the data is document-shaped.
5. Derive repositories from `PostgresRepository<OrderDbContext, TEntity, TId>` or
   `MongoRepository<TEntity, TId>`, both in `ADNoirBase.Shared.Infrastructure`.
6. Register the infrastructure extension in `src/ADNoirBase.API/Program.cs`.

If you use Cursor, `.cursor/agents/module-creator.md` scaffolds all of this.

## Project layout

```
ADNoirBase/
├── src/
│   ├── ADNoirBase.API/              Controllers, middleware, composition root
│   ├── Shared/
│   │   ├── …Shared.Contracts/       BaseResponse, MessageCodes
│   │   ├── …Shared.Domain/          Entity hierarchy, IRepository
│   │   ├── …Shared.Application/     Service interfaces, PagedRequest
│   │   └── …Shared.Infrastructure/  JWT, BCrypt, AuditInterceptor
│   ├── Modules/
│   │   └── Identity/                Domain · Application · Infrastructure
│   └── Tools/
│       └── ADNoirBase.DbMigrator/   Migrations + seeding
├── tests/                           xUnit projects, one per source project
├── ui/                              Next.js 16 frontend
├── docs/
│   ├── PROJECT_OVERVIEW.md          Architecture reference
│   └── SECURITY_ISSUES.md           Tracked hardening backlog
├── .cursor/                         Rules and agents for AI-assisted work
├── Directory.Build.props            Shared MSBuild properties
└── Directory.Packages.props         Central package versions
```

## Testing and CI

```bash
dotnet build          # must be warning-free
dotnet test

cd ui
npx tsc --noEmit
npm run lint
npm run build
```

The test projects are scaffolded but intentionally empty — this is a template,
and the tests you need depend on what you build. The structure mirrors `src/`
so there is an obvious home for each new test.

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs the backend and
frontend jobs on every push and pull request to `main`. Dependabot opens
grouped weekly dependency pull requests for NuGet, npm, and GitHub Actions.

## Contributing

Issues and pull requests are welcome. Read [`CONTRIBUTING.md`](CONTRIBUTING.md)
first — most review feedback is about matching the existing structure, and that
document spells out what the structure is.

For security problems, do not open a public issue; follow
[`SECURITY.md`](SECURITY.md).

## License

[MIT](LICENSE) © Ali Demirkaya
