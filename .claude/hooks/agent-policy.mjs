#!/usr/bin/env node
/*
 * Fail-closed tool policy for FamilyHub's restricted Claude Code subagents.
 *
 * Used in two ways (same code):
 *   1. From guard.mjs (settings PreToolUse hook), keyed on the hook input's `agent_type`.
 *   2. As each agent's own frontmatter hook: `node agent-policy.mjs <agent-name>` with
 *      onFailure: block, so a policy that cannot run blocks the agent's tool call.
 *
 * Deny by default: a restricted agent's shell command runs only if every segment is on the
 * agent's allowlist; anything the parser cannot vouch for (wrappers, substitution, variables,
 * file redirects, heredocs, paths that escape the test folders) is denied. This is
 * defense-in-depth, not a sandbox; see .claude/README.md. Node built-ins only.
 */

import { readFileSync, realpathSync, existsSync } from 'node:fs';
import { resolve, relative, dirname, basename, isAbsolute } from 'node:path';
import { pathToFileURL } from 'node:url';
import { extractHeredocs, tokenize, programName, ghRules } from './guard.mjs';

export const RESTRICTED_AGENTS = new Set(['security-reviewer', 'test-writer']);
const EDIT_TOOLS = new Set(['Edit', 'Write', 'MultiEdit', 'NotebookEdit']);

const deny = (agent, reason) => ({ action: 'deny', reason: `${agent}: ${reason}` });
const ask = (agent, reason) => ({
  action: 'ask',
  reason: `${agent}: ${reason} This is outside the agent's default scope and needs owner approval.`,
});

// ---------------------------------------------------------------------------------------------
// Shell commands
// ---------------------------------------------------------------------------------------------

/** True when every quote is closed (bash: \ escapes; PowerShell: ` escapes). */
function quotesBalanced(text, dialect) {
  let quote = null;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (quote === "'") { if (c === "'") quote = null; continue; }
    if ((dialect === 'bash' && c === '\\') || (dialect === 'powershell' && c === '`')) { i++; continue; }
    if (quote === '"') { if (c === '"') quote = null; continue; }
    if (c === "'" || c === '"') quote = c;
  }
  return quote === null;
}

const NULL_SINKS = new Set(['/dev/null', 'nul', '$null']);
// Programs with NO option that writes files or runs other programs (audited: GNU coreutils
// cat/head/tail/wc/ls/cut/basename/dirname/pwd/true, diffutils diff, grep, bash echo/printf, and
// read-only PowerShell cmdlets). Any options are accepted for these. Every other program below
// validates its options against an allowlist, so an unknown option is denied (fail closed).
const NO_WRITE_OPTIONS = new Set(['cat', 'head', 'tail', 'wc', 'ls', 'cut', 'diff', 'grep', 'egrep', 'pwd', 'echo', 'printf', 'basename', 'dirname', 'true']);
const CHANGE_DIRECTORY = new Set(['cd', 'pushd', 'popd', 'set-location', 'sl', 'push-location', 'pop-location']);
const PS_READ = new Set(['get-content', 'gc', 'type', 'get-childitem', 'gci', 'dir', 'select-string', 'sls', 'test-path', 'get-item', 'gi', 'measure-object', 'measure', 'select-object', 'select', 'sort-object', 'get-location', 'gl', 'resolve-path', 'write-output', 'write']);
const GIT_READ = new Set(['status', 'diff', 'log', 'show', 'blame', 'ls-files', 'merge-base', 'rev-parse', 'grep', 'shortlog', 'describe']);
// For these git subcommands the complete set of write/exec options is: --output (diff/log/show),
// -O/--open-files-in-pager (grep), --ext-diff and --textconv (run configured programs).
const GIT_UNSAFE_OPTION = /^(--output|--ext-diff|--textconv|-O|--open-files-in-pager|--exec)/;
const BRANCH_FLAGS = new Set(['-a', '-r', '-v', '-vv', '--list', '--show-current', '--all', '--remotes', '--verbose']);
const SECRET_REFERENCE = /usersecrets|(^|[\\/])\.env(\.|$)/i;

/**
 * Validates options against an allowlist. spec: switches (RegExp of value-less options),
 * values (Set of options taking a value, as `--opt v` or `--opt=v`), attached (RegExp of
 * options with an attached value such as `-k2`), valueCheck(name, value), maxPositional,
 * positional (RegExp each positional argument must match).
 */
function optionsProblem(command, args, spec) {
  let positional = 0;
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (!arg.startsWith('-') || arg === '-') {
      positional++;
      if (spec.positional && !spec.positional.test(arg)) return `\`${command}\` argument \`${arg}\` is not allowed.`;
      continue;
    }
    if (spec.switches?.test(arg)) continue;
    const eq = arg.indexOf('=');
    const name = eq > 0 ? arg.slice(0, eq) : arg;
    if (spec.values?.has(name)) {
      const value = eq > 0 ? arg.slice(eq + 1) : args[++i];
      if (value === undefined) return `\`${command} ${name}\` needs a value.`;
      if (spec.valueCheck && !spec.valueCheck(name, value)) return `\`${command} ${name} ${value}\` is not allowed.`;
      continue;
    }
    if (spec.attached?.test(arg)) continue;
    return `\`${command}\` option \`${arg}\` is not on the allowlist (it may write files or run programs).`;
  }
  if (spec.maxPositional !== undefined && positional > spec.maxPositional) {
    return `\`${command}\` accepts at most ${spec.maxPositional} file argument(s) here (an extra one would be an output file).`;
  }
  return null;
}

// sort: -o/--output and -T write files, --compress-program runs a program; only these are allowed.
const SORT_OPTIONS = {
  switches: /^(-[rnufbdghiMRVcCmsz]+|--(reverse|numeric-sort|unique|ignore-case|ignore-leading-blanks|dictionary-order|general-numeric-sort|human-numeric-sort|month-sort|random-sort|version-sort|check|merge|stable|zero-terminated))$/,
  values: new Set(['-k', '--key', '-t', '--field-separator']),
  attached: /^-[kt].+$/,
};
// uniq: a second file argument is an OUTPUT file, so at most one input is accepted.
const UNIQ_OPTIONS = {
  switches: /^(-[cduiDz]+|--(count|repeated|unique|ignore-case|all-repeated(=\w+)?|group(=\w+)?|zero-terminated))$/,
  values: new Set(['-f', '--skip-fields', '-s', '--skip-chars', '-w', '--check-chars']),
  attached: /^-[fsw]\d+$/,
  maxPositional: 1,
};
// gitleaks: no reports, diagnostics, --log-opts (passes options such as --output to git) or configs.
const GITLEAKS_OPTIONS = {
  switches: /^(--redact(=\d+)?|-v|--verbose|--no-banner|--no-color|--follow-symlinks)$/,
  values: new Set(['--exit-code', '--log-level', '--max-target-megabytes', '--timeout']),
  maxPositional: 1,
};
// npm read commands: only output-format/filter flags (no --prefix, --logs-dir, --cache, configs).
const NPM_READ_OPTIONS = {
  switches: /^--(json|production|all|long|parseable|omit=[\w-]+|include=[\w-]+|audit-level=\w+|depth=\d+)$/,
};
// dotnet-ef read commands: no --msbuildprojectextensionspath, -o/--output, --connection, etc.
const EF_READ_OPTIONS = {
  switches: /^(--no-build|--json|-v|--verbose|--no-color|--prefix-output|--idempotent|--no-transactions)$/,
  values: new Set(['-c', '--context', '-p', '--project', '-s', '--startup-project', '--configuration', '--framework']),
};

/** Read-only commands both restricted agents may run. Returns a deny reason or null. */
function readOnlyProblem(program, args) {
  if (NO_WRITE_OPTIONS.has(program) || PS_READ.has(program) || CHANGE_DIRECTORY.has(program)) return null;
  if (program === 'sort') return optionsProblem('sort', args, SORT_OPTIONS);
  if (program === 'uniq') return optionsProblem('uniq', args, UNIQ_OPTIONS);
  if (program === 'rg') {
    return args.some((a) => /^--(pre|hostname-bin)/.test(a)) ? '`rg --pre`/`--hostname-bin` run other programs.' : null;
  }
  if (program === 'find') {
    // GNU find's complete set of actions that run programs or write files.
    return args.some((a) => /^-(exec|execdir|ok|okdir|delete|fprint|fprint0|fprintf|fls)$/.test(a))
      ? '`find` with an action that runs or writes is not allowed.'
      : null;
  }
  if (program === 'git') {
    let i = 0;
    while (args[i]?.startsWith('-')) {
      if (args[i] === '--no-pager') { i++; continue; }
      if (args[i] === '-C' && args[i + 1] !== undefined) { i += 2; continue; }
      return `git global option \`${args[i]}\` is not allowed (it can change what git executes).`;
    }
    const sub = args[i];
    const rest = args.slice(i + 1);
    if (rest.some((a) => GIT_UNSAFE_OPTION.test(a))) return 'This git option writes files or runs external programs.';
    if (GIT_READ.has(sub)) return null;
    if (sub === 'branch') return rest.every((a) => BRANCH_FLAGS.has(a)) ? null : 'Only listing branches is allowed.';
    if (sub === 'remote') return rest.every((a) => a === '-v') ? null : 'Only `git remote -v` is allowed.';
    return `\`git ${sub ?? ''}\` is not a read-only git command.`;
  }
  if (program === 'gitleaks') {
    if (args.length === 1 && args[0] === 'version') return null;
    if (!['git', 'dir', 'detect', 'protect'].includes(args[0])) return 'Only gitleaks scans are allowed.';
    if (!args.some((a) => /^--redact(=\d+)?$/.test(a))) return 'gitleaks must run with --redact.';
    return optionsProblem(`gitleaks ${args[0]}`, args.slice(1), GITLEAKS_OPTIONS);
  }
  if (program === 'npm') {
    if (args.length === 1 && ['--version', '-v'].includes(args[0])) return null;
    if (['audit', 'outdated'].includes(args[0])) return optionsProblem(`npm ${args[0]}`, args.slice(1), { ...NPM_READ_OPTIONS, maxPositional: 0 });
    if (['ls', 'view'].includes(args[0])) {
      return optionsProblem(`npm ${args[0]}`, args.slice(1), { ...NPM_READ_OPTIONS, positional: /^@?[\w.-]+(\/[\w.-]+)?(@[\w.^~<>=*-]+)?$/ });
    }
    return `\`npm ${args[0] ?? ''}\` is not allowed for this agent.`;
  }
  if (program === 'dotnet') {
    if (args.length === 1 && ['--version', '--info', '--list-sdks', '--list-runtimes'].includes(args[0])) return null;
    const p = args.indexOf('package');
    if (args[0] === 'list' && p > 0 && args.slice(1, p).every((a) => !a.startsWith('-') && !a.startsWith('/'))
      && args.slice(p + 1).every((a) => /^--(vulnerable|outdated|deprecated|include-transitive)$/.test(a))) return null;
    if (args[0] === 'tool' && args[1] === 'run' && args[2] === 'dotnet-ef' && args[3] === 'migrations') {
      const rest = args.slice(5);
      if (['list', 'has-pending-model-changes'].includes(args[4])) return optionsProblem(`dotnet-ef migrations ${args[4]}`, rest, { ...EF_READ_OPTIONS, maxPositional: 0 });
      if (args[4] === 'script') return optionsProblem('dotnet-ef migrations script', rest, { ...EF_READ_OPTIONS, maxPositional: 2, positional: /^[\w.]+$/ });
    }
    return 'This dotnet command is not allowed for this agent.';
  }
  if (program === 'gh') {
    const [group, sub] = args;
    if ((group === 'pr' && ['view', 'diff', 'checks', 'list'].includes(sub)) || (group === 'run' && ['view', 'list'].includes(sub)) || (group === 'repo' && sub === 'view')) return null;
    if (group === 'api') return ghRules(['gh', ...args]) ? 'Only read-only (GET) GitHub API calls are allowed.' : null;
    return `\`gh ${group ?? ''} ${sub ?? ''}\` is not allowed for this agent.`;
  }
  if (program === 'node') {
    if (args.length === 1 && /^(-v|--version)$/.test(args[0])) return null;
    return args[0] === '--test' && args.length > 1 && args.slice(1).every((a) => /^\.claude\/hooks\/[\w-]+\.test\.mjs$/.test(a.replace(/\\/g, '/')))
      ? null
      : 'Only `node --test .claude/hooks/<name>.test.mjs` is allowed.';
  }
  return `\`${program}\` is not on this agent's allowlist.`;
}

// dotnet build/test/restore: no -o/--output, --results-directory, --diag, -bl/binlogs, file
// loggers, lock files or MSBuild -p:/-t: (which can run arbitrary commands, e.g. PostBuildEvent).
const DOTNET_BUILD_OPTIONS = {
  switches: /^(--no-build|--no-restore|--nologo|-nologo|-warnaserror|\/warnaserror|--no-dependencies|--no-incremental)$/,
  values: new Set(['-c', '--configuration', '-f', '--framework', '-v', '--verbosity', '--filter', '-r', '--runtime', '-a', '--arch', '--os', '-l', '--logger']),
  valueCheck: (name, value) => !['-l', '--logger'].includes(name) || /^console(;verbosity=\w+)?$/i.test(value),
  maxPositional: 1,
  // A project/solution path. Must not start with '/', which MSBuild also reads as a switch (/bl, /p:…).
  positional: /^(?!\/)[\w.\\/-]+$/,
};
// vitest run: test-name filters and console reporters only (no -u, --coverage, --outputFile, --config).
const VITEST_RUN_OPTIONS = {
  switches: /^(--silent|--reporter=(default|verbose|dot|basic|tap))$/,
  values: new Set(['-t', '--testNamePattern']),
  positional: /^[\w.\\/-]+$/,
};

/** Extra commands for test-writer: build and run the test suites. */
function testWriterProblem(program, args) {
  if (program === 'dotnet' && ['build', 'test', 'restore'].includes(args[0])) {
    return optionsProblem(`dotnet ${args[0]}`, args.slice(1), DOTNET_BUILD_OPTIONS);
  }
  if (program === 'npm') {
    if (args[0] === 'test' && args.length === 1) return null;
    if (args[0] === 'run' && ['test', 'lint', 'build', 'i18n:check'].includes(args[1]) && args.length === 2) return null;
  }
  if (program === 'npx' && args[0] === 'vitest' && args[1] === 'run') return optionsProblem('vitest run', args.slice(2), VITEST_RUN_OPTIONS);
  return readOnlyProblem(program, args);
}

/** Evaluates a shell command for a restricted agent; deny unless every segment is allowlisted. */
export function evaluateRestrictedCommand(agent, command, dialect) {
  if (typeof command !== 'string' || !command.trim()) return deny(agent, 'Empty or missing command.');
  if (command.includes('\0')) return deny(agent, 'NUL character in command.');
  if (!quotesBalanced(command, dialect)) return deny(agent, 'Unbalanced quotes; the command cannot be verified.');

  // Variables, command/process substitution and PowerShell subexpressions can hide what runs.
  const withoutNullSink = command.replace(/[12]?>>?\s*\$null\b/gi, '');
  if (/\$|`|<\(|>\(/.test(dialect === 'bash' ? withoutNullSink : withoutNullSink.replace(/`(?=\r?\n)/g, ''))) {
    return deny(agent, 'Variables, `$(...)`, backticks and process substitution are not allowed for restricted agents.');
  }
  const { text, bodies } = extractHeredocs(command);
  if (bodies.size > 0) return deny(agent, 'Heredocs and here-strings are not allowed for restricted agents.');

  const segments = tokenize(text, dialect);
  if (segments.length === 0) return deny(agent, 'Empty command.');
  for (const { argv, redirects } of segments) {
    if (!argv.length) return deny(agent, 'A redirection without a command is not allowed.');
    const target = redirects.find((r) => !NULL_SINKS.has(r.toLowerCase()));
    if (target !== undefined) return deny(agent, `Redirecting output to \`${target}\` is not allowed (only to /dev/null, NUL or $null).`);
    if (/^[A-Za-z_][A-Za-z0-9_]*=/.test(argv[0])) return deny(agent, 'Environment-variable prefixes are not allowed for restricted agents.');
    if (/[\\/]/.test(argv[0])) return deny(agent, 'Run programs by name, not by path.');
    if (argv.some((a) => SECRET_REFERENCE.test(a))) return deny(agent, 'Secret stores and .env files are off limits.');

    const program = programName(argv[0]);
    const args = argv.slice(1);
    const problem = agent === 'test-writer' ? testWriterProblem(program, args) : readOnlyProblem(program, args);
    if (problem) return deny(agent, `${problem} (Allowed commands are listed in .claude/README.md.)`);
  }
  return null;
}

// ---------------------------------------------------------------------------------------------
// File edits (test-writer only; security-reviewer may never write)
// ---------------------------------------------------------------------------------------------

const GUARDED_INFRASTRUCTURE = /^backend\/familyhub\.api\.integrationtests\/(infrastructure\/(testdatabaseguard|dockerendpointguard|familyhubapifixture|familyhubapifactory)\.cs|safety\/)/;
const ASK_FILES = /^(backend\/familyhub\.api\.(unit|integration)tests\/[^/]+\.csproj|frontend\/familyhub-web\/(package\.json|package-lock\.json|vitest\.config\.ts|src\/test\/setup\.ts))$/;
const TEST_FILES = [
  /^backend\/familyhub\.api\.unittests\/.+\.cs$/,
  /^backend\/familyhub\.api\.integrationtests\/.+\.cs$/,
  /^frontend\/familyhub-web\/src\/.+\.test\.tsx?$/,
  /^frontend\/familyhub-web\/src\/test\/.+\.tsx?$/,
];

/** Real path of the nearest existing ancestor plus the remaining (not yet existing) segments. */
function realPathOf(absolutePath) {
  let existing = absolutePath;
  const rest = [];
  while (!existsSync(existing)) {
    const parent = dirname(existing);
    if (parent === existing) break;
    rest.unshift(basename(existing));
    existing = parent;
  }
  return resolve(realpathSync.native(existing), ...rest);
}

/** Classifies a test-writer edit target. `root` is the repository root (CLAUDE_PROJECT_DIR). */
export function evaluateTestWriterPath(agent, rawPath, root) {
  const raw = String(rawPath ?? '');
  if (!raw) return deny(agent, 'Missing file path.');
  if (!root) return deny(agent, 'Repository root unknown (CLAUDE_PROJECT_DIR); cannot verify the path.');
  if (raw.includes('\0')) return deny(agent, 'NUL character in path.');
  if (/^(\\\\|\/\/)/.test(raw)) return deny(agent, 'UNC and device paths are not allowed.');
  if (raw.lastIndexOf(':') > 1) return deny(agent, 'Alternate data streams and colons in paths are not allowed.');
  // Segment checks apply below the repository root (the root itself may legitimately contain
  // e.g. an 8.3 short name such as a Windows temp folder); a path outside the root is denied.
  const lexical = isAbsolute(raw) ? relative(root, raw) : raw;
  if (isAbsolute(raw) && (isAbsolute(lexical) || lexical.split(/[\\/]+/)[0] === '..')) {
    return deny(agent, 'The path is outside the repository.');
  }
  const segments = lexical.split(/[\\/]+/).filter(Boolean);
  if (segments.some((s) => /[. ]$/.test(s) && s !== '.' && s !== '..')) return deny(agent, 'Path segments ending in a dot or space are not allowed.');
  if (segments.some((s) => /~\d/.test(s))) return deny(agent, '8.3 short names are not allowed.');

  const absolute = resolve(root, raw);
  let real;
  let realRoot;
  try {
    real = realPathOf(absolute);
    realRoot = realpathSync.native(root);
  } catch {
    return deny(agent, 'The path could not be resolved safely.');
  }
  const rel = relative(realRoot, real);
  if (!rel || rel.startsWith('..') || isAbsolute(rel)) return deny(agent, 'The path is outside the repository (or escapes it through a link).');
  const key = rel.replace(/\\/g, '/').toLowerCase();

  if (/(^|\/)(bin|obj)\//.test(key)) return deny(agent, 'Build output folders are not editable.');
  if (GUARDED_INFRASTRUCTURE.test(key)) {
    return deny(agent, 'The integration-test isolation guards (TestDatabaseGuard, DockerEndpointGuard, fixture, factory, Safety/) must never be weakened.');
  }
  if (ASK_FILES.test(key)) return ask(agent, `Changing ${rel.replace(/\\/g, '/')} affects test infrastructure or dependencies.`);
  if (TEST_FILES.some((pattern) => pattern.test(key))) return null;
  return deny(agent, `${rel.replace(/\\/g, '/')} is outside the test folders; production code and configuration are not editable by test-writer.`);
}

// ---------------------------------------------------------------------------------------------
// Entry points
// ---------------------------------------------------------------------------------------------

/** Policy for a restricted agent's tool call; null for other agents or allowed calls. Never throws. */
export function agentPolicy(input, agentName = input?.agent_type, root = process.env.CLAUDE_PROJECT_DIR || input?.cwd) {
  if (!RESTRICTED_AGENTS.has(agentName)) return null;
  try {
    const tool = input?.tool_name;
    const toolInput = input?.tool_input || {};
    if (EDIT_TOOLS.has(tool)) {
      if (agentName === 'security-reviewer') return deny(agentName, 'This agent is read-only; it reports findings and never edits files.');
      return evaluateTestWriterPath(agentName, toolInput.file_path ?? toolInput.notebook_path, root);
    }
    if (tool === 'Bash') return evaluateRestrictedCommand(agentName, toolInput.command, 'bash');
    if (tool === 'PowerShell') return evaluateRestrictedCommand(agentName, toolInput.command, 'powershell');
    return null;
  } catch (error) {
    return deny(agentName, `Policy error (${error?.message || error}); failing closed.`);
  }
}

/** Frontmatter-hook mode: `node agent-policy.mjs <agent-name>`. Fails closed on any error. */
function main() {
  const agent = process.argv[2];
  const emit = (decision) => {
    if (decision) {
      process.stdout.write(JSON.stringify({
        hookSpecificOutput: {
          hookEventName: 'PreToolUse',
          permissionDecision: decision.action,
          permissionDecisionReason: `FamilyHub agent policy: ${decision.reason}`,
        },
      }));
    }
    process.exit(0);
  };
  if (!RESTRICTED_AGENTS.has(agent)) {
    process.stderr.write(`FamilyHub agent policy: unknown agent "${agent}"\n`);
    process.exit(2); // blocking: a misconfigured registration must not silently allow
  }
  let input;
  try {
    input = JSON.parse(readFileSync(0, 'utf8'));
  } catch (error) {
    emit(deny(agent, `Unreadable hook input (${error?.message || error}); failing closed.`));
  }
  emit(agentPolicy(input, agent));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) main();
