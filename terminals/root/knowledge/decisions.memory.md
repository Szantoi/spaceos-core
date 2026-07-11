# Root Decisions Memory

> **Architectural decisions cache** — Cold memory (365d TTL)
>
> Stratégiai döntések és roadmap.

## Strategic Decisions

### First Customer
**Decision:** Doorstar Kft. (ajtógyártó) — 2026 Q2 Soft Launch
**Rationale:** Real customer feedback loop, manageable complexity

### Target Market
**Decision:** Hungarian woodworking SMEs (1300-2500 cég)
**Rationale:** Addressable market size, underserved niche

### Roadmap
**2026 Q2:** Doorstar Soft Launch
**2026 Q3:** Szabászat modul + 2. ügyfél
**2027:** 5+ éles ügyfél, DACH belépés

## Architecture Decisions

### 7-Terminal Consolidation (2026-06-21)
**Decision:** 19+ terminál → 7 szerepkör-alapú terminál
**Rationale:** Simplicity, clear responsibilities

### Agent Infrastructure
**Decision:** Datahaven Dashboard + Nightwatch automation
**Rationale:** Centralized monitoring, autonomous operation

## 5 Golden Rules

1. **Data → Rules → Geometry** — Frontend rajzol, C# számol, LLM paraméterez
2. **Modular Monolith** — Kernel nem tudja mi az asztalos
3. **Immutability & Trust** — Nincs UPDATE CAD adatokon, SHA-256 audit
4. **Need-to-Know RBAC** — Megrendelő nem látja gyártó anyaglistát
5. **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb

---

**Last updated:** YYYY-MM-DD
