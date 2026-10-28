/*
 * Tests for agent-policy.mjs (restricted subagents). Run from the repository root:
 *   node --test .claude/hooks/agent-policy.test.mjs
 * Pure input → decision checks plus CLI contract tests. Path tests use a disposable temp
 * repository with junctions/symlinks; nothing under test is executed against the real repo.
 */

import { test, after } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { agentPolicy } from './agent-policy.mjs';
import { evaluate } from './guard.mjs';

const action = (decision) => decision?.action ?? 'none';

// A disposable repository layout with links that try to escape the allowed folders.
const ROOT = mkdtempSync(join(tmpdir(), 'familyhub-policy-'));
const OUTSIDE = mkdtempSync(join(tmpdir(), 'familyhub-outside-'));
for (const dir of [
  'backend/FamilyHub.Api/Services', 'backend/FamilyHub.Api.UnitTests/Services',
  'backend/FamilyHub.Api.IntegrationTests/Infrastructure', 'backend/FamilyHub.Api.IntegrationTests/Safety',
  'frontend/familyhub-web/src/test', 'frontend/familyhub-web/src/pages', '.claude/hooks',
]) mkdirSync(join(ROOT, dir), { recursive: true });
writeFileSync(join(ROOT, 'backend/FamilyHub.Api/Program.cs'), '// production');
let linksCreated = true;
try {
  symlinkSync(join(ROOT, 'backend/FamilyHub.Api'), join(ROOT, 'backend/FamilyHub.Api.UnitTests/escape'), 'junction');
  symlinkSync(OUTSIDE, join(ROOT, 'backend/FamilyHub.Api.UnitTests/outside'), 'junction');
} catch {
  linksCreated = false; // platform without junction/symlink support: those cases are skipped
}
after(() => {
  rmSync(ROOT, { recursive: true, force: true });
  rmSync(OUTSIDE, { recursive: true, force: true });
});

const bash = (agent, command) => agentPolicy({ tool_name: 'Bash', tool_input: { command } }, agent, ROOT);
const ps = (agent, command) => agentPolicy({ tool_name: 'PowerShell', tool_input: { command } }, agent, ROOT);
const write = (agent, file_path, tool_name = 'Write') => agentPolicy({ tool_name, tool_input: { file_path, content: 'x' } }, agent, ROOT);

// ---------------------------------------------------------------------------------------------
// Commands both restricted agents may run (read-only inspection)
// ---------------------------------------------------------------------------------------------
const READ_ONLY_BASH = [
  'git status', 'git diff origin/main...HEAD --stat', 'git diff --name-only origin/main...HEAD', 'git log --oneline -10',
  'git show HEAD:AGENTS.md', 'git -C backend log -1', 'git --no-pager diff', 'git blame -L 10,20 backend/FamilyHub.Api/Program.cs',
  'git merge-base origin/main HEAD', 'git branch --show-current', 'git branch -a', 'git remote -v', 'git grep -n Authorize -- backend',
  'git diff origin/main...HEAD | head -50', 'git status > /dev/null', 'git diff 2>&1 | tail -5',
  'rg -n AuthorizeAsync backend/FamilyHub.Api/Services', 'grep -rn Jwt backend/FamilyHub.Api/Program.cs', 'cat docs/ARCHITECTURE.md',
  'ls -la backend', 'find backend -name "*.cs" -newer AGENTS.md', 'wc -l backend/FamilyHub.Api/Program.cs',
  'gitleaks git --redact .', 'gitleaks dir --redact backend', 'cd frontend/familyhub-web && npm audit', 'npm audit --omit=dev',
  'cd backend/FamilyHub.Api && dotnet list package --vulnerable', 'dotnet list FamilyHub.Api package --vulnerable --include-transitive',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations list --no-build',
  'gh pr view 5 --json files', 'gh pr diff 5', 'gh pr checks 5', 'gh run list --limit 3', 'gh api repos/o/r/pulls/5/files',
  'node --test .claude/hooks/guard.test.mjs', 'node --version',
];
const READ_ONLY_PS = [
  'Get-Content docs\\TESTING.md', 'Select-String -Path backend\\FamilyHub.Api\\Program.cs -Pattern Authorize',
  'Get-ChildItem backend -Recurse -Filter *.cs', 'git diff --stat', 'git status > $null', 'Set-Location backend; git log -1',
];

// ---------------------------------------------------------------------------------------------
// Denied for both restricted agents: writes, mutations, chaining, wrappers, substitution, paths
// ---------------------------------------------------------------------------------------------
const DENIED_BOTH_BASH = [
  // writes and redirection
  'echo x > f.txt', 'git diff > out.patch', 'git diff >> out.patch', 'git log | tee log.txt', 'touch x', 'rm x', 'mkdir x',
  'cp a b', 'mv a b', 'sed -i s/a/b/ f', 'awk "{print}" f',
  // git mutations and unsafe options
  'git commit -m x', 'git add .', 'git push', 'git checkout main', 'git stash', 'git fetch origin', 'git branch new-branch',
  'git remote add x y', 'git config user.name x', 'git -c core.pager=sh log', 'git diff --output=patch.diff',
  'git diff --ext-diff', 'git grep -O vim x', 'git --git-dir=x log', 'git reset --hard',
  // chaining: every segment must be allowed
  'git diff; rm -rf x', 'git diff && echo x > f', 'git log | sh', 'git status\nrm -rf x', 'git diff || curl http://x', 'git diff & rm x',
  // wrappers and indirection
  'env git commit -m x', 'timeout 5 git push', 'bash -c "git diff"', 'sh -c "git status"', 'xargs rm', 'nohup git diff',
  'sudo git status', 'eval "git diff"', 'command git diff',
  // substitution, variables, heredocs, env prefixes, programs by path
  'git diff $(rm x)', 'git diff `rm x`', 'cat $HOME/.ssh/id_rsa', 'git log ${X}', 'diff <(git show a) <(git show b)',
  "cat <<'EOF'\nx\nEOF", 'GIT_PAGER=sh git log', 'GIT_EXTERNAL_DIFF=x git diff', '/usr/bin/git status', './scripts/x.sh',
  // secrets
  'cat ~/AppData/Roaming/Microsoft/UserSecrets/x/secrets.json', 'cat frontend/familyhub-web/.env', 'dotnet user-secrets list',
  // other programs and risky options
  'curl https://example.com', 'wget x', 'npm install', 'npm audit fix', 'dotnet run', 'gh pr merge 5', 'gh pr comment 5 -b x',
  'gh api -X POST repos/o/r/issues', 'gh api repos/o/r/issues -f title=x', 'rg --pre cat x', 'find . -delete',
  'find . -exec rm {} ;', 'gitleaks git .', 'gitleaks git --redact --report-path r.json .', 'node x.js',
  'node --test other/file.test.mjs', 'python -c "print(1)"', 'dotnet tool run dotnet-ef database update',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations script -o out.sql',
  // malformed or empty
  'git diff "unterminated', '', '   ',
];
const DENIED_BOTH_PS = [
  'Set-Content f x', 'Out-File x', 'git diff | Out-File d.txt', 'Remove-Item x', 'Invoke-Expression "git diff"', 'iex x',
  'Start-Process git', 'pwsh -Command "git diff"', 'cmd /c "git diff"', 'powershell -EncodedCommand ZQBjAGgAbwA=',
  'Get-Content $env:APPDATA\\Microsoft\\UserSecrets\\x\\secrets.json', 'git diff > d.txt', 'New-Item x',
  'Get-ChildItem | ForEach-Object { Remove-Item $_ }', "git commit -m @'\nmsg\n'@",
];

// Only security-reviewer is denied these (test-writer may build and run the suites).
const TEST_COMMANDS_BASH = [
  'cd backend && dotnet build FamilyHub.sln -c Release', 'cd backend && dotnet test FamilyHub.Api.UnitTests',
  'dotnet test FamilyHub.Api.IntegrationTests --filter "FullyQualifiedName~TestDatabaseGuardTests"', 'dotnet restore',
  'cd frontend/familyhub-web && npm test', 'npm run lint', 'npm run build', 'npm run i18n:check',
  'npx vitest run src/api/client.test.ts',
];
// Denied for test-writer too (dependency changes, MSBuild injection, app start, git writes).
const TEST_WRITER_DENIED_BASH = [
  'dotnet build -p:PostBuildEvent=calc', 'dotnet build /p:X=1', 'dotnet test -t:Exec', 'dotnet test --property:X=1',
  'dotnet run --project FamilyHub.Api', 'npm install', 'npm ci', 'npm install -D vitest', 'npm run dev', 'npm test -- --watch',
  'npm run test:watch', 'npx some-package', 'git add .', 'git commit -m x', 'dotnet user-secrets set a b',
  'echo x > backend/FamilyHub.Api/Program.cs', 'dotnet ef database update',
];

for (const agent of ['security-reviewer', 'test-writer']) {
  for (const c of READ_ONLY_BASH) test(`${agent} allows (Bash): ${c}`, () => assert.equal(action(bash(agent, c)), 'none', bash(agent, c)?.reason));
  for (const c of READ_ONLY_PS) test(`${agent} allows (PowerShell): ${c}`, () => assert.equal(action(ps(agent, c)), 'none', ps(agent, c)?.reason));
  for (const c of DENIED_BOTH_BASH) test(`${agent} denies (Bash): ${JSON.stringify(c).slice(0, 80)}`, () => assert.equal(action(bash(agent, c)), 'deny'));
  for (const c of DENIED_BOTH_PS) test(`${agent} denies (PowerShell): ${JSON.stringify(c).slice(0, 80)}`, () => assert.equal(action(ps(agent, c)), 'deny'));
}
for (const c of TEST_COMMANDS_BASH) {
  test(`test-writer allows test command: ${c}`, () => assert.equal(action(bash('test-writer', c)), 'none', bash('test-writer', c)?.reason));
  test(`security-reviewer denies test command: ${c}`, () => assert.equal(action(bash('security-reviewer', c)), 'deny'));
}
for (const c of TEST_WRITER_DENIED_BASH) test(`test-writer denies: ${c}`, () => assert.equal(action(bash('test-writer', c)), 'deny'));

// ---------------------------------------------------------------------------------------------
// Regression (Codex review, PR #6, P1): "read-only" programs with write-capable or exec options.
// Options are now allowlisted per command; unknown options are denied.
// ---------------------------------------------------------------------------------------------
const WRITE_OPTION_BYPASSES = [
  // sort: -o/--output and -T write files, --compress-program runs a program
  'sort -o backend/FamilyHub.Api/Program.cs input.txt', 'sort --output=backend/FamilyHub.Api/Program.cs input.txt',
  'sort --output backend/FamilyHub.Api/Program.cs input.txt', 'sort -uo backend/FamilyHub.Api/Program.cs input.txt',
  'sort -rko backend/FamilyHub.Api/Program.cs input.txt', 'sort -obackend/FamilyHub.Api/Program.cs input.txt',
  'cd backend/FamilyHub.Api && sort -o Program.cs Program.cs', 'git log --oneline | sort -o out.txt',
  'sort -T /tmp input.txt', 'sort -T/tmp input.txt', 'sort --temporary-directory=/tmp input.txt', 'sort --compress-program=calc input.txt',
  // uniq: a second file argument is an output file
  'uniq input.txt backend/FamilyHub.Api/Program.cs', 'uniq -c input.txt backend/FamilyHub.Api/Program.cs', 'uniq input.txt -',
  // rg options that run programs
  'rg --pre=sh pattern', 'rg --pre sh pattern', 'rg --hostname-bin=calc pattern',
  // gitleaks: reports, diagnostics, --log-opts (passes --output to git), configs and baselines
  'gitleaks git --redact --log-opts=--output=pwn.txt .', 'gitleaks git --redact --log-opts "--output=pwn.txt" .',
  'gitleaks git --redact --diagnostics=cpu --diagnostics-dir=. .', 'gitleaks dir --redact --report-path=r.json .',
  'gitleaks git --redact -r r.json .', 'gitleaks git --redact --config evil.toml .', 'gitleaks git --redact --baseline-path b.json .',
  // npm read commands with path or config options
  'npm audit --logs-dir=backend/FamilyHub.Api', 'npm ls --prefix=backend/FamilyHub.Api', 'npm audit --cache=backend/x',
  'npm view vitest --userconfig=x.npmrc', 'npm ls --globalconfig=x', 'npm outdated --prefix x', 'npm audit signatures',
  // dotnet read commands with write-capable options
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations list --msbuildprojectextensionspath obj2',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations script --output=out.sql',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations list --connection "Server=x"',
  'dotnet list package --vulnerable --source x', 'dotnet list --interactive FamilyHub.Api package --vulnerable',
  'dotnet --version --diagnostics',
];
const READ_OPTIONS_STILL_ALLOWED = [
  'sort -rn input.txt', 'sort -k2 -t, input.txt', 'sort -u --key=2 input.txt', 'sort -k 2 -t , input.txt',
  'git log --oneline | sort | uniq -c', 'uniq -c input.txt', 'uniq -d -f 1 input.txt', 'rg -n --hidden AuthorizeAsync backend',
  'gitleaks git --redact --no-banner -v .', 'gitleaks dir --redact --exit-code 0 backend', 'gitleaks version',
  'npm audit --json --omit=dev', 'npm audit --audit-level=high', 'npm ls --depth=0', 'npm view vitest version', 'npm --version',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations list --no-build --project FamilyHub.Api',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations script --idempotent',
  'cd backend/FamilyHub.Api && dotnet tool run dotnet-ef migrations script InitialIdentity AddFamilyDomain',
  'dotnet list FamilyHub.Api package --vulnerable --include-transitive', 'dotnet --info',
];
// test-writer build/test options that write outside the test folders or run arbitrary programs.
const TEST_WRITER_OPTION_BYPASSES = [
  'dotnet build backend/FamilyHub.Api.UnitTests -o backend/FamilyHub.Api', 'dotnet build FamilyHub.sln --output=backend/FamilyHub.Api',
  'dotnet test FamilyHub.Api.UnitTests --results-directory backend/FamilyHub.Api',
  'dotnet test FamilyHub.Api.UnitTests --diag backend/FamilyHub.Api/diag.txt',
  'dotnet test FamilyHub.Api.UnitTests --logger "trx;LogFileName=../FamilyHub.Api/x.trx"',
  'dotnet test FamilyHub.Api.UnitTests -l "html;LogFileName=x.html"', 'dotnet build -bl:backend/FamilyHub.Api/x.binlog',
  'dotnet build /bl', 'dotnet restore --use-lock-file', 'dotnet build -p:PostBuildEvent=calc', 'dotnet build /p:X=1',
  'dotnet test -t:Exec', 'dotnet test --property:X=1', 'dotnet test --blame', 'dotnet build FamilyHub.sln FamilyHub.Api',
  'npx vitest run -u', 'npx vitest run --update', 'npx vitest run --coverage', 'npx vitest run --outputFile=src/App.tsx --reporter=json',
  'npx vitest run --config evil.config.ts', 'npx vitest run --root ..', 'npx vitest run --reporter=json', 'npx vitest run --watch',
];
const TEST_WRITER_OPTIONS_STILL_ALLOWED = [
  'cd backend && dotnet test FamilyHub.Api.UnitTests --configuration Release --no-build --logger "console;verbosity=normal"',
  'cd backend && dotnet build FamilyHub.sln -c Release -warnaserror', 'dotnet test FamilyHub.Api.IntegrationTests --filter "FullyQualifiedName~TestDatabaseGuardTests"',
  'dotnet test --no-restore -v minimal', 'dotnet restore', 'npx vitest run src/api/client.test.ts -t "attaches the bearer token"',
  'npx vitest run --reporter=verbose', 'npx vitest run src/pages --silent',
];

for (const agent of ['security-reviewer', 'test-writer']) {
  for (const c of WRITE_OPTION_BYPASSES) test(`${agent} denies write-capable option: ${c}`, () => assert.equal(action(bash(agent, c)), 'deny'));
  for (const c of READ_OPTIONS_STILL_ALLOWED) test(`${agent} still allows: ${c}`, () => assert.equal(action(bash(agent, c)), 'none', bash(agent, c)?.reason));
  test(`${agent}: the reported sort -o bypass is denied in PowerShell too`, () => {
    assert.equal(action(ps(agent, 'sort -o backend\\FamilyHub.Api\\Program.cs input.txt')), 'deny');
  });
}
for (const c of TEST_WRITER_OPTION_BYPASSES) {
  test(`test-writer denies write-capable build/test option: ${c}`, () => assert.equal(action(bash('test-writer', c)), 'deny'));
  test(`security-reviewer denies (not a read-only command): ${c}`, () => assert.equal(action(bash('security-reviewer', c)), 'deny'));
}
for (const c of TEST_WRITER_OPTIONS_STILL_ALLOWED) {
  test(`test-writer still allows: ${c}`, () => assert.equal(action(bash('test-writer', c)), 'none', bash('test-writer', c)?.reason));
}
test('settings guard path: the reported bypass is denied through agent_type for both agents', () => {
  for (const agent of ['security-reviewer', 'test-writer']) {
    const decision = evaluate({ tool_name: 'Bash', agent_type: agent, tool_input: { command: 'sort -o backend/FamilyHub.Api/Program.cs input.txt' } });
    assert.equal(action(decision), 'deny');
    assert.match(decision.reason, /option `-o` is not on the allowlist/);
  }
});

// ---------------------------------------------------------------------------------------------
// File edits
// ---------------------------------------------------------------------------------------------
test('security-reviewer can never edit, even a test file', () => {
  for (const tool of ['Write', 'Edit', 'MultiEdit', 'NotebookEdit']) {
    assert.equal(action(write('security-reviewer', 'backend/FamilyHub.Api.UnitTests/Services/XTests.cs', tool)), 'deny');
  }
});

const TEST_WRITER_PATHS = [
  ['allow', 'backend/FamilyHub.Api.UnitTests/Services/NewTests.cs'],
  ['allow', join(ROOT, 'backend\\FamilyHub.Api.UnitTests\\Services\\Abs.cs')],
  ['allow', 'BACKEND\\familyhub.api.unittests\\MixedCase.cs'],
  ['allow', 'backend/FamilyHub.Api.IntegrationTests/Authorization/NewTests.cs'],
  ['allow', 'backend/FamilyHub.Api.IntegrationTests/Infrastructure/ApiDriver.cs'],
  ['allow', 'frontend/familyhub-web/src/pages/Tasks.test.tsx'],
  ['allow', 'frontend/familyhub-web/src/utils/date.test.ts'],
  ['allow', 'frontend/familyhub-web/src/test/renderWithProviders.tsx'],
  ['ask', 'backend/FamilyHub.Api.UnitTests/FamilyHub.Api.UnitTests.csproj'],
  ['ask', 'backend/FamilyHub.Api.IntegrationTests/FamilyHub.Api.IntegrationTests.csproj'],
  ['ask', 'frontend/familyhub-web/package.json'],
  ['ask', 'frontend/familyhub-web/vitest.config.ts'],
  ['ask', 'frontend/familyhub-web/src/test/setup.ts'],
  ['deny', 'backend/FamilyHub.Api/Services/FamilyService.cs'],
  ['deny', 'backend/FamilyHub.Api/Program.cs'],
  ['deny', 'backend/FamilyHub.Api/appsettings.json'],
  ['deny', 'backend/FamilyHub.Api/Data/Migrations/20990101000000_X.cs'],
  ['deny', 'backend/FamilyHub.Api.IntegrationTests/Infrastructure/TestDatabaseGuard.cs'],
  ['deny', 'backend/FamilyHub.Api.IntegrationTests/Infrastructure/DockerEndpointGuard.cs'],
  ['deny', 'backend/FamilyHub.Api.IntegrationTests/Infrastructure/FamilyHubApiFixture.cs'],
  ['deny', 'backend\\FamilyHub.Api.IntegrationTests\\Infrastructure\\familyhubapifactory.cs'],
  ['deny', 'backend/FamilyHub.Api.IntegrationTests/Safety/DatabaseIsolationTests.cs'],
  ['deny', 'backend/FamilyHub.Api.UnitTests/../FamilyHub.Api/Program.cs'],
  ['deny', 'backend/FamilyHub.Api.UnitTests/bin/Debug/x.cs'],
  ['deny', 'frontend/familyhub-web/src/App.tsx'],
  ['deny', 'frontend/familyhub-web/src/pages/Tasks.tsx'],
  ['deny', '.claude/hooks/guard.mjs'],
  ['deny', '.claude/settings.json'],
  ['deny', '.github/workflows/ci.yml'],
  ['deny', 'AGENTS.md'],
  ['deny', 'backend/FamilyHub.Api.UnitTests/X.cs::$DATA'],
  ['deny', 'backend/FamilyHub.Api.UnitTests/X.cs.'],
  ['deny', 'backend/FamilyHub.Api.UnitTests/X.cs '],
  ['deny', 'backend/FAMILY~1/X.cs'],
  ['deny', '\\\\server\\share\\x.cs'],
  ['deny', '//server/share/x.cs'],
  ['deny', '\\\\?\\C:\\x.cs'],
  ['deny', '../outside.cs'],
  ['deny', join(OUTSIDE, 'x.cs')],
  ['deny', 'backend/FamilyHub.Api.UnitTests/X\0.cs'],
  ['deny', ''],
];
for (const [expected, path] of TEST_WRITER_PATHS) {
  test(`test-writer edit ${expected}: ${JSON.stringify(path).slice(0, 90)}`, () => {
    const decision = write('test-writer', path);
    assert.equal(action(decision) === 'none' ? 'allow' : action(decision), expected, decision?.reason);
  });
}
test('test-writer: Edit, MultiEdit and NotebookEdit use the same path policy', () => {
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api/Program.cs', 'Edit')), 'deny');
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api/Program.cs', 'MultiEdit')), 'deny');
  assert.equal(action(agentPolicy({ tool_name: 'NotebookEdit', tool_input: { notebook_path: 'x.ipynb' } }, 'test-writer', ROOT)), 'deny');
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api.UnitTests/A.cs', 'Edit')), 'none');
});
test('test-writer: links that escape the test folders are denied', { skip: !linksCreated && 'links unsupported' }, () => {
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api.UnitTests/escape/Program.cs')), 'deny');
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api.UnitTests/escape/Services/New.cs')), 'deny');
  assert.equal(action(write('test-writer', 'backend/FamilyHub.Api.UnitTests/outside/x.cs')), 'deny');
});
test('test-writer: unknown repository root fails closed', () => {
  const decision = agentPolicy({ tool_name: 'Write', tool_input: { file_path: 'backend/FamilyHub.Api.UnitTests/A.cs' } }, 'test-writer', undefined);
  assert.equal(action(decision), 'deny');
});

// ---------------------------------------------------------------------------------------------
// Scope, precedence and fail-closed behaviour
// ---------------------------------------------------------------------------------------------
test('other agents and the main session are not restricted by the agent policy', () => {
  assert.equal(agentPolicy({ tool_name: 'Bash', tool_input: { command: 'touch x' } }), null);
  assert.equal(agentPolicy({ tool_name: 'Bash', agent_type: 'general-purpose', tool_input: { command: 'touch x' } }), null);
  assert.equal(action(evaluate({ tool_name: 'Bash', tool_input: { command: 'npm install' } })), 'none');
  assert.equal(action(evaluate({ tool_name: 'Bash', agent_type: 'Explore', tool_input: { command: 'npm install' } })), 'none');
});
test('guard.evaluate applies the agent policy for restricted agent_type values', () => {
  const previous = process.env.CLAUDE_PROJECT_DIR;
  process.env.CLAUDE_PROJECT_DIR = ROOT;
  try {
    assert.equal(action(evaluate({ tool_name: 'Bash', agent_type: 'security-reviewer', tool_input: { command: 'npm install' } })), 'deny');
    assert.equal(action(evaluate({ tool_name: 'Bash', agent_type: 'security-reviewer', tool_input: { command: 'git diff' } })), 'none');
    assert.equal(action(evaluate({ tool_name: 'Write', agent_type: 'test-writer', tool_input: { file_path: 'backend/FamilyHub.Api/Program.cs', content: 'x' } })), 'deny');
    assert.equal(action(evaluate({ tool_name: 'Write', agent_type: 'test-writer', tool_input: { file_path: 'backend/FamilyHub.Api.UnitTests/A.cs', content: 'x' } })), 'none');
  } finally {
    if (previous === undefined) delete process.env.CLAUDE_PROJECT_DIR;
    else process.env.CLAUDE_PROJECT_DIR = previous;
  }
});
test('deny wins: the agent policy turns guard "ask" into deny, and guard denies still apply', () => {
  assert.equal(action(evaluate({ tool_name: 'Bash', agent_type: 'security-reviewer', tool_input: { command: 'gh pr merge 5' } })), 'deny');
  assert.equal(action(evaluate({ tool_name: 'Bash', agent_type: 'test-writer', tool_input: { command: 'git push --force' } })), 'deny');
});
test('Read/Grep/Glob are not restricted by the policy (permission deny rules still protect secrets)', () => {
  assert.equal(agentPolicy({ tool_name: 'Read', tool_input: { file_path: 'AGENTS.md' } }, 'security-reviewer', ROOT), null);
  assert.equal(agentPolicy({ tool_name: 'Grep', tool_input: { pattern: 'x' } }, 'test-writer', ROOT), null);
});
test('an exception inside the policy fails closed', () => {
  const input = { tool_name: 'Bash', tool_input: { get command() { throw new Error('boom'); } } };
  const decision = agentPolicy(input, 'security-reviewer', ROOT);
  assert.equal(action(decision), 'deny');
  assert.match(decision.reason, /failing closed/);
});

// CLI contracts: frontmatter-hook mode (agent-policy.mjs <agent>) and the settings guard.
const POLICY = fileURLToPath(new URL('./agent-policy.mjs', import.meta.url));
const GUARD = fileURLToPath(new URL('./guard.mjs', import.meta.url));
const run = (script, args, stdin) => spawnSync(process.execPath, [script, ...args], { input: stdin, encoding: 'utf8', env: { ...process.env, CLAUDE_PROJECT_DIR: ROOT } });
const decisionOf = (r) => JSON.parse(r.stdout).hookSpecificOutput;

test('cli (frontmatter mode): deny JSON for a disallowed command', () => {
  const r = run(POLICY, ['security-reviewer'], JSON.stringify({ tool_name: 'Bash', tool_input: { command: 'git commit -m x' } }));
  assert.equal(r.status, 0);
  assert.equal(decisionOf(r).permissionDecision, 'deny');
  assert.match(decisionOf(r).permissionDecisionReason, /^FamilyHub agent policy: security-reviewer:/);
});
test('cli (frontmatter mode): allowed command prints nothing', () => {
  const r = run(POLICY, ['test-writer'], JSON.stringify({ tool_name: 'Bash', tool_input: { command: 'dotnet test FamilyHub.Api.UnitTests' } }));
  assert.equal(r.status, 0);
  assert.equal(r.stdout, '');
});
test('cli (frontmatter mode): malformed input fails closed with deny', () => {
  const r = run(POLICY, ['test-writer'], '{not json');
  assert.equal(r.status, 0);
  assert.equal(decisionOf(r).permissionDecision, 'deny');
});
test('cli (frontmatter mode): unknown agent name blocks (exit 2)', () => {
  const r = run(POLICY, ['someone-else'], JSON.stringify({ tool_name: 'Bash', tool_input: { command: 'git status' } }));
  assert.equal(r.status, 2);
});
test('cli (settings guard): malformed input naming a restricted agent blocks (exit 2)', () => {
  const r = run(GUARD, [], '{"agent_type":"security-reviewer","tool_name":"Bash", BROKEN');
  assert.equal(r.status, 2);
  assert.match(r.stderr, /restricted agent/);
});
test('cli (settings guard): malformed main-session input still fails open (exit 1, Phase 3 behaviour)', () => {
  const r = run(GUARD, [], '{not json');
  assert.equal(r.status, 1);
});
test('cli (settings guard): restricted agent deny through agent_type', () => {
  const r = run(GUARD, [], JSON.stringify({ tool_name: 'Bash', agent_type: 'security-reviewer', tool_input: { command: 'echo x > f' } }));
  assert.equal(r.status, 0);
  assert.equal(decisionOf(r).permissionDecision, 'deny');
});
