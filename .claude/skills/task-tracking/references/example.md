## todo.md Example

**At task start:**

```markdown
# Task: Migrate users table to add MFA support

## Goal
Add TOTP-based MFA to user auth without breaking existing sessions.

## Steps
[ ] 1. Read current users table schema
[ ] 2. Write migration: add mfa_secret, mfa_enabled columns
[ ] 3. Update User model in /src/models/user.ts
[ ] 4. Update auth middleware to check mfa_enabled
[ ] 5. Add /auth/mfa/setup and /auth/mfa/verify endpoints
[ ] 6. Write tests for all three new paths
[ ] 7. Update API docs

## Constraints
- Must not break users with mfa_enabled = false
- Migration must be reversible (down() required)
- Do not touch /src/auth/legacy-session.ts
```

**After step 2:**

```markdown
## Steps
[x] 1. Read current users table schema
[x] 2. Migration written: 20260314_add_mfa_to_users.sql
[ ] 3. Update User model in /src/models/user.ts  <- CURRENT
[ ] 4. Update auth middleware to check mfa_enabled
[ ] 5. Add /auth/mfa/setup and /auth/mfa/verify endpoints
[ ] 6. Write tests for all three new paths
[ ] 7. Update API docs

## Notes from step 2
mfa_secret is nullable (NULL = MFA not set up yet)
mfa_enabled defaults to false
```