---
title: "Project State: MCP RBAC Tool Constraints"
type: project-state
project: mcp-rbac
track: delivery
updated: 2026-02-26
mode: test-operation
rollback_safe: true
origin_discovery: docs/joinerytech-flow/discovery/meta-security/03_prototype/experiments/exp-mcp-rbac-constraints.md
fsm_state: "CLOSED_DONE"
---

# 📊 Project State: MCP RBAC Tool Constraints

> [!IMPORTANT]
> Ez egy **Teszt Üzem** (Test-Operation) projekt. Célja a Discovery fázisban azonosított `exp-mcp-rbac-constraints` kísérlet implementálása visszabontható formában. A fejlesztés egyetlen követelménye: **a meglévő funkciók sérülése nélkül telepíthető és eltávolítható legyen.**

## Projekt Célja

Az `exp-mcp-rbac-constraints` kísérlet igazolta, hogy szükség van az MCP szerver eszközlistájának (tool registry) role-alapú dinamikus szűrésére. Ez megakadályozza, hogy egy alsóbb jogosultságú agent (pl. `explorer`) hozzáférjen magasabb privilégiumú eszközökhöz (pl. `write_to_file`).

Ez a projekt azt az architektúrát valósítja meg, ahol az MCP szerver a hívó kliens által megadott `role` kontextus alapján korlátozza a visszaadott eszközlistát.

**Forrás kísérlet:** [`exp-mcp-rbac-constraints.md`](../../Plans/Discoveries/agent-system-v2/meta-security/03_prototype/experiments/exp-mcp-rbac-constraints.md)

## Visszabonthatóság (Rollback Safety)

A projekt **izolált komponensekből** áll:

- `RbacFilter.ts` — önálló service, csak az MCP server hivja
- MCP szerver — a role-szűrő logika egy komment-jelölt blokk, törölhető
- `constraints.md` fájlok a role schema-kban — opcionális kiterjesztés
- Nincs adatbázis séma módosítás

## Fő Kockázat

> [!WARNING]
> Az MCP kliensek (pl. Claude Desktop, VS Code Copilot) **induláskor cache-elik** az elérhető eszközlistát. Ha a role futásidőben változik, a kliens nem feltétlenül kap frissített tool listát kapcsolatbontás nélkül. **Ezt a limitációt a kísérletnek explicit tesztelnie kell.**

## Milestone Progress

| Milestone | Epics | Kész | Státusz |
|:----------|:------|:-----|:--------|
| M01: RBAC Filter Service & MCP Integration | EPIC-01, EPIC-02 | 2/2 | 🟢 Kész |

## Következő Lépések

- [x] EPIC-01: `RbacFilter` service implementálása és MCP integráció
- [x] EPIC-02: MCP kliens cache-elés tesztelése és limitáció dokumentálása
