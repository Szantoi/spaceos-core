---
title: "Milestone 01: RBAC Filter Service & MCP Integration"
type: milestone
milestone: M01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
---

# 🚩 Milestone 01: RBAC Filter Service & MCP Integration

Ez a mérföldkő megvalósítja a role-alapú eszközszűrést az MCP szerveren belül, és teszteli az MCP kliens cache-elési limitációit.

## Epic Lista

1. **EPIC-01: RbacFilter Service & MCP Tool Registry Szűrés**
   - `RbacFilter.ts` service implementálása
   - MCP szerverben role-kontextus olvasása (pl. `x-active-role` HTTP header vagy init param)
   - Tool lista dinamikus szűrése az aktív role alapján
   - `role.schema.yaml` fájlok `mcp_tool_permissions` blokkjának definiálása
   - E2E teszt: két lekérdezés (`explorer` vs `tech_lead`) — az `explorer` ne lássa a `write_to_file`-t

2. **EPIC-02: MCP Kliens Cache Limitáció Tesztelése**
   - Teszt: session közbeni role-csere — megkapja-e a kliens az új tool listát?
   - A limitáció dokumentálása és mitigation stratégia (pl. session újraindítás, graceful reconnect)
   - Végső döntés: production-ready-e az RBAC, vagy csak session-init szinten alkalmazható?
