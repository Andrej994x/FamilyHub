---
name: fullstack-feature
description: FamilyHub end-to-end feature workflow (ASP.NET Core API + React/TypeScript UI) covering risk level, layering, family authorization, en/mk i18n, tests and verification. Use when adding or changing a user-facing feature that spans backend and frontend.
---

# Full-stack feature (FamilyHub)

Follow `AGENTS.md`. This skill orders the work and points to the rules; it doesn't repeat them.

## 1. Classify and plan
- State the **S0–S3** level (AGENTS.md → *Task risk classification*). The highest matching level wins.
- **S2:** present a concise plan (scope, files, risks, verification), then implement within it.
- **S3** (auth, membership/roles, invitations, files, secrets, CORS/HTTPS, data-changing migrations): use Plan Mode and **stop until the owner approves**.
- If the scope grows or turns into S3 mid-way, stop and re-plan.

## 2. Backend (`backend/FamilyHub.Api`), in this order
1. **DTOs:** `record`s in `DTOs/<Area>/`. Never expose entities.
2. **Validator:** one FluentValidation validator per request in `Validators/`. The controller calls it explicitly (`ToValidationProblem`).
3. **Service:** interface in `Interfaces/`, implementation in `Services/`, returning `Result`/`Result<T>` with an `ErrorType`.
   - Reuse the service's existing authorization helper (`AuthorizeAsync`/`GetMembershipAsync`) in the required order: family exists (404) → member (403) → Owner/Parent for manage operations (403).
   - Load entities by `Id` **and** `FamilyId`. Routes without `familyId` go through the parent's family.
4. **Controller:** thin, class-level `[Authorize]`, `CurrentUserId` from the token, and `HandleResult`/`Error`.
5. **Registration:** add the service as scoped in `Program.cs`.
6. **Notifications:** only through `INotificationService` (it never notifies the actor).
7. **Schema change?** Use the **`ef-migration`** skill. Don't hand-edit migrations that exist on `main`.

## 3. Frontend (`frontend/familyhub-web/src`)
1. **Types** in `types/`: enums as `as const` objects whose integer order matches the backend enum.
2. **API calls** only through a `services/*Service.ts` object on `api/client.ts`.
3. **UI:** page or component; shared state via `contexts/` + `hooks/`; route guards in `routes/`; Tailwind with the `brand-*` tokens.
4. **Strings:** every visible string uses `t('…')`. Add each key to **both** `locales/en/translation.json` and `locales/mk/translation.json` with identical `{{placeholders}}`.
5. **Permissions in the UI** (`utils/roles.ts`) are cosmetic only; the backend must enforce them.

## 4. Tests (required for S2/S3)
Use the **`testing`** skill:
- unit tests for logic and validators;
- integration tests for endpoints, the 401/403/404 contract and roles;
- Vitest for UI behaviour.

For S2/S3 you may delegate gap analysis to the **`test-writer`** subagent (no extra approval). To have it write tests, pass the approved plan as a `FEATURE SCOPE:` block. Small or S0/S1 changes: write the tests yourself, since delegating costs more tokens than it saves.

## 5. Security
- S3, or S2 touching endpoints, input handling or dependencies: delegate to the **`security-reviewer`** subagent and summarise its findings in the PR.
- Optionally, run Claude Code's built-in `/security-review` as a second pass.

## 6. Verify (commands from AGENTS.md → *Commands*)
- **Backend:** `cd backend && dotnet build FamilyHub.sln -c Release -warnaserror`, then `dotnet test FamilyHub.Api.UnitTests`, then `dotnet test FamilyHub.Api.IntegrationTests` (needs Docker).
- **Frontend:** `cd frontend/familyhub-web && npm run lint && npm test && npm run build && npm run i18n:check`.
- **Migrations:** `dotnet tool run dotnet-ef migrations has-pending-model-changes` from `backend/FamilyHub.Api`.
- **Manual checks** (PWA, push, mobile layout): describe exactly what you did. Starting the API asks for owner approval, because Development startup migrates and seeds the database.
- **Reporting:** give exact counts. Never call CI or manual checks "test coverage".

## 7. Hand-off
- Fill in `.github/pull_request_template.md`, including the risk level and the verification evidence.
- **Don't commit, push, open a PR or merge unless the owner asks.**
- **Codex review:** recommended for S2 and required for S3. The owner usually runs `codex review --base origin/main`. For a complex S2/S3 change, the owner can first run `/codex-review` to prepare a risk brief.
