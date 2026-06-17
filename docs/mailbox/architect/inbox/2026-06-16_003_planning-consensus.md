---
id: MSG-ARCH-003
from: planning-pipeline
to: architect
type: task
priority: medium
status: READ
model: opus
created: 2026-06-16
---

# Architect — Konsenzus terv megvalósíthatóvá tétele

A planning pipeline (Haiku→Sonnet→2×Opus debate) elkészítette a következő
fejlesztési irányt. A te feladatod ezt implementálható spec-ké alakítani.

## Konsenzus terv:
---
created: 2026-06-16
plan_a: /opt/spaceos/docs/planning/plans/2026-06-16_plan-sonnet-a.md
plan_b: /opt/spaceos/docs/planning/plans/2026-06-16_plan-sonnet-b.md
status: ready_for_architect
---

# SpaceOS Konsenzus Implementációs Terv

## Összefoglalás

Mindkét tervező azonos sorrendben és kb. azonos időbecsléssel (15 nap) gondolkodik: Nesting Vizualizáció → Design→Cutting Workflow → Machine Scheduling. A legnagyobb érdemi vita a drag-drop library körül volt — Sonnet-B native HTML5-öt javasolt, Sonnet-A `@dnd-kit`-et, végül Sonnet-B átvette a `@dnd-kit` érvet (tablet touch events + accessibility). A másik konszenzuális újítás az "auto-nesting trigger" pattern: a Design→Cutting POST után azonnal elindul a nesting számítás, hogy a ProductionPage-re érve a felhasználó kész vizualizációt lásson.

---

## Elfogadott prioritás sorrend

1. **Nesting Vizualizáció** (`GET /cutting/api/cutting/sheets/{id}/nesting` → SVG canvas) — 0 backend függőség, mindkét tervező egyetért; az "Aha-élmény sorrend" érv (Sonnet-B) meggyőzte Sonnet-A-t is
2. **Design → Cutting Workflow** (DesignPage Step 4 mock → valódi POST + ProductionPage highlight) — 0 backend függőség; szinergia: ha Nesting Viz kész, az auto-trigger azonnal vizualizálja az új tervet
3. **Machine & Operator Scheduling** — FSM backend endpoint szükséges, ez az egyetlen valódi blokkoló; mindkét tervező utoljára hagyja

**Párhuzamosíthatóság:** Ha FE-B track elérhető, az 1. és 2. sprint párhuzamosan futhat (Nesting Viz ∥ Design→Cutting), ami ~1 sprinttel rövidíti a delivery-t. Ez a legfontosabb taktikai döntés.

---

## Backend szükségletek (összesített)

### Kész endpointok (0 backend munka az 1-2. featurehez)
- `GET /cutting/api/cutting/sheets/{id}/nesting` — nesting adatok SVG-hez
- `POST /cutting/api/sheets` — cutting plan létrehozás DesignPage-ről

### Szükséges új endpoint (3. feature, ~1 nap)
```
POST /cutting/api/plans/{date}/assign-batch
Body: { batchId, machineId, operatorId, scheduledStart }
FSM:  CuttingExecution Planned → InProgress
```

### API adatmodell (Nesting response — Sonnet-A pontosítása alapján)
```typescript
interface NestingSheet {
  sheetIndex: number;
  width: number;           // mm egységben
  height: number;
  placements: {
    partId: string;
    x: number; y: number;
    w: number; h: number;
    rotated: boolean;
  }[];
  wastePercent: number;
}
interface NestingResult {
  strategy: 'Guillotine' | 'FFDH';
  sheets: NestingSheet[];
  totalWastePercent: number;
}
```

### Cutting POST body (Sonnet-A spec alapján)
```typescript
POST /cutting/api/sheets
Body: { designTemplateId, materialGroups: [...], requestedDate }
Response: { sheetId, cuttingPlanId, estimatedSheets }
```

---

## Frontend megközelítés (legjobb elemek)

### 1. Nesting Vizualizáció komponens struktúra (Sonnet-A)
```
ProductionPage
  └── NestingViewerPanel
        ├── useNestingData(sheetId)        ← SWR/useApi hook
        ├── NestingCanvas                  ← SVG, scale-aware
        │     ├── PanelBackground          ← fehér rect, arány-helyes
        │     └── PlacedPartRect[]         ← x,y,w,h + szín katalógus szerint
        ├── SheetNavigator                 ← ha sheets.length > 1
        └── WasteStatsBadge               ← waste%, strategy, sheets count
```

**Scale formula** (Sonnet-A, explicit): `scale = canvasWidth / panel.widthMm`, y-axis invertált (SVG 0,0 bal felső sarok).  
**Szín kódolás:** `CATALOG_LOOKUP[part.materialCode]` → hex, fallback szürke.

### 2. Design→Cutting auto-trigger pattern (Sonnet-B ötlet, Sonnet-A által elfogadva)
```typescript
// handleSubmitCuttingList() — DesignPage Step 4
const res = await api.post('/cutting/api/sheets', { designTemplateId, materialGroups, requestedDate });
// Optimistic: azonnal indítjuk a nesting számítást (ha külön endpoint létezik)
api.post(`/cutting/api/sheets/${res.data.sheetId}/calculate-nesting`).catch(() => {});
toast(`Vágási terv létrehozva — #${res.data.cuttingPlanId}`);
navigate('/production', { state: { highlightPlanId: res.data.cuttingPlanId } });

// ProductionPage — fogadás
useEffect(() => {
  if (location.state?.highlightPlanId) {
    scrollToPlan(highlightPlanId);
    setSelectedPlan(highlightPlanId);  // auto-nyitja NestingViewer-t
  }
}, []);
```

**Loading state kötelező:** Ha a nesting számítás 3-5 mp, spinner szükséges a NestingCanvas-ban — az "auto-trigger varázslat" csak akkor működik, ha a loading állapot is jól kezelt.

### 3. Machine Scheduling (Sonnet-A struktúra + @dnd-kit döntés)
```
MachiningTab
  └── SchedulingBoard
        ├── UnassignedBatchList      ← GET /cutting/api/plans/{date}/batches?assigned=false
        ├── MachineColumn[CNC]       ← @dnd-kit drop target
        ├── MachineColumn[Edge]
        └── MachineColumn[QC]
              └── BatchCard
                    ├── OperatorCombobox   ← GET /kernel/api/users?role=operator
                    ├── TimePicker
                    └── AssignButton → POST assign-batch
```

---

## Amit Sonnet-A-tól veszünk át

- **Explicit scale formula** (`canvasWidth / panel.widthMm` + y-axis inversion megjegyzés) — valódi gotcha, az implementátor megköszöni
- **Pontosabb API body schema** (`materialGroups`, `requestedDate`, `estimatedSheets` mezők) — jobb spec mint általánosabb alternatíva
- **`@dnd-kit/core`** drag-drop könyvtár — accessibility-ready, tablet touch events megbízhatóan kezelve (Sonnet-B is átvette)
- **FSM kockázat explicit jelzése** — `CuttingExecution` aggregate guard-ok → 409 kezelés UI-ban error boundary-vel

## Amit Sonnet-B-től veszünk át

- **Nesting Viz először sorrend indoklása** — "Aha-élmény sorrend": ha a workflow kész de a ProductionPage üres, a WOW-élmény elpárolog; a nesting viz az a vizuális kapocs ami meggyőzi az ügyvezetőt
- **Auto-nesting trigger** (optimistic POST) — a Design→Cutting POST után azonnal `calculate-nesting` hívás, hogy a ProductionPage-re érve kész vizualizáció fogadja a felhasználót
- **Párhuzamosíthatóság hangsúlyozása** — FE-B track esetén 1. + 2. feature párhuzamosan futhat

## Amit egyik tervtől sem veszünk át

- **Staggered fade-in animáció** (Sonnet-B) — MVP-ben felesleges kockázat, SVG render kontextusban törékeny lehet; post-launch polish
- **Native HTML5 drag-drop** (Sonnet-B eredeti javaslat) — Sonnet-B maga is visszavonta a `@dnd-kit` javára

---

## Időbecslés

| # | Feature | BE nap | FE nap | Blokkoló |
|---|---|---|---|---|
| 1 | Nesting Viz | 0 | 3–4 | semmi — most indítható |
| 2 | Design→Cutting | 0 | 2–3 | párhuzamosan futhat az 1.-gyel |
| 3 | Machine Sched. | 1 | 4–5 | backend FSM endpoint |

**Szekvenciális összidő:** ~15 fejlesztési nap  
**Párhuzamos (FE-A + FE-B):** ~10 nap (1+2 párhuzamos, majd 3)

---

## Nyitott kérdések az Architect-nek

1. **`calculate-nesting` önálló endpoint?** — Sonnet-B auto-trigger pattern feltételez egy `POST /cutting/api/sheets/{id}/calculate-nesting` hívást. Ha a nesting számítás a sheet creation részeként fut (szinkron vagy background job), az auto-trigger pattern eltér. Kérjük az API contract megerősítését.

2. **Nesting koordináta egység** — Az API `placements` mezői mm-ben vagy pixel-egységben adnak vissza értékeket? Scale számítás (mm → SVG viewport) triviális, de az első API hívás előtt dokumentálandó.

3. **`CuttingExecution` FSM guard-ok** — A Scheduling featurenél: enged-e a domain aggregate dupla assignment-et (pl. ugyanaz az operátor két gépre egyszerre)? Ha igen, UI-ban 409 kezelés, ha nem, mi a validáció szintje (BE vagy FE)?

4. **Operator RBAC szűrés** — A `GET /kernel/api/users?role=operator` endpoint létezik és RBAC szűrt? Vagy az operátor lista más forrásból jön?

5. **FE-B track elérhetősége** — Ha párhuzamos delivery kell (~10 nap), a Nesting Viz és Design→Cutting egyidejűleg indítható. Van-e szabad FE-B kapacitás most?

## Feladatod

1. Értékeld a konsenzust a meglévő kódbázis szempontjából
2. Ellenőrizd: nem ütközik-e meglévő architekturális döntéssel?
3. Alakítsd konkrét implementációs spec-ké (mint a FE Domain Matrix)
4. Határozd meg melyik terminál(ok) valósítsák meg
5. Küldj DONE outbox-ot a spec-cel — a pipeline továbbadja a terminálnak
