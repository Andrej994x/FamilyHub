---
name: testing
description: FamilyHub automated testing - choose and write xUnit unit tests, SQL Server Testcontainers integration tests or Vitest tests, regression tests for bug fixes, and run them with exact results. Use when adding or changing tests or verifying a change with tests.
---

# Testing (FamilyHub)

The source of truth is `docs/TESTING.md` (structure, isolation and gaps) and AGENTS.md → *Testing note*. Read the section you need rather than the whole file.

## 1. Pick the suite
| Behaviour under test | Suite | Location and helpers |
|---|---|---|
| Validators, services, rules, mapping | xUnit **unit** | `backend/FamilyHub.Api.UnitTests` (`TestSupport/TestDb` on EF Core InMemory) |
| Endpoints, JWT, the 401/403/404 contract, roles, cross-family isolation, SQL-specific behaviour (`ExecuteUpdate`, constraints, migrations) | xUnit **integration** | `backend/FamilyHub.Api.IntegrationTests` (`Infrastructure/ApiDriver`, shared `FamilyHubApiFixture`) |
| Components, pages, hooks, the API client, i18n rendering | **Vitest** + React Testing Library | `frontend/familyhub-web/src/**/X.test.ts(x)` next to the code |

InMemory doesn't enforce relational rules, so anything relational needs an integration test.

## 2. Write it the FamilyHub way
- **Names:** test classes are `<Subject>Tests`. Methods describe behaviour (`A_member_of_another_family_gets_403_for_every_family_resource`).
- **Integration:**
  - each test registers **fresh users** (`<role>-<guid>@test.familyhub`) and its own family, so no test depends on another;
  - assert status codes **and** the effect, e.g. not deleted, or the actor not notified;
  - cover every role that matters: Owner, Parent, Member, Child.
- **Database rule (absolute):** integration tests only use the disposable Testcontainers SQL Server from `FamilyHubApiFixture`, checked by `TestDatabaseGuard`.
  - Never change the guards, `FamilyHubApiFactory`, `DockerEndpointGuard` or `Safety/`.
  - Never use LocalDB, the local FamilyHub database, or a shared or production connection.
  - If Docker is missing, the tests fail. They must never fall back to another server.
- **Vitest:**
  - import `describe`/`it`/`expect`/`vi` from `vitest`;
  - mock only at boundaries (the Axios adapter or a service module);
  - read expected strings from `locales/en|mk/translation.json` instead of hard-coding them, and test Macedonian where text matters.
- **Regression tests for bugs:** first write a test that **fails** and reproduces the bug, then fix, then show it passing. In the report, state that it failed before the fix.
- **Never** weaken an assertion, add a skip, or loosen a guard to get a green run.

## 3. Run (exact commands)
```bash
cd backend && dotnet test FamilyHub.Api.UnitTests                         # no Docker
cd backend && dotnet test FamilyHub.Api.IntegrationTests                  # Docker (Linux containers) required
cd backend && dotnet test FamilyHub.Api.IntegrationTests --filter "FullyQualifiedName~TestDatabaseGuardTests"   # Docker-free guard checks
cd frontend/familyhub-web && npm test                                     # Vitest (CI mode)
```
CI runs all three suites in the required jobs (Backend build, EF Core migrations, Frontend build & lint).

## 4. Report honestly
- The exact command and the **exact** passed/failed/skipped counts from the runner output.
- Say when a suite didn't run (for example, Docker unavailable). Never present CI, manual checks or a Codex review as test coverage.
- List the remaining gaps, including any from `docs/TESTING.md` that are still relevant.

## Delegation
For S2/S3 features, the **`test-writer`** subagent can do gap analysis without extra approval, and can write tests within the approved `FEATURE SCOPE:`. For small or S0/S1 changes, write the tests directly.
