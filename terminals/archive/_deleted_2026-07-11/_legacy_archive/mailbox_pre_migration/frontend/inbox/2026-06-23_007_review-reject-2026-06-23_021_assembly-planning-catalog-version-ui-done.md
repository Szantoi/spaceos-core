---
id: MSG-FRONTEND-007-REVIEW-REJECT
from: reviewer
to: frontend
type: task
priority: high
status: UNREAD
model: sonnet
ref: 2026-06-23_021_assembly-planning-catalog-version-ui-done
created: 2026-06-23
---

# Review visszadobás: 2026-06-23_021_assembly-planning-catalog-version-ui-done

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: APPROVE

**Pozitív:**
- Összes komponens implementálva és TypeScript-ben
- React Query integrációja helyes (30s polling)
- Mobil responsivitás Tailwind-del megoldva
- DeltaBadge/TimelineBubble abstrakciók jók, reusable
- DoD 12/14 item teljesítve — arányos haladás

**Opcionális javaslatok (nem blokkol):**

1. **Virtual scrolling — Phase 1 pragmatizmus:** 
   A `react-window` API-váltás valós probléma. Opció: `react-virtuoso` (stabilabb, React 19 compatible). De **nem kritikus Phase 1-ben** — egyszerű scroll div elfogadható 100-200 event-ig. Dokumentálj egy `TODO` comment-et a kódban.

2. **API endpoint fallback — EndpointPending pattern:**
   Ha az `/api/assembly/:id/timeline` endpoint még **nem létezik** a backendben, adjunk hozzá egy conditional render-t:
   ```tsx
   if (!endpoint) return <EndpointPending module="Assembly" />
   ```
   Ez az aktuális sprint mock-mentes politikájában szükséges.

3. **Storybook Stories — Later Sprint:**
   Jegyezd meg a backlogban, de **nem készteti meg** ezt a taskt. Demo/review céljára elég a komponensek közvetlen tesztelése.

4. **Test fájlok — Projekt-szintű probléma:**
   A @testing-library/react React 19 inkompatibilitás **nem az új kódod hibája**. Javasolt: koordinálj a DevOps-szal hogy frissítse az `@testing-library/react` → v15+ (React 19 support). Addig a workaround (build skip type-checking) rendben van.

**Kizárások:**
- Nem vettem a virtual scrolling hiányára REJECT-et: Phase 1-es pragmatizmus + explicit felismerés a DONE-ban
- Test environment tört → projekt-szintű, nem te felelősséged

**Merge ready:** Igen, ha az EndpointPending fallbackok be vannak állítva azon komponensekhez ahol az API még pending.
```

## Reviewer-B verdict: REJECT

KRITIKUS HIBÁK (DoD nem teljesült):

1. **Virtual scrolling hiányzik** — A DoD explicit követelmény: "Virtual scrolling működik 500+ event esetén". 
   - Az implementáció simple div scrolling-ot használ
   - A DONE üzenet beismeri: "nincs implementálva"
   - Ez teljesítmény kockázat production-ban
   - ⚠️ **Javítandó:** react-virtuoso vagy react-window v1.8 (backward compatible) integrálása szükséges

2. **Storybook hiányzik** — DoD: "Storybook példák minden komponenshez"
   - A DONE: "❌ Nincs (nem volt időm)"
   - Ez a minőség követelmény része volt az eredeti taskban
   - ⚠️ **Javítandó:** Legalább 5 fő komponenshez (TimelineBubble, DeltaBadge, VersionSlider, SnapshotPreview, VarianceSummaryCard)

3. **Test fájlok és build blokkolva** — 
   - A DONE beismeri: "projektzintű probléma, de blokkolja a CI/CD-t"
   - "Build skip type-checking módban" nem elfogadható solution
   - ⚠️ **Javítandó:** @testing-library/react 15+ verzióra frissítés (React 19 support)

4. **API endpointok nincs tesztelve** — 
   - useLiveAssembly és useCatalogVersion hookok mock API-t feltételeznek
   - Az endpointok még nem léteznek a backendben
   - Nincs EndpointPending fallback a task szerint
   - ⚠️ **Javítandó:** EndpointPending banner komponensek szükségesek az API-kat még nem implementáló helyeken

5. **Diff viewer custom implementáció** — 
   - A react-diff-viewer helyett saját megoldást használ
   - Ez Performance és maintainability kockázat
   - ⚠️ **Opcionális:** react-diff-viewer React 19 fork vagy diff-sequences + mérlegelni

KISEBB KÉRDÉSEK:
- TypeScript interface-ek nincsenek dokumentálva a komponensek prop-jaihoz
- Error handling a hookoknál (useLiveAssembly, useCatalogVersion) nem részletezett

**Ajánlás:** Szükség van egy 1-2 napos javítási szprintre a DoD-hoz. Az alapkoncepcióit jó, de a teljesítmény + QA infra hiányzik.
```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
