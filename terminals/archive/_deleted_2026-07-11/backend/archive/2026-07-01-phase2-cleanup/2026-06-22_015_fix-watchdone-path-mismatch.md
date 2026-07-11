---
id: MSG-BACKEND-015
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-008
created: 2026-06-22
content_hash: fbf41fa0eff515d2b74142785a1548c4d076c56b22b0182f0fb37bdf1ed5ffa9
---

# Fix watchDone.ts Path Mismatch (DONE Reviewer Pipeline)

## Problem

The **nightwatch DONE reviewer pipeline** is NOT processing DONE messages because `watchDone.ts` uses the **old path** (`docs/mailbox/*/outbox/`) instead of the **new 7-terminal architecture path** (`terminals/*/outbox/`).

**Impact:** 2 DONE messages stuck in UNREAD state.

## Task

Modify `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts`:

```diff
async function findUnreadDones(): Promise<string[]> {
  const dones: string[] = [];
- const mailboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox');
+ const mailboxPath = path.join(SPACEOS_ROOT, 'terminals');

  try {
    const terminals = await fs.readdir(mailboxPath);

    for (const terminal of terminals) {
      const outboxPath = path.join(mailboxPath, terminal, 'outbox');
      // ... rest unchanged
```

## Requirements

1. ✅ Modify watchDone.ts line ~18: `docs/mailbox` → `terminals`
2. ✅ Test: `npm run build` in knowledge-service
3. ✅ Restart knowledge-service: `pm2 restart spaceos-nexus`
4. ✅ Verify: DONE detection works (check next DONE message is processed)

## Timeline

**Effort:** 15-30 min

---

**Escalated from:** MSG-CONDUCTOR-008 (BLOCKED)
**Reason:** Conductor cannot modify code (per CLAUDE.md)
