# Context quality gate
After any modification to `AGENTS.md` or skills (in `.claude/skills/`), run the **context-engineering** skill to reevaluate whether the changes adhere to context engineering guidelines. Fix any issues before considering the task complete.

# Stack
VB6 desktop app (Birokrat) + C# server (BiroNext Server) + React frontend (BiroNext Web). VB6 source files are Windows-1250 encoded — never write them with UTF-8 tools.

# Build
Cannot compile locally. Use `tools\vb6-build\vb6-build.ps1` — see the **build-install** skill.

# Git
Never run bare `git add/commit/push`. Use `tools\gitcommit\gitcommit.ps1` — see the **git-commit** skill.

# Refactor
After every code edit, ask yourself what can we refactor?

# Skills
Skills are in `.claude/skills/`. Use `skill-creator` to create, evaluate, and benchmark new skills.
- **Context engineering** → `.claude/skills/context-engineering/SKILL.md`
  Use when: setting up AI projects, writing AGENTS.md, creating skills, structuring .claude/ directories
- **Research** → `.claude/skills/research/SKILL.md`
  Use when: conducting research on topics related to company goals (competitive analysis, market research, ERP, accounting, AI, cloud)
- **Task tracking** → `.claude/skills/task-tracking/SKILL.md`
  Use when: task touches 3+ files, involves migration, spans multiple layers, takes 10+ steps, or is irreversible (2+ criteria → activate)

# Concept Map
When a task references a domain concept, read the relevant map file:
- create-local-env (powershell profile, compiler, git aliases, git functions, windows functions, startsln, utils, envarlist, common library, shell, database, deployment, encoding, installs, resources, tools, vars) → `context/concept-maps/create-local-env.md`

# Updating context
When the user says to "update context", "add to the concept map", "add this to our context", "remember this in context", or similar — they mean the **`context/` folder** in this repository, NOT Claude Code's memory system. Update or create the appropriate file in `context/`. If the information also warrants a memory entry (useful across conversations), update both `context/` and memory.
