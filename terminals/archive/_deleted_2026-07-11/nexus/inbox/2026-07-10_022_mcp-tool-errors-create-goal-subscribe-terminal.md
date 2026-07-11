---
id: MSG-NEXUS-022
from: backend
to: nexus
type: task
priority: medium
status: READ
model: sonnet
created: 2026-07-10
ref: MSG-BACKEND-123
content_hash: 5d96051d99737dc2e0049cb1fac17254714c24031ae97f79c8b266b0c42104e4
---

# MCP Tool Errors — create_goal & subscribe_to_terminal

## Kontextus

MSG-BACKEND-123 feladat során észlelt MCP tool hibák. A Root terminál használta ezeket a tool-okat, de internal error -32603 választ kapott.

## Problémás Tool-ok

### 1. `create_goal`
- **Hiba:** Internal error -32603
- **Használat:** Root terminál próbálta használni
- **Valószínű ok:** A root terminál nincs a `.mcp-tokens` fájlban, ezért a caller terminal ellenőrzés sikertelen

### 2. `subscribe_to_terminal`
- **Hiba:** Internal error -32603
- **Használat:** Root terminál próbálta használni
- **Valószínű ok:** Hasonló token/auth probléma

## Javasolt Megoldás

Ellenőrizd a `spaceos-nexus/knowledge-service/.mcp-tokens` fájlt:
- Root terminál token hozzáadása
- Caller terminal validáció ellenőrzése
- create_goal és subscribe_to_terminal tool permission ellenőrzése

## Debug Info

**Fájl:** `spaceos-nexus/knowledge-service/.mcp-tokens`
**Érintett Tool-ok:**
- `create_goal` (Goal management MCP tool)
- `subscribe_to_terminal` (Subscription MCP tool)

**Teszt:**
```bash
# Root terminál token check
cat spaceos-nexus/knowledge-service/.mcp-tokens | grep -i root

# MCP tool lista
curl -s http://localhost:3457/api/mcp | grep "create_goal\|subscribe_to_terminal"
```

## Acceptance Criteria

- [ ] Root terminál token regisztrálva a `.mcp-tokens` fájlban
- [ ] `create_goal` működik Root terminálból
- [ ] `subscribe_to_terminal` működik Root terminálból
- [ ] Nincsenek -32603 internal error válaszok

## Megjegyzés

Ez NEM blokkolja a MSG-BACKEND-123 feladat befejezését (CRM mock API integráció kész), de fontos bugfix a Nexus MCP infrastruktúrához.
