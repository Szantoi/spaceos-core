---
domain: manufacturing
segment: knowledge-adr
type: endpoint_gap
priority: high
created: 2026-06-18
---

# Marvin Planning Pipeline ← Manufacturing Real-Time Constraints

## Problem Statement

ADR-043 (Marvin Orchestration) definiál egy **planning pipeline-t** (Scanner → Selector → Debater A/B → Synthesizer).

De a **Scanner fázis nem lekérdezi a gyártás aktuális állapotát**:

- Inventory szint (nyersanyag, panelek)
- Gép terhelés (CNC, edge banding, szekrény assembly)
- Operator skill profil (van-e elég képzett varrófasadis?)
- Szünetidő, kalibráció, szerviz window
- Material lead time (ha import szükséges)

**Eredmény:** A Planning Pipeline általános üzleti ötleteket debatál, de **nem tudja hogy a gyárban nincs szabad kapacitás** vagy **nincs meg a szükséges nyersanyag**. Majd az *implementátor terminál* meglöki ezt az infót, és az ötlet "nem implementálható" lesz.

## Jelenlegi állapot

- **Marvin phases** (ADR-043): Scanner (doc scan) → Selector → Debater → Synthesizer
- **Knowledge Service** (ADR-044): Semantic search az ADR/vision docsban
- **Manufacturing data**: Kernel-ben van (Inventory, WorkOrder, EquipmentStatus), de Scanner ezt nem olvassa

## Bekötési lehetőség

**Fázis 2 (Marvin pipeline integráció után, ~8-10 nap):**

1. **Scanner agent** extension:
   - Knowledge Service query: "`discovery_search` manufacturing constraints"
   - **Live queries** (új tool):
     - `/api/inventory/materials/low-stock` (Critical = 0)
     - `/api/equipment/status` (busy schedule napi)
     - `/api/operators/skills` (Joinery/Cutting/Assembly who's free)
   - Output: Constraint facts list

2. **Synthesizer agent** modification:
   - Before writing consensus: validate against constraint list
   - If blocking: output `blocked_reason: "insufficient_capacity"` + mitigation path

3. **New McpServer tool** (ADR-045):
   - `getManufacturingConstraints()` → JSON
   ```json
   {
     "inventory": { "oak_panel_2500x800": 0, "birch_veneer": 15 },
     "equipment": { "cnc_busy_until": "2026-06-18 16:30", "edge_banding": "available" },
     "operators": { "joinery_capable": 1, "on_shift": true },
     "lead_times": { "walnut_special": 14 }
   }
   ```

## Iparági relevancia

- **Doorstar feedback loop**: "Jó ötlet, de nincs gépkapacitás" == frusztráció
- **MES integration path**: Marvin lekérdezi az MES-t (Siemens, Fastems, stb.)
- **Realistic roadmap**: Planning előrejelzése (6 feature/quarter helyett 2, mert valódi kapacitás)

## Technikai hangsúlyok

- **Walking Skeleton (ADR-005)**: Ez az első olyan pont ahol a pipeline *valódi* gyártási realitást vesz figyelembe
- **RBAC (ADR-004)**: Inventory data RLS-ed (szállító nem lát gyári terhelést)
- **Audit Trail (ADR-003)**: Constraint query-k logolódnak (kinek kellett, mikor, mi volt az eredmény)

---

**Integráció sorrend:** Marvin core complete → Live queries → McpServer tool hozzáadás.
**Blocker:** Knowledge Service system integration kell előtte (ADR-044 Fázis 2).
