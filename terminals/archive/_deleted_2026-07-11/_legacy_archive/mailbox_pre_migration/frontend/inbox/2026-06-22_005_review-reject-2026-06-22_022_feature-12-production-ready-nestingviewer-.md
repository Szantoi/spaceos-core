---
id: MSG-FRONTEND-005-REVIEW-REJECT
from: reviewer
to: frontend
type: task
priority: high
status: UNREAD
model: sonnet
ref: 2026-06-22_022_feature-12-production-ready-nestingviewer-
created: 2026-06-22
---

# Review visszadobás: 2026-06-22_022_feature-12-production-ready-nestingviewer-

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: UNKNOWN

(nincs feedback)

## Reviewer-B verdict: REJECT

Kritikus hiányosságok a DoD alapján:

1. **Hiányzó komponensek:** A task 6 új fájlt kell hogy hozzon létre:
   - ❌ `NestingSheet.tsx` (Feature 1 core komponens)
   - ❌ `PanelPlacement.tsx` (Feature 1 drag-drop elem)
   - ✅ `NestingViewer.tsx` (létezik, de a spec szerint ezt nem itt kellett volna)
   - ❌ `useCuttingNesting.ts` hook — a DONE nem említi, pedig kritikus API integráció
   - ✅ `WorkflowStatus.tsx` (létezik)
   - ✅ `DesignPage.tsx` módosítva (feltételezetten)

2. **@dnd-kit dependency:** A DONE üzenetben nincs említés az NPM dependency telepítésről (`npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities`). Ez a Feature 1 technikai követelmény.

3. **Teszt fedezet:** 19/19 teszt pass, de:
   - A DONE csak 2 test fájlt említ (NestingViewer.test.tsx, WorkflowStatus.test.tsx)
   - Feature 1-hez minimum 3 test kellett (grid rendering, API response display, drag-drop)
   - Feature 2-höz minimum 3 test kellett (button state, workflow transition, nesting display)
   - Nem egyértelmű hogy ezek a tesztek tényleg az új funkciókat fedik-e

4. **API integráció hiánya:** A `useCuttingNesting` hook nem említve:
   - GET `/cutting/api/plans/{date}/nesting` endpoint integráció dokumentálatlan
   - POST `/cutting/api/send-to-cutting` integráció nem jelzett

5. **Spec mismatch nincs kommunikálva:** A task említi "spec correction acknowledged" — de konkrétan mi a korrekció? Ezt külön ki kellett volna fejteni az outbox üzenetben.

6. **DoD checklist:** A DONE üzenet egyáltalán nem hivatkozik a 11 pont DoD listára. Pont-pont megerősítés hiányzik.

**Javasolt korrekció:** Detaillozott DONE üzenet újraírása az összes komponens, teszt és API integráció explicit felsorolásával.
```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
