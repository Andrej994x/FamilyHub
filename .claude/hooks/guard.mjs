#!/usr/bin/env node
/*
 * FamilyHub PreToolUse guard for Claude Code (registered in .claude/settings.json).
 *
 * Reads the hook input JSON from stdin and, for a risky Bash/PowerShell command or file edit,
 * prints a PreToolUse decision: "deny" for irreversible operations, "ask" for operations that
 * AGENTS.md reserves for the owner's approval. Anything else gets no output, so the normal
 * permission flow applies. Deterministic checks only; Node built-ins only.
 *
 * This is a guardrail against accidents, not a sandbox: obfuscated or indirect invocations
 * (scripts, aliases, variables, `$(...)` inside quotes) are not detected. See .claude/README.md.
 */

import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const DENY = 'deny';
const ASK = 'ask';
const SEVERITY = { [ASK]: 1, [DENY]: 2 };
const APPROVAL = 'This needs explicit owner approval (AGENTS.md → "Actions that need explicit owner approval").';

const deny = (reason) => ({ action: DENY, reason });
const ask = (reason) => ({ action: ASK, reason: `${reason} ${APPROVAL}` });

/** Keeps the most severe decision; reasons of equally severe decisions are combined. */
function strongest(decisions) {
  let best = null;
  for (const d of decisions) {
    if (!d) continue;
    if (!best || SEVERITY[d.action] > SEVERITY[best.action]) best = { ...d };
    else if (d.action === best.action && !best.reason.includes(d.reason)) best.reason += ` | ${d.reason}`;
  }
  return best;
}

// ---------------------------------------------------------------------------------------------
// Command parsing: split into segments (simple commands) with argv, honouring quotes, so text
// inside commit messages, heredoc bodies and here-strings is data, not commands.
// ---------------------------------------------------------------------------------------------

const HEREDOC = /(?<!<)<<-?\s*(['"]?)([A-Za-z_][A-Za-z0-9_]*)\1/;

/** Removes heredoc bodies; returns the remaining text and the bodies keyed by line index. */
function extractHeredocs(command) {
  const lines = command.split(/\r?\n/);
  const kept = [];
  const bodies = new Map();
  for (let i = 0; i < lines.length; i++) {
    const match = HEREDOC.exec(lines[i]);
    kept.push(lines[i]);
    if (!match) continue;
    const body = [];
    while (i + 1 < lines.length && lines[i + 1].trim() !== match[2]) body.push(lines[++i]);
    i++; // skip the terminator line
    bodies.set(kept.length - 1, body.join('\n'));
  }
  return { text: kept.join('\n'), bodies };
}

/**
 * Tokenizes shell text into segments. `dialect` is "bash" (backslash escapes) or "powershell"
 * (backslash is literal, backtick escapes). Returns [{ argv, redirects, line }].
 */
function tokenize(text, dialect) {
  const segments = [];
  let argv = [];
  let redirects = [];
  let token = '';
  let inToken = false;
  let pendingRedirect = false;
  let line = 0;

  const endToken = () => {
    if (!inToken) return;
    if (pendingRedirect) {
      redirects.push(token);
      pendingRedirect = false;
    } else {
      argv.push(token);
    }
    token = '';
    inToken = false;
  };
  const endSegment = () => {
    endToken();
    if (argv.length || redirects.length) segments.push({ argv, redirects, line });
    argv = [];
    redirects = [];
    pendingRedirect = false;
  };

  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    const next = text[i + 1];

    // PowerShell here-strings @' ... '@ / @" ... "@ are one token.
    if (dialect === 'powershell' && c === '@' && (next === "'" || next === '"') && /\r?\n/.test(text.slice(i + 2, i + 4))) {
      const close = text.indexOf(`\n${next}@`, i + 2);
      const end = close === -1 ? text.length : close;
      token += text.slice(i + 2, end).replace(/^\r?\n/, '');
      inToken = true;
      line += (text.slice(i, end).match(/\n/g) || []).length;
      i = close === -1 ? text.length : close + 2;
      continue;
    }
    if (c === "'") {
      const close = text.indexOf("'", i + 1);
      const end = close === -1 ? text.length : close;
      token += text.slice(i + 1, end);
      inToken = true;
      line += (text.slice(i, end).match(/\n/g) || []).length;
      i = end;
      continue;
    }
    if (c === '"') {
      inToken = true;
      for (i++; i < text.length && text[i] !== '"'; i++) {
        const q = text[i];
        if (q === '\n') line++;
        if (dialect === 'bash' && q === '\\' && '"\\$`'.includes(text[i + 1])) token += text[++i];
        else if (dialect === 'powershell' && q === '`' && i + 1 < text.length) token += text[++i];
        else token += q;
      }
      continue;
    }
    if (dialect === 'bash' && c === '\\') {
      if (next === '\n') { i++; line++; continue; } // line continuation
      if (next !== undefined) { token += next; inToken = true; i++; }
      continue;
    }
    if (dialect === 'powershell' && c === '`') {
      if (next === '\n' || (next === '\r' && text[i + 2] === '\n')) { i += next === '\r' ? 2 : 1; line++; continue; }
      if (next !== undefined) { token += next; inToken = true; i++; }
      continue;
    }
    if (c === '#' && !inToken) {
      while (i + 1 < text.length && text[i + 1] !== '\n') i++;
      continue;
    }
    if (c === '\n') { endSegment(); line++; continue; }
    if (c === ' ' || c === '\t' || c === '\r') { endToken(); continue; }
    if ('&|;(){}'.includes(c)) { endSegment(); if ((c === '&' || c === '|') && next === c) i++; continue; }
    if (c === '>' || c === '<') {
      if (/^\d$/.test(token)) { token = ''; inToken = false; } // 2> / 1>
      endToken();
      if (next === c) i++;
      if (text[i + 1] === '&') { i++; while (/\d/.test(text[i + 1] || '')) i++; continue; } // 2>&1
      pendingRedirect = true;
      continue;
    }
    token += c;
    inToken = true;
  }
  endSegment();
  return segments;
}

const WRAPPERS = new Set(['sudo', 'env', 'timeout', 'time', 'nice', 'nohup', 'stdbuf', 'command', 'builtin', 'exec', 'xargs', 'noglob']);

/** Program name without directory or Windows extension, lower-cased. */
function programName(token) {
  return token.split(/[\\/]/).pop().toLowerCase().replace(/\.(exe|cmd|bat|ps1)$/, '');
}

/** Drops leading VAR=value assignments and wrapper commands (with their options). */
function unwrap(argv) {
  let a = [...argv];
  for (;;) {
    while (a.length && /^[A-Za-z_][A-Za-z0-9_]*=/.test(a[0])) a.shift();
    if (!a.length || !WRAPPERS.has(programName(a[0]))) return a;
    const wrapper = programName(a.shift());
    while (a.length && (a[0].startsWith('-') || /^[A-Za-z_][A-Za-z0-9_]*=/.test(a[0]))) {
      const opt = a.shift();
      if (['-n', '-o', '-e', '-i', '-s', '-k', '-u'].includes(opt) && a.length) a.shift();
    }
    if (wrapper === 'timeout' && a.length && /^\d/.test(a[0])) a.shift();
  }
}

// ---------------------------------------------------------------------------------------------
// Rules per program
// ---------------------------------------------------------------------------------------------

const shortFlags = (args) => args.filter((a) => /^-[A-Za-z]+$/.test(a)).join('');
const has = (args, ...flags) => args.some((a) => flags.includes(a));
const hasPrefix = (args, ...prefixes) => args.some((a) => prefixes.some((p) => a === p || a.startsWith(`${p}=`)));
const PROTECTED_BRANCHES = new Set(['main', 'master']);

function gitRules(argv) {
  // Skip git's global options (-C <dir>, -c <k=v>, --git-dir=..., --no-pager, ...).
  const a = argv.slice(1);
  while (a.length && a[0].startsWith('-')) {
    const opt = a.shift();
    if (['-C', '-c', '--git-dir', '--work-tree', '--namespace', '--config-env'].includes(opt)) a.shift();
  }
  const sub = (a.shift() || '').toLowerCase();
  const args = a;
  const short = shortFlags(args);

  switch (sub) {
    case 'push': {
      const refspecs = args.filter((x) => !x.startsWith('-')).slice(1); // first non-flag is the remote
      if (hasPrefix(args, '--force', '--force-with-lease', '--force-if-includes') || short.includes('f') || refspecs.some((r) => r.startsWith('+')))
        return deny('Force-pushing rewrites remote history. Push normally, or ask the owner to run it.');
      if (has(args, '--mirror', '--prune'))
        return deny('`git push --mirror/--prune` can delete remote branches.');
      if (has(args, '--delete') || short.includes('d') || refspecs.some((r) => r.startsWith(':')))
        return deny('Deleting remote branches or tags is not allowed for agents. Ask the owner.');
      if (refspecs.some((r) => PROTECTED_BRANCHES.has(r.split(':').pop().replace(/^refs\/heads\//, ''))))
        return deny('Pushing directly to main is not allowed; main only changes through a reviewed PR.');
      if (has(args, '--no-verify')) return ask('`git push --no-verify` skips git hooks.');
      if (has(args, '--all')) return ask('`git push --all` pushes every local branch.');
      return null;
    }
    case 'reset':
      return has(args, '--hard') ? deny('`git reset --hard` discards uncommitted work irreversibly. Use `git stash` or `git reset --soft/--mixed`.') : null;
    case 'clean': {
      const dryRun = has(args, '--dry-run') || short.includes('n');
      return !dryRun && (has(args, '--force') || short.includes('f'))
        ? deny('`git clean -f` deletes untracked files irreversibly. Run `git clean -n` to preview and ask the owner.')
        : null;
    }
    case 'checkout':
      return has(args, '--', '.', '-f', '--force', '--ours', '--theirs')
        ? ask('This `git checkout` form overwrites uncommitted changes in the working tree.')
        : null;
    case 'restore': {
      const stagedOnly = has(args, '--staged', '-S') && !has(args, '--worktree', '-W');
      return stagedOnly ? null : ask('`git restore` overwrites uncommitted changes in the working tree.');
    }
    case 'switch':
      return has(args, '--discard-changes', '-f', '--force') ? ask('`git switch --discard-changes/--force` throws away local changes.') : null;
    case 'stash':
      return ['drop', 'clear'].includes((args[0] || '').toLowerCase()) ? ask(`\`git stash ${args[0]}\` permanently deletes stashed work.`) : null;
    case 'branch':
      return has(args, '--delete', '--force') || /[dDf]/.test(short) ? ask('Deleting or force-moving a branch.') : null;
    case 'commit':
      return has(args, '--no-verify', '-n') ? ask('`git commit --no-verify` skips git hooks.') : null;
    case 'add':
      return has(args, '--force', '-f') ? ask('`git add --force` stages ignored files, which may contain secrets or build output.') : null;
    case 'filter-branch':
    case 'filter-repo':
      return deny(`\`git ${sub}\` rewrites repository history.`);
    case 'reflog':
      return ['expire', 'delete'].includes((args[0] || '').toLowerCase()) ? deny('Expiring reflog entries destroys the safety net for recovering lost commits.') : null;
    case 'gc':
      return args.some((x) => /^--prune=(now|all)$/i.test(x)) ? deny('`git gc --prune=now` permanently removes unreachable commits.') : null;
    case 'update-ref':
      return has(args, '-d', '--delete') ? deny('`git update-ref -d` deletes a ref directly.') : null;
    default:
      return null;
  }
}

function ghRules(argv) {
  const [group = '', sub = ''] = argv.slice(1).map((x) => x.toLowerCase());
  const args = argv.slice(3);
  if (group === 'pr' && sub === 'merge') return ask('Merging a pull request.');
  if (group === 'repo' && sub === 'delete') return deny('Deleting a GitHub repository is irreversible.');
  if (group === 'repo' && ['edit', 'rename', 'archive'].includes(sub)) return ask('Changing GitHub repository settings.');
  if (['secret', 'variable'].includes(group) && ['set', 'delete', 'remove'].includes(sub)) return ask(`Changing GitHub ${group}s.`);
  if (group === 'release' && sub === 'delete') return ask('Deleting a GitHub release.');
  if (group === 'auth' && sub === 'token') return ask('`gh auth token` prints a credential.');
  if (group === 'api') {
    const apiArgs = argv.slice(2);
    let method = null;
    apiArgs.forEach((x, i) => {
      if (x === '-X' || x === '--method') method = apiArgs[i + 1];
      else if (/^-X./.test(x)) method = x.slice(2);
      else if (x.startsWith('--method=')) method = x.slice(9);
    });
    const hasFields = apiArgs.some((x) => /^(-f|-F|--field|--raw-field|--input)(=|$)/.test(x) || /^-[fF]./.test(x));
    if (sub === 'graphql') return /\bmutation\b/i.test(apiArgs.join(' ')) ? ask('GitHub GraphQL mutation.') : null;
    const effective = (method || (hasFields ? 'POST' : 'GET')).toUpperCase();
    return ['GET', 'HEAD'].includes(effective) ? null : ask(`GitHub API ${effective} request (changes repository or account state).`);
  }
  return null;
}

/** Normalizes `dotnet ef`, `dotnet tool run dotnet-ef`, `dotnet dotnet-ef` and `dotnet-ef`. */
function efArgs(argv) {
  const p = argv.map((x) => x.toLowerCase());
  if (programName(p[0]) === 'dotnet-ef') return p.slice(1);
  if (p[1] === 'ef' || p[1] === 'dotnet-ef') return p.slice(2);
  if (p[1] === 'tool' && p[2] === 'run' && p[3] === 'dotnet-ef') return p.slice(4);
  return null;
}

function dotnetRules(argv) {
  const ef = efArgs(argv);
  if (ef) {
    // Locate the command noun explicitly so options placed before it (--project X) don't confuse parsing.
    const at = ef.findIndex((x) => x === 'database' || x === 'migrations');
    if (at === -1) return null;
    const [noun, verb, target] = [ef[at], ef[at + 1], ef[at + 2]];
    if (noun === 'database' && verb === 'drop') return deny('`dotnet ef database drop` deletes the database.');
    if (noun === 'database' && verb === 'update') {
      if (target === '0') return deny('`dotnet ef database update 0` reverts every migration and drops all data.');
      return ask('Applying migrations to a database (the local DB is shared dev data; CI validates migrations on a throwaway SQL Server).');
    }
    if (noun === 'migrations' && verb === 'remove') return ask('`dotnet ef migrations remove` deletes migration files and reverts the model snapshot.');
    return null;
  }
  const p = argv.map((x) => x.toLowerCase());
  if (p[1] === 'user-secrets') {
    const verb = p.slice(2).find((x) => !x.startsWith('-'));
    if (verb === 'clear') return deny('`dotnet user-secrets clear` wipes every local secret.');
    if (verb === 'set' || verb === 'remove' || verb === 'init') return ask(`Changing user-secrets (\`${verb}\`). Agents must not modify or rotate secrets.`);
    if (verb === 'list') return ask('`dotnet user-secrets list` prints secret values into the conversation.');
  }
  return null;
}

const SQL_DESTRUCTIVE = /\b(DROP|TRUNCATE|SHUTDOWN)\b/i;
const SQL_WRITE = /\b(DELETE|UPDATE|INSERT|MERGE|ALTER|CREATE|EXEC|EXECUTE|GRANT|REVOKE|DENY|RESTORE|BACKUP|DBCC|sp_\w+)\b/i;

function sqlRules(argv, heredoc) {
  const program = programName(argv[0]);
  const lower = argv.map((x) => x.toLowerCase());
  if (program === 'sqllocaldb') return ['delete', 'd'].includes(lower[1]) ? deny('Deleting a LocalDB instance destroys its databases.') : null;
  if (program === 'bcp') return lower.includes('in') ? ask('`bcp in` bulk-loads data into a table.') : null;

  // sqlcmd / osql / Invoke-Sqlcmd
  const queries = [];
  let inputFile = false;
  argv.forEach((x, i) => {
    const flag = x.toLowerCase();
    if (['-q', '-query'].includes(flag) && argv[i + 1] !== undefined) queries.push(argv[i + 1]);
    if (['-i', '-inputfile'].includes(flag)) inputFile = true;
  });
  if (heredoc) queries.push(heredoc);
  const sql = queries.join('\n');
  if (SQL_DESTRUCTIVE.test(sql)) return deny('Destructive SQL (DROP/TRUNCATE/SHUTDOWN) is not allowed for agents.');
  if (SQL_WRITE.test(sql)) return ask('SQL that modifies data or schema. Database record changes need owner approval.');
  if (inputFile) return ask('Running a SQL script file (its contents are not inspected by the guard).');
  return null;
}

const DELETE_COMMANDS = new Set(['rm', 'rmdir', 'rd', 'del', 'erase', 'remove-item', 'ri']);
const READ_ONLY = new Set(['cat', 'type', 'get-content', 'gc', 'grep', 'rg', 'egrep', 'head', 'tail', 'less', 'more', 'findstr', 'select-string', 'sls', 'wc', 'diff', 'gitleaks', 'git', 'ls', 'dir', 'get-childitem', 'gci', 'test-path', 'stat', 'file']);

/** Checks that apply to every command: deleting .git, secret stores, .gitleaksignore writes. */
function genericRules(argv, redirects, program) {
  const targets = argv.slice(1).map((x) => x.replace(/\\/g, '/'));
  if (DELETE_COMMANDS.has(program) && targets.some((t) => /(^|\/)\.git\/?$/.test(t)))
    return deny('Deleting the .git directory destroys the repository history.');
  if (DELETE_COMMANDS.has(program) && targets.some((t) => /(^|\/)\.claude(\/|$)/.test(t)))
    return ask('Deleting Claude Code project configuration or guard hooks.');

  const all = [...targets, ...redirects.map((x) => x.replace(/\\/g, '/'))];
  if (program !== 'dotnet' && all.some((t) => /(^|\/)(\.)?microsoft\/usersecrets(\/|$)/i.test(t)))
    return ask('Accessing the .NET user-secrets store directly. Use `dotnet user-secrets` (which is itself gated).');
  const touchesIgnore = (t) => /(^|\/)\.gitleaksignore$/.test(t);
  if (redirects.some((r) => touchesIgnore(r.replace(/\\/g, '/'))) || (targets.some(touchesIgnore) && !READ_ONLY.has(program)))
    return ask('Modifying .gitleaksignore can silence secret-scan findings (allowed only for rotated secrets).');
  return null;
}

const SHELLS = { bash: 'bash', sh: 'bash', zsh: 'bash', dash: 'bash', pwsh: 'powershell', powershell: 'powershell', cmd: 'powershell' };

/** Evaluates one shell command string (recursing into `bash -c`, `pwsh -Command`, `cmd /c`, eval). */
export function evaluateCommand(command, dialect = 'bash', depth = 0) {
  if (typeof command !== 'string' || depth > 3) return null;
  const { text, bodies } = extractHeredocs(command);
  const decisions = [];

  for (const segment of tokenize(text, dialect)) {
    const argv = unwrap(segment.argv);
    if (!argv.length) {
      decisions.push(genericRules([''], segment.redirects, ''));
      continue;
    }
    const program = programName(argv[0]);
    decisions.push(genericRules(argv, segment.redirects, program));

    if (SHELLS[program]) {
      const i = argv.findIndex((x, n) => n > 0 && /^(-[a-z]*c|-command|\/c|\/k)$/i.test(x));
      if (i > 0 && argv[i + 1] !== undefined) decisions.push(evaluateCommand(argv.slice(i + 1).join(' '), SHELLS[program], depth + 1));
      if (argv.some((x) => /^-(e|ec|enc|encodedcommand)$/i.test(x))) decisions.push(ask('An encoded PowerShell command cannot be inspected.'));
    } else if (['eval', 'iex', 'invoke-expression'].includes(program)) {
      decisions.push(evaluateCommand(argv.slice(1).join(' '), dialect, depth + 1));
    } else if (program === 'git') {
      decisions.push(gitRules(argv));
    } else if (program === 'gh') {
      decisions.push(ghRules(argv));
    } else if (program === 'dotnet' || program === 'dotnet-ef') {
      decisions.push(dotnetRules(argv));
    } else if (['sqlcmd', 'osql', 'invoke-sqlcmd', 'sqllocaldb', 'bcp'].includes(program)) {
      decisions.push(sqlRules(argv, bodies.get(segment.line)));
    }
  }
  return strongest(decisions);
}

const SECRET_PROPERTY = /"([A-Za-z0-9_]*(?:key|secret|password|pwd|token))"\s*:\s*"([^"]+)"/gi;
const CONNECTION_PASSWORD = /\b(password|pwd)\s*=\s*[^;"\s]+/i;

/** Evaluates Write/Edit/MultiEdit/NotebookEdit calls. */
export function evaluateFileEdit(toolName, toolInput = {}) {
  const path = String(toolInput.file_path || toolInput.notebook_path || '').replace(/\\/g, '/');
  const name = path.split('/').pop().toLowerCase();
  if (/(^|\/)(\.)?microsoft\/usersecrets\//i.test(path))
    return deny('Agents must not modify .NET user-secrets. The owner manages secret values.');
  if (name === '.gitleaksignore')
    return ask('Modifying .gitleaksignore can silence secret-scan findings (allowed only for rotated secrets).');
  if (/^appsettings(\.[\w-]+)?\.json$/.test(name)) {
    const content = [toolInput.content, toolInput.new_string, ...(toolInput.edits || []).map((e) => e.new_string)]
      .filter((x) => typeof x === 'string')
      .join('\n');
    const found = [...content.matchAll(SECRET_PROPERTY)].map((m) => m[1]);
    if (found.length || CONNECTION_PASSWORD.test(content))
      return deny(
        `Secret-looking value in ${name} (${found.join(', ') || 'connection-string password'}). ` +
          'Commit only empty placeholders; real values go in user-secrets or environment variables (AGENTS.md → Config & secrets).',
      );
  }
  return null;
}

/** Entry point used by the hook and the tests. Returns null or { action, reason }. */
export function evaluate(input) {
  const tool = input?.tool_name;
  const toolInput = input?.tool_input || {};
  if (tool === 'Bash') return evaluateCommand(toolInput.command, 'bash');
  if (tool === 'PowerShell') return evaluateCommand(toolInput.command, 'powershell');
  if (['Write', 'Edit', 'MultiEdit', 'NotebookEdit'].includes(tool)) return evaluateFileEdit(tool, toolInput);
  return null;
}

function main() {
  try {
    const decision = evaluate(JSON.parse(readFileSync(0, 'utf8') || '{}'));
    if (decision) {
      process.stdout.write(JSON.stringify({
        hookSpecificOutput: {
          hookEventName: 'PreToolUse',
          permissionDecision: decision.action,
          permissionDecisionReason: `FamilyHub guard: ${decision.reason}`,
        },
      }));
    }
    process.exit(0);
  } catch (error) {
    // Non-blocking failure (exit 1): Claude Code shows a hook-error notice and the permission
    // deny rules in .claude/settings.json remain the backstop. See .claude/README.md.
    process.stderr.write(`FamilyHub guard internal error: ${error?.message || error}\n`);
    process.exit(1);
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) main();
