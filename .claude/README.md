# Claude Code guardrails (`.claude/`)

This is the project-level Claude Code configuration for FamilyHub. It adds **deterministic guardrails** against accidental destructive Git operations, changes to secrets, and dangerous database commands. It enforces part of the approval rules in [AGENTS.md](../AGENTS.md#actions-that-need-explicit-owner-approval) without relying on prompt instructions.

> **Not a sandbox.** These checks catch the commands an agent normally writes. They don't catch deliberately obfuscated or indirect invocations, such as commands inside scripts, aliases, variables, `$(...)` inside quotes, or other tools. The S0–S3 process, owner approvals, branch protection and CI remain the real controls.

Verified against **Claude Code 2.1.311** (VS Code extension, Windows) and the official [hooks](https://code.claude.com/docs/en/hooks) and [permissions](https://code.claude.com/docs/en/permissions) documentation.

| File | Purpose |
|---|---|
| `settings.json` | Shared (committed) permission rules and hook registration |
| `hooks/guard.mjs` | The PreToolUse guard (Node.js built-ins only) |
| `hooks/guard.test.mjs` | Fixture tests: `node --test .claude/hooks/guard.test.mjs` |
| `settings.local.json` | Your personal overrides (gitignored, optional) |

**Requirements:**
- Start Claude Code **in the repository root**, so the project settings load.
- Node.js on `PATH`. Node 22 is already a project prerequisite.
- The hook runs in exec form (`node <script>`), so it needs neither Git Bash nor PowerShell.

## How the layers fit together
1. **PreToolUse hook (`guard.mjs`).** It runs before every `Bash`, `PowerShell`, `Edit`, `Write`, `MultiEdit` and `NotebookEdit` call. It parses the command (honouring quotes, heredoc bodies and PowerShell here-strings, so text inside commit messages isn't treated as a command) and returns:
   - **`deny`**: irreversible operations. Claude receives the reason and a safer alternative.
   - **`ask`**: operations that `AGENTS.md` reserves for owner approval. You get a permission prompt, even in Auto Mode. In headless `claude -p` runs, an ask becomes a refusal.
   - **nothing**: everything else, which then follows the normal permission flow. The hook never auto-approves anything.
2. **Permission `deny` rules.** They protect the secret stores (file tools *and* recognized Bash file commands and redirections). They also repeat the most destructive commands, so protection remains if the hook ever fails.
3. **Permission `ask` rules.** Edits to the guard itself, `.gitleaksignore` and `appsettings*.json` always prompt.

Deny and ask rules can't be overridden by hooks or by `allow` rules in other settings files.

## Hook: `PreToolUse` → `guard.mjs`
- **Trigger:** matcher `Bash|PowerShell|Edit|Write|MultiEdit|NotebookEdit`. Per the docs it runs before every matching tool call, whether or not that call needs permission.
- **Timeout:** 10 s.
- **Input:** hook JSON on stdin (`tool_name`, `tool_input.command` / `file_path` / `content` / `new_string`).
- **Output:** a `hookSpecificOutput.permissionDecision` of `deny` or `ask` with a `FamilyHub guard: …` reason, and exit 0.
- **Failure behaviour:** if the script itself errors, it exits 1. Claude Code shows a hook-error notice and continues (fail-open), and the permission deny rules below still apply.

### Commands it checks (Bash and PowerShell)
It also looks inside `bash -c`, `sh -c`, `pwsh -Command`, `cmd /c`, `eval` and `Invoke-Expression`. It strips `VAR=value` prefixes and the `timeout`, `nice`, `env` and `sudo` wrappers, and skips git global options such as `-C dir` and `-c k=v`.

| Area | `deny` | `ask` (owner approval) |
|---|---|---|
| Git history | `push --force`/`-f`/`--force-with-lease`/`+refspec`, `push --mirror`/`--prune`, `push --delete`/`:branch`, direct push to `main`/`master`, `reset --hard`, `clean -f` (unless `-n`), `filter-branch`/`filter-repo`, `reflog expire`/`delete`, `gc --prune=now`, `update-ref -d`, deleting `.git` | `checkout --`/`.`/`-f`, `restore` (working tree), `switch --discard-changes`, `stash drop`/`clear`, `branch -d`/`-D`/`-f`, `commit`/`push --no-verify`, `push --all`, `add --force` |
| GitHub CLI | `gh repo delete` | `gh pr merge`; `gh repo edit`/`rename`/`archive`; `gh secret`/`variable set`/`delete`; `gh release delete`; `gh api` with a non-GET method or fields (except read-only GraphQL queries); `gh auth token` |
| Secrets | `dotnet user-secrets clear` | `dotnet user-secrets set`/`remove`/`init`/`list` (`list` prints secret values); any command naming the user-secrets directory; writes to `.gitleaksignore` |
| Database | `dotnet ef database drop`, `dotnet ef database update 0`, SQL containing `DROP`/`TRUNCATE`/`SHUTDOWN` (`sqlcmd -Q`, `Invoke-Sqlcmd -Query`, heredoc input), `sqllocaldb delete` | `dotnet ef database update`, `dotnet ef migrations remove`, SQL with `INSERT`/`UPDATE`/`DELETE`/`MERGE`/`ALTER`/`CREATE`/`EXEC`/…, `sqlcmd -i` scripts, `bcp … in` |
| Guard config | — | deleting anything under `.claude/` |

Read-only `SELECT` queries, `git status`/`diff`/`log`, normal `push -u`, builds, lint, tests, `gh pr create`/`view`/`edit`, `dotnet ef migrations add`/`script`/`has-pending-model-changes` and `dotnet run` are **not** affected.

### File edits it checks (Write, Edit, MultiEdit)
| Target | Decision |
|---|---|
| `appsettings*.json` containing a non-empty `…Key`/`…Secret`/`…Password`/`…Token` value, or a connection string with `Password=` | `deny`: commit placeholders only, real values go in user-secrets or environment variables |
| Anything in the user-secrets store (`…/Microsoft/UserSecrets/…`) | `deny` |
| `.gitleaksignore` | `ask` |

## Permission rules (`settings.json`)
| Rule | Type | Why |
|---|---|---|
| `Read`/`Edit(~/AppData/Roaming/Microsoft/UserSecrets/**)`, `Read`/`Edit(~/.microsoft/usersecrets/**)` | deny | Keeps secret values out of the conversation and stops changes (Windows and macOS/Linux store paths) |
| `Read`/`Edit(**/.env)`, `**/.env.local`, `**/.env.*.local` | deny | Local env files that by convention hold secrets (none exist in the repo today) |
| `Bash`/`PowerShell(git push --force *)`, `(git push -f *)`, `(git reset --hard *)`, `(git clean -f*)`, `(dotnet ef database drop *)`, `(dotnet user-secrets clear *)`, `(gh repo delete *)` | deny | Backstop for the most destructive commands if the hook fails |
| `Edit(/.claude/**)` | ask | The guard can't be weakened silently |
| `Edit(/.gitleaksignore)` | ask | Fingerprints are added only for rotated secrets (CLAUDE.md) |
| `Edit(/backend/FamilyHub.Api/appsettings*.json)` | ask | Configuration security is S3 territory |

## Trade-offs
**Enabled, with a small cost to normal work:**
- `dotnet user-secrets list` asks, because it prints secret values. Checking which keys exist now needs one approval.
- Discarding working-tree changes (`git checkout -- f`, `git restore f`) asks. Reverting an agent's own edits needs one approval; `git stash` is the unprompted alternative.
- `dotnet ef database update` asks. The local database holds shared development data; CI already validates migrations against a throwaway SQL Server.

**Deliberately not enabled, to avoid blocking development:**
- **`permissions.disableBypassPermissionsMode`.** It would stop the owner using bypass mode. In bypass mode, permission prompts are skipped, so `ask` decisions and rules stop nothing there.
- **Prompting on `dotnet run`,** even though Development startup migrates and seeds the database (CLAUDE.md). Running the app is everyday work.
- **Blocking general `rm -rf` / `Remove-Item`.** Only `.git` (deny) and `.claude` (ask) are protected.
- **Fail-closed hooks (`onFailure: "block"`).** A broken guard would block every shell command. The deny rules are the floor instead.

## Limitations
- Static text analysis only. It doesn't detect:
  - commands built at runtime (variables, aliases, `$(...)` inside quotes);
  - commands run from script files (`bash x.sh`, `powershell -File x.ps1`, `npm run <script>`);
  - other programs that change git or databases (IDE features, GUI tools, `dotnet run` migrations);
  - SQL sent through other clients.
- `git checkout <path>` without `--` can't be told apart from `git checkout <branch>`, so it isn't flagged.
- SQL keyword matching is conservative and can ask for harmless queries that contain words like `DELETE` in string literals.
- The guard applies only to Claude Code sessions started in this repository. It doesn't apply to Codex, other tools, or commands you run yourself.
- In `bypassPermissions` mode, prompts are skipped, so `ask` protections don't apply. The `deny` behaviour was verified only in the default mode (headless `claude -p`), not in bypass mode. Avoid bypass mode in this repository.

## Verifying and changing the guard
- **Tests:** `node --test .claude/hooks/guard.test.mjs`. This runs pure fixture checks plus stdin/stdout contract tests. No command under test is executed.
- **Changing a rule:** update `guard.mjs` and add a test case in the same PR. Changes to `.claude/` are **S2** (shared infrastructure) and must not weaken an approval rule from `AGENTS.md`.
- **Personal overrides** go in `.claude/settings.local.json`, which is gitignored. Deny and ask rules from `settings.json` still apply.
