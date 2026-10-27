# FamilyHub

A family organizer PWA: shared tasks, calendar, shopping lists, school pickups, a Family Vault
for important records, in-app and push notifications, and reminders. It is built as a React +
TypeScript frontend on an ASP.NET Core 8 + SQL Server backend.

- Architecture: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- Engineering conventions and workflow (for people and AI agents): [AGENTS.md](AGENTS.md)

## Tech Stack

### Frontend — `frontend/familyhub-web`
- [React](https://react.dev/) + [TypeScript](https://www.typescriptlang.org/)
- [Vite](https://vite.dev/) (build tool / dev server)
- [React Router](https://reactrouter.com/) — client-side routing
- [Tailwind CSS](https://tailwindcss.com/) (v4, via `@tailwindcss/vite`) — styling
- [Axios](https://axios-http.com/) — HTTP client
- [React Hook Form](https://react-hook-form.com/) — forms
- [i18next](https://www.i18next.com/) / react-i18next — internationalization

### Backend — `backend/FamilyHub.Api`
- [ASP.NET Core 8](https://learn.microsoft.com/aspnet/core) Web API (`net8.0`)
- [Entity Framework Core 8](https://learn.microsoft.com/ef/core) + SQL Server provider
- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [JWT Bearer authentication](https://learn.microsoft.com/aspnet/core/security/authentication/jwt)
- [Swagger / Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Serilog](https://serilog.net/) — structured logging

## Repository Structure

```
FamilyHub/
├── frontend/
│   └── familyhub-web/          # React + TypeScript + Vite app
├── backend/
│   ├── FamilyHub.sln
│   ├── global.json             # pins the .NET 8 SDK
│   └── FamilyHub.Api/          # ASP.NET Core 8 Web API (EF Core migrations in Data/Migrations)
├── docs/
│   └── ARCHITECTURE.md         # system architecture
├── .github/                    # CI workflow and PR template
├── AGENTS.md                   # engineering conventions (shared by people and AI agents)
├── CLAUDE.md                   # Claude Code-specific instructions
├── dotnet-tools.json           # pinned dotnet-ef
└── README.md
```

## Prerequisites
- [Node.js](https://nodejs.org/) 22 LTS (Vite 8 needs 20.19+ or 22.12+) and npm
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or full) — required once data access is wired up

## Getting Started

### Backend
```bash
cd backend
dotnet restore
dotnet build
dotnet run --project FamilyHub.Api
```
Swagger UI is served at `https://localhost:<port>/swagger` in Development.

### Local development secrets
Secrets are never committed: `appsettings*.json` only hold empty placeholders. In Development the
real values come from [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets),
which are stored outside the repository (`%APPDATA%\Microsoft\UserSecrets\` on Windows). The API refuses
to start without a JWT signing key of at least 32 bytes. The EF Core tools read the same secrets.

```bash
cd backend/FamilyHub.Api

# Required: JWT signing key (e.g. 64 random bytes, base64-encoded)
dotnet user-secrets set "Jwt:Key" "<random-key>"

# Optional: Web Push (VAPID) key pair — without it, pushes are logged instead of sent
dotnet user-secrets set "Push:PublicKey" "<vapid-public-key>"
dotnet user-secrets set "Push:PrivateKey" "<vapid-private-key>"

# Optional: only when not using the default LocalDB instance
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>"
```

Outside Development, supply the same settings as environment variables (`Jwt__Key`,
`Push__PublicKey`, `Push__PrivateKey`, `ConnectionStrings__DefaultConnection`).

### Database migrations
The EF Core CLI is pinned in `dotnet-tools.json` to match the EF Core packages, so a globally
installed `dotnet-ef` of another version is never used by accident.

```bash
dotnet tool restore                      # once per clone, from anywhere in the repo
cd backend/FamilyHub.Api
dotnet tool run dotnet-ef migrations add <Name>
dotnet tool run dotnet-ef database update
```

### Frontend
```bash
cd frontend/familyhub-web
npm install
npm run dev         # start the Vite dev server
npm run build       # type-check and produce a production build
npm run lint        # oxlint
npm run i18n:check  # en/mk translation key + placeholder parity
```

## Continuous integration
GitHub Actions (`.github/workflows/ci.yml`) runs on every pull request to `main` and on pushes to `main`:

| Job | What it checks |
|---|---|
| Backend build | `dotnet build` in Release with compiler warnings as errors |
| EF Core migrations | No model changes without a migration; all migrations apply to an empty SQL Server 2022; the idempotent script re-applies cleanly |
| Frontend build & lint | `npm ci`, oxlint, TypeScript type-check and Vite build |
| i18n parity (en/mk) | Both locales have identical keys and `{{placeholders}}`, with no empty strings |
| Secret scan (gitleaks) | Full git history; rotated historical findings are listed by fingerprint in `.gitleaksignore` |

## Progressive Web App (PWA)

The frontend is an installable PWA (web app manifest + service worker + icons). Push
notifications are intentionally **not** implemented yet.

### Local / same-origin hosting
For an installable experience the app is served from the **same origin** as the API. Copy the
built frontend into the API's `wwwroot`, then run the API:

```bash
cd frontend/familyhub-web && npm run build          # outputs dist/
cp -r dist/* ../../backend/FamilyHub.Api/wwwroot/    # single-origin hosting
cd ../../backend/FamilyHub.Api && dotnet run
```

When `wwwroot/index.html` is present the API serves the app and falls back to `index.html`
for client-side deep links (so refresh/deep links never 404); unknown `/api/*` paths still
return real JSON 404s. When it is absent the project stays a pure API (the default dev flow,
with the Vite dev server on `:5173` talking to the API on `:5271`). `http://localhost` is a
secure context, so the PWA installs and the service worker runs without a certificate.

### Production notes
- **CORS** is environment-based (`Program.cs`). Development reflects any origin; production
  allows only the origins in `Cors:AllowedOrigins` (appsettings). With same-origin hosting no
  CORS is involved at all.
- **HTTPS**: `UseHttpsRedirection` + HSTS (production only). Behind a TLS-terminating reverse
  proxy, forwarded headers (`X-Forwarded-Proto`/`For`) are honoured so the real scheme is used.
- **Auth** is stateless JWT bearer tokens (no cookies), which works cleanly from installed
  PWAs and mobile browsers and survives refreshes/deep links via `localStorage`.

## Status
The core features are implemented: families and invitations, tasks, calendar, shopping, pickups,
the Family Vault, notifications with Web Push, and reminders. CI covers build, lint, migrations,
i18n parity and secret scanning. There are no automated tests yet. See
[docs/ARCHITECTURE.md → Known gaps](docs/ARCHITECTURE.md#12-known-gaps-observed-in-the-code).
