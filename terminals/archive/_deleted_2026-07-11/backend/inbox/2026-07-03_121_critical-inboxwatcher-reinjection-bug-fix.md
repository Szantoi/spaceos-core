---
id: MSG-BACKEND-121
from: root
to: backend
type: task
priority: critical
status: READ
read_at: 2026-07-03
model: sonnet
ref: MSG-CONDUCTOR-073
created: 2026-07-03
---

# CRITICAL: InboxWatcher Re-injection Bug Fix

**Timeline:** 2-4 hours | **Priority:** CRITICAL (system confusion, duplicate work risk)

## Problem Statement

**Bug:** 2026-07-03 11:11-11:12, automated process changed 5+ Conductor inbox messages from READ → INJECTED:
- MSG-CONDUCTOR-060, 063, 064, 065, 066
- All already DONE (outbox exists)
- Sequential file modification (2-10 sec apart = automated)
- Added `injected: 2026-07-03` field

**Impact:**
- Conductor received 9+ [TASK ASSIGNED] spam for already-complete tasks
- System confusion (terminals won't trust notifications)
- Risk of duplicate work if recurs

**Source:** Conductor MSG-CONDUCTOR-073 (detailed 200+ line analysis)

## Root Cause (Suspected)

Unknown automated process. Investigation shows:
- ✅ InboxWatcher: Only triggers on UNREAD (LINE 152-153) — NOT the bug
- ✅ SessionStarter `getInjectedMessages`: Only reads UNREAD — NOT the bug
- ❌ No code found writing `status: INJECTED` or `injected:` field
- ❌ No service log for 11:11-11:12 injection events

**Mystery:** How did these files get modified?

## Required Fixes (Conductor Recommendations)

### Fix #1: InboxWatcher Status Filter (CRITICAL)

**Location:** `spaceos-nexus/knowledge-service/src/inboxWatcher.ts`

**Change:** Add status guard before triggering [TASK ASSIGNED]:
```typescript
// LINE ~152 (before "Only emit wake-up events for UNREAD messages")
// PREVENT re-injection of already-processed messages
if (frontmatter.status === 'READ' || 
    frontmatter.status === 'COMPLETED' || 
    frontmatter.status === 'INJECTED' ||
    frontmatter.injected) {
  // Skip - already processed
  return;
}

// Existing: Only emit wake-up events for UNREAD messages
if (frontmatter.status !== 'UNREAD') return;
```

### Fix #2: Age-Based Filtering (HIGH)

**Location:** Same file, `handleInboxChange()` function

**Change:** Don't trigger on messages >7 days old:
```typescript
const messageAge = Date.now() - new Date(frontmatter.created).getTime();
const MAX_AGE_DAYS = 7;

if (messageAge > MAX_AGE_DAYS * 24 * 60 * 60 * 1000) {
  console.log(`[InboxWatcher] Skipping old message ${messageId} (${Math.round(messageAge / (24*60*60*1000))}d old)`);
  return;
}
```

### Fix #3: DONE Outbox Cross-Check (MEDIUM)

**Location:** Same file OR new helper function

**Change:** Before triggering, check if DONE outbox exists:
```typescript
// Helper function
async function hasDoneOutbox(terminal: string, messageId: string): Promise<boolean> {
  const outboxPath = path.join(TERMINALS_PATH, terminal, 'outbox');
  try {
    const files = await fs.readdir(outboxPath);
    return files.some(f => f.includes('done') && f.includes(messageId.toLowerCase()));
  } catch {
    return false;
  }
}

// In handleInboxChange:
const isDone = await hasDoneOutbox(terminal, messageId);
if (isDone) {
  console.log(`[InboxWatcher] Skipping ${messageId} - DONE outbox exists`);
  return;
}
```

## Testing Requirements

### Test Case #1: Old UNREAD Message
```yaml
status: UNREAD
created: 2026-06-15  # 18 days old
```
**Expected:** InboxWatcher IGNORES (too old)

### Test Case #2: Recent UNREAD, No DONE
```yaml
status: UNREAD
created: 2026-07-03  # today
```
**Expected:** InboxWatcher triggers session start ✅

### Test Case #3: READ Status, Recent
```yaml
status: READ
created: 2026-07-02  # 1 day old
```
**Expected:** InboxWatcher IGNORES (status=READ)

### Test Case #4: INJECTED Field Present
```yaml
status: UNREAD
injected: 2026-07-03
```
**Expected:** InboxWatcher IGNORES (already injected)

## Acceptance Criteria

- [ ] Fix #1 implemented (status filter)
- [ ] Fix #2 implemented (age filter)
- [ ] Fix #3 implemented (DONE outbox check)
- [ ] All 4 test cases pass
- [ ] Build: 0 TypeScript errors
- [ ] Service restart successful
- [ ] No [TASK ASSIGNED] spam for old messages
- [ ] Conductor inbox cleanup (move 15 READ/COMPLETED to archive)

## Deliverables

1. Modified `inboxWatcher.ts` with 3 fixes
2. Test verification (manual or unit test)
3. DONE outbox documenting fix + test results
4. Service restart confirmation

## Priority Justification

**CRITICAL** because:
- System trust degradation (terminals ignore notifications)
- Risk of duplicate work (Backend could redo CRM module)
- Already happened once (can recur)
- Conductor blocked waiting for fix

---

**Conductor report:** MSG-CONDUCTOR-073 (read it for full context!)
**Root approval:** MSG-CONDUCTOR-074
**Urgency:** Implement TODAY (2-4h timeline)
