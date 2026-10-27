# AGENTS.md — FamilyHub engineering conventions

Shared instructions for every AI coding agent (Claude Code, Codex) and human contributor.
Architecture reference: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Setup: [README.md](README.md).

## Project at a glance
- `backend/FamilyHub.Api`: ASP.NET Core 8 Web API, EF Core 8, SQL Server, ASP.NET Core Identity + JWT.
- `frontend/familyhub-web`: React 19 + TypeScript + Vite 8 PWA, Tailwind CSS v4, i18next (English `en`, Macedonian `mk`).
- No automated tests exist yet. Do not claim test coverage. Verify behavior by building, running and exercising the app.

## Commands
Run from the repository root unless a directory is given.

| Purpose | Command |
|---|---|
| Restore pinned tools (`dotnet-ef` 8.0.11) | `dotnet tool restore` |
| Backend build (as CI) | `cd backend && dotnet build FamilyHub.sln -c Release -warnaserror` |
| Run API (Development) | `cd backend && dotnet run --project FamilyHub.Api --launch-profile http` |
| Model changed without a migration? | `cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations has-pending-model-changes` (add `--no-build` while a local API process locks `bin/Debug`) |
| Add a migration | `cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations add <Name>` |
| Frontend install / lint / build | `cd frontend/familyhub-web && npm ci && npm run lint && npm run build` |
| i18n parity | `cd frontend/familyhub-web && npm run i18n:check` |
| Secret scan | `gitleaks git --redact .` (gitleaks 8.30.1 in CI) |

Always use the pinned `dotnet tool run dotnet-ef`, never a global `dotnet ef` of another version.

## Backend conventions
- **Layers:** a thin controller (`Controllers/`, derives from `ApiControllerBase`) calls a service through its interface (`Interfaces/`, registered as scoped in `Program.cs`), and the service uses `AppDbContext`. Business logic and authorization live in services, not controllers.
- **Results:** services return `Result` / `Result<T>` with an `ErrorType` (Validation→400, NotFound→404, Forbidden→403, Conflict→409). Controllers return `HandleResult(...)` / `Error(...)`. Throw only for unexpected failures; `ExceptionHandlingMiddleware` turns them into a 500 `ErrorResponse`.
- **Validation:** one FluentValidation validator per request DTO in `Validators/`, injected and called explicitly in the controller, returning `ToValidationProblem(...)`. There is no auto-validation.
- **DTOs:** C# `record`s in `DTOs/<Area>/`. Never return EF entities; map with a private static `ToResponse` in the service.
- **Authorization (critical):** every family-scoped operation must do the following, in this order and in the service:
  1. The family exists, otherwise `NotFound`.
  2. The caller (`CurrentUserId`, from the JWT `sub` claim) is a `FamilyMember`, otherwise `Forbidden`.
  3. For "manage" operations, the caller's role is `Owner` or `Parent`, otherwise `Forbidden`.

  Look up child entities by **both** `Id` and `FamilyId`. For routes without a `familyId`, such as shopping items, load the parent entity first and check membership in its `FamilyId`. Follow the existing helper in the service you are changing (`AuthorizeAsync` / `GetMembershipAsync`). Frontend role checks (`utils/roles.ts`) only affect the UI and are never sufficient.
- **Data:** configure entities in `Data/Configurations/` (`IEntityTypeConfiguration<T>`). Enum columns are stored as strings (`HasConversion<string>()`). Use `DateTimeOffset` (UTC) for timestamps and `DateOnly` for calendar dates.
- **Migrations:** never edit or delete a migration that exists on `main`. Review generated migrations before committing. A migration that drops or rewrites data is S3.
- **Notifications:** create them only through `INotificationService` (it persists them first, then sends push, and never notifies the actor). Never call `IPushSender` directly from features.
- **API contract:** JSON enums are integers. Keep `frontend/familyhub-web/src/types` constant objects in sync with backend enum order.
- **Config & secrets:** commit only empty placeholders in `appsettings*.json`. Real values go in user-secrets (dev) or environment variables (`Jwt__Key`, `Push__PublicKey`, `Push__PrivateKey`, `ConnectionStrings__DefaultConnection`). Never log secrets, tokens or passwords.
- **Logging:** inject `ILogger<T>` and use structured message templates (`{Name}` placeholders, not string interpolation).

## Frontend conventions
- **HTTP:** only through `src/services/*Service.ts` objects using the shared `src/api/client.ts`. That client attaches the JWT and redirects to `/login` on a 401.
- **Types:** in `src/types/`. Model enums as `as const` objects (`erasableSyntaxOnly` forbids TypeScript `enum`).
- **State and routes:** shared state lives in `src/contexts/` and is exposed via hooks in `src/hooks/`. Route guards are in `src/routes/`.
- **i18n:** every user-visible string goes through `t(...)`. Add each key to **both** `src/locales/en/translation.json` and `src/locales/mk/translation.json` with the same `{{placeholders}}` (CI enforces this).
- **Styling:** Tailwind utilities plus the `brand-*` theme tokens in `src/index.css`.
- **TypeScript `strict` is off.** Write code that would still compile with it on: no implicit `any`, and handle `null`/`undefined`.
- **Service worker** (`public/sw.js`): never cache `/api`. Bump `CACHE` when changing the app shell.

## Task risk classification
Classify every task before starting. The highest matching level wins; if unsure, pick the higher one.

| Level | Meaning | Examples in this codebase |
|---|---|---|
| **S0** Trivial | No runtime behavior change | Docs, comments, typo fixes, translation text that keeps keys/placeholders, formatting |
| **S1** Low | Contained change in one feature, no data model, permission or dependency change | UI layout or styling, a new display field from existing data, a small bug fix inside one service or page |
| **S2** Moderate | Changes behavior across layers or shared infrastructure | New or changed endpoint or DTO contract, additive EF migration, new npm/NuGet dependency, notification/reminder/push logic, service worker/PWA caching, CI workflow, background services |
| **S3** High | Security, data integrity or irreversible impact | Authentication/JWT/Identity, family membership or role checks, invitations, file upload/storage, secrets/keys/config security, CORS/HTTPS/forwarded headers, migrations that drop or transform data, GitHub repository settings, production data |

## Required process by level

| Gate | S0 | S1 | S2 | S3 |
|---|---|---|---|---|
| Written plan before coding | — | Short, in the PR | **Concise plan** presented first, then implement within that scope | **Plan explicitly approved by the owner** before any implementation |
| Security review | — | — | Required if it touches endpoints, input handling or dependencies | **Required**. Results recorded in the PR |
| Testing | CI green | CI green + manual check of the changed behavior | CI green + manual verification steps and evidence in the PR | Same as S2, plus negative cases (unauthorized user, wrong family, invalid input) |
| Independent Codex review | — | — | Recommended | **Required** before merge |

**Planning and approval:**
- **S2:** present a concise plan covering scope, affected files, risks and the verification approach. Then implement it autonomously (e.g. Claude Code Auto Mode) within that scope, without asking before each safe step. Stop and re-plan if the scope grows or the work turns out to be S3.
- **S3:** do not start implementing until the owner explicitly approves the plan.
- **At every level:** the actions listed under *Actions that need explicit owner approval* below still need their own approval.

**Security review:** Claude Code has a built-in `/security-review` command that reviews the current branch's pending changes; it must run from inside the repository. Where it is not available (for example Codex or another tool), do a manual review of the diff and record each item in the PR:
1. **Authentication and authorization:** every new or changed endpoint is `[Authorize]`d. The service follows the family authorization order (family exists → caller is a member → role), and entities are looked up by `Id` + `FamilyId` (or through the parent's family).
2. **Input:** request DTOs have a FluentValidation validator that the controller calls, and there is no raw SQL built from strings.
3. **Data exposure:** responses are DTOs, not entities, and contain no other family's data.
4. **Secrets and logging:** nothing secret is committed or logged. `gitleaks git --redact .` is clean.
5. **Files and URLs:** uploads keep the `FamilyVaultStorage` limits, and no user-controlled paths or redirects are introduced.
6. **Dependencies:** new packages are justified. Run `npm audit` in `frontend/familyhub-web` and `dotnet list package --vulnerable` in `backend/FamilyHub.Api`. Run the latter from the project directory: SDK 8.0.201 fails when given a relative `.sln` path.
7. **Config and transport:** CORS, HTTPS, forwarded headers and the service worker's caching of `/api` are unchanged, or the change is explicitly justified.

**Testing note:** there is no test project yet, so "testing" means CI plus documented manual verification. When test infrastructure is added, S2/S3 changes must add automated tests.

**Codex note:** Codex is not configured in this repository yet. Until it is, the owner runs the independent Codex review manually and records the outcome in the PR.

## CI quality gates
`.github/workflows/ci.yml` runs on every PR to `main` and on pushes to `main`. The `main` ruleset requires all five checks to pass on a branch that is up to date with `main`:

1. **Backend build**: Release build with warnings as errors.
2. **EF Core migrations**: fails if the model has changes without a migration. Otherwise it applies all migrations to an empty SQL Server 2022 and re-runs the idempotent script.
3. **Frontend build & lint**: `npm ci`, oxlint, `tsc -b`, Vite build.
4. **i18n parity (en/mk)**: identical keys and placeholders, no empty values.
5. **Secret scan (gitleaks)**: full git history, with `.gitleaksignore` for rotated findings only.

Renaming a CI job breaks the required check in the ruleset. Treat job names as part of the contract (S2).

## Git and PR workflow
- Branch from up-to-date `main` with a type prefix (`feat/`, `fix/`, `docs/`, `chore/`, `ci/`). Use Conventional Commit messages (`feat: …`, `fix: …`).
- `main` is protected: changes land only through a PR with all CI checks green. There are no force pushes or bypasses.
- Fill in `.github/pull_request_template.md` and state the risk level (S0–S3) in the PR.
- Keep PRs to one concern. Do not mix refactors with behavior changes.

## Actions that need explicit owner approval
Agents must stop and ask before doing any of the following, even when the task seems to imply it:
- merging a PR, pushing to `main`, or deleting branches;
- changing GitHub repository settings, rulesets or secrets;
- modifying or deleting database records, or running migrations against a shared or production database;
- rotating keys or changing secret values;
- any destructive or irreversible command (`git reset --hard`, force push, dropping data, deleting files outside the task).

The approval covers only the specific action approved.
