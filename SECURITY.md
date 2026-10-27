# Security Policy

## Supported Versions

ADNoirBase is a boilerplate, not a deployed service. Security fixes land on the
default branch (`main`) only. There are no maintained release branches yet.

| Version | Supported |
| ------- | --------- |
| `main`  | Yes       |
| Tagged releases | Not yet — no tags published |

## Reporting a Vulnerability

**Do not open a public issue for security problems.**

Report privately through
[GitHub Security Advisories](https://github.com/alidmrky/ADNoirBase/security/advisories/new).
If you cannot use that form, open a regular issue containing only the words
"security report, please contact me" and no technical detail, and a maintainer
will reach out.

Please include:

- The affected file and line, or the endpoint and HTTP method.
- What an attacker can do with it, concretely.
- Reproduction steps or a proof of concept.
- The commit SHA you tested against.

**Response expectations.** This project is maintained by one person in their
spare time. Expect an acknowledgement within 7 days and an assessment within
30 days. These are intentions, not a contractual SLA.

Please allow 90 days before public disclosure, or less if we agree on a fix
sooner.

## Scope

In scope:

- Authentication and authorization logic in the Identity module.
- The JWT issuing and validation path (`ADNoirBase.Shared.Infrastructure/Authentication`).
- Injection, deserialization, and data-exposure flaws in API controllers.
- Insecure defaults that a user following `README.md` would inherit.

Out of scope:

- Findings that require a deliberately misconfigured deployment
  (for example, setting `Cors:AllowedOrigins` to `*` in production, which the
  application already refuses to start with).
- Vulnerabilities in third-party packages with no exploitable path in this
  codebase. Report those upstream; Dependabot tracks our dependency versions.
- Missing hardening that is already tracked in
  [`docs/SECURITY_ISSUES.md`](docs/SECURITY_ISSUES.md).

## Known Gaps

This boilerplate has not undergone an independent security audit. Known,
deliberately tracked weaknesses are listed in
[`docs/SECURITY_ISSUES.md`](docs/SECURITY_ISSUES.md). Read that file before
deploying anything built on this template.

## Secrets

No secrets are committed to this repository. All configuration files ship with
empty values, and the application fails to start when a required secret is
missing rather than falling back to a default.

If you believe a secret has been committed, report it privately using the
process above and do not include the secret value in the report.

## Hardening Checklist Before You Deploy

- Generate a unique `Jwt__SecretKey` of at least 32 characters.
- Set `Cors__AllowedOrigins__N` to your real origins.
- Use a dedicated database role, not the Postgres superuser.
- Set a strong `ADMIN_PASSWORD` and rotate it after the first login.
- Terminate TLS in front of the API; the app does not serve HTTPS itself.
- Review every open item in [`docs/SECURITY_ISSUES.md`](docs/SECURITY_ISSUES.md).
