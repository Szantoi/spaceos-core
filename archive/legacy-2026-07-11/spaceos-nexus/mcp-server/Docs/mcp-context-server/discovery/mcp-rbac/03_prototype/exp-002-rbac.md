---
id: exp-002-rbac
title: "Kísérleti Terv: Dinamikus RBAC (Role-Based Access Control) az MCP Eszközökre (MVE)"
status: draft
date: 2026-02-27
hypothesis: hyp-mcp-rbac-constraints
adr: ADR-011
---

# Kísérleti Terv: Dinamikus RBAC az MCP Eszközökre (MVE)

## Cél
Bebizonyítani, hogy az MCP szerver (`JoineryTech.Flow.Api`) képes kontextusfüggően – az éppen futó vagy paraméterben kapott Role alapján – dinamikusan szűrni a kliens számára elérhetővé tett eszközök (`tools`) listáját. Az infrastruktúra védelme fizikai gátakkal, ne csak prompt-utasításokkal történjen.

## Minimum Viable Experiment (MVE) definíció
1.  **Szűkített hatókör:** Csak a `list_tools` végpont, vagy az azt kiszolgáló Tool Registry logika módosítása a szerveroldalon (C#).
2.  **Kliens kontextus azonosítása:** A HTTP/JSON-RPC hívásba (pl. headerbe: `X-Agent-Role`) beiktatunk egy dummy értéket, hogy szimuláljuk az aktuális ágens szerepkörét (pl. `Explorer` vs `Developer`).
3.  **Prototípus megvalósítás:** A tool lista betöltése közben egy ideiglenes logika (hardcode-olt FSM/szabály) kiszűri a "veszélyes" (írási műveleteket végző) eszközöket, ha a ráküldött role = `Explorer`.
4.  **Visszabonthatóság (Reversible Rule):** Az új kód szigorúan egy ideiglenes fájlban (`RbacPrototypeMiddleware` vagy hasonló) lesz bekötve, ami könnyen kikapcsolható a `Program.cs`-ből, vagy csak lokálisan tesztelve Git commit eldobásával rollback-elhető. Nincs adatbázis (EF) integráció.

## Sikerkritériumok
- [ ] Ha a `list_tools` hívás az `X-Agent-Role: Explorer` header-rel érkezik, az írási jogosultságú eszközök hiányoznak a visszaadott listából.
- [ ] Ha a `list_tools` hívás `X-Agent-Role: Developer` vagy nincs header, az összes eszköz megjelenik.
- [ ] Az implementáció letesztelése után egyetlen `git checkout` vagy fájl törlés eltünteti a kísérleti maradványokat, nem okoz törést az Api projektben.

## Szerepkör: Experimenter
Kísérletvégrehajtó: **The Experimenter** (`03_prototype` fázis).
- **Megjegyzés:** Ha a kísérlet sikeres állapotfrissítést (MVE completed) eredményez, az eredményeket az `exp-result-mcp-rbac.md` naplózza majd le az `04_test_and_learn` fázis számára.
