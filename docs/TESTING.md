# FamilyHub testing

FamilyHub has three automated suites. All three run in CI on every pull request (see [AGENTS.md → CI quality gates](../AGENTS.md#ci-quality-gates)).

| Suite | Project | Runner | Needs | Count |
|---|---|---|---|---|
| Backend unit tests | `backend/FamilyHub.Api.UnitTests` | xUnit v3 | .NET 8 SDK only | 155 |
| API integration tests | `backend/FamilyHub.Api.IntegrationTests` | xUnit v3 + `WebApplicationFactory` + Testcontainers | .NET 8 SDK, **Docker** (Linux containers) | 127 (31 need no Docker) |
| Frontend tests | `frontend/familyhub-web/src/**/*.test.ts(x)` | Vitest + React Testing Library on jsdom | Node 22.13+ | 105 |

Counts are test cases as reported by the runners (each `[Theory]`/`it.each` row counts once).

## Running the tests
```bash
# Backend: unit tests (no Docker, no database server)
cd backend
dotnet test FamilyHub.Api.UnitTests

# Backend: integration tests (Docker must be running)
dotnet test FamilyHub.Api.IntegrationTests

# Only the Docker-free isolation-guard checks
dotnet test FamilyHub.Api.IntegrationTests --filter "FullyQualifiedName~TestDatabaseGuardTests"

# Frontend
cd frontend/familyhub-web
npm test            # vitest run (CI mode)
npm run test:watch  # watch mode while developing
```

`dotnet test` and `npm test` exit non-zero when a test fails, so a failing test fails its CI job.

## Structure and naming
```
backend/
  FamilyHub.Api.UnitTests/
    Validators/    FluentValidation rules (register, task, child, invitation, pickup, reminder offsets, expenses)
    Security/      TokenService (claims, expiry, signature), FamilyVaultStorage (size/type limits, path traversal)
    Services/      Family, ChildProfile, FamilyTask, Notification, Expense, ExpenseCategory services on EF Core InMemory
    Common/        ReminderCategoryDefaults, PushSettings
    Controllers/   ApiControllerBase (ErrorType → HTTP status, CurrentUserId)
    TestSupport/   TestDb (InMemory context + family builder), fakes
  FamilyHub.Api.IntegrationTests/
    Infrastructure/  FamilyHubApiFixture (container + host), FamilyHubApiFactory, TestDatabaseGuard,
                     DockerEndpointGuard, ApiDriver (register/login/family/invite helpers)
    Auth/  Authorization/  Validation/  Workflows/  Safety/
frontend/familyhub-web/
  vitest.config.ts   reuses vite.config.ts; jsdom; setup file below
  src/test/setup.ts  jest-dom matchers, real i18n (reset to `en`), storage reset, RTL cleanup
  src/**/X.test.ts(x) next to the code under test
```
- Test classes are named `<Subject>Tests`. Test methods describe the behavior in plain words (`A_member_of_another_family_gets_403_for_every_family_resource`).
- The backend test projects are in `FamilyHub.sln`, so the CI build (`-warnaserror`) compiles them. Analyzer rule `xUnit1051` (pass a cancellation token to every call) is suppressed in the test projects only.
- Frontend tests import `describe`/`it`/`expect`/`vi` from `vitest` explicitly (no globals). They are type-checked by `tsc -b` as part of `npm run build`, and Vite never bundles them.

## What is covered
**Backend unit tests**
- Validation rules and their exact messages, including the "cannot invite as Owner" rule.
- The JWT contract: `sub` maps to `ClaimTypes.NameIdentifier` (what `CurrentUserId` reads), issuer and audience, expiry, and rejection when signed with another key.
- Vault storage limits: empty files, over 5 MB, extension and content type allow-lists, a random stored name, and `Resolve`/`Delete` never leaving the vault folder.
- Service authorization order (family exists → member → role), Owner/Parent manage rights, lookups by `Id` + `FamilyId`.
- Notification rules: the actor is never notified, duplicates collapse, role-filtered fan-out, the push flag is set only when a device accepted.
- Task status, `CompletedAt`, and list ordering and filtering.
- Expenses:
  - validation: amount > 0, ≤ 10,000,000 and at most 2 decimals, no future or pre-2000 dates, the required year and month on the history filter, category names;
  - Child denied even on reads, Member changes only their own expenses, Owner/Parent change any;
  - lookups by `Id` + `FamilyId`, another family's category or payer rejected;
  - archived categories kept on existing expenses but not newly chosen, and a former member kept as payer;
  - history month bounds, ordering and filters;
  - default categories seeded once per family, case-insensitive unique names, renamed defaults become custom;
  - delete only when unused (also removes the category's budget rows), and the last active category is protected;
  - budgets: request validation, carry-forward until changed (earlier months keep theirs), removal from a month on, same-month updates in place, an omitted category losing its budget, archived and foreign categories rejected, Members view only;
  - monthly summary: totals, remaining and usage percentage overall and per category (including over budget and budget-only categories), the five most recent expenses, month bounds and family isolation; the usage percentage is capped instead of overflowing for a tiny budget with huge spending.

**API integration tests** (the real pipeline: routing, JWT bearer auth, validators, services, EF Core on SQL Server)
- **401:** no token on protected endpoints, a token signed with another key, an expired token, a wrong password.
- **404/403** per the contract:
  - an unknown family returns 404;
  - a member of another family gets 403 on the family, members, tasks, children, events, shopping lists and invitations;
  - another family's task through your own family route returns 404, and it is not deleted;
  - a shopping item via another family's list returns 403 (a route without `familyId`);
  - another family's expense or expense category through your own family route returns 404 and is not changed, and another family's category or payer is a 400.
- **Roles:**
  - Owner and Parent can manage children and invite; Member and Child cannot;
  - only the Owner renames the family;
  - every role can work with tasks;
  - the Owner cannot be removed and a Parent cannot remove members;
  - removing a member unassigns their tasks and revokes access. This exercises `ExecuteUpdate`/`ExecuteDelete` on real SQL Server;
  - expenses: Owner, Parent and Member can add and a Child gets 403 (including on reads, the summary and budgets); only Owner and Parent set budgets, Members view them, and another family's budgets can't be changed, and a budget can't use another family's category; a Member can change only their own expenses; only Owner and Parent manage categories; removing a member keeps their expenses and payer name.
- **Validation:**
  - ValidationProblem fields for register, task title and invitation role;
  - a task assigned to another family's member;
  - reminder offset bounds and null offsets;
  - duplicate email;
  - one family per user (409);
  - accepting an invitation while in another family (409).
- **Workflow:** register → login → create family → invite → accept → create task (others notified, the creator is not) → complete → filter → delete → 404.
- **Expenses workflow:** the default categories, add with a decimal amount (`1234.56`), list by month and payer, update, delete → 404. Per-field validation problems, a category in use rejected on delete (409, real FK) and archived instead with history kept, and unique category names (case-insensitive, per family). Concurrent archive or delete requests on the last two active categories (15 rounds each of archive/archive, archive/delete and delete/delete) always leave exactly one active category, and the losing request gets 400. Creating or updating an expense while its category is being deleted never returns a 500: either the expense is saved and the delete gets 409, or the category is deleted and the expense gets 400. Twelve expenses added at once by one family are all saved.
- **Budgets workflow:** budgets set for October carry into November, a December change leaves October unchanged, and the summary totals (summed in SQL) match each month. Also: per-field validation, no new budget for an archived category, and concurrency: two complete budget sets saved at once (for a new month, and for an existing month where they change different rows) always leave one request's whole set, never a mix, and a budget change racing a category delete never returns a 500. The summary's family isolation is checked on SQL Server, a 0.01 MKD budget with a 10,000,000 MKD expense still returns 200 (percentage capped), and out-of-range months, an empty category id and more than 100 category budgets are rejected at the API.
- **Safety:** see below.

**Frontend tests**
- `api/client`:
  - the Bearer header is attached;
  - a 401 clears the token and redirects to `/login`, except on `/auth/login` and `/auth/register`, or when already on `/login`;
  - other errors keep the token.
- `contexts/AuthContext` with `useAuth`:
  - `useAuth` throws outside the provider;
  - with no token, no API call is made;
  - a stored token restores the session;
  - an invalid stored token is cleared;
  - login and logout update the stored token;
  - registering signs the user in.
- `pages/Login`:
  - required-field and email validation;
  - submitting state, then navigation to `/dashboard`, or to the pending invitation;
  - the server error message, or a translated fallback;
  - **Macedonian** rendering and validation messages.
- `components/LanguageSwitcher`: switches to `mk` and persists it to `familyhub_language`.
- `routes/ProtectedRoute`: loader, redirect to `/login`, and the outlet.
- Utilities: `apiError`, `roles` (including enum parity with the backend and the expense access/edit rules), `vaultStatus` (fixed clock), `pendingInvitation`, `money` (MKD formatting in en/mk, amount parsing with `.` or `,`), `month` (month arithmetic, local calendar dates), `expenseCategories` (translated default categories, enum parity).
- Family Expenses UI:
  - navigation: Expenses in the sidebar and under More on mobile, hidden from children, as is the dashboard's Add expense quick action; `RequireExpensesAccess` sends children to the dashboard;
  - `ExpenseDialog`: validation (amount, future date, required fields), the create/update payload, archived categories and former payers kept when editing, Macedonian text, server errors;
  - `pages/Expenses` (mocked `expenseService`): month totals, budget, remaining, usage and categories in MKD; no-budget, empty and over-budget states; Members edit only their own expenses; month switching (no future months); history filters; delete; the `?add=1` link; load errors, with another month's summary or history never shown while a month loads or after it fails; Macedonian rendering.
- HTTP is mocked at boundaries only: the Axios **adapter** (for `client.ts`) or the `authService` module (for the context). Expected UI strings are read from `locales/en|mk/translation.json`, not hard-coded.

## Integration tests and database isolation
The integration tests **never** use the local FamilyHub database, LocalDB, a shared SQL Server, or a development, CI or production connection string. `FamilyHubApiFixture` creates everything it uses, once per test run, in this order. Each step must pass before the next one starts:

1. **Local Docker only.** `DockerEndpointGuard` checks the Docker endpoint Testcontainers found. Only `npipe://`, `unix://` or a loopback `tcp://` endpoint is allowed; anything else stops the run.
2. **A disposable SQL Server container.**
   - It uses the pinned image `mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04`, the same tag as CI. `FAMILYHUB_TEST_SQL_IMAGE` may override it, but only with another `mcr.microsoft.com/mssql/server:*` image.
   - It has the label `org.familyhub.purpose=integration-tests`.
   - The **SA password is random per run** and is never written to disk or logs.
3. **A loopback, random port.** The mapped host must be `localhost` or a loopback IP, and the mapped port must not be 1433, so a locally installed SQL Server can never be reached by mistake.
4. **The server is the container.** A read-only query on the container's `master` database must report the container's own machine name (its ID prefix) and the `Linux` host platform.
5. **`TestDatabaseGuard`.** The connection string must name exactly the container's `host,port` as data source and a database called `FamilyHubTest_<guid>`. It must not use Integrated Security, `AttachDbFilename`, LocalDB, `.`, `(local)`, named instances or named pipes. `FamilyHub`, `FamilyHubCi` and the system databases are refused by name.
6. **The API host can't be redirected.** `FamilyHubApiFactory`:
   - runs the API in the **`Testing`** environment. User-secrets are not loaded (so no real secrets are needed or read), `DbSeeder` does not run, and Swagger is off.
   - **replaces** the `DbContextOptions<AppDbContext>` registration with the container connection string, so no appsettings value, environment variable or user-secret can win;
   - uses a random per-run JWT key;
   - removes the reminder background service;
   - replaces email and push senders with no-ops, so nothing leaves the process and invitation links (bearer tokens) are not logged;
   - sends Serilog's file sink to a temporary folder.
7. **Guard again, then migrate.** After the host is built, the guard re-checks the connection string that the API's `AppDbContext` actually uses. Only then are the existing EF Core migrations applied, inside the new test database.
8. **Cleanup.** The factory and container are disposed after the run. Testcontainers' resource reaper (Ryuk) removes the container even if the test process crashes. No volume is mounted, so the database disappears with the container.

`Safety/DatabaseIsolationTests` checks this from inside the running host: the data source, the database name (`DB_NAME()`), the server's machine name against the container ID, all migrations applied, no development seed users, and the `Testing` environment. `Safety/TestDatabaseGuardTests` checks the guards without Docker. They refuse:
- the connection string committed in `appsettings.json`;
- the CI `FamilyHubCi` string;
- LocalDB, `.`, named instances and pipes;
- a wrong port, a remote host, Integrated Security, `AttachDbFilename`;
- a database name without the prefix;
- remote Docker endpoints and port 1433.

**Independence.** Every test registers brand-new users (`<role>-<guid>@test.familyhub`, random passwords) and creates its own family, then works only with those. No test reads data another test created, and no cleanup between tests is needed. Tests share one container and one host for speed. They run sequentially in one xUnit collection, but don't depend on order.

If Docker is not available, the fixture fails with a "Docker is required" message. The 96 container tests then fail; **they never fall back to another server**. The 31 guard checks still run.

### Docker setup
- **Local (Windows/macOS):** Docker Desktop with Linux containers, started before `dotnet test`. The first run pulls the SQL Server image (about 1.5 GB). A full run then takes about 1–4 minutes, depending on image cache and machine.
- **GitHub Actions:** `ubuntu-latest` runners include Docker, so no extra setup is needed. The integration step runs inside the **EF Core migrations** job, before that job exports its own `Jwt__Key` and CI connection string, and it `unset`s them as defense in depth. The job's timeout is 30 minutes.

## Known limitations and gaps
- **No end-to-end/browser tests.** Service worker, PWA install, Web Push delivery and mobile layouts are still checked manually.
- **Integration coverage is representative, not exhaustive.** Not yet covered:
  - events and pickups (including assigned-member confirm/reject);
  - documents and all Family Vault records;
  - attachment upload and download;
  - invitation resend and cancel;
  - notification read/delete endpoints;
  - push subscription endpoints;
  - the reminder engine.
- **Unit tests use EF Core InMemory**, which enforces no relational constraints and doesn't support `ExecuteUpdate`/`ExecuteDelete`. Those paths are covered only by the SQL Server integration tests.
- **Frontend coverage is limited.** It covers auth, routing guards, the API client, localization switching and utilities. Pages such as Tasks, Calendar, Shopping and Vault have no component tests yet (Expenses does).
- **No coverage reporting or thresholds.**
- **Known defect found while writing tests** (not fixed in Phase 4; production code unchanged):
  - `UpdateReminderPreferenceRequestValidator` throws `NullReferenceException` when `ReminderOffsetsDays` is `null`. Its `Must(o => o.Length …)` rules still run after `NotNull()` fails, because FluentValidation's default cascade mode is `Continue`.
  - The API is **not** affected today: `[ApiController]` treats the non-nullable array as required and returns 400 before the validator runs. `InputValidationTests` asserts that.
  - Calling the validator directly would throw. A fix such as `.Cascade(CascadeMode.Stop)` belongs in its own change.
- **Existing `npm audit` findings** (not introduced by Phase 4; the test packages add none): 8 findings, 7 high and 1 moderate, in `axios` 1.18.1, `react-router-dom` 7.18.1 and `vite` 8.1.5, plus transitive `follow-redirects`, `nanoid`, `postcss`, `source-map-js`. Upgrading them is a separate dependency change.
- **Vitest is pinned to 5.0.5** (dev-only).
  - Vite 8's optional peer `@vitejs/devtools-vitest` requests `vitest@*`. With npm 10.5–10.9, installing any vitest other than the latest crashes npm's resolver (`Cannot read properties of null (reading 'edgesOut')`).
  - `npm ci` from the lockfile is unaffected.
  - When upgrading vitest later, install the latest release.
