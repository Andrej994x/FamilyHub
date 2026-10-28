# Claude Code guardrails (`.claude/`)

This is the project-level Claude Code configuration for FamilyHub. It adds **deterministic guardrails** against accidental destructive Git operations, changes to secrets, and dangerous database commands. It enforces part of the approval rules in [AGENTS.md](../AGENTS.md#actions-that-need-explicit-owner-approval) without relying on prompt instructions.

> **Not a sandbox.** These checks catch the commands an agent normally writes. They don't catch deliberately obfuscated or indirect invocations, such as commands inside scripts, aliases, variables, `$(...)` inside quotes, or other tools. The S0–S3 process, owner approvals, branch protection and CI remain the real controls.

Verified against **Claude Code 2.1.311** (VS Code extension, Windows) and the official [hooks](https://code.claude.com/docs/en/hooks) and [permissions](https://code.claude.com/docs/en/permissions) documentation.

| File | Purpose |
|---|---|
| `settings.json` | Shared (committed) permission rules and hook registration |
| `hooks/guard.mjs` | The PreToolUse guard (Node.js built-ins only) |
| `hooks/guard.test.mjs` | Fixture tests: `node --test .claude/hooks/guard.test.mjs` |
| `hooks/agent-policy.mjs` | Fail-closed tool policy for the restricted subagents (used by `guard.mjs` and by each agent's own hook) |
| `hooks/agent-policy.test.mjs` | Policy tests, including adversarial chaining, wrapper and path cases |
| `hooks/config.test.mjs` | Least-privilege invariants for skills, agents and settings |
| `skills/<name>/SKILL.md` | Project skills: `fullstack-feature`, `ef-migration`, `testing`, `codex-review` |
| `agents/<name>.md` | Project subagents: `security-reviewer`, `test-writer` |
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
| API startup | — | Starting the FamilyHub API (details below) |
| Guard config | — | deleting anything under `.claude/` |

Read-only `SELECT` queries, `git status`/`diff`/`log`, normal `push -u`, builds, lint, tests, `gh pr create`/`view`/`edit`, `dotnet ef migrations add`/`script`/`has-pending-model-changes` and `dotnet run` of other projects are **not** affected.

### FamilyHub API startup (`ask`)
In Development, API startup **applies EF Core migrations and seeds sample data** (`DbSeeder`) into the configured database. Starting it therefore needs owner approval.

**Asks for approval:**
- **`dotnet run`** when it targets the API, either through `--project`/`-p` (a path ending in `FamilyHub.Api` or `FamilyHub.Api.csproj`), or by running in `backend/FamilyHub.Api` with no `--project`. The directory comes from the hook's `cwd` field plus earlier `cd`/`pushd`/`Set-Location` segments of the same command.
- **`dotnet watch`** with no command, or `dotnet watch run`, under the same rules.
- **Built output:** `dotnet [exec] …/FamilyHub.Api.dll`, and the apphost `FamilyHub.Api`/`FamilyHub.Api.exe`, including PowerShell `& .\…\FamilyHub.Api.exe`.
- **PowerShell `Start-Process`** for any of the above. The guard re-checks `-FilePath` with `-ArgumentList` and `-WorkingDirectory`.
- **Wrapped forms:** `nohup`, `timeout`, `ASPNETCORE_ENVIRONMENT=… dotnet run`, `bash -c "…"`, and `pwsh -Command "…"`.

**Not prompted:** `dotnet build`/`test`/`restore`/`publish`, `dotnet watch build`/`test`, EF Core commands (the EF tools stop before the seeding code runs), `dotnet run` of other projects (including `FamilyHub.Api.Tests`), and `dotnet run` outside the API directory.

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
- **Starting the API asks.** Running the app needs one approval per start, because Development startup migrates and seeds the database. Builds, EF commands and the frontend dev server are unaffected.

**Deliberately not enabled, to avoid blocking development:**
- **`permissions.disableBypassPermissionsMode`.** It would stop the owner using bypass mode. In bypass mode, permission prompts are skipped, so `ask` decisions and rules stop nothing there.
- **Blocking general `rm -rf` / `Remove-Item`.** Only `.git` (deny) and `.claude` (ask) are protected.
- **Fail-closed hooks (`onFailure: "block"`).** A broken guard would block every shell command. The deny rules are the floor instead.

## Limitations
- Static text analysis only. It doesn't detect:
  - commands built at runtime (variables, aliases, `$(...)` inside quotes);
  - commands run from script files (`bash x.sh`, `powershell -File x.ps1`, `npm run <script>`);
  - other programs that change git or databases (IDE features, GUI tools, `dotnet run` migrations);
  - SQL sent through other clients.
- `git checkout <path>` without `--` can't be told apart from `git checkout <branch>`, so it isn't flagged.
- **API startup detection** recognizes the API by name (`FamilyHub.Api` directory, project, `.dll` or `.exe`). It misses:
  - a bare `dotnet run` when the hook gets no `cwd`;
  - a copy of the project under another directory name;
  - launches from IDE debuggers (Visual Studio or VS Code F5), scripts, `npm` scripts or task runners;
  - commands built at runtime.

  A `cd` inside a subshell `( … )` is treated as if it persisted afterwards, so the check errs towards asking.
- SQL keyword matching is conservative and can ask for harmless queries that contain words like `DELETE` in string literals.
- The guard applies only to Claude Code sessions started in this repository. It doesn't apply to Codex, other tools, or commands you run yourself.
- In `bypassPermissions` mode, prompts are skipped, so `ask` protections don't apply. The `deny` behaviour was verified only in the default mode (headless `claude -p`), not in bypass mode. Avoid bypass mode in this repository.

## Skills and subagents (Phase 5)

### Skills (`skills/`)
Skills run **inline** in the main session; none forks a subagent context. **No skill sets `allowed-tools`**, which in Claude Code *grants* pre-approval, so every tool call still goes through normal permissions and the guard. None uses `!` shell injection, which would bypass permission prompts. `config.test.mjs` enforces all of this.

| Skill | Invocation | Purpose |
|---|---|---|
| `fullstack-feature` | Claude or `/fullstack-feature` | Feature workflow: risk level, backend layering and family authorization, frontend types/services/i18n, then tests, security review, verification and hand-off |
| `ef-migration` | Claude or `/ef-migration` | Pinned `dotnet-ef`, an `Up`/`Down` review checklist, data loss = S3, never touch the local database |
| `testing` | Claude or `/testing` | Choosing the suite, FamilyHub test conventions, the integration-database rule, regression tests, honest reporting |
| `codex-review` | **`/codex-review` only** (`disable-model-invocation`) | **Optional**, for complex S2/S3 changes. Writes a neutral, facts-only brief to the gitignored `.codex-review/` and prints the owner's commands. It never runs Codex. It can't edit or delegate (`disallowed-tools: Edit NotebookEdit Agent`). Routine PRs use `codex review --base origin/main` directly |

`paths` scoping is deliberately **not** used. In Claude Code 2.1.312, a `paths`-scoped skill is neither listed nor invocable by name (`/testing`) until a matching file is in play. That breaks `fullstack-feature`'s hand-offs, and the saving (about 300 characters of description) isn't worth it.

### Subagents (`agents/`)
Delegation is explicit. Neither description says "use proactively", and neither agent has the `Agent` tool, so they can't nest. Neither sets `permissionMode` (ignored under Auto Mode anyway), `mcpServers` or `memory`.

| Agent | Tools | Model | When it's used |
|---|---|---|---|
| `security-reviewer` | `Read, Grep, Glob, Bash` (no write tools) | `inherit`, effort high, max 25 turns | Required for S3, and for S2 changes touching endpoints, input handling or dependencies. Also on owner request. Reviews `origin/main...HEAD` against the FamilyHub authorization model and the AGENTS.md checklist, and reports findings only |
| `test-writer` | `Read, Grep, Glob, Edit, Write, Bash` | `sonnet`, max 40 turns | S2/S3 features: gap **analysis** without extra approval. **Writes** tests only within the approved feature scope (a `FEATURE SCOPE:` block); anything beyond it comes back as a proposal for the owner. Not for S0/S1 |

### Enforced policy (`hooks/agent-policy.mjs`): defense-in-depth, deny by default
Two layers run the same code:
1. **Settings guard** (`guard.mjs`), keyed on the hook input's `agent_type`. It runs everywhere, including headless `-p` sessions and untrusted folders.
2. **Each agent's frontmatter hook** (`node agent-policy.mjs <agent>`) with **`onFailure: block`**. If the policy can't run, the agent's tool call is blocked. Frontmatter hooks run only after workspace trust is accepted, never in `-p`.

**What the policy allows:**

| | `security-reviewer` | `test-writer` |
|---|---|---|
| Write/Edit/MultiEdit/NotebookEdit | **always denied** | Allowed in `backend/FamilyHub.Api.UnitTests/**`, `backend/FamilyHub.Api.IntegrationTests/**`, `frontend/familyhub-web/src/**/*.test.ts(x)` and `src/test/**`. **Ask:** test `*.csproj`, `package.json`/lock, `vitest.config.ts`, `src/test/setup.ts`. **Denied:** the isolation guards (`TestDatabaseGuard`, `DockerEndpointGuard`, `FamilyHubApiFixture`, `FamilyHubApiFactory`, `Safety/`), `bin/`/`obj/`, and everything else, including all production code |
| Shell allowlist | Read-only git (`status`, `diff`, `log`, `show`, `blame`, `ls-files`, `merge-base`, `rev-parse`, `grep`, branch listing), `rg`/`grep`/`cat`-style filters, `find` without actions, `gitleaks … --redact` without reports, `npm audit` (no `fix`), `dotnet list package --vulnerable`, read-only EF migration listing, read-only `gh` (`pr view`/`diff`/`checks`, `run view`/`list`, GET `api`), `node --test .claude/hooks/*.test.mjs`, `cd` | The same **plus** `dotnet build`/`test`/`restore`, `npm test`, `npm run test`/`lint`/`build`/`i18n:check`, `npx vitest run` |

**Options are allowlisted per command, not just program names.** Several "read-only" programs have options that write files or run programs. The PR #6 Codex review found `sort -o`; the follow-up audit found `uniq in out`, `sort -T`/`--compress-program`, `gitleaks --log-opts`/`--diagnostics-dir`, `npm --prefix`/`--logs-dir`/`--cache`, and EF `--msbuildprojectextensionspath`. For test-writer it also found:
- `dotnet build -o`, `--results-directory`, `--diag`, file loggers, `-bl`/`/bl`, `--use-lock-file` and `-p:`/`-t:`;
- `vitest -u`/`--coverage`/`--outputFile`/`--config`.

So every program with options validates them against its own allowlist, and an **unknown option is denied**. `uniq` takes at most one file argument, and a `dotnet` argument starting with `/` is treated as an MSBuild switch, not a path.

Only programs audited to have *no* write or exec options accept any options: coreutils `cat`, `head`, `tail`, `wc`, `ls`, `cut`, `basename`, `dirname`, `pwd`, `true`, plus `diff`, `grep`, `echo`/`printf`, and the read-only PowerShell cmdlets. For the remaining denylist-style cases, the denied options are the complete documented set: git's `--output`/`-O`/`--ext-diff`/`--textconv`, find's `-exec`/`-ok`/`-delete`/`-fprint*`/`-fls`, and rg's `--pre`/`--hostname-bin`.

**Always denied for both:**
- **chaining:** any part of `&&`, `||`, `;`, `|`, `&` or a newline that isn't allowlisted;
- **indirection:** wrappers (`bash -c`, `pwsh -Command`, `cmd /c`, `eval`, `iex`, `Start-Process`, `env`, `xargs`, `sudo`, `timeout`, `nohup`), `$` variables, `$(…)`, backticks, `<(…)`, heredocs and here-strings, `VAR=value` prefixes, programs given by path;
- **output and secrets:** redirects other than `2>&1` or to `/dev/null`, `NUL` or `$null`; references to the user-secrets store or `.env` files;
- **malformed input:** unbalanced quotes, empty commands.

**Paths** are resolved against `CLAUDE_PROJECT_DIR` and checked case-insensitively, through the **real path** of the nearest existing folder, so junctions and symlinks can't escape. These are denied: `..` escapes, UNC/`\\?\` paths, alternate data streams (`::$DATA`), trailing dots or spaces, 8.3 short names, NUL, and an unknown repository root.

**Fail-closed:** a policy exception gives deny. Unparseable hook input that names a restricted agent is blocked (`guard.mjs` exits 2; `agent-policy.mjs` emits deny). An unknown agent name in a frontmatter registration exits 2.

### Limitations
- **Defense-in-depth, not a sandbox.** The hard boundaries are the agents' `tools` lists (security-reviewer has no write tools), the permission deny rules, CI, branch protection and owner approval.
- **test-writer can run code it wrote.** It runs the tests, and test code can do anything the test process can. Review its diffs like any other change; CI re-runs everything.
- **The feature scope isn't machine-checked.** The test-writer's `FEATURE SCOPE:` limit is enforced by its instructions. The policy enforces the *test folders*, not the individual feature.
- **Headless sessions have one layer.** In `-p` and untrusted folders only the settings layer applies, and its main-session failure mode stays fail-open (Phase 3). For restricted agents, errors inside the policy still fail closed.
- `Read`/`Grep`/`Glob` aren't restricted by the policy. The secret-store `Read` deny rules apply as before.

## Verifying and changing the guard
- **Tests:** `node --test .claude/hooks/guard.test.mjs`. This runs pure fixture checks plus stdin/stdout contract tests. No command under test is executed.
- **All `.claude` tests:** `node --test .claude/hooks/guard.test.mjs .claude/hooks/agent-policy.test.mjs .claude/hooks/config.test.mjs`.
- **CI:** these run as the "Claude Code guard tests" step inside the required **`i18n parity (en/mk)`** job (`.github/workflows/ci.yml`). Failing guard tests therefore fail a required check and block the merge. The job keeps its name because the `main` ruleset refers to it.
- **Changing a rule:** update `guard.mjs` and add a test case in the same PR. Changes to `.claude/` are **S2** (shared infrastructure) and must not weaken an approval rule from `AGENTS.md`.
- **Personal overrides** go in `.claude/settings.local.json`, which is gitignored. Deny and ask rules from `settings.json` still apply.
