---
domain: manufacturing
segment: knowledge-adr
type: endpoint_gap
priority: high
created: 2026-06-18
---

# Joinery CAD Export Formátumok — Gyártásgép integráció

## Problem Statement

ADR-005 (Walking Skeleton) kimondja: Joinery v1 = **"sima méretes asztalos (width/height/depth) → PDF nyomtatás"**.

De a gyárban több formátum szükséges:

1. **CNC Panelfűrész** (asztalosipar standard): STEP, DXF, vagy proprietáry NC code
2. **Edge Banding Machine**: 2D layout (szélezési sorrend, anyagadatok)
3. **Workshop offline**: CAD cache szinkronizálás (ha internet kimegy)
4. **Minőség**: Dimension checkpoints PDF-ben (operátor mérési pontok)

Jelenleg: csak PDF. Gépekkel való integrációs pont **nem definiálva**, és így a gyártási flow fragmentált (manual PDF → gépkezelő).

## Jelenlegi állapot

- **Kernel IParametricProduct.GenerateGeometry()** → CAD engine
- **Joinery driver** → ? (konkrét output formátumok nincsenek spec-ezve)
- **Portal** → PDF download, kész

## Bekötési lehetőség

**Fázis 2 (Slice 1 után):**
1. `GeometryResult` kiterjesztés: `Dictionary<string, Stream> ExportFormats { get; }`
2. Joinery driver: STEP/DXF generátor (pl. CadLib.NET vagy Rhino SDK)
3. Orchestrator BFF: `/orders/{id}/exports` endpoint
   - `?format=pdf` (current)
   - `?format=step` (CNC)
   - `?format=dxf` (CAM software)
   - `?format=nc` (proprietáry — Doorstar gépeinek)
4. Workflow FSM: PDF után → `waiting_for_export_confirmation` (operátor: "NC kód OK?")

## Iparági relevancia

- **Doorstar first customer**: Van CNC Weeke panelfűrész + edge banding. NC import már mai cél.
- **Offline resiliency**: Gyárban 2-3x/nap kimegy az internet → pre-downloaded CAD szükséges.
- **Quality gate**: Operátor manuális mérés == pain point. CAD dimension checkpoints automatikus.

## Technikai hangsúlyok

- **Modular Monolith**: Új export format = új Joinery driver method, Kernel nem tudja
- **Audit Trail**: Melyik operátor használta fel a CAD-et? → AuditEvents trakk
- **Need-to-Know RBAC**: Szállító nem látja az NC kódot, csak gyári admin

---

**Fázis meghatározás:** Slice 1-ben Walking Skeleton (PDF), Slice 2 = CAD integráció.
