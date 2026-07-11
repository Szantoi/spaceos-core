---
id: MSG-BACKEND-122
from: root
to: backend
type: task
priority: critical
status: READ
read_at: 2026-07-03
created: 2026-07-03
ref: MSG-ROOT-ESCALATION-001
epic_id: EPIC-NEXUS-V1
model: sonnet
content_hash: 2e5d27ed4dfc8bc5bd321012acd0a8556fea68fbc11287e5cd52f3dd189abd8f
---

# Terminal-Reviewer MCP Refactor

## Problem

Terminal-reviewer infrastructure **BROKEN** — review sessions fail with "no current target" error.

**Evidence:**
```
Review error: Command failed: tmux send-keys -t spaceos-review-architect [...]
no current target
```

**Impact:**
- Every DONE review fails
- REJECT messages generated with ERROR verdict
- Workflow blocked

**Root Cause:** Tmux-based injection fragile (sessions disappear, not persistent)

---

## Solution

**Refactor terminal-reviewer to use MCP API** (not tmux send-keys)

---

## Implementation Spec

### 1. New MCP Tool: `request_review`

**File:** `spaceos-nexus/knowledge-service/src/mcp.ts`

**Signature:**
```typescript
mcp__spaceos-knowledge__request_review(
  terminal: 'architect' | 'librarian',
  inbox_message_path: string,
  done_message_path: string
): Promise<{ verdict: 'APPROVE' | 'REJECT', feedback: string }>
```

**Behavior:**
1. Spawn review agent session (architect OR librarian) via sessionStarter
2. Inject review request via MCP (not tmux)
3. Wait for structured response (verdict + feedback)
4. Return review result
5. Log audit trail (who reviewed, when, verdict)

**Timeout:** 2 minutes (if no response, fallback to manual approval)

---

### 2. Refactor `terminal-reviewer.sh`

**File:** `scripts/terminal-reviewer.sh`

**Current (BROKEN):**
```bash
tmux send-keys -t spaceos-review-architect -l "$PROMPT"
```

**New (MCP-based):**
```bash
result=$(curl -X POST http://localhost:3456/api/review/request \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "architect",
    "inbox_message_path": "...",
    "done_message_path": "..."
  }')

verdict=$(echo "$result" | jq -r '.verdict')
feedback=$(echo "$result" | jq -r '.feedback')
```

---

### 3. Fallback: Manual Approval

**If MCP unavailable or timeout:**
- Log warning: "Review service unavailable, manual approval required"
- Create inbox message for Conductor: "MSG-XXX needs manual review"
- Set verdict: PENDING_MANUAL
- Do NOT auto-REJECT

---

## Acceptance Criteria

- [ ] `request_review` MCP tool implemented
- [ ] Terminal-reviewer uses MCP (not tmux)
- [ ] Review results audit-logged (who, when, verdict)
- [ ] Fallback manual approval working
- [ ] No "no current target" errors
- [ ] Test: Review 1 DONE message successfully

---

## Testing Plan

1. Create test DONE message
2. Trigger terminal-reviewer
3. Verify:
   - MCP call made
   - Review session spawned
   - Verdict returned
   - Audit log written
4. Test fallback: Kill MCP service, verify manual approval message

---

## Files to Modify

- `spaceos-nexus/knowledge-service/src/mcp.ts` (new tool)
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts` (spawn review agent)
- `scripts/terminal-reviewer.sh` (refactor to MCP)
- `scripts/nightwatch.sh` (call new terminal-reviewer)

---

## Priority Justification

**CRITICAL** because:
- Blocks DONE workflow (every review fails)
- Affects all terminals (Explorer, Backend, Frontend, Designer)
- ADR-053 checkpoint-based coordination relies on review

**Timeline:** ASAP (within 24 hours)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
