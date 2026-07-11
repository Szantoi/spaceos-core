---
id: MSG-ARCHITECT-007
from: mcp-server
to: architect
type: task
priority: high
status: READ
created: 2026-06-23
content_hash: 8f53b800d8147b57b7e612c04371844b7115a3a817ea2d9489f64d3b85c4f43c
---

# Consensus feldolgozás: Katalógus szűrő + EHS kalkulátor (v1→v4 pipeline)

## Kontextus

A planning pipeline elkészítette a 2026-06-23-i consensust. Ez egy **hibrid UI-first + backend-safety stratégia** 2 hétre:

1. **Katalógus szűrő** (Week 1, napok 1-3) — frontend-only, azonnali üzleti érték
2. **EHS kalkulátor backend alapok** (Week 1, napok 2-4, párhuzamos) — compliance kritikus
3. **EHS kalkulátor UI + gamification** (Week 2, napok 5-8)
4. **Katalógus ajánlás + voice search** (Week 2, napok 7-10)

## Feladat

Futtasd a **v1→v4 pipeline-t** (`/spaceos-arch-planner` skill):

1. **v1** — Első vázlat: domain model, DB schema, API surface, komponens struktúra
2. **v2** — DB review (ha van schema módosítás)
3. **v3** — Security review (OWASP, RBAC, audit trail)
4. **v4** — Backend review (ha v3-ban maradt CRITICAL/HIGH)

## Elvárt output

**Artifact:** `SpaceOS_CatalogEHS_Hybrid_Architecture_v4.md` → `docs/tasks/new/`

**Lebontás kis taskokra:**
- Katalógus szűrő frontend (1-2 órás taskok)
- EHS backend model + migration + endpoint (1-2 órás taskok)
- EHS frontend UI komponensek (1-2 órás taskok)

**Terminál hozzárendelés:**
- Frontend terminál → katalógus szűrő, EHS UI
- Backend terminál → EHS model, migration, API endpoint

## Források

- **Consensus:** `/opt/spaceos/docs/planning/queue/2026-06-23_0550_consensus.md`
- **Katalógus szűrő spec:** consensus → "Frontend megközelítés" szekció
- **EHS kalkulátor spec:** consensus → "Backend szükségletek" szekció

## Prioritás

**HIGH** — Week 1 napok 1-3-ra indítandó (katalógus szűrő), napok 2-4-re EHS backend.

Kezdd el a pipeline-t!
