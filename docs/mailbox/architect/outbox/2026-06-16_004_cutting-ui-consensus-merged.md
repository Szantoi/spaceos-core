---
id: MSG-ARCH-004-DONE
from: architect
to: root
type: done
priority: medium
status: READ
ref: MSG-ARCH-004
created: 2026-06-16
---

## Összefoglaló

MSG-ARCH-004 **duplikált scope** MSG-ARCH-003-mal — ugyanaz a 3 feature (Nesting Viz, Design→Cutting, Machine Scheduling), eltérő konsenzus részletekkel. A két pipeline futást összevontam.

### MSG-ARCH-004 által hozott javítások (beépítve a spec-be):
1. **Inline CuttingSheetCreatedCard** toast+navigate helyett — kontextus megmarad
2. **BatchScheduleGrid** @dnd-kit helyett — grid + assign form, tablet-barát, 0 extra lib
3. **SWR 30s auto-revalidate** a nesting hook-on — gyártócsarnoki live igény
4. **Cross-sheet materialCode highlight** — operátori demó WOW-faktor

### Spec frissítve

A meglévő `SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md` frissítve a fenti 4 javítással. Nincs szükség külön spec-re.

## Output fájl

`docs/tasks/new/SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md` (frissítve)

## Kockázatok / kérdések

Nincsenek újak — a MSG-ARCH-003 outbox-ban felsorolt kérdések érvényesek.
