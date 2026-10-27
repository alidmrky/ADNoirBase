# Contributing to ADNoirBase

Thanks for taking the time to contribute. ADNoirBase is an opinionated
boilerplate, so most review feedback is about *consistency with the existing
structure* rather than personal preference. This document tells you what that
structure is, so you can match it before you open a pull request.

## Table of Contents

- [Ground rules](#ground-rules)
- [Getting set up](#getting-set-up)
- [The non-negotiable conventions](#the-non-negotiable-conventions)
- [Adding a module](#adding-a-module)
- [Commit messages](#commit-messages)
- [Pull requests](#pull-requests)
- [Reporting bugs](#reporting-bugs)
- [Security issues](#security-issues)

## Ground rules

- Be civil. Disagree with the code, not the person.
- Open an issue before starting large work, so you do not build something that
  gets rejected on architectural grounds.
- Small, focused pull requests get reviewed; 3000-line ones do not.
- Everything in `.cursor/rules/` is binding. Read those files; they are short.

## Getting set up

Requirements: .NET SDK 9.0, Docker (for Postgres and MongoDB), Node.js 20+ for
the `ui/` frontend.

```bash
git clone https://github.com/alidmrky/ADNoirBase.git
cd ADNoirBase

cp .env.example .env     # then fill in the empty values
docker compose up -d postgres mongodb

dotnet restore
dotnet build
dotnet test
```

See [`README.md`](README.md) for running the API and the UI.

## The non-negotiable conventions

These are enforced in review. A pull request that breaks one of them will be
sent back regardless of how good the underlying idea is.

### One type per file

A file contains exactly one top-level type, and the file is named after it.
Nested helper classes stuffed into a single file are not accepted. Generic
types get an `OfT` suffix in the filename, for example `BaseResponseOfT.cs`
holds `BaseResponse<T>`.

### Layer direction is one-way

Dependencies point inward only:

```
Shared.Contracts → Shared.Domain → Shared.Application → Shared.Infrastructure
                                        ↑                        ↑
                        [Module].Domain → [Module].Application → [Module].Infrastructure → API
```

An `Application` project must never reference an `Infrastructure` project. When
application code needs infrastructure behaviour, declare an interface in
`Application` and implement it in `Infrastructure`. `IPasswordHasher` and
`ITokenService` are the existing examples to copy.

### Every method returns `BaseResponse`

Service and controller methods return `BaseResponse` or `BaseResponse<T>`. Do
not return `ProblemDetails`, raw DTOs, or bare `IActionResult` bodies. Controllers
map the response status to HTTP through `BaseApiController`.

### No literal message strings

Every message is a constant in a `MessageCodes` catalog. Shared codes live in
`ADNoirBase.Shared.Contracts/Messages/MessageCodes.cs`; module codes live in
`[Module].Application/Messages/[Module]MessageCodes.cs`. Codes are lowercase and
dotted: `identity.user.not_found`.

### No CQRS

This is deliberate. Application logic lives in services, not in command and
query handlers. Do not add MediatR.

### Builds must be warning-free

`TreatWarningsAsErrors` is on, set in `Directory.Build.props`. If an analyzer
rule is genuinely wrong for this codebase, disable it in `.editorconfig` with a
comment explaining why — do not sprinkle `#pragma warning disable` through the
source.

### Package versions are centralised

Add the version to `Directory.Packages.props` and reference the package without
a `Version` attribute in the `.csproj`.

## Adding a module

A module named `Order` means three projects under `src/Modules/Order/`:

1. Create `Order.Domain`, `Order.Application`, and `Order.Infrastructure`, and
   wire the project references to match the dependency direction above.
2. Add `Order.Application/Messages/OrderMessageCodes.cs`.
3. Implement `IModuleRegistration` in `Order.Application/DependencyInjection.cs`
   so the module is discovered automatically.
4. Add `OrderDbContext` in `Order.Infrastructure` with its own Postgres schema,
   or a MongoDB collection if the data suits documents better.
5. Derive repositories from `BasePostgresRepository`.
6. Register the infrastructure extension in `src/ADNoirBase.API/Program.cs`.

`.cursor/agents/module-creator.md` automates this if you use Cursor.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(identity): add refresh token rotation
fix(api): return 409 instead of 500 on duplicate email
docs(readme): document the health endpoints
chore(deps): bump Npgsql to 9.0.5
```

Scope is the module or project, lowercase. Keep the subject under 72
characters and in the imperative mood.

## Pull requests

Before opening one:

- `dotnet build` succeeds with zero warnings.
- `dotnet test` passes.
- For `ui/` changes, `npm run lint` and `npx tsc --noEmit` both pass.
- No secrets, connection strings, or keys in the diff.
- New configuration keys are documented in `.env.example`.

In the description, say what changed and why. Link the issue it closes. If the
change is visible in the API surface, include a sample request and response.

Every pull request runs the CI workflow in `.github/workflows/ci.yml` and needs
at least one approving review before merge.

## Reporting bugs

Open an issue with: what you expected, what happened, the commit SHA, your .NET
SDK version (`dotnet --info`), and the smallest reproduction you can manage.
Logs help; stack traces help more.

## Security issues

Do not open a public issue. Follow the process in
[`SECURITY.md`](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the MIT
License, the same as the rest of the project.
