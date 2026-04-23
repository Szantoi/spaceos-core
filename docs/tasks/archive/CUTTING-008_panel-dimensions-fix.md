---
id: CUTTING-008
title: Panel méret fix — PanelStockDto WidthMm + HeightMm + Contracts 1.1.0
status: active
priority: critical
assignee: cutting
epic: modules-cutting-v1
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/cutting/inbox/2026-04-16_005_panel-dimensions-fix.md
---

## Leírás

A CUTTING-007 DONE elfogadásakor azonosított kritikus hiba: a `GetNestingResultQueryHandler`
hardcode 2800×2070mm értékkel inicializálja az `AvailablePanel` listát.

Valóságban sok különböző tábla méret létezik — ezeket a `MaterialCatalog` tartalmazza.

## Javítás scope

1. `SpaceOS.Modules.Inventory.Contracts` → 1.1.0 (`PanelStockDto` + `OffcutDto` + `WidthMm` + `HeightMm`)
2. `GetStockQueryHandler` → MaterialCatalog JOIN, méret kitöltése
3. `GetNestingResultQueryHandler` → hardcode eltávolítása, `PanelStockDto.WidthMm/HeightMm` használata
4. NuGet 1.1.0 csomag
5. Tesztek frissítése (változatos panel méretek)
