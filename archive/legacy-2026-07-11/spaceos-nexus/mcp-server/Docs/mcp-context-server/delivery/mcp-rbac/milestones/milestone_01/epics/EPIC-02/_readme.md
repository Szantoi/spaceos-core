---
title: "EPIC-02: MCP Kliens Cache Limitáció Tesztelése"
type: epic
epic: EPIC-02
milestone: M01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
effort_estimate: M
depends_on: EPIC-01
---

# 🎯 EPIC-02: MCP Kliens Cache Limitáció Tesztelése

## Cél

Az `exp-mcp-rbac-constraints.md` kísérlet azonosított egy kulcskockázatot: az MCP kliensek (Claude Desktop, VS Code Copilot) cache-elik az eszközlistát. Ha a role session közben változik, a kliens nem biztos, hogy kap frissített tool listát. Ezt a limitációt ebben az epic-ben kell tesztelni és dokumentálni.

## Sikerességi Kritérium

- Dokumentált válasz: **session-init szintű RBAC** production-ready, de **futásidejű role-csere** nem (vagy csak reconnect után).
- Mitigation stratégia definiálva.

## Feladatok

- [x] **TASK-01**: Session közbeni role-csere tesztelése
  - MCP kliens csatlakozik → `explorer` role → lekéri a tool listát
  - Role megváltoztatása → `tech_lead` → lekéri a tool listát újra
  - Megfigyelt viselkedés dokumentálása
- [x] **TASK-02**: Mitigation stratégiák értékelése
  - A. Session újraindítás szükséges — dokumentálni, elfogadni
  - B. MCP `notifications/tools/list_changed` push értesítés (ha az SDK támogatja) vizsgálata
- [x] **TASK-03**: Döntés dokumentálása ADR-ban
  - `ADR-XXX-mcp-rbac-scope.md` létrehozása
