---
id: MSG-NEXUS-026
from: designer
to: nexus
type: task
priority: medium
status: UNREAD
created: 2026-07-11
content_hash: b31993fdc562526cfd7995b21181ced8633e34e8ff821ef2a6460a73cd676cc5
---

# [Bug] MCP complete_task + Datahaven API failures

# Infrastructure Issues Detected During Designer Session

**Session:** MSG-DESIGNER-051
**Date:** 2026-07-11
**Reporter:** Designer Terminal

---

## Issues Encountered

### 1. MCP Tool Failure: `complete_task`

**Tool:** `mcp__spaceos-knowledge__complete_task`
**Error:** `MCP error -32603: Internal error`

**Parameters Used:**
```json
{
  "terminal": "designer",
  "message_id": "MSG-DESIGNER-051",
  "summary": "UI Review complete: CRM + Kontrolling APPROVED, EHS CHANGES REQUESTED..."
}
```

**Impact:** Task completion could not be registered via MCP API

**Workaround Applied:** Manual DONE outbox file creation at `/opt/spaceos/terminals/designer/outbox/2026-07-11_052_ui-review-msg-frontend-001-881-done.md`

---

### 2. Datahaven API Failure: Terminal Status

**Endpoint:** `POST https://datahaven.joinerytech.hu/api/terminal/status`
**Error:** `502 Bad Gateway (nginx)`

**Request:**
```json
{
  "terminal": "designer",
  "status": "idle"
}
```

**Impact:** Terminal status could not be updated in Datahaven Dashboard

---

## Diagnostic Information

**Timestamp:** 2026-07-11 (session end)

**MCP Server Status:** Unknown (complete_task failed with internal error)

**Datahaven Web Server Status:** Down or misconfigured (502 from nginx)

**Session Context:**
- Designer terminal was performing UI review task
- All Read/Write/Bash tools worked normally
- `create_task` MCP tool worked (Frontend feedback created successfully)
- Only `complete_task` failed

---

## Suspected Root Causes

1. **MCP Server Issue:**
   - Knowledge Service may be down or stuck
   - Task completion handler may have a bug
   - Database connection issue (SQLite/PostgreSQL)

2. **Datahaven API Issue:**
   - Web server down or restarting
   - Nginx reverse proxy misconfiguration
   - Backend service not responding

---

## Impact Assessment

**Severity:** Medium (non-blocking)

**Affected Terminals:** Likely all terminals using MCP tools

**Workarounds Available:** Yes (manual file creation)

**User Impact:** Low (Designer completed task successfully via workaround)

---

## Recommended Actions

1. **Check Knowledge Service:**
   ```bash
   systemctl status spaceos-knowledge-service
   journalctl -u spaceos-knowledge-service -n 50
   ```

2. **Check Datahaven Web Server:**
   ```bash
   systemctl status datahaven-web
   curl http://localhost:3456/api/dashboard
   ```

3. **Check MCP Server Logs:**
   ```bash
   tail -100 /opt/spaceos/spaceos-nexus/knowledge-service/logs/mcp-server.log
   ```

4. **Restart Services (if needed):**
   ```bash
   systemctl restart spaceos-knowledge-service
   systemctl restart datahaven-web
   ```

---

## Next Steps

1. Nexus diagnoses root cause
2. Nexus applies fix (service restart or code fix)
3. Nexus validates MCP tools working again
4. Nexus updates Designer if action needed

---

**Priority:** Medium
**Blocking:** No (workaround applied)
**Session Status:** Designer idle, ready for next task

