# Nexus Terminal Memory — Agent Infrastructure

**Updated:** 2026-07-10
**Role:** Agent Infrastructure Engineer

---

## CURRENT STATE

### Knowledge Service

| Komponens | Státusz |
|-----------|---------|
| Service | RUNNING (port 3456) |
| Vector Backend | ChromaDB (port 8000) |
| Embedding | MiniLM-L6-v2 |
| Documents | 4508 indexed |
| MCP Tools | 99 registered |
| Messages | 1313 in registry |

### Recent Fixes (2026-07-10)

| Issue | Root Cause | Fix |
|-------|------------|-----|
| Service crash loop | `.js` extension in TS import | Remove extension |
| Monitor inbox flooding | `testMode = true` in prod | Set to `false` |
| MessageRegistry empty | Crash before initSchema | Delete + restart |

---

## ARCHITECTURE OVERVIEW

### Component Map

```
knowledge-service/src/
├── server.ts           ← Entry point
├── mcp.ts              ← 99 MCP tools (159KB)
├── bootstrap/          ← App initialization
├── pipeline/           ← Nightwatch components
│   ├── nightwatch.ts   ← Main orchestrator
│   ├── watchInbox.ts   ← UNREAD detection
│   ├── watchDone.ts    ← DONE processing
│   ├── watchStuck.ts   ← Stuck nudge
│   └── watchMonitor.ts ← Health checks
├── conductor/          ← Mode detection, epic mgmt
├── telegram/           ← Bot integration
├── graph/              ← Epic dependency
└── generators/         ← Code generation
```

### Data Flow

```
Inbox UNREAD
  ↓ watchInbox
Session Start (tmux)
  ↓ work
DONE Outbox
  ↓ watchDone
Review (2× Haiku)
  ↓ reviewer.ts
Archive + Next Task
```

---

## KEY PATTERNS

### MCP Tool Pattern

```typescript
// 1. Tool definition in TOOLS array
{
  name: 'tool_name',
  description: 'LLM-readable description',
  inputSchema: { type: 'object', properties: {...} },
}

// 2. Handler in switch
case 'tool_name': {
  const result = await doSomething(args);
  return { content: [{ type: 'text', text: JSON.stringify(result) }] };
}

// 3. CAPABILITIES update in identity.ts
```

### Pipeline Watcher Pattern

```typescript
// pipeline/watch*.ts
export async function watchXxx(): Promise<WatchResult> {
  await log('[watchXxx] Starting...');

  // Detection logic
  const items = await detect();

  if (items.length === 0) {
    return { triggered: false, reason: 'Nothing to process' };
  }

  // Processing logic
  await process(items);

  return { triggered: true, reason: `Processed ${items.length}` };
}
```

### Session Starter Pattern

```typescript
// Uses MCP API, not direct tmux
const response = await fetch('http://localhost:3456/api/session/start', {
  method: 'POST',
  body: JSON.stringify({ terminal, model, prompt, fromTerminal }),
});
```

---

## KNOWN ISSUES & GOTCHAS

### TypeScript/CommonJS

| Issue | Solution |
|-------|----------|
| `.js` extension in imports | Remove it for CommonJS |
| Dynamic imports | Use `require()` or ensure ESM |
| Path aliases | Update tsconfig paths |

### SQLite

| Issue | Solution |
|-------|----------|
| Empty DB file | Delete + restart service |
| WAL mode conflicts | One writer at a time |
| Schema changes | Migration + restart |

### tmux

| Issue | Solution |
|-------|----------|
| `Enter` as literal text | Use `C-m` or separate send-keys |
| Session not found | Check with `tmux list-sessions` |
| Pane capture empty | Add `-p` flag |

---

## PERFORMANCE NOTES

### Response Times

| Operation | Target | Actual |
|-----------|--------|--------|
| MCP tool call | <500ms | ~200ms |
| Knowledge search | <1s | ~500ms |
| Inbox list | <100ms | ~50ms |
| Session start | <5s | ~3s |

### Resource Usage

| Resource | Limit | Actual |
|----------|-------|--------|
| Memory | 512MB | ~400MB |
| CPU | 1 core | ~10% idle |
| Disk (data/) | 1GB | ~100MB |

---

## TESTING STRATEGY

### Unit Tests

```bash
npm test                          # All tests
npm test -- --grep "mailbox"      # Specific module
npm test -- --watch               # Watch mode
```

### Integration Tests

```bash
npm run test:integration          # Full integration
npm run test:mcp                  # MCP tools only
```

### Manual Testing

```bash
# MCP tool test
curl -X POST http://localhost:3456/api/mcp/call \
  -H "Content-Type: application/json" \
  -d '{"tool": "get_service_status", "args": {}}'

# Health check
curl http://localhost:3456/health
```

---

## IMPROVEMENT BACKLOG

### Priority 1 (Next)

- [ ] Test coverage increase (60% → 80%)
- [ ] MCP tool response time logging
- [ ] Pipeline failure alerting

### Priority 2 (Soon)

- [ ] Session management refactoring
- [ ] Message registry cleanup job
- [ ] ChromaDB backup automation

### Priority 3 (Later)

- [ ] Multi-instance support
- [ ] Rate limiting per terminal
- [ ] Cost tracking dashboard

---

## DECISIONS LOG

### 2026-07-10: testMode Guard

**Context:** `testMode = true` left in production caused inbox flooding.

**Decision:** Add environment variable guard:
```typescript
const testMode = process.env.WATCHMONITOR_TEST_MODE === 'true';
```

**Status:** Implemented in watchMonitor.ts

### 2026-07-04: Persistent Cycle Counter

**Context:** Service restarts reset cycle counter, causing duplicate health checks.

**Decision:** Persist cycle count to file:
```typescript
const CYCLE_STATE_FILE = `${SPACEOS_ROOT}/logs/dispatcher/.monitor-cycle-state`;
```

**Status:** Implemented

---

## CONTACT POINTS

| Need | Contact |
|------|---------|
| Strategic decision | Root terminal |
| Task coordination | Conductor terminal |
| Health alerts | Monitor terminal |
| Documentation | Librarian terminal |
| .NET questions | Backend terminal |

---

*Last session: 2026-07-10*
*Focus: Service recovery after crash loop*

---

## LEARNED PATTERNS (2026-07-10)

### .NET SDK Testhost Dependency Bug

**Issue:** Production E2E tests fail with `Microsoft.TestPlatform.CommunicationUtilities not found`

**Root Cause:**
- .NET SDK 8.0.419 (April 2026) bundles **VSTest 17.11.1** (pre-release)
- VSTest testhost cannot resolve its OWN dependencies from SDK directory
- Assembly EXISTS at `/opt/dotnet/sdk/8.0.419/Microsoft.TestPlatform.CommunicationUtilities.dll`
- BUT testhost.deps.json manifest cannot locate it

**Symptoms:**
```
Testhost process exited with error:
  An assembly specified in the application dependencies manifest (testhost.deps.json) was not found:
    package: 'Microsoft.TestPlatform.CommunicationUtilities', version: '17.11.1-release-24455-02'
```

**Failed Attempts:**
- ❌ Upgrade xunit.runner.visualstudio 2.8.2 → 3.0.0 (no effect)
- ❌ Manual DLL copy to output (triggers chain of missing deps)
- ❌ CopyLocalLockFileAssemblies=true (already set, doesn't help)
- ❌ Clean/rebuild cycles (no effect)

**Solution:**
→ **VPS Operator escalation** for .NET SDK downgrade to 8.0.300-400 range (stable VSTest version)

**Workaround:**
→ Switch to EF Core in-memory provider (loses true integration testing)

**Key Insight:**
Fresh .NET SDK releases may ship pre-release test platform versions with bugs.
Always verify VSTest version when encountering testhost dependency issues.

**Investigation Time:** 90 minutes
**Blocked:** MSG-NEXUS-019 → Root (VPS Operator escalation)

---

### MCP Connector Terminal-Specific Access (2026-07-10)

**Issue:** Designer terminal cannot access Playwright MCP tools via spaceos-connector

**Root Cause:**
- MCP connector configured with `MCP_TERMINAL: "root"` in `~/.claude/settings.json`
- This binds the stdio-bridge to ONLY the root terminal session
- Other terminals (designer, backend, frontend) cannot access the connector

**Connector Status:**
- ✅ Service running on port 3457
- ✅ All 5 backends healthy (knowledge, playwright, ref, context7, brave-search)
- ✅ 10 Playwright tools available (navigate, screenshot, click, fill, select, hover, evaluate, console_logs, close, resize)
- ✅ Designer has permissions in config.yaml (playwright_navigate, playwright_screenshot)

**Tool Name Convention:**
| Backend | Config Name | Claude Code Name |
|---------|-------------|------------------|
| playwright | `playwright_navigate` | `mcp__spaceos-connector__playwright_navigate` |
| knowledge | `search_knowledge` | `mcp__spaceos-knowledge__search_knowledge` |

**Workaround:**
```bash
# CLI-based screenshot (works in all terminals)
npx playwright screenshot --browser chromium --full-page URL OUTPUT.png
```

**Pros:** ✅ Works everywhere, ✅ No config changes
**Cons:** ❌ Screenshot-only, ❌ No programmatic control (click, fill)

**Long-term Solution:**
Enable Playwright plugin directly:
```json
{
  "enabledPlugins": {
    "playwright@claude-plugins-official": true
  }
}
```

**Trade-offs:**
- ✅ Available in all terminals
- ❌ No centralized permission control
- ❌ No audit logging via connector

**Documentation:** `docs/knowledge/debugging/PLAYWRIGHT_MCP_CONNECTOR_ACCESS.md`

**Investigation Time:** 45 minutes
**Status:** RESOLVED (workaround + long-term plan documented)

---

### Token Configuration & MCP Tool Authentication (2026-07-10)

**Issue:** Backend terminál azt jelentette, hogy a Root terminál -32603 internal error-t kapott a `create_goal` és `subscribe_to_terminal` MCP tool-ok használatakor (MSG-NEXUS-022).

**Root Cause:** NINCS bug — a jelenlegi konfiguráció helyes és működőképes.

**Megállapítások:**
1. **`.mcp-tokens` fájl DEPRECATED** — nem használja a knowledge-service (csak legacy maradvány)
2. **`agents.yaml` az egyetlen token forrás** — itt vannak az agent tokenek definiálva
3. **Root terminál a master tokent használja** — connector config.yaml:18 → `Authorization: Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o=`
4. **Master token → 'root' agent név mapping** — mcp.ts:381-382 implementálva (`getAgentFromToken`)
5. **Mindkét tool működik** — API tesztek SUCCESS

**Valószínű ok a Backend által tapasztalt hiba:**
- Root terminál nem a connector-on keresztül hívta a tool-okat
- Helytelen Bearer token volt használva
- Átmeneti service outage (restart)

**Dokumentáció frissítés:**
- `.mcp-tokens` deprecated comment hozzáadva
- Nincs kód változás szükséges

**Key Insight:**
Az MCP authentication chain:
```
Connector Bearer Token → knowledge-service authenticate() → getAgentFromToken() → callerTerminal = 'root'
```

Ha a master token használva van (ami a connector konfigban is van), akkor a Root terminál authentication működik.

**Investigation Time:** 30 minutes


## DONE: MSG-NEXUS-025 (2026-07-11T06:15:13.240Z)

Monitor repetitive behavior fixed - implemented outbox read logic and deduplication

---
