# SpaceOS Cutting UI — Nesting Vizualizáció + Design→Cutting Workflow

> **Verzió:** v1 FINAL · **Dátum:** 2026-06-16
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ (Feature 1-2) · BLOKKOLVA (Feature 3 — BE endpoint hiányzik)
> **Forrás:** MSG-ARCH-003 + MSG-ARCH-004 (Planning Pipeline konsenzus tervek, összevonva)
> **Terminál:** FE (joinerytech-portal)
> **v1→v4 pipeline:** Feature 1-2 tisztán FE read-only munka (meglévő API-k), nincs DB/Security/Backend impakt → v2-v4 review NEM szükséges. Feature 3 assign endpoint-ja a CUTTING terminál CLAUDE.md alapján triviális (1 endpoint, meglévő FSM) → nem indokol külön tervdokumentumot.

---

## 0. Összefoglaló

A Planning Pipeline (Haiku→Sonnet→2×Opus debate) konsenzus tervet produkált 3 Cutting UI feature-re. Az Architect kódbázis-verifikáció alapján a tervet **részben módosítva** fogadom el — az API feltételezések 2/4-e hibás volt.

### Konsenzus terv vs. valóság

| Elem | Konsenzus feltételezés | Tényleges állapot | Következmény |
|---|---|---|---|
| `GET /sheets/{id}/nesting` | Létezik | ✅ LÉTEZIK (`NestingResultResponse`) | Indítható |
| `POST /cutting/api/sheets` | Létezik | ✅ LÉTEZIK | Indítható |
| `POST calculate-nesting` | Külön endpoint | ❌ NEM LÉTEZIK — nesting implicit | Auto-trigger pattern MÓDOSUL |
| `POST assign-batch` | Szükséges (~1 nap BE) | ❌ NEM LÉTEZIK | Feature 3 BE munka szükséges |
| `NestingSheet` DTO | TypeScript interface | ❌ ELTÉRŐ — `NestingResultResponse` + `PanelAssignmentResponse` | FE mapping szükséges |
| `@dnd-kit` | Meglévő | ❌ NEM TELEPÍTVE (HTML5 native van) | NPM install szükséges Feature 3-hoz |
| `CuttingExecution` FSM | 2 state (Planned→InProgress) | ✅ 6 STATE (Scheduled→Started→InProgress→Completed/Cancelled/Failed) | Gazdagabb, jó |

---

## 1. Feature-ek — módosított prioritás sorrend

A konsenzus sorrendet elfogadom (azonos):
1. **Nesting Vizualizáció** — ProductionPage SVG canvas
2. **Design→Cutting Workflow** — DesignPage→ProductionPage navigáció
3. **Machine & Operator Scheduling** — @dnd-kit board (BLOKKOLVA: BE endpoint hiányzik)

### Párhuzamosíthatóság

Feature 1 és 2 **NEM** párhuzamosítható egyetlen FE tracken — a Feature 2 az "auto-nyitás" logikája a Feature 1 NestingViewer komponensre épül. **Szekvenciális: 1 → 2 → 3.**

Ha lenne FE-B track: Feature 1 (FE-A) ∥ Feature 3 BE prep (CUTTING terminál) párhuzamosítható.

---

## 2. Feature 1 — Nesting Vizualizáció

### 2.1 Meglévő állapot

- `ProductionPage.tsx` — placeholder kártya létezik ("Válasszon vágási tervet a megjelenítéshez")
- API hook: `useApi` → `${API_BASE.cutting}/api/cutting/plans` már bekötve
- Nesting endpoint: `GET /cutting/api/cutting/sheets/{sheetId}/nesting` → `NestingResultResponse`

### 2.2 Backend DTO (tényleges, nem a konsenzus TypeScript interface)

```
NestingResultResponse {
  SheetId: Guid
  OrderReference: string
  Groups: NestingGroupResponse[]
  TotalParts: int
  PanelAssignments: PanelAssignmentResponse[]?
}

PanelAssignmentResponse {
  SheetId: Guid
  Placements: PanelPlacementDto[]
  WastePercentage: decimal
  PanelsRequired: int
}
```

**Fontos:** A `PanelPlacementDto` mezőit az FE implementátor ellenőrizze a Cutting Contracts NuGet-ből:
```bash
cat /opt/spaceos/backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Contracts/Dtos/PanelPlacementDto.cs
```

### 2.3 Komponens struktúra (módosított)

```
ProductionPage
  └── NestingViewerPanel (ÚJ — a placeholder helyére)
        ├── useNestingData(sheetId)      ← SWR hook, 30s auto-revalidate (gyártócsarnoki live igény)
        ├── NestingCanvas                ← natív SVG, responsive, külső rajzlib nélkül
        │     ├── PanelBackground        ← fehér rect, arány-helyes
        │     └── PlacedPartRect[]       ← x,y,w,h a PanelPlacementDto-ból
        ├── SheetNavigator               ← ha PanelAssignments.length > 1
        └── WasteStatsBadge              ← WastePercentage, TotalParts
```

**Cross-sheet highlight:** Hover/kattintás egy placement-en → kiemeli az összes azonos `materialCode`-ú darabot az összes sheeten. Magas WOW-faktor operátori demón.
**Performance:** `React.memo` + csak a látható sheet renderelése (>200 part esetén kritikus).

### 2.4 Scale formula

```typescript
const scale = Math.min(canvasWidth / panelWidthMm, canvasHeight / panelHeightMm);
// SVG 0,0 = bal felső → y-axis NEM invertálandó (SVG standard)
// Nesting koordináták mm-ben jönnek → scale-lel szorzás
```

**Panel dimenziók:** A `PanelAssignmentResponse`-ban vannak-e width/height mm mezők? Ha nem, a sheet eredeti méretei kellenek (`GET /cutting/api/cutting/sheets/{id}/status` → dimenziók). Az implementátor ellenőrizze.

### 2.5 Szín kódolás

Egyszerű kategória-alapú szín: anyag típus → fix hex szín. A színpaletta a portál meglévő Tailwind konfigurációjából:
- Tölgy → amber-400
- Bükk → yellow-600
- MDF → slate-400
- Fehér lap → white (sötét border)
- Fallback → gray-300

### 2.6 Becslés

**3-4 FE nap.** 0 BE munka.

---

## 3. Feature 2 — Design→Cutting Workflow

### 3.1 Meglévő állapot

- `DesignPage.tsx` — ApiParamWizard, cutting list generálás aktív
- `POST /cutting/api/sheets` — létezik, sheet submission működik
- ProductionPage — létezik, cutting plans listázás működik

### 3.2 Módosított auto-trigger pattern

A konsenzus terv `POST calculate-nesting` auto-triggert javasol. **Ez az endpoint nem létezik.** A nesting számítás a sheet creation részeként fut (szinkron).

**Módosított flow — inline CuttingSheetCreatedCard (nem toast+navigate):**

A felhasználó kontextust nem veszít — inline success card jelenik meg a Step 4 alján, navigáció opcionális gombbal.

```typescript
// DesignPage Step4SubmitPanel.tsx
const res = await api.post('/cutting/api/sheets', {
  // SubmitSheetRequest body — az implementátor a meglévő DTO-t használja
});
setCreatedSheet(res.data);  // → CuttingSheetCreatedCard megjelenik Step 4 alján

// CuttingSheetCreatedCard.tsx (ÚJ komponens):
// [Terv #{cuttingPlanId} létrehozva · → Gyártáshoz megnyitása]
// navigate('/production', { state: { highlightPlanId: res.data.cuttingPlanId } })
```

```typescript
// ProductionPage — fogadás (location.state, nem URL param)
useEffect(() => {
  if (location.state?.highlightPlanId) {
    scrollToPlan(location.state.highlightPlanId);
    setHighlightId(location.state.highlightPlanId);  // 3s CSS fade-out
    setSelectedPlan(location.state.highlightPlanId);  // auto-nyitja NestingViewerPanel-t
  }
}, []);
```

**Nincs optimistic nesting trigger** — a nesting a sheet POST részeként már fut. A NestingViewerPanel a `GET /sheets/{id}/nesting` hívás eredményét mutatja.

**Új komponensek:** `CuttingSheetCreatedCard.tsx`, `PlanHighlightRow.tsx`.

### 3.3 Loading state

Ha a nesting számítás lassú (>1s): a NestingCanvas-ban skeleton loader, nem spinner. A portál meglévő loading pattern-jét követi (pulse animáció).

### 3.4 Becslés

**2-3 FE nap.** 0 BE munka. Feature 1 NestingViewerPanel szükséges.

---

## 4. Feature 3 — Machine & Operator Scheduling (BLOKKOLVA)

### 4.1 Blokkoló

**Backend endpoint hiányzik:** `POST /cutting/api/plans/{date}/assign-batch` nem létezik.

**Szükséges BE munka (CUTTING terminál):**
```
POST /api/cutting/planning/{planId}/assign
Body: { batchId: Guid, machineId: Guid, operatorId: Guid, scheduledStart: DateTime }
Validáció: CuttingExecution FSM → Scheduled state required
Response: { executionId: Guid, status: "Scheduled" }
```

Ez az egyetlen pontja ahol a CuttingExecution FSM-et hívja (Scheduled→Started). Az FSM guard-ok már megvannak a domainben.

### 4.2 Operator lista kérdés

A konsenzus `GET /kernel/api/users?role=operator` endpointot feltételez. **Nem ellenőriztem** — a Kernel-ben van-e ilyen szűrő. Az implementátor verifikálja:
```bash
grep -r "role" /opt/spaceos/backend/spaceos-kernel/src/*/Api/Endpoints/ --include="*.cs" | grep -i "user\|operator"
```

### 4.3 UI megközelítés — Grid + Assign form (NEM drag-drop)

A két pipeline futás közül a második konsenzus jobb: **grid + kattintásra assign form**, DnD nélkül. Indoklás:
- Tablet operátor — touch sensor extra config + tesztelési teher nem éri meg
- 0 extra lib — nincs @dnd-kit, nincs verziókonflik kockázat React 19-cel
- Mobilon megbízhatóbb mint drag-drop

Post-launch revisit ha operátorok DnD-t igényelnek.

### 4.4 Komponens struktúra

```
ProductionPage → MachiningTab
  └── BatchScheduleGrid (ÚJ)
        ├── UnassignedBatchList     ← anyag, qty, dátum, prioritás chip
        └── MachineColumns: [CNC | Edgebanding | QC]
              └── BatchCard (kattintásra assign form nyílik)
                    ├── OperatorCombobox  ← operator lista (forrás TBD)
                    ├── TimePicker        ← scheduledStart
                    └── [Kiosztás] → POST assign → optimistic UI update
```

### 4.5 Becslés

**1 BE nap** (CUTTING terminál: assign endpoint) + **4-5 FE nap.**

### 4.6 Blokkoló feloldás

Root kiadja a CUTTING terminálnak az assign endpoint implementálását. Feature 3 FE munka csak utána indulhat.

---

## 5. Implementációs sorrend és terminál hozzárendelés

| Sorrend | Feature | Terminál | Előfeltétel | Becsült nap |
|---|---|---|---|---|
| 1 | Nesting Vizualizáció | FE | semmi | 3-4 |
| 2 | Design→Cutting Workflow | FE | Feature 1 kész | 2-3 |
| 3a | Assign-batch endpoint | CUTTING | semmi (párhuzamos Feature 1-2-vel) | 1 |
| 3b | Machine Scheduling UI | FE | Feature 1 + 3a kész | 4-5 |

**Teljes szekvenciális:** ~11-13 FE nap + 1 BE nap
**Optimális (3a párhuzamosan):** ~10-12 FE nap

---

## 6. Nyitott kérdések válaszai

| # | Kérdés | Válasz |
|---|---|---|
| 1 | `calculate-nesting` endpoint? | **Nem létezik, nem is kell.** Nesting a sheet POST részeként fut. Auto-trigger pattern kivezetendő. |
| 2 | Nesting koordináta egység? | **Valószínűleg mm** (domain konvenció), de az implementátor ellenőrizze a `PanelPlacementDto.cs` mezőit. |
| 3 | `CuttingExecution` FSM dupla assignment? | **6 állapotú FSM**, guard-ok a domain aggregate-ben. UI 409 kezelés szükséges ha invalid transition. |
| 4 | Operator RBAC szűrés? | **Nem ellenőriztem** — Kernel users endpoint verifikálandó. |
| 5 | FE-B track? | **Nem elérhető** (egyetlen FE track van jelenleg). Szekvenciális delivery. |

---

## 7. Architekturális kockázatok

| Kockázat | Severity | Mitigation |
|---|---|---|
| `PanelPlacementDto` mezői eltérhetnek a konsenzus TS interface-től | MEDIUM | Implementátor ellenőrizze a tényleges DTO-t mielőtt SVG render-t ír |
| Nesting response panel dimenziói hiányozhatnak | MEDIUM | Sheet GET endpoint-ból vett dimenzióval pótolható |
| Operator lista forrása nem tiszta | LOW | Feature 3 előtt tisztázandó (Kernel vs. Cutting scope) |
| Grid form UX nem elég intuitív operátoroknak | LOW | Post-launch user feedback → DnD upgrade ha igény van |

---

## 8. DoD (Definition of Done)

### Feature 1 — Nesting Viz
- [ ] NestingViewerPanel komponens ProductionPage-en
- [ ] SVG canvas arány-helyes renderrel
- [ ] SheetNavigator ha több panel
- [ ] WasteStatsBadge százalékkal
- [ ] Mock fallback ha API nem elérhető
- [ ] Tesztek (vitest + testing-library)
- [ ] `pnpm build` 0 error

### Feature 2 — Design→Cutting
- [ ] DesignPage Step 4 → POST /cutting/api/sheets
- [ ] Navigate → ProductionPage highlight
- [ ] Auto-open NestingViewerPanel
- [ ] Toast feedback
- [ ] Tesztek
- [ ] `pnpm build` 0 error

### Feature 3 — Machine Scheduling
- [ ] BE: assign endpoint CUTTING modulban
- [ ] BatchScheduleGrid komponens
- [ ] MachineColumns grid layout
- [ ] BatchCard + assign form + OperatorCombobox
- [ ] 409 error kezelés FSM guard-oknál
- [ ] Tesztek
- [ ] `pnpm build` 0 error (FE) + `dotnet test` 0 fail (BE)
