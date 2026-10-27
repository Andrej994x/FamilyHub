/*
 * Fixture tests for guard.mjs. Run from the repository root:  node --test .claude/hooks/guard.test.mjs
 * Pure input → decision checks; nothing here executes the commands under test.
 */

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { evaluate } from './guard.mjs';

const bash = (command) => evaluate({ tool_name: 'Bash', tool_input: { command } });
const ps = (command) => evaluate({ tool_name: 'PowerShell', tool_input: { command } });
const action = (decision) => decision?.action ?? 'none';

// Everyday development commands must pass through untouched (no decision → normal permissions).
const ALLOWED_BASH = [
  'git status', 'git diff', 'git diff --cached --stat', 'git log --oneline -5', 'git show HEAD',
  'git branch --show-current', 'git branch -vv', 'git fetch origin', 'git pull --ff-only origin main',
  'git checkout main', 'git checkout -b feat/x', 'git switch -c feat/y', 'git add -A', 'git add README.md',
  'git restore --staged README.md', 'git stash', 'git stash pop', 'git clean -n -d', 'git reset --soft HEAD~1',
  'git push -u origin feat/x', 'git push', 'git rm --cached file.txt', 'git -C backend status',
  'git commit -m "docs: explain why git reset --hard and git push --force are blocked"',
  "git commit -q -F - <<'EOF'\nci: mention git clean -fdx and DROP TABLE in a message\n\nrm -rf .git\nEOF",
  'cd backend && dotnet build FamilyHub.sln -c Release -warnaserror',
  'dotnet tool restore', 'dotnet run --project FamilyHub.Api --launch-profile http',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations has-pending-model-changes --no-build',
  'dotnet tool run dotnet-ef migrations add AddThing', 'dotnet ef migrations script --idempotent -o out.sql',
  'cd frontend/familyhub-web && npm ci && npm run lint && npm run build', 'npm run i18n:check',
  'gitleaks git --redact .', 'node --test .claude/hooks/guard.test.mjs', 'cat .gitleaksignore', 'grep -n Key appsettings.json',
  'gh pr create --base main --title t --body-file b.md', 'gh pr view 3 --json state', 'gh pr edit 3 --body-file b.md',
  'gh run watch 123 --exit-status', 'gh api repos/o/r/rulesets', 'gh api repos/o/r/commits/abc/check-runs -q ".check_runs[].name"',
  "gh api graphql -f query='query{repository(owner:\"o\",name:\"r\"){id}}'",
  'sqlcmd -S "(localdb)\\MSSQLLocalDB" -d FamilyHub -E -Q "SELECT Id, UpdatedAt, CreatedAt FROM PushSubscriptions"',
  'rm -rf frontend/familyhub-web/dist', 'rm .git/index.lock', 'ls -la .github',
];

const ALLOWED_PS = [
  'git status', 'Get-ChildItem C:\\dev\\FamilyHub', 'dotnet build', 'npm run lint',
  'Remove-Item -Recurse -Force frontend\\familyhub-web\\dist',
  "git commit -m @'\nfix: never run git reset --hard here\n'@",
];

const ASK_BASH = [
  'git checkout -- src/App.tsx', 'git checkout .', 'git restore src/App.tsx', 'git switch --discard-changes main',
  'git stash drop', 'git stash clear', 'git branch -D old', 'git branch -d merged', 'git commit --no-verify -m x',
  'git push --no-verify', 'git add -f .env',
  'gh pr merge 3 --merge', 'gh repo edit --visibility private', 'gh secret set TOKEN', 'gh release delete v1',
  'gh api -X POST repos/o/r/rulesets --input r.json', 'gh api --method=DELETE repos/o/r/rulesets/1',
  'gh api repos/o/r/issues -f title=x', "gh api graphql -f query='mutation { x }'", 'gh auth token',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef database update', 'dotnet ef --project FamilyHub.Api database update',
  'dotnet ef migrations remove', 'dotnet user-secrets set "Jwt:Key" "x"', 'dotnet user-secrets remove Push:PrivateKey',
  'dotnet user-secrets list', 'cat "$APPDATA/Microsoft/UserSecrets/abc/secrets.json"',
  'sqlcmd -S . -d FamilyHub -E -Q "UPDATE PushSubscriptions SET IsActive = 0 WHERE Id = 1"',
  'sqlcmd -S . -E -i script.sql', "sqlcmd -S . -E <<'SQL'\nDELETE FROM Notifications\nSQL",
  'echo abc >> .gitleaksignore', 'rm -rf .claude/hooks', 'bash -c "git stash drop"',
];

const ASK_PS = [
  'git restore .', 'dotnet user-secrets set Jwt:Key x', 'gh pr merge 2',
  'Invoke-Sqlcmd -ServerInstance . -Query "DELETE FROM Notifications"',
  'Get-Content $env:APPDATA\\Microsoft\\UserSecrets\\abc\\secrets.json',
  'Add-Content .gitleaksignore "abc"', 'powershell -EncodedCommand ZQBjAGgAbwA=',
];

const DENY_BASH = [
  'git push --force', 'git push -f origin feat/x', 'git push origin feat/x --force-with-lease', 'git push -fu origin x',
  'git push origin +feat/x', 'git push origin --delete feat/x', 'git push origin :feat/x', 'git push origin main',
  'git push origin HEAD:main', 'git push --mirror', 'git -C . push --force', 'git -c push.default=current push -f',
  'git reset --hard', 'git reset --hard origin/main', 'git clean -fd', 'git clean -xdf', 'git clean --force',
  'git filter-branch --tree-filter x', 'git reflog expire --expire=now --all', 'git gc --prune=now', 'git update-ref -d refs/heads/x',
  'git status && git reset --hard', 'echo ok; git clean -f', 'FOO=1 git push --force', 'timeout 30 git reset --hard',
  'bash -c "git reset --hard"', 'sh -c \'git push -f\'', '/usr/bin/git reset --hard', 'eval "git clean -fdx"',
  'gh repo delete o/r --yes', 'dotnet ef database drop --force', 'dotnet tool run dotnet-ef database update 0',
  'dotnet user-secrets clear', 'sqlcmd -S . -E -Q "DROP DATABASE FamilyHub"', 'sqlcmd -Q "truncate table Notifications"',
  'sqllocaldb delete MSSQLLocalDB', 'rm -rf .git', 'rm -rf ./.git/',
];

const DENY_PS = [
  'git push --force', 'git reset --hard', 'git status; git clean -fdx', 'Remove-Item -Recurse -Force .git',
  'Remove-Item -Recurse C:\\dev\\FamilyHub\\.git', 'dotnet ef database drop', 'Invoke-Sqlcmd -Query "DROP TABLE Notifications"',
  'pwsh -Command "git push -f"', 'cmd /c "git reset --hard"', 'Invoke-Expression "git clean -f"',
];

const cases = [
  ...ALLOWED_BASH.map((c) => ['Bash', c, 'none']), ...ALLOWED_PS.map((c) => ['PowerShell', c, 'none']),
  ...ASK_BASH.map((c) => ['Bash', c, 'ask']), ...ASK_PS.map((c) => ['PowerShell', c, 'ask']),
  ...DENY_BASH.map((c) => ['Bash', c, 'deny']), ...DENY_PS.map((c) => ['PowerShell', c, 'deny']),
];

for (const [tool, command, expected] of cases) {
  test(`${tool} ${expected}: ${command.replace(/\n/g, '⏎').slice(0, 90)}`, () => {
    const decision = tool === 'Bash' ? bash(command) : ps(command);
    assert.equal(action(decision), expected, decision?.reason);
  });
}

// File edits (Write / Edit / MultiEdit)
const edit = (tool_name, tool_input) => action(evaluate({ tool_name, tool_input }));
const APPSETTINGS = 'C:\\dev\\FamilyHub\\backend\\FamilyHub.Api\\appsettings.json';

test('edit: empty secret placeholders in appsettings are allowed', () => {
  assert.equal(edit('Edit', { file_path: APPSETTINGS, old_string: 'x', new_string: '"Key": "",\n"PrivateKey": ""' }), 'none');
  assert.equal(edit('Write', { file_path: APPSETTINGS, content: '{ "Jwt": { "Issuer": "FamilyHub.Api", "Key": "", "ExpiryMinutes": 60 } }' }), 'none');
});
test('edit: secret values in appsettings are denied', () => {
  assert.equal(edit('Edit', { file_path: APPSETTINGS, old_string: '"Key": ""', new_string: '"Key": "super-secret-signing-key-1234567890"' }), 'deny');
  assert.equal(edit('Write', { file_path: APPSETTINGS.replace('.json', '.Development.json'), content: '{"Push":{"PrivateKey":"abc"}}' }), 'deny');
  assert.equal(edit('Edit', { file_path: APPSETTINGS, old_string: 'a', new_string: '"DefaultConnection": "Server=.;User Id=sa;Password=P@ss1;"' }), 'deny');
  assert.equal(edit('MultiEdit', { file_path: APPSETTINGS, edits: [{ old_string: 'a', new_string: '"ApiKey": "k-123"' }] }), 'deny');
});
test('edit: user-secrets store is denied, .gitleaksignore asks, normal files pass', () => {
  assert.equal(edit('Write', { file_path: 'C:\\Users\\me\\AppData\\Roaming\\Microsoft\\UserSecrets\\id\\secrets.json', content: '{}' }), 'deny');
  assert.equal(edit('Edit', { file_path: 'C:\\dev\\FamilyHub\\.gitleaksignore', old_string: 'a', new_string: 'b' }), 'ask');
  assert.equal(edit('Edit', { file_path: 'C:\\dev\\FamilyHub\\README.md', old_string: 'a', new_string: 'Password: see user-secrets' }), 'none');
  assert.equal(edit('Write', { file_path: 'C:\\dev\\FamilyHub\\frontend\\familyhub-web\\src\\App.tsx', content: 'const token = "x";' }), 'none');
});
test('other tools are ignored', () => {
  assert.equal(action(evaluate({ tool_name: 'Read', tool_input: { file_path: 'x' } })), 'none');
  assert.equal(action(evaluate({})), 'none');
});

// End-to-end through stdin/stdout exactly as Claude Code invokes the hook.
const GUARD = fileURLToPath(new URL('./guard.mjs', import.meta.url));
const run = (input) => spawnSync(process.execPath, [GUARD], { input: typeof input === 'string' ? input : JSON.stringify(input), encoding: 'utf8' });

test('cli: deny emits a PreToolUse decision JSON with exit 0', () => {
  const r = run({ hook_event_name: 'PreToolUse', tool_name: 'Bash', tool_input: { command: 'git reset --hard' } });
  assert.equal(r.status, 0);
  const out = JSON.parse(r.stdout).hookSpecificOutput;
  assert.equal(out.hookEventName, 'PreToolUse');
  assert.equal(out.permissionDecision, 'deny');
  assert.match(out.permissionDecisionReason, /^FamilyHub guard: .*git reset --hard/);
});
test('cli: ask decision carries the owner-approval pointer', () => {
  const out = JSON.parse(run({ tool_name: 'Bash', tool_input: { command: 'gh pr merge 3' } }).stdout).hookSpecificOutput;
  assert.equal(out.permissionDecision, 'ask');
  assert.match(out.permissionDecisionReason, /explicit owner approval/);
});
test('cli: allowed command prints nothing and exits 0', () => {
  const r = run({ tool_name: 'Bash', tool_input: { command: 'git status' } });
  assert.equal(r.status, 0);
  assert.equal(r.stdout, '');
});
test('cli: malformed input fails open with exit 1 and a stderr notice', () => {
  const r = run('{not json');
  assert.equal(r.status, 1);
  assert.match(r.stderr, /FamilyHub guard internal error/);
});
