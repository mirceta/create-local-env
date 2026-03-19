# Context Engineering — Detailed Examples

## Progressive Disclosure

**Step 1 — AGENTS.md (always loaded):**

```markdown
# Project: checkout-service
Stack: Node.js, PostgreSQL, React
Build: pnpm install -> pnpm dev
Tests: pnpm test (Jest)

# Skills — read before working in these areas:
- Auth system       -> .claude/skills/auth/SKILL.md
- Database          -> .claude/skills/database/SKILL.md
- Payment flows     -> .claude/skills/payments/SKILL.md
- API endpoints     -> .claude/skills/api-conventions/SKILL.md

# Never touch:
- /legacy (deprecated, kept for reference only)
- .env files (managed by ops team)
```

**Step 2 — Skill file (loads only when working on auth):**

```markdown
---
name: auth-patterns
description: Use when reading or modifying anything in /src/auth,
  /src/middleware, or any file that imports from auth. Covers JWT
  patterns, session management, and permission checks.
---

# Auth conventions

All auth logic lives in /src/auth. Never duplicate it elsewhere.

JWT tokens expire in 15 minutes. Refresh tokens in 7 days.
Store refresh tokens in httpOnly cookies — never localStorage.

Permission checks must use the hasPermission() helper.
  Don't: if (user.role === 'admin')
  Do:    if (hasPermission(user, 'admin:write'))

For full middleware chain documentation:
  read .claude/skills/auth/references/middleware-chain.md
```

**Step 3 — Reference file (loads only when deeper detail needed):**

```markdown
# Middleware execution order

Every request passes through these in sequence:
1. rateLimiter      — blocks >100 req/min per IP
2. parseJWT         — attaches user to req.user (null if no token)
3. requireAuth      — returns 401 if req.user is null
4. checkPermissions — returns 403 if user lacks the required role
5. auditLog         — writes to audit_events table
```

**Result:** The agent loaded 25 lines of auth conventions. The 60-line middleware doc only loaded when needed. Payment and database skills loaded nothing.

**Folder structure:**

```
project/
├── AGENTS.md                          <- 30-50 lines, always loaded
└── .claude/
    └── skills/
        ├── auth/
        │   ├── SKILL.md               <- triggers + core instructions
        │   └── references/
        │       └── middleware-chain.md <- full detail, on demand
        ├── database/
        │   ├── SKILL.md
        │   └── references/
        │       └── schema.md
        └── testing/
            ├── SKILL.md
            └── scripts/
                └── run-tests.sh       <- script output only, never enters context
```

---

## AGENTS.md Sizing

**Too long (180 lines) — hurts performance:**

```markdown
Stack: Node.js, PostgreSQL, React
Build: pnpm dev

# Database schema
users table: id, email, role, created_at, updated_at...
orders table: id, user_id, total, status, stripe_id...
[...40 more lines of schema...]

# Auth conventions
JWT expires in 15 min...
[...20 more lines...]

# Testing rules
Use Jest. Never mock DB...
[...30 more lines...]
```

**Right size (18 lines) — stays reliable:**

```markdown
Stack: Node.js, PostgreSQL, React
Build: pnpm install -> pnpm dev
Tests: pnpm test

# Skills (read before touching these areas)
Auth     -> .claude/skills/auth/SKILL.md
Database -> .claude/skills/db/SKILL.md
Testing  -> .claude/skills/testing/SKILL.md
Deploy   -> .claude/skills/deploy/SKILL.md

# Hard rules (apply everywhere, every session)
- Never commit directly to main
- Never touch /legacy directory
- All secrets via process.env only
```

---

## Skill Description Quality

**Vague — will almost never trigger:**

```yaml
---
name: database
description: Handles database stuff
---
```

**Specific — triggers reliably:**

```yaml
---
name: database-conventions
description: Use when reading from or writing to PostgreSQL. Covers
  migration naming rules, query optimization patterns, connection pool
  config, and test fixture setup. Always use before touching any file
  in /src/db/ or any file that imports from db/client.
---
```

---

## Concrete Examples Over Abstract Rules

**Abstract — agent will guess wrong:**

```markdown
- Follow RESTful naming conventions
- Return consistent error formats
- Always validate requests
```

**Concrete — agent follows reliably:**

```markdown
# API endpoint conventions

Naming:
  Don't: GET /getUserById/:id
  Do:    GET /users/:id

Error format:
  Don't: return { message: "failed" }
  Do:    return { error: "User not found", code: 404 }

Request validation (always before DB calls):
  Don't: async (req, res) => {
           const user = await db.find(req.body.id)

  Do:    async (req, res) => {
           const { id } = userSchema.parse(req.body)
           const user = await db.find(id)


---

## Diagnostic Signals When Testing Skills

| Signal | Meaning | Fix |
|--------|---------|-----|
| Agent reads files in unexpected order | Skill structure unclear | Reorder sections to match agent's thinking |
| Agent ignores a linked reference file | Link not prominent enough | Add: "IMPORTANT: read references/X.md before writing code" |
| Agent always reads the same reference | Content is essential | Move it into SKILL.md directly |
| Agent never reads a reference | It's unnecessary | Remove it |
| Agent gets step 3 right but step 5 wrong | Step 5 depends on step 3 context | Add a note at step 5 with the relevant finding |

---

## Contradiction Detection

```bash
# Check for library conflicts
grep -r "Redux" .claude/skills/
grep -r "Zustand" .claude/skills/
# If both return results — you have a contradiction.

# Check for stale file references
grep -r "\.claude/skills" . | grep -v ".claude/skills/"
# Any path that doesn't resolve is a dead pointer.
```

---

## Sources

All references verified as of March 14, 2026.

- Anthropic — Equipping Agents with Agent Skills (Oct 2025)
- Anthropic — Effective Context Engineering for AI Agents (Sep 2025)
- Anthropic — Agent Skills Overview (Feb 2026)
- Anthropic — Skill Authoring Best Practices (Feb 2026)
- Martin Fowler — Context Engineering for Coding Agents (Jan 2026)
- HumanLayer — Writing a Good CLAUDE.md (Nov 2025)
- Faros AI — Context Engineering for Developers (Dec 2025)
- Victor Dibia — Context Engineering 101 (Mar 11, 2026)
- Sankalp — Claude Code 2.0 Deep Dive (Feb 2026)
- Data Science Collective — Complete Guide to AGENTS.md (Mar 13, 2026)
- Lee Han Chung — Claude Agent Skills Deep Dive (Oct 2025)
- Manus AI — Context Engineering Blog (2025)
