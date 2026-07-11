---
id: MSG-NEXUS-021
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-07-10
content_hash: 535c2c6c786356763f583123fef5fced59f7dc2147b6cd583532612a36fdc00d
---

# Fix MCP Tool Authentication Errors

## Probléma

Több MCP tool "Internal error -32603" hibát dob, valószínűleg az autentikáció/jogosultság ellenőrzés miatt.

### Hibás tool-ok
- `create_goal`
- `subscribe_to_terminal`
- `subscribe_to_task`
- `create_task`

### Hibaüzenet
```
MCP error -32603: Internal error:
```

## Diagnózis

### 1. Token hiány
A `.mcp-tokens` fájlban csak 3 terminál van:
```
monitor:eL4LWZ1K...
chat-root:Lv/II7NP...
nexus:RwybOC7o...
```

A **root** terminál NINCS benne! Ezért a caller terminal ellenőrzés sikertelen.

### 2. mcp.ts jogosultság ellenőrzés
```typescript
// mcp.ts:5039-5047
case 'create_goal': {
  if (callerTerminal && callerTerminal !== 'root' && callerTerminal !== 'conductor') {
    return { error: `Terminal ${callerTerminal} cannot create goals...` };
  }
  // ...
}
```

Ha a callerTerminal `undefined` vagy nem azonosítható, a hiba "Internal error" lesz a catch blokkban.

## Megoldás

### 1. Root token hozzáadása
```bash
# .mcp-tokens
root:<generated-token>
```

### 2. Graceful error handling
Ha a caller terminal nem azonosítható, ne dobjon Internal error-t, hanem értelmes hibaüzenetet:

```typescript
case 'create_goal': {
  if (!callerTerminal) {
    return {
      content: [{ type: 'text', text: JSON.stringify({
        success: false,
        error: 'Caller terminal not identified. Check MCP token configuration.'
      }, null, 2) }],
    };
  }
  // ...
}
```

## Fájlok

- `spaceos-nexus/knowledge-service/.mcp-tokens` - token hozzáadás
- `spaceos-nexus/knowledge-service/src/mcp.ts` - error handling javítás

## Acceptance Criteria

- [ ] Root terminál token hozzáadva `.mcp-tokens`-hez
- [ ] `create_goal` működik root terminálból
- [ ] `subscribe_to_terminal` működik
- [ ] `create_task` működik root-ból
- [ ] Hibás hívások értelmes error message-t adnak, nem "Internal error"

## Teszt

```bash
# MCP tool tesztelése
curl -X POST http://localhost:3456/api/mcp/call \
  -H "Content-Type: application/json" \
  -d '{"tool": "create_goal", "args": {"created_by": "root", "description": "Test", "completion_criteria": [], "trigger_terminal": "conductor", "prompt": "Test"}}'
```

---

## ADDITIONAL ISSUE (2026-07-10 23:50) — MCP DB Sync Lag

**Ref:** MSG-ROOT-101

A Monitor jelentette, hogy az MCP backend DB nincs szinkronban a filesystem-mel:

```
MCP fetch fails: "Task MSG-MONITOR-094 is not assigned to terminal monitor"
Filesystem OK: Outbox report exists ✅
```

**Likely Cause:** Task table foreign key constraints fail because inbox files aren't synced to DB.

**Suggested Fix:**
1. Root token hozzáadása (fő probléma)
2. Task index rebuild szkript: `scripts/mcp/rebuild-task-index.sh`
3. Vagy auto-sync a `watchInbox.ts`-ben filesystem → DB
