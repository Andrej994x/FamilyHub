# CLAUDE.md

@AGENTS.md

The shared conventions, risk levels (S0–S3), required gates and approval rules are in `AGENTS.md`, imported above. This file adds only what is specific to Claude Code.

## Working in Claude Code
- **Classify first.** State the S0–S3 level at the start of a task.
- **S2:** present a concise plan, then continue in Auto Mode for that scope without asking before each safe step. Re-plan if the scope grows.
- **S3:** use Plan Mode, and do not implement until the owner explicitly approves the plan.
- **Security review.** When `AGENTS.md` requires one, delegate to the **`security-reviewer`** subagent, which applies the AGENTS.md checklist to the FamilyHub authorization model, and summarize its findings in the PR. The built-in `/security-review` (run from the repository root) is an optional second pass.
- **Database side effects.** Starting the API in Development **applies all migrations and seeds sample data** (`DbSeeder`) into the configured database. Only run it against your local LocalDB/SQL Server.
- **Report verification honestly.** Say exactly what you built, ran and exercised, with the actual test counts and results. Claim coverage only for behavior a test really exercises ([docs/TESTING.md](docs/TESTING.md) lists the gaps). Never describe CI or manual checks as test coverage.
- **Integration tests need Docker.** `dotnet test FamilyHub.Api.IntegrationTests` starts a disposable SQL Server container and never touches the local database. If Docker isn't running, report that rather than pointing the tests at another server.
- **Secret-scan findings.** Never add a fingerprint to `.gitleaksignore` to silence a finding unless the owner confirms that secret has been rotated.
- **Shell.** The development machine is Windows: both PowerShell and Git Bash are available, and user-secrets live in `%APPDATA%\Microsoft\UserSecrets\`.

## Guardrails (`.claude/`)
`.claude/settings.json` registers a deterministic PreToolUse guard (`.claude/hooks/guard.mjs`) and permission rules:
- **Denied:** destructive Git, database and secret operations, such as force push, `reset --hard`, `clean -f`, `ef database drop` and secret stores.
- **Prompted:** actions that need owner approval, such as merging a PR, deleting branches, starting the FamilyHub API (it migrates and seeds the database), `ef database update`, SQL writes and user-secrets changes.

A block includes a `FamilyHub guard:` reason. Follow its suggested alternative or ask the owner. Never work around a block with a different spelling of the same command. These are guardrails, not a sandbox. See [.claude/README.md](.claude/README.md) for every rule, its trade-offs and its limitations.

## Skills & subagents
- **Skills** (in `.claude/skills/`, run inline):
  - `fullstack-feature` for features spanning API and UI;
  - `ef-migration` for model or migration changes;
  - `testing` when writing or running tests;
  - `/codex-review` (owner-invoked, **optional**): a neutral risk brief for complex S2/S3 changes. Routine PRs use `codex review --base origin/main` directly. Codex is an independent reviewer and never replaces automated tests.
- **Delegate sparingly:** each subagent costs its own tokens.
  - **`security-reviewer`** (read-only): for S3, and for S2 touching endpoints, input handling or dependencies.
  - **`test-writer`**: coverage-gap analysis for S2/S3 features (no extra approval). Test writing only with the approved plan passed as a `FEATURE SCOPE:` block; anything beyond it goes back to the owner.
  - Don't delegate searches, S0/S1 work or small tasks; do them directly.
- Both agents are restricted by `.claude/hooks/agent-policy.mjs`: deny-by-default command allowlists, test-folder-only edits for test-writer, and fail-closed hooks. A block reads `FamilyHub guard: <agent>: …` or `FamilyHub agent policy: …`. See [.claude/README.md](.claude/README.md).
- **Not configured:** project MCP servers, and Codex automation (Claude never runs Codex).
