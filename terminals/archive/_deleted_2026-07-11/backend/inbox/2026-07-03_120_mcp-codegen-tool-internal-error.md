---
id: MSG-BACKEND-120
from: root
to: backend
type: task
priority: low
status: READ
read_at: 2026-07-03
model: haiku
created: 2026-07-03
---

# MCP Codegen Tools - Internal Error Investigation

**Státusz:** Low priority (non-blocking - fallback működik)

## Probléma

Frontend (MSG-FRONTEND-099) próbálta használni az MCP codegen tool-okat:
- `mcp__spaceos-knowledge__generate_component`
- Error: "MCP error -32603: Internal error"
- Fallback: bash script (`/opt/spaceos/scripts/codegen/generate-component.sh`) **működik ✅**

## Jelenlegi Állapot

**MCP Tool Registration:** ✅ OK
- mcp.ts LINE 1697: `generate_component` tool definition
- mcp.ts LINE 3489: handler implementation
- mcp.ts LINE 111: import from `./codegen/index`

**Codegen Module:** ✅ Built
- src/codegen/codegenEngine.ts - engine implementation
- src/codegen/index.ts - exports
- dist/codegen/*.js - compiled  

**Bash Scripts:** ✅ Working
- `/opt/spaceos/scripts/codegen/generate-component.sh` működik
- Frontend sikeresen használja fallback-ként

## Vizsgálandó

1. **Error handling**: MCP handler-ben nincs try/catch, throw-olt exception nem logolódik
2. **Spawn execution**: codegenEngine.ts spawn() call lehet hogy MCP context-ben fail-el
3. **Permissions**: Script execute permission OK (`-rwx--x---`), de lehet MCP user/group issue

## Megoldási Javaslat

**MOST:** Nincs sürgős teendő, Frontend dolgozik bash script-tel (user preference szerint is ez a jobb).

**KÉSŐBB (ha idő van):**
1. Add try/catch + console.error to mcp.ts generate_component handler
2. Test direct MCP tool call: `mcp__spaceos-knowledge__generate_component name: "TestComponent"`
3. Check service logs for actual stack trace
4. Ha permission issue → spawn() config tweak vagy sudo wrapper

## Priority

**LOW** - Bash fallback működik, Frontend nem blokkolt, user is így preferálja.

---

**Jelzés:** Root session 2026-07-03 12:14 - Frontend monitoring közben észrevéve.
