---
id: result-mcp-rbac
hypothesis: hyp-mcp-rbac-constraints
experiment: exp-002-rbac
status: success
date: 2026-02-27
---

# Kísérlet Eredménye: Dinamikus RBAC az MCP Eszközökre (MVE)

## Eredmény Összefoglaló
A kísérlet **sikeresen igazolta**, hogy az MCP szerver képes a hívó kontextusából (pl. `X-Agent-Role` HTTP header) kinyert szerepkör alapján dinamikusan szűrni a kiközölt eszközöket.

## Tényleges Eredmények
A `test-rbac-direct.ps1` futtatása az alábbi eredményt hozta:

- **Explorer Tools:** `get_pending_handoffs, get_task_status, list_projects`
- **Developer Tools:** `create_agent_epic, get_pending_handoffs, get_task_status, create_agent_project, create_agent_task, update_task_status, create_project, trigger_handoff, list_projects`

Mint látható, az Explorer szerepkör fizikailag nem volt képes felsorolni vagy hozzáférni a módosítást végző (`create_`, `update_`, `trigger_`) végpontokhoz.

## Visszabonthatósági jelentés
Az MVE teljesítette a "Reversible Prototype Rule"-t. A módosítás csupán a `McpService.cs` és `McpController.cs` fájlokban elhelyezett `#if PROTOTYPE_RBAC` / `#else` blokkokra és ideiglenes tesztszkriptekre korlátozódott, amik nem érintenek sem adatbázis sémát (EF módosítások), sem az operatív környezetet véglegesen.

## Konklúzió / Tanulságok
Az "Architecture Decision" ADR-011 helyes és megvalósítható. Ez fizikai védvonalat húz a hallucináló ágensek ellen.
Ezek az eredmények alátámasztják, hogy a Role Protocolok élesben (production) az MCP Tool Registry szintjén szigorúan betartathatóak.
