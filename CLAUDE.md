# CLAUDE.md

@AGENTS.md

The shared conventions, risk levels (S0–S3), required gates and approval rules are in `AGENTS.md`, imported above. This file adds only what is specific to Claude Code.

## Working in Claude Code
- **Classify first.** State the S0–S3 level at the start of a task.
- **S2:** present a concise plan, then continue in Auto Mode for that scope without asking before each safe step. Re-plan if the scope grows.
- **S3:** use Plan Mode, and do not implement until the owner explicitly approves the plan.
- **Security review.** When `AGENTS.md` requires one, run the built-in `/security-review` from the repository root (verified in Claude Code 2.1.311). It reviews the current branch's pending changes. Summarize the findings in the PR. If the command is unavailable, use the manual checklist in `AGENTS.md`.
- **Database side effects.** Starting the API in Development **applies all migrations and seeds sample data** (`DbSeeder`) into the configured database. Only run it against your local LocalDB/SQL Server.
- **Report verification honestly.** There are no automated tests. Say exactly what you built, ran and exercised. Never describe CI or manual checks as test coverage.
- **Secret-scan findings.** Never add a fingerprint to `.gitleaksignore` to silence a finding unless the owner confirms that secret has been rotated.
- **Shell.** The development machine is Windows: both PowerShell and Git Bash are available, and user-secrets live in `%APPDATA%\Microsoft\UserSecrets\`.

## Guardrails (`.claude/`)
`.claude/settings.json` registers a deterministic PreToolUse guard (`.claude/hooks/guard.mjs`) and permission rules:
- **Denied:** destructive Git, database and secret operations, such as force push, `reset --hard`, `clean -f`, `ef database drop` and secret stores.
- **Prompted:** actions that need owner approval, such as merging a PR, deleting branches, starting the FamilyHub API (it migrates and seeds the database), `ef database update`, SQL writes and user-secrets changes.

A block includes a `FamilyHub guard:` reason. Follow its suggested alternative or ask the owner. Never work around a block with a different spelling of the same command. These are guardrails, not a sandbox. See [.claude/README.md](.claude/README.md) for every rule, its trade-offs and its limitations.

## Not configured yet
This repository has no project-specific Claude Code skills, subagents or MCP servers, and Codex is not set up. Don't assume any of these exist.
