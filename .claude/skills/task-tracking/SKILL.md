---
name: task-tracking
description: Use when a task meets 2 or more complexity criteria (touches 3+ files, involves migration, spans multiple layers, takes 10+ steps, or involves something irreversible). Creates and maintains a todo.md file to prevent goal drift during long sessions. Always use this skill before starting work on complex tasks.
---

# Task Tracking

When the complexity check in AGENTS.md triggers (2+ criteria met), create `todo.md` before writing any code.

## Why this matters

After ~50 tool calls, context window attention degrades. Earlier goals drift to the low-attention middle zone. Writing progress to `todo.md` and re-reading it after each step reinjects the current goal into the end of the context — the position with strongest attention.

## todo.md format

```markdown
# Task: [one line description]

## Goal
[what done looks like — one short paragraph]

## Steps
[ ] 1. [first step]
[ ] 2. [second step]
...

## Constraints
- [things that must never be violated]

## Notes
[updated after each step with findings future steps need]
```

## Rules

1. Create `todo.md` before writing any code
2. Update it after every completed step — mark done with `[x]`
3. Add to Notes anything the next step needs to know
4. Do not begin a new step without updating first
5. If you discover the plan needs to change, update the Steps list before continuing
6. Delete `todo.md` when the task is fully complete
