---
id: MSG-BACKEND-123
from: root
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
created: 2026-07-03
ref: MSG-ROOT-ESCALATION-001
model: sonnet
content_hash: 79ccb5a3af6bc2e074bdb343bb436a4bf37fec57fc837e2489f52141d8bede78
---

# Deprecate INJECTED Workflow

## Problem

**INJECTED status causes infinite loops** in terminal sessions.

**Flow:**
1. Nightwatch injects message → `status: INJECTED`
2. Terminal processes → **doesn't update status**
3. Nightwatch sees INJECTED again → re-triggers session
4. **LOOP!**

**Evidence:**
- Explorer stuck in 27+ iteration loop
- 4 INJECTED messages in Explorer inbox (3 already completed)
- MCP `list_inbox(status=UNREAD)` returns 0 (INJECTED not recognized)

---

## Root Decision

**Option A: Deprecate INJECTED** (APPROVED by Root)

**Rationale:**
- Simpler flow = fewer bugs
- MCP API doesn't recognize INJECTED anyway
- Information loss minimal (injected timestamp = metadata)

---

## Implementation Spec

### 1. Nightwatch Refactor

**File:** `scripts/nightwatch.sh` OR `spaceos-nexus/knowledge-service/src/inboxWatcher.ts`

**Current:**
```bash
status: INJECTED
injected: 2026-07-03
```

**New:**
```bash
status: INJECTED
injected: 2026-07-03
created: 2026-07-03
```

**Note:** Remove `injected` field entirely (use `created` timestamp instead)

---

### 2. Bulk Convert Existing INJECTED

**Find all INJECTED messages:**
```bash
grep -rl "status: INJECTED" terminals/*/inbox/
```

**For each:**
- If `completed: <date>` frontmatter exists → archive
- Else → update `status: INJECTED` → `status: INJECTED
injected: 2026-07-03`

---

### 3. Update Documentation

**File:** `docs/WORKFLOW.md`

**Remove:**
- INJECTED status definition
- INJECTED lifecycle description

**Update:**
- Inbox workflow: "Messages created as UNREAD"
- No mention of INJECTED

---

### 4. MCP API Update (Optional)

**If transition period needed:**

`mcp__spaceos-knowledge__list_inbox` should:
- Include INJECTED in results (for backwards compatibility)
- Log deprecation warning

**After Q3 2026:**
- Remove INJECTED support entirely

---

## Acceptance Criteria

- [ ] Nightwatch no longer creates INJECTED status
- [ ] All existing INJECTED messages converted (or archived)
- [ ] WORKFLOW.md updated (no INJECTED mention)
- [ ] No loop recurrence (verify with Explorer)
- [ ] MCP API backwards compatibility (optional, for transition)

---

## Testing Plan

1. Trigger nightwatch inbox injection
2. Verify: `status: UNREAD` (NOT INJECTED)
3. Check all terminals for remaining INJECTED
4. Process 1 UNREAD message → verify no loop

---

## Files to Modify

- `scripts/nightwatch.sh` OR `spaceos-nexus/knowledge-service/src/inboxWatcher.ts`
- `docs/WORKFLOW.md`
- Bulk update existing INJECTED messages (script or manual)

---

## Priority Justification

**HIGH** because:
- Prevents future loops (reliability)
- Cleans up ambiguous workflow
- Enables MCP API consistency

**Timeline:** Q3 2026 (not blocking, but high priority)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
