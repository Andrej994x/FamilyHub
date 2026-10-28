/*
 * Least-privilege invariants for the Claude Code project configuration (.claude/).
 * Run from the repository root:  node --test .claude/hooks/config.test.mjs
 * Fails when a future change silently widens permissions: a skill granting tools, an agent able
 * to spawn agents or switch permission modes, a removed fail-closed hook, or a weakened guard.
 */

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const CLAUDE_DIR = fileURLToPath(new URL('..', import.meta.url));
const read = (...p) => readFileSync(join(CLAUDE_DIR, ...p), 'utf8');

/** Splits a Markdown file into { meta, frontmatter, body } (top-level YAML scalars and lists). */
function parse(text) {
  const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n([\s\S]*)$/.exec(text);
  assert.ok(match, 'file must start with a YAML frontmatter block');
  const [, frontmatter, body] = match;
  const meta = {};
  let listKey = null;
  for (const line of frontmatter.split(/\r?\n/)) {
    const top = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(line);
    if (top) {
      const [, key, value] = top;
      meta[key] = value === '' ? [] : value.replace(/^["']|["']$/g, '');
      listKey = value === '' ? key : null;
      continue;
    }
    const item = /^\s+-\s+["']?(.*?)["']?$/.exec(line);
    if (listKey && item && Array.isArray(meta[listKey])) meta[listKey].push(item[1]);
  }
  return { meta, frontmatter, body };
}

const skills = readdirSync(join(CLAUDE_DIR, 'skills'), { withFileTypes: true })
  .filter((d) => d.isDirectory())
  .map((d) => ({ dir: d.name, ...parse(read('skills', d.name, 'SKILL.md')) }));
const agents = readdirSync(join(CLAUDE_DIR, 'agents'))
  .filter((f) => f.endsWith('.md'))
  .map((f) => ({ file: f, ...parse(read('agents', f)) }));
const settings = JSON.parse(read('settings.json'));

test('expected skills and agents exist', () => {
  assert.deepEqual(skills.map((s) => s.dir).sort(), ['codex-review', 'ef-migration', 'fullstack-feature', 'testing']);
  assert.deepEqual(agents.map((a) => a.meta.name).sort(), ['security-reviewer', 'test-writer']);
});

for (const skill of skills) {
  test(`skill ${skill.dir}: name, concise description, no permission grants, no forks, no shell injection`, () => {
    assert.equal(skill.meta.name, skill.dir);
    const description = `${skill.meta.description ?? ''}${skill.meta.when_to_use ?? ''}`;
    assert.ok(description.length > 40, 'description must say what the skill does and when to use it');
    assert.ok(description.length <= 400, `description is ${description.length} chars; keep listings cheap (hard limit 1536)`);
    assert.equal(skill.meta['allowed-tools'], undefined, 'skills must not pre-approve tools (allowed-tools grants permissions)');
    assert.equal(skill.meta.context, undefined, 'skills run inline; no forked subagent contexts');
    assert.equal(skill.meta.hooks, undefined, 'skills must not register session hooks');
    assert.doesNotMatch(skill.body, /!`|```!/, 'no !-command injection (it bypasses permission prompts)');
    assert.ok(skill.body.split('\n').length < 500, 'SKILL.md must stay under 500 lines');
  });
}

test('codex-review is user-only, optional, and cannot edit or delegate', () => {
  const skill = skills.find((s) => s.dir === 'codex-review');
  assert.equal(skill.meta['disable-model-invocation'], 'true');
  for (const tool of ['Edit', 'Agent']) assert.ok(skill.meta['disallowed-tools'].split(/[\s,]+/).includes(tool), `${tool} must be disallowed`);
  assert.match(skill.meta.description, /^Optional/);
  assert.match(skill.body, /codex review --base origin\/main\s*#\s*standard/);
});

const MAX_TOOLS = {
  'security-reviewer': ['Read', 'Grep', 'Glob', 'Bash'],
  'test-writer': ['Read', 'Grep', 'Glob', 'Edit', 'Write', 'Bash'],
};
for (const agent of agents) {
  const name = agent.meta.name;
  test(`agent ${name}: explicit least-privilege tools, no nesting, no mode/MCP/memory overrides`, () => {
    assert.equal(agent.file, `${name}.md`);
    assert.equal(typeof agent.meta.tools, 'string', 'tools must be listed explicitly (omitting it inherits every tool)');
    const tools = agent.meta.tools.split(/\s*,\s*/);
    assert.deepEqual(tools.filter((t) => !MAX_TOOLS[name].includes(t)), [], 'no tools beyond the approved set');
    assert.ok(!tools.includes('Agent'), 'restricted agents must not spawn subagents');
    for (const key of ['permissionMode', 'mcpServers', 'memory', 'disallowedTools', 'isolation', 'background']) {
      assert.equal(agent.meta[key], undefined, `${key} must not be set`);
    }
    assert.doesNotMatch(agent.meta.description, /proactiv/i, 'no proactive auto-delegation');
    assert.ok(Number(agent.meta.maxTurns) > 0 && Number(agent.meta.maxTurns) <= 50, 'maxTurns caps token use');
  });
  test(`agent ${name}: its only hook is the fail-closed agent policy for itself`, () => {
    const hookBlock = agent.frontmatter.slice(agent.frontmatter.indexOf('hooks:'));
    assert.match(hookBlock, /PreToolUse:/);
    assert.match(hookBlock, /matcher: "Bash\|PowerShell\|Edit\|Write\|MultiEdit\|NotebookEdit"/);
    assert.match(hookBlock, /command: node/);
    assert.ok(hookBlock.includes(`args: ["\${CLAUDE_PROJECT_DIR}/.claude/hooks/agent-policy.mjs", "${name}"]`), 'policy hook must name this agent');
    assert.match(hookBlock, /onFailure: block/);
    assert.equal((hookBlock.match(/type: command/g) || []).length, 1, 'exactly one hook');
  });
}
test('security-reviewer cannot write by any tool', () => {
  const tools = agents.find((a) => a.meta.name === 'security-reviewer').meta.tools.split(/\s*,\s*/);
  for (const tool of ['Edit', 'Write', 'MultiEdit', 'NotebookEdit']) assert.ok(!tools.includes(tool));
});
test('subagent models follow the approved cost split', () => {
  assert.equal(agents.find((a) => a.meta.name === 'security-reviewer').meta.model, 'inherit');
  assert.equal(agents.find((a) => a.meta.name === 'test-writer').meta.model, 'sonnet');
});

test('settings.json keeps the Phase 3 guard and backstops; no broad grants or bypass', () => {
  const [registration] = settings.hooks.PreToolUse;
  assert.equal(registration.matcher, 'Bash|PowerShell|Edit|Write|MultiEdit|NotebookEdit');
  assert.equal(registration.hooks[0].command, 'node');
  assert.match(registration.hooks[0].args[0], /\/\.claude\/hooks\/guard\.mjs$/);
  for (const rule of ['Bash(git push --force *)', 'Bash(git reset --hard *)', 'PowerShell(git reset --hard *)', 'Read(~/AppData/Roaming/Microsoft/UserSecrets/**)']) {
    assert.ok(settings.permissions.deny.includes(rule), `missing deny rule ${rule}`);
  }
  assert.equal(settings.permissions.allow, undefined, 'the shared settings grant no allow rules');
  assert.notEqual(settings.permissions.defaultMode, 'bypassPermissions');
  assert.notEqual(settings.disableAllHooks, true);
});
test('the hook scripts referenced by the configuration exist', () => {
  for (const file of ['guard.mjs', 'agent-policy.mjs']) assert.ok(existsSync(join(CLAUDE_DIR, 'hooks', file)));
});
