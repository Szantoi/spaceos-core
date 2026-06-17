# SpaceOS Knowledge Base — INDEX

**Latest Update:** 2026-06-17 — PHASE 2 COMPLETE ✅ + Nexus Phase 1 Knowledge Service OPERATIONAL ✅

Minden doc 1 soros összefoglalója. A Librarian és a planning scanner ebből indul.

## 📚 Knowledge Service Status

- **Implementáció:** Nexus Phase 1 ✅ COMPLETE
- **Vector Store:** ChromaDB (port 8001) + Voyage AI embeddings
- **Search API:** Port 3456 (GET/POST /api/knowledge/search)
- **Indexing:** Librarian cron (5-hourly) + manual POST /api/knowledge/index
- **Documents:** ~440+ indexed (docs/knowledge/ + engineering/)
- **Response Time:** <500ms semantic search
- **Status:** OPERATIONAL (VPS activation pending, see DEPLOYMENT_RUNBOOK.md)

---

## architecture/
- [DESIGN_MEMORY.md](architecture/DESIGN_MEMORY.md) — Zárolt arch döntések, elvek, biztonsági minták (claude.ai migrált)
- [DEPRECATED_APPROACHES.md](architecture/DEPRECATED_APPROACHES.md) — Elvetett megközelítések és miért
- [DESIGN_PIPELINE_STRATEGY.md](architecture/DESIGN_PIPELINE_STRATEGY.md) — v1→v4 review pipeline stratégia
- [ECOSYSTEM_MODULE_ARCHITECTURE.md](architecture/ECOSYSTEM_MODULE_ARCHITECTURE.md) — T-shape ökoszisztéma, 6 actor típus
- [ADR_CATALOGUE.md](architecture/ADR_CATALOGUE.md) — Architekturális döntések: JWT RS256, RLS, RBAC, data-rules-geometry, walking skeleton
- [API_CONTRACT_CATALOGUE.md](architecture/API_CONTRACT_CATALOGUE.md) — Minden endpoint (ha létezik)
- [SpaceOS_ADR_038_Offcut_Creation_At_Plan_Freeze.md](architecture/SpaceOS_ADR_038_Offcut_Creation_At_Plan_Freeze.md) — ADR-038 offcut döntés
- [SpaceOS_Growth_Strategy_v1.md](architecture/SpaceOS_Growth_Strategy_v1.md) — Növekedési stratégia v1
- [SpaceOS_VPS_Infrastructure_Runbook_v1.md](architecture/SpaceOS_VPS_Infrastructure_Runbook_v1.md) — VPS infrastruktúra runbook
- [SpaceOS_Doorstar_Onboarding_v4.md](architecture/SpaceOS_Doorstar_Onboarding_v4.md) — Doorstar onboarding folyamat v4

## context/
- [VISION.md](context/VISION.md) — SpaceOS projekt vízió és célok (2026-04-13)

## deployment/
- [KNOWN_GOTCHAS.md](deployment/KNOWN_GOTCHAS.md) — Ismert csapdák (ha létezik)

## patterns/
- [DATABASE_PATTERNS.md](patterns/DATABASE_PATTERNS.md) — EF Core migrations, RLS policies, suppressTransaction risks, Testcontainers
- (Librarian tölti fel fejlesztés során)

## security/
- (Librarian tölti fel fejlesztés során)
