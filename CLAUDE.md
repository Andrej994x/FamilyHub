# CLAUDE.md

@AGENTS.md

The shared conventions, risk levels (S0–S3), required gates and approval rules are in `AGENTS.md`, imported above. This file adds only what is specific to Claude Code.

## Working in Claude Code
- **Classify first.** State the S0–S3 level at the start of a task. For S2/S3, use Plan Mode and get the plan agreed before editing.
- **S3 approval.** Do not start implementing an S3 change until the owner approves the plan.
- **Security review.** When `AGENTS.md` requires one, run Claude Code's built-in `/security-review` on the branch and summarize the findings in the PR.
- **Database side effects.** Starting the API in Development **applies all migrations and seeds sample data** (`DbSeeder`) into the configured database. Only run it against your local LocalDB/SQL Server.
- **Report verification honestly.** There are no automated tests. Say exactly what you built, ran and exercised. Never describe CI or manual checks as test coverage.
- **Secret-scan findings.** Never add a fingerprint to `.gitleaksignore` to silence a finding unless the owner confirms that secret has been rotated.
- **Shell.** The development machine is Windows: both PowerShell and Git Bash are available, and user-secrets live in `%APPDATA%\Microsoft\UserSecrets\`.

## Not configured yet
This repository has no project-specific Claude Code skills, subagents, hooks or MCP servers, and Codex is not set up. Don't assume any of these exist. Adding them is separate, planned work.
