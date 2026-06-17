---
id: MSG-ARCH-004
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

# SpaceOS Manufacturing — Konsenzus Implementációs Terv

## Összefoglalás

A két tervező lényegében azonos backlog-ot azonosított: a három feature (Design→Cutting Workflow, Nesting Vizualizáció, Machine Scheduling) mindkét tervben szerepel, a backend szükséglet megítélésében teljes az egyezés. Az egyetlen érdemi vita az első két feature sorrendjéről szólt — Sonnet-A az üzleti értéket (TOP 1: workflow-link), Sonnet-B az azonnali indíthatóságot (TOP 2: nesting viz) helyezte előre. A keresztértékelés hozta a valódi áttörést: a kettő **párhuzamosan futtatható**, ami feleslegessé teszi a vitát. TOP 3 (Machine Scheduling) sorrendje vitatott nem volt — a BE blokkoló miatt utolsó.

---

## Elfogadott prioritás sorrend

1. **Nesting Vizualizáció** (`GET /cutting/api/cutting/sheets/{id}/nesting` → SVG canvas)
   — 0 backend függőség, azonnal indítható; ha egyetlen FE fejlesztő van, ezzel kezd, mert nincs mock-kód megértési overhead
2. **Design → Cutting Workflow** (DesignPage Step 4 → valódi POST + ProductionPage highlight)
   — 0 backend függőség; párhuzamosan futhat az 1.-gyel, ha két fejlesztő elérhető
3. **Machine & Operator Scheduling** — `POST assign-batch` endpoint szükséges (1 nap BE); FE csak utána indulhat

**Párhuzamosíthatóság (kulcsdöntés):** Ha FE-A és FE-B track egyszerre elérhető, az 1. és 2. sprint párhuzamosan fut → a szekvenciális ~15 fejlesztési nap ~10 napra rövidül. A sorrend (nesting first vs workflow first) ebben az esetben irreleváns.

---

## Backend szükségletek (összesített)

### Kész endpointok — 0 backend munka az 1–2. featurehöz

- `GET /cutting/api/cutting/sheets/{id}/nesting` — 32/32 teszt zöld
- `POST /cutting/api/sheets` — meglévő, kész

### Szükséges új endpoint — 3. feature (~1 nap BE)

```
POST /cutting/api/plans/{date}/assign-batch
Body: { batchId: uuid, machineId: uuid, operatorId: uuid,
        priority: int, scheduledStart: ISO8601 }
Response: { assignmentId: uuid, fsm: "Planned" }
FSM trigger: CuttingExecution  Planned → InProgress  (első Start action-nél, nem az assign-nál)
Auth: machine_operator | production_manager
```

### Cutting POST request body (Sonnet-A spec alapján pontosítva)

```typescript
// POST /cutting/api/sheets
{
  orderReference: string,
  templateId: string,       // uuid
  calculatedParts: Part[]   // !! schema egyeztetés az API Contract Catalogue-gal session elején kötelező
}
// Response
{ sheetId: uuid, cuttingPlanId: uuid }
```

### Nesting API response modell (feltételezett — Architect megerősítése kérve)

```typescript
interface NestingResult {
  sheetWidth: number;       // mm — egység Architect által megerősítendő
  sheetHeight: number;
  totalWastePercent: number;
  strategy: 'Guillotine' | 'FFDH';
  sheets: {
    index: number;
    placements: {
      partId: string;
      x: number; y: number;
      w: number; h: number;
      rotated: boolean;
      materialCode: string;
    }[];
    wastePercent: number;
  }[];
}
```

---

## Frontend megközelítés (legjobb elemek szintézise)

### 1. Nesting Vizualizáció

**Komponens struktúra (Sonnet-A névadása + Sonnet-B hook design):**

```
ProductionPage
  └── plan row expand
        └── NestingViewer.tsx (standalone, önállóan tesztelhető)
              ├── useNestingData(sheetId)     ← SWR hook, 30s auto-revalidate (gyártócsarnoki live igény)
              ├── NestingCanvas.tsx           ← natív SVG, külső rajzlib nélkül
              │     └── <rect> per placement, scale = canvasWidth / sheetWidth
              ├── SheetSelector.tsx           ← ha sheets.length > 1: prev/next vagy tab
              └── WasteStatsBadge.tsx         ← waste%, strategy chip
```

**Szín kódolás:** `CATALOG_LOOKUP[placement.materialCode]` → hex, fallback `#ccc` + `console.warn`.
**SVG scale:** `scale = containerWidth / sheetWidth_mm` — y-axis invertált (SVG 0,0 bal felső sarok).
**Cross-sheet highlight (Sonnet-B):** hover/kattintás egy placementen → kiemeli az összes azonos `materialCode`-ú darabot az összes sheeten.
**Performance:** `React.memo` + csak a látható sheet renderelése (>200 part esetén kritikus).

### 2. Design → Cutting Workflow

**UX: inline CuttingSheetCreatedCard (Sonnet-B ötlet — Sonnet-A által elfogadott):**

```typescript
// DesignPage Step4SubmitPanel.tsx
const res = await api.post('/cutting/api/sheets', { orderReference, templateId, calculatedParts });
// Inline success card (NEM toast, NEM navigation — kontextus megmarad)
setCreatedSheet(res.data);  // → CuttingSheetCreatedCard megjelenik a Step 4 alján

// CuttingSheetCreatedCard — csak navigate gombbal:
// [Terv #42 · → Gyártáshoz megnyitása]
// navigate('/production', { state: { highlightPlanId: res.data.cuttingPlanId } })
```

**ProductionPage fogadás (Sonnet-A location.state megközelítése — tisztább mint URL-param):**

```typescript
useEffect(() => {
  if (location.state?.highlightPlanId) {
    scrollToPlan(location.state.highlightPlanId);
    setHighlightId(location.state.highlightPlanId);  // 3s CSS fade-out
    setSelectedPlan(location.state.highlightPlanId); // auto-nyitja NestingViewer-t
  }
}, []);
```

**Új komponensek:** `CuttingSheetCreatedCard.tsx`, `PlanHighlightRow.tsx` (Sonnet-A névadása).

### 3. Machine & Operator Scheduling

**UI: `BatchScheduleGrid` — sávos nézet, drag-drop nélkül (Sonnet-B, Sonnet-A által elfogadott):**

Sonnet-A `@dnd-kit/core`-t javasolt (accessibility-ready, ~15KB gzip), de a keresztértékelés után mindkét tervező elismerte: tablettel dolgozó operátornál a touch sensor extra config + tesztelési terhe nem éri meg. A grid+assign form egyszerűbb és mobilon megbízhatóbb.

```
MachiningTab
  └── BatchScheduleGrid
        ├── UnassignedBatchList   ← anyag, qty, dátum, prioritás chip
        └── MachineColumns: [CNC | Edgebanding | QC]
              └── BatchCard (kattintásra assign form nyílik):
                    ├── OperatorCombobox    ← GET /kernel/api/users?role=operator (RBAC szűrt — Architect megerősítendő)
                    ├── TimePicker          ← scheduledStart
                    └── [Kiosztás] → POST assign-batch → optimistic UI update
```

**Error handling:** 409 (dupla assign guard) + 403 (RBAC mismatch) UI-ban error boundary-vel.

---

## Amit Sonnet-A-tól veszünk át

- **`calculatedParts` schema-mismatch kockázat explicit jelzése** (`→ 422`): az API Contract Catalogue egyeztetése session elején kötelező lépés
- **`location.state` highlight flow**: tisztább és rövidebb mint az URL-param alternatíva
- **`PlanHighlightRow.tsx` és `NestingViewer.tsx` komponens névadás**: jól szeparált, tesztelhető egységek
- **SVG scale formula explicit leírása** (`canvasWidth / sheetWidthMm`, y-axis inversion): valódi implementációs gotcha, az implementátor megköszöni
- **FSM guard kockázat jelzése** (`CuttingExecution` aggregate → 409 kezelés szükséges)

## Amit Sonnet-B-től veszünk át

- **Inline `CuttingSheetCreatedCard`** a navigáció helyett: „vezet, nem keres" — a user kontextust nem veszít
- **`BatchScheduleGrid` drag-drop nélkül**: tablet-barát, 0 extra lib, mobilon megbízható
- **`useNestingData` SWR hook 30s auto-revalidate-tel**: gyártócsarnokban a live frissítés kritikus igény
- **Cross-sheet materialCode highlight**: operátori demón azonnal érthető, magas WOW-faktor
- **Párhuzamosíthatóság hangsúlyozása**: ha két FE track elérhető, 1+2 párhuzamosan fut → ~1 sprint nyeresége

## Amit egyik tervtől sem veszünk át

- **`@dnd-kit/core`** (Sonnet-A): a cross-eval konszenzus a grid felé mozdult; post-launch revisit ha az operátorok igénylik
- **`GET`-paraméter alapú highlight** (Sonnet-B URL-param): `location.state` elegánsabb, nem szennyezi az URL history-t

---

## Időbecslés

| # | Feature | BE nap | FE nap | Blokkoló |
|---|---|---|---|---|
| 1 | Nesting Vizualizáció | 0 | 3–4 | semmi — azonnal indítható |
| 2 | Design → Cutting Workflow | 0 | 2–3 | párhuzamosan futhat az 1.-gyel |
| 3 | Machine & Operator Scheduling | 1 | 4–5 | BE FSM endpoint |

**Szekvenciális összidő:** ~13–15 fejlesztési nap
**Párhuzamos (FE-A ∥ FE-B):** ~8–10 nap (1+2 párhuzamos, majd 3)

---

## Nyitott kérdések az Architect-nek

1. **Nesting koordináta egység** — A `placements` mezők (`x`, `y`, `w`, `h`) mm-ben vagy más egységben érkeznek? A scale formula (`containerWidth / sheetWidth`) csak mm esetén helyes közvetlenül; pixel-egységnél más adapter kell.

2. **`calculate-nesting` önálló endpoint?** — Ha a nesting számítás a sheet creation részeként fut (szinkron vagy background job), az auto-trigger pattern (`POST .../calculate-nesting`) felesleges. Ha aszinkron job: polling vagy WebSocket? Ez befolyásolja az `useNestingData` hook dizájnját (SWR poll vs realtime).

3. **`CuttingExecution` FSM guard-ok** — A Scheduling featurenél: van-e domain-szintű guard az dupla assignment ellen (pl. operátor két gépre egyszerre)? 409 esetén mi az elvárt UI viselkedés?

4. **Operator RBAC szűrés endpoint** — A `GET /kernel/api/users?role=operator` endpoint létezik és RBAC-szűrt? Vagy az operátor lista más forrásból jön (pl. `GET /kernel/api/roles/machine_operator/members`)?

5. **`calculatedParts` schema** — A `POST /cutting/api/sheets` body `calculatedParts` mezőjének pontos típusa: a DesignPage-en számított `Part[]` séma megegyezik a Cutting BE által várt sémával? (Sonnet-A által azonosított 422-kockázat feloldása.)

## Feladatod

1. Értékeld a konsenzust a meglévő kódbázis szempontjából
2. Ellenőrizd: nem ütközik-e meglévő architekturális döntéssel?
3. Alakítsd konkrét implementációs spec-ké (mint a FE Domain Matrix)
4. Határozd meg melyik terminál(ok) valósítsák meg
5. Küldj DONE outbox-ot a spec-cel — a pipeline továbbadja a terminálnak
