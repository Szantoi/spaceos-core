---
id: MSG-ORCH-086
from: root
to: orchestrator
type: task
priority: medium
status: READ
model: sonnet
created: 2026-06-16
---

# Orchestrator — DESIGN_MEMORY + ADR-039

## Tájékoztató

A VPS-en megjelent a DESIGN_MEMORY dokumentum:

```
docs/knowledge/architecture/DESIGN_MEMORY.md
```

## ADR-039 — Az Orchestrator szerepe cross-modul kommunikációban

Ez a legfontosabb zárolt döntés az Orchestrator számára:

- **Az Orchestrator (BFF) a cross-modul koordinátor**
- Kernel, Joinery, Cutting, Inventory egymást NEM hívják közvetlenül
- Minden cross-service flow az Orchestratoron keresztül fut
- LLM Tool Calling itt történik, nem a Kernel-ben

### Praktikusan

- Ha egy feature pl. Joinery orderből Cutting plan-t generál → az Orchestrator hívja mindkettőt sorban
- Ha Inventory és Joinery adatot kell összefésülni → Orchestrator aggregál, nem egyik service
- BFF route-ok névkonvenciója: `/api/<modul>/<resource>` → proxy a megfelelő service-re

## ADR-014 — pgvector (RAG tervezés kapcsán)

Ha jövőben RAG lekérdezés kerül az Orchestratorba:
- A vektoros keresés a **Kernel PostgreSQL-ben** él (pgvector extension)
- Az Orchestrator hívhat egy Kernel RAG endpoint-ot, de nem kezeli a vektoros DB-t

## Nincs teendő

Ez tájékoztató — nem kell DONE outbox.
