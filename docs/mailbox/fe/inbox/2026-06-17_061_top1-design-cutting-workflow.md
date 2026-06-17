---
id: MSG-FE-061
from: root
to: fe
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-ROOT-001
created: 2026-06-17
---

# FE — TOP 1: Design→Cutting Workflow implementáció (APPROVED)

## Összefoglaló

A Planning Pipeline consensus tervét **APPROVED** státuszban adtam ki az Architect ellenőrzés után. TOP 1-2 azonnali indítható, TOP 3 egy napot vár az Identity + Cutting backend-re.

**Scope:** DesignPage Step 4 submit → valódi `POST /cutting/api/sheets` endpoint + ProductionPage auto-navigation + highlight

**Becs. munka:** 2-3 nap FE (0 backend)

---

## PRE-IMPLEMENTATION ellenőrzés eredménye

Futattam 5 nyitott kérdést a consensus tervből. **Eredmény: 0 blokk TOP 1-2 előtt.**

| # | Kérdés | Válasz | OK |
|---|---|---|---|
| 1 | `cuttingList` format | API már formáza helyesen | ✅ |
| 2 | Identity endpoint | TOP 3-hoz kell (0.5 nap, nem TOP 1) | ✅ |
| 3 | nesting CATALOG mapping | Validálható FE-ben | ✅ |
| 4 | @dnd-kit library | TOP 3-hoz kell install (5 perc) | ✅ |
| 5 | FSM RBAC | Build után ellenőrizendő | ✅ |

---

## Implementation spec (consensus tervből)

### DesignPage Step 4 submit logika

**Jelenlegi:** mock `cuttingPlanId` generálás
**Cél:** valódi API hívás

1. **Lecserélni a mock logikát:**
   - `useApi()` hook: `POST ${API_BASE.cutting}/api/sheets`
   - Body: `{ orderReference: currentOrderRef, templateId: selectedTemplate.id, calculatedParts: cuttingList }`
   - Response: `{ sheetId, cuttingPlanId }`
   - Success callback: `navigate('/w/production/cutting', { state: { highlightPlanId: cuttingPlanId } })`

2. **ProductionPage auto-scroll + highlight:**
   - `useLocation()` hook: `state.highlightPlanId` olvasás
   - Ha van `highlightPlanId`:
     - Auto-select: `setSelectedPlan(highlightPlanId)`
     - Scroll to row: `ref.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })`
     - Visual highlight: 3s-ig border-teal-500 animáció a kiválasztott sorban

3. **Plan sorban UI frissítés:**
   - Customer name + order context hozzáadása (jelenleg "")
   - Breadcrumb button szöveg: "Terv létrehozása és tovább a Gyártásba"

### Teszt cél

- +3 FE teszt: submit flow, navigation, highlight
- Teljes FE teszt coverage: ~768 → 771 teszt

### DoD (Definition of Done)

- [ ] DesignPage Step 4 submit hívja a valódi `POST /cutting/api/sheets` endpoint-ot (mock eltávolítva)
- [ ] ProductionPage auto-navigál a frissen létrehozott plan-ra (URL state + scroll)
- [ ] Plan sorban megjelenik a customer name + order context
- [ ] +3 FE teszt pass (submit flow, navigation, highlight)
- [ ] 0 pnpm build error

---

## Kockázatok

1. **`cuttingList` formátum eltérés** — API-ből jövő adatok nem egyeznek a `CuttingLineInput` DTO-val
   - **Mitigation:** FE-ben strict TypeScript interface + validation
   - **Fallback:** error toast + mock-ra nem fallback (strict mode)

2. **HTTP error kezelés** — `409 duplicate`, `400 validation` stb.
   - **Mitigation:** user-friendly error message Toast
   - **Fallback:** megtartja a DesignPage state, újra próbálható

---

## Relationship (TOP 2-vel)

TOP 2 (Nesting Viz) **nem függ** TOP 1-től, de **kevesebb értéket ad** TOP 1 nélkül (az adott plan-hoz még nincsen nesting data). **Szekvenciális delivery ajánlott** (TOP 1 → TOP 2 →  TOP 3).

---

## Siguiente lépés

1. Conductor fieldolgozza ezt az inbox üzenetet (session start)
2. FE terminál: TOP 1 implementáció (2-3 nap)
3. Deploy + Doorstar smoke test (POST /cutting/api/sheets)
4. FE terminál: TOP 2 implementáció (3-4 nap)

🚀 Indítás: **AZONNAL**
