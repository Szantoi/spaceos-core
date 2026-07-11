---
id: SPEC-002
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_003_cutting-ui-spec-done.md
type: Implementation plan
scope: [fe, cutting, kernel]
priority: high
complexity: 3
dependencies: [SPEC-FE-DOMAIN-001]
status: NEW
created: 2026-06-17
---

# Cutting UI — Nesting Visualization & Design→Cutting Workflow

## Összefoglaló

3 szomszédos FE feature a Cutting modul feltárásához: nesting vizualizáció, design-from-cutting workflow integráció, és machine scheduling UI. Az Architect spec kódvalidáció után 4 API feltételezésből 2-t korrigált (nincs `calculate-nesting` és `assign-batch` endpoint). Feature 1-2 read-only FE munka (13+ nap szekvenciális), Feature 3 backend assign endpoint implementációtól függ.

## Scope

- **Frontend:** `joinerytech-portal` — nesting viz, workflow, operator UI
- **Backend:** `spaceos-modules-cutting` — machine scheduling logic + assign endpoint
- **Infrastructure:** `@dnd-kit` NPM dependency telepítés szükséges
- **Kernel:** users endpoint verifikáció (operator lista forrása)

## Implementációs sorrend

### Feature 1: Nesting Visualization (3-4 FE nap, 0 BE)
- `NestingSheet` API contract → `NestingResultResponse` (korrigált DTO)
- React komponens: panel placement grid + drag-drop interaction
- `@dnd-kit` integráció (pre-install szükséges)
- Status: independent, indítható azonnal

### Feature 2: Design→Cutting Workflow (2-3 FE nap, 0 BE, depends on #1)
- "Send to Cutting" button a Design portálon
- Workflow state machine: design_active → cutting_assigned
- Read-only nesting result display integráció
- Status: blocker-free, egymás után hajtható

### Feature 3: Machine Scheduling (4-5 FE + 1 BE nap, **BLOCKED**)
- Operator selection UI, machine capacity, scheduling
- Backend: `POST /assign-batch` endpoint hiányzik
- `PanelAssignmentResponse` DTO
- **Blocker:** CUTTING terminálnak kell megvalósítani az assign endpointot
- Security: operator lista forrása (Kernel users endpoint) előtte clarify-andó

## Kockázatok

1. **DTO mismatch nem teljesen dokumentált** — Feature 1-2 implementátor által ellenőrizendő: `PanelPlacementDto` pontos mezőkészlete
2. **Operator lista forrása** — Kernel users endpoint feltételezés, de nem verifikált. Feature 3 előtt `docs/knowledge/` reference kontextusban clarify szükséges
3. **Szekvenciális FE track** — egyetlen frontend fejlesztő, ~11-13 nap delivery time, kockázat: üzleti deadline missz
4. **DB impact Feature 3-ban** — v2 DB review szükséges assign endpoint-hoz; v3 security review

## Státusz az iterációban

- **v1 (jelenlegi):** DRAFT — API spec, DTO mismatch korrigált, sorrend validált
- **v2 (szükséges):** DB review — CUTTING terminálnak assign endpoint DB schéma
- **v3 (szükséges):** Security review — RLS on assignments, user isolation
- **v4 (Release):** final spec, termináloknak kiadás

## Eredeti dokumentum

`docs/mailbox/architect/outbox/2026-06-16_003_cutting-ui-spec-done.md`

## Root akciók

1. CUTTING terminálnak inbox: Feature 3 assign endpoint fejlesztés (blocker)
2. Kernel users endpoint verifikáció (operator list source)
3. FE terminálnak Feature 1-2 kezdés (read-only, independence)
4. v2 DB review ütemezése CUTTING outputja után
