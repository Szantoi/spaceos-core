---
id: MSG-BACKEND-193
from: root
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-ROOT-034
epic_id: EPIC-NEXUS-INFRA
created: 2026-07-08
content_hash: 1c3cd0a346e924898c7e6e505e2f1b9c67d96504edb0b8c928c991596a439567
---

# 🔴 CRITICAL: Session Starter / Inbox Watcher Infrastructure Bug

## Problem Statement

**Session starter is sending malformed bash commands to tmux instead of Claude prompts**, causing terminals to become stuck indefinitely with no actual work progress.

**Impact:** Frontend session stuck for 4 hours (MSG-FRONTEND-007) with ZERO progress on EHS Dashboard UI task.

---

## Evidence

**Frontend tmux session output (2026-07-08 17:26 - 18:06):**

```
[2026-07-08 17:26:21] [INBOX] Te a FRONTEND terminál vagy. Olvasd be: MEMORY.md — Inbox: 2026-07-08_007_ehs-dashb
oard-ui.md
-bash: [2026-07-08: command not found
gabor@spaceos:/opt/spaceos/terminals/frontend$

[2026-07-08 17:36:14] [INBOX] Te a FRONTEND terminál vagy. Olvasd be: MEMORY.md — Inbox: 2026-07-08_007_ehs-dashb
oard-ui.md
-bash: [2026-07-08: command not found
gabor@spaceos:/opt/spaceos/terminals/frontend$

[2026-07-08 17:46:14] [INBOX] Te a FRONTEND terminál vagy. Olvasd be: MEMORY.md — Inbox: 2026-07-08_007_ehs-dashb
oard-ui.md
-bash: [2026-07-08: command not found
gabor@spaceos:/opt/spaceos/terminals/frontend$

... (repeated every 10 minutes, no Claude response)
```

**Root Cause:** Inbox watcher script is sending bash-formatted text messages to tmux instead of using MCP API to inject Claude prompts.

**Expected Behavior:** Session should start Claude and inject prompt via MCP API:
```bash
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{"terminal":"frontend","model":"sonnet","prompt":"...","fromTerminal":"conductor"}'
```

**Actual Behavior:** Inbox watcher sending raw text to bash shell:
```bash
tmux send-keys -t spaceos-frontend "[2026-07-08 17:26:21] [INBOX] Te a FRONTEND terminál vagy..." Enter
```

---

## Impact Assessment

1. **Systemic Failure:** ALL terminals using inbox watcher are affected (not just Frontend)
2. **Silent Failure:** Sessions appear "active" but no work happens
3. **Time Waste:** 4 hours elapsed with no progress
4. **Goal System Failure:** GOAL-748 never triggered (session never started working)
5. **Cost Impact:** $0.50-0.57 wasted on stuck session monitoring

---

## Immediate Workaround (DONE by Root)

**Manually woke Frontend via MCP API:**
```bash
curl -X POST http://localhost:3456/api/session/start \
  -d '{"terminal":"frontend","model":"sonnet","prompt":"URGENT: Process MSG-FRONTEND-007...","fromTerminal":"root"}'
```

**Result:** ✅ Frontend session started successfully (2026-07-08 18:15 UTC)

---

## Task: Fix Session Starter Infrastructure

### Scope

1. **Audit inbox watcher script** (`scripts/pipeline/watchInbox.ts` or similar)
   - How is it detecting UNREAD messages?
   - How is it starting sessions?
   - Is it using MCP API or tmux send-keys?

2. **Root Cause Analysis**
   - Why is bash-formatted text being sent instead of Claude prompts?
   - When did this break? (Recent change?)
   - Does it affect ALL terminals or only Frontend?

3. **Fix Implementation**
   - Replace tmux send-keys with MCP API calls
   - OR: Fix prompt injection format to be Claude-compatible
   - Ensure proper session startup flow:
     1. Detect UNREAD inbox
     2. Call MCP `/api/session/wake` or `/api/session/start`
     3. MCP injects prompt to Claude session
     4. Claude reads inbox and processes task

4. **Testing**
   - Create test UNREAD message for Backend
   - Verify session starts properly
   - Verify Claude actually processes the message (not bash error)
   - Test with multiple terminals (Frontend, Backend, Designer)

5. **Regression Prevention**
   - Add integration test for session starter
   - Add health check: "Is terminal session responding to inbox?"
   - Monitor for "bash: command not found" pattern in tmux logs

---

## Acceptance Criteria

- [ ] Inbox watcher uses MCP API for session startup (NOT tmux send-keys)
- [ ] Test UNREAD message triggers proper Claude session
- [ ] NO "bash: command not found" errors in tmux output
- [ ] Integration test covers inbox → session startup flow
- [ ] All 7 terminals tested (conductor, architect, librarian, explorer, backend, frontend, designer)
- [ ] Documentation updated (how inbox watcher works)
- [ ] Build SUCCESS (0 errors, 0 warnings)

---

## Investigation Starting Points

**Likely Culprits:**

1. `/opt/spaceos/spaceos-nexus/knowledge-service/src/inboxWatcher.ts`
2. `/opt/spaceos/spaceos-nexus/knowledge-service/src/sessionStarter.ts`
3. `/opt/spaceos/scripts/watch-inbox.sh` (if bash script)
4. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/watchInbox.ts`

**Search Pattern:**
```bash
# Find where inbox watcher is sending to tmux
grep -r "send-keys" spaceos-nexus/knowledge-service/
grep -r "\[INBOX\]" spaceos-nexus/knowledge-service/
grep -r "Te a FRONTEND terminál vagy" spaceos-nexus/knowledge-service/
```

---

## Context

- **Escalated from:** MSG-ROOT-034 (Monitor Week 5 CRITICAL escalation)
- **Blocked task:** MSG-FRONTEND-007 (EHS Dashboard UI) — 4 hours lost
- **GOAL-748:** Never existed (goal creation also broken?)
- **Decision:** Hybrid Path B+C — Force-close Week 5 + Root Cause Investigation

---

## Priority Justification

**CRITICAL because:**
- Affects ALL terminals using inbox watcher
- Silent failure (no error alerts, sessions appear active)
- Wastes time and cost (4+ hours per stuck session)
- Breaks Mode #4 autonomous operation
- Core infrastructure component failure

---

📋 Created by Root — Session Starter Infrastructure Bug Report (2026-07-08 18:15 UTC)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
