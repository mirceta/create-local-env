---
name: context-engineering
description: Use when setting up a new project for AI-assisted development, writing
  or reviewing AGENTS.md, creating or editing skills, structuring .claude/ directories,
  or when the user asks about context engineering best practices. Covers progressive
  disclosure, skill authoring, description-driven discovery, and context maintenance.
---

# Context Engineering

Apply these principles when structuring AI agent instructions, skills, and memory.

## 1. Progressive Disclosure

Load only what the agent needs, when it needs it.

- AGENTS.md: 30-50 lines max. Project identity, build commands, skill pointers, hard rules.
- Skills: Domain-specific instructions, loaded when task matches description.
- References: Deep detail inside `references/` subdirectories, loaded only when a skill points to them.
- Files on disk cost zero tokens until accessed.

## 2. AGENTS.md as Orientation, Not Manual

Every line must pass this test: "Is this relevant every single time, regardless of task?"

- If no -> move it to a skill.
- Only keep: stack, build/test commands, skill pointers, and hard rules that apply everywhere.

## 3. Description-Driven Discovery

The `description` field in SKILL.md frontmatter is the only mechanism that triggers a skill.

A good description has four parts:
1. **Trigger condition** — when to use it
2. **Coverage** — what it covers (keywords the agent associates with the task)
3. **Scope** — boundaries of what it handles
4. **File triggers** — specific paths or import patterns

Write in third person. Be specific enough that a human skimming it would know exactly when to use it.

## 4. Concrete Examples Over Abstract Rules

Show right/wrong pairs instead of abstract principles.

- Every convention should have a `do this` / `not this` example.
- The agent pattern-matches against examples far more reliably than it interprets principles.

## 5. The todo.md Trick

For tasks with 5+ files or 3+ steps, create `todo.md` before writing code.

- Goal statement, numbered steps, constraints.
- Update after every step: mark done, add notes for future steps.
- This keeps the goal in the high-attention end of context and prevents goal drift.

## 6. Build Skills From Observation

Do the task manually first. Codify what you repeatedly explained.

1. Run a task with no skills loaded. Note every correction you make.
2. Turn those corrections into a SKILL.md draft.
3. Test with a fresh session. Note what the agent still gets wrong.
4. Iterate until the agent gets it right consistently.

## 7. Prune Actively

Outdated instructions actively mislead. Contradictions cause silent failures.

- Review context files on: dependency upgrades, folder restructures, new/deprecated patterns, build changes.
- Every line must answer yes to: "If the agent reads only this line, will it make a correct decision?"
- Shorter and accurate always beats longer and partially correct.

For detailed examples of each principle, read `references/examples.md`.
