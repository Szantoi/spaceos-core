---
title: "TASK-02: Mitigation Stratégiák Értékelése a Cache Limitációra"
type: task
task: TASK-02
epic: EPIC-02
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
depends_on: EPIC-02/TASK-01
---

# 📋 TASK-02: Mitigation Stratégiák Értékelése a Cache Limitációra

## Leírás

Ha a TASK-01 kiderítette, hogy az MCP kliens cache-elés miatt a futásidejű tool lista frissítés nem garantált, mitigation stratégiákat kell értékelni és döntést hozni.

### Értékelendő Stratégiák

| Stratégia | Leírás | Pro | Con |
|:----------|:-------|:----|:----|
| **A — Session Újraindítás** | Role-váltáskor a kliens session-t újraindítjuk | Egyszerű, megbízható | Felhasználói élmény megszakad |
| **B — MCP Notifications** | `notifications/tools/list_changed` push küldése az SDK-val | Automatikus, ha az SDK támogatja | Nem minden kliens implementálja |
| **C — Session-Init Only RBAC** | A role csak az első csatlakozáskor rögzíthető | Legegyszerűbb implementáció | Rugalmatlan — nincs futásidejű role-csere |

### Döntési Kritérium

- Az MCP SDK (`@modelcontextprotocol/sdk`) támogatja-e a `notifications/tools/list_changed` küldést?
- Ha igen → **B stratégia** a javasolt
- Ha nem → **C stratégia** a javasolt, **A stratégia** mint workaround dokumentálva

## Elfogadási Kritériumok

- [x] A 3 stratégia értékelve és a döntés dokumentálva
- [x] Az MCP SDK notifications támogatás ellenőrizve (kód vagy dokumentáció alapján)
- [x] A választott mitigation stratégia leírva — `implementation_decision.md` fájlban vagy ADR-ban

## Megvalósítási összefoglaló
Az értékelés alapján a **C stratégia** (Session-Init Only RBAC) lett a kiválasztott. A push notifications bár az SDK-ban technikailag megoldható, a kliensoldali kliensek implementációja eltérő, amire nem alapozhatunk. Ezért az architektúra szempontjából hivatalosan is támogatott és letisztult megoldás, hogy szerepkörváltásnál új inicializáció és session indítása az elvárt folyamat. Ezt rögzítettük ADR dokumentumban (ADR-009).
