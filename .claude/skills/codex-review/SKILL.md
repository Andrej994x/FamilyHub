---
name: codex-review
description: Optional - prepare a detailed, neutral risk-based brief for an independent Codex review of the current branch against origin/main. For complex S2/S3 changes; routine PRs use `codex review --base origin/main` directly.
disable-model-invocation: true
disallowed-tools: Edit NotebookEdit Agent
argument-hint: "[S0|S1|S2|S3]"
---

# Codex review brief (optional)

**Routine PRs don't need this skill.** There the owner runs `codex review --base origin/main` directly. Use this skill when a detailed, risk-based brief helps Codex, mainly for complex S2/S3 changes.

**Independence rules:**
- Codex is an **independent reviewer**, so the brief contains **facts only**: no assessment, verdict, reassurance or summary of your own reasoning about correctness.
- You **do not run Codex**, post PR comments or record the outcome. The owner does.
- Codex never replaces automated tests: missing tests are listed as gaps, never waived.

## Steps
1. **Up-to-date base:** run `git fetch origin main`.
   - If `git status --porcelain` is not empty, **stop**: Codex reviews committed state, so ask the owner to commit or stash first.
   - If `HEAD` equals `origin/main`, stop: there is nothing to review.
2. **Collect facts** (read-only):
   - branch name, short SHA, and the base: `git merge-base origin/main HEAD`;
   - `git log --oneline origin/main..HEAD`;
   - `git diff --stat origin/main...HEAD`;
   - changed files grouped by area: API (Controllers, Services, DTOs, Validators), data (Models, Data/Configurations, Migrations), frontend (pages/components, services, locales), tests, CI, `.claude/` and docs;
   - **risk level:** the argument (`$ARGUMENTS`) if given; otherwise from the PR description (`gh pr view --json body` if a PR exists); otherwise write "not stated", and don't guess;
   - **test evidence:** test files added or changed in the diff. If a PR exists, add `gh pr checks` results. Note when production files changed with no test changes.
3. **Write the brief** to `.codex-review/<branch-with-slashes-as-dashes>-<shortsha>.md`. The folder is gitignored, so **never** commit the brief. Use exactly these sections:
   - **Change under review:** base and head SHAs, the commit list and the diff stat.
   - **Stated risk level** and the AGENTS.md rules it triggers.
   - **Changed areas** (from step 2).
   - **Review against these project rules:** link the relevant AGENTS.md sections (Authorization order and `Id`+`FamilyId`, validation, DTOs, secrets, migrations, i18n parity, testing rules) and, if relevant, `docs/ARCHITECTURE.md` §5 and §12 and `docs/TESTING.md`.
   - **Focus questions,** derived **only** from the changed areas and the risk level. Examples:
     - "Does every new family-scoped query filter by FamilyId?"
     - "Can a Member or Child reach a manage operation?"
     - "Does the migration drop or transform data?"
     - "Are new UI strings present in en and mk?"
   - **Test evidence and gaps** (facts from step 2).
   - **Out of scope / known limitations:** only those already documented in the repo, quoted with their source.
4. **Print** the file path and the owner's commands:
   ```bash
   codex review --base origin/main - < .codex-review/<file>.md   # with the brief
   codex review --base origin/main                               # standard, without a brief
   ```
   Then remind the owner to record Codex's outcome in the PR. It's required for S3 and recommended for S2.
