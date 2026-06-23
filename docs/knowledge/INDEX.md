# SpaceOS Knowledge Base — INDEX

**Latest Update:** 2026-06-22 — Explorer External Research: Competitive Analysis, Multi-Tenant RLS, React Modernization, .NET Clean Architecture

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
- [MULTI_TENANT_RLS_ARCHITECTURE_2026.md](architecture/MULTI_TENANT_RLS_ARCHITECTURE_2026.md) — Multi-tenant SaaS patterns (Shared Schema + RLS best practices 2026, SpaceOS validáció)
- [DOTNET_8_CLEAN_ARCHITECTURE_2026.md](architecture/DOTNET_8_CLEAN_ARCHITECTURE_2026.md) — .NET 8 Clean Architecture (4-layer, CQRS+MediatR, DDD aggregates, SpaceOS 100% compliance)

## context/
- [VISION.md](context/VISION.md) — SpaceOS projekt vízió és célok (2026-04-13)

## deployment/
- [KNOWN_GOTCHAS.md](deployment/KNOWN_GOTCHAS.md) — Ismert csapdák (ha létezik)

## patterns/
- [DATABASE_PATTERNS.md](patterns/DATABASE_PATTERNS.md) — EF Core migrations, RLS policies, suppressTransaction risks, Testcontainers
- [EVENT_SOURCING_PATTERNS.md](patterns/EVENT_SOURCING_PATTERNS.md) — Event sourcing implementation (EHS module, idempotency, GDPR compliance, offline-first)
- [FRONTEND_DRAG_DROP_PATTERNS.md](patterns/FRONTEND_DRAG_DROP_PATTERNS.md) — Native HTML5 drag-drop Kanban (Cutting Module TOP3, visual feedback, state management)
- [OFFLINE_FIRST_WIZARD_PATTERN.md](patterns/OFFLINE_FIRST_WIZARD_PATTERN.md) — Multi-step wizard with Zustand+localForage, photo compression, auto-retry (EHS Incident Report)
- [LOCALSTORAGE_KPI_DASHBOARD_PATTERN.md](patterns/LOCALSTORAGE_KPI_DASHBOARD_PATTERN.md) — Custom hooks (useKPICalculator), trend tracking, localStorage→API migration path (Catalog MVP)
- [TEST_COVERAGE_PATTERNS.md](patterns/TEST_COVERAGE_PATTERNS.md) — .NET backend test strategy: Domain+App ≥90%, Integration ≥40%, Testcontainers setup, xUnit patterns
- [FRONTEND_VERIFICATION_WORKFLOW.md](patterns/FRONTEND_VERIFICATION_WORKFLOW.md) — Verification DONE (0 files, 5-10 min) vs Implementation DONE (5-20+ files, 1-4 hours)
- [BLOCKED_MESSAGE_STRUCTURE.md](patterns/BLOCKED_MESSAGE_STRUCTURE.md) — Type A (Backend API missing) vs Type B (Architectural decision needed), resolution workflows
- [MCP_INTEGRATION_WORKFLOW.md](patterns/MCP_INTEGRATION_WORKFLOW.md) — stdio-HTTP bridge pattern, session ritual (register_working/idle), graceful degradation to curl
- [REACT_18_TYPESCRIPT_MODERNIZATION.md](patterns/REACT_18_TYPESCRIPT_MODERNIZATION.md) — React 18 TypeScript best practices (strict mode fokozatos, TanStack Query, feature-based folders, gap analysis)

## security/
- [SECURITY_AUDIT_2026-06-20.md](security/SECURITY_AUDIT_2026-06-20.md) — Nexus biztonsági audit (4 agent: DevOps, Security, Architect, Devil's Advocate)

## debugging/
- [MCP_CONFIG_GUIDE.md](debugging/MCP_CONFIG_GUIDE.md) — **KRITIKUS:** Claude Code MCP szerver konfiguráció (~/.claude.json vs ~/.claude/settings.json)

## reading-list/
- [2026-06-22_reading-list.md](reading-list/2026-06-22_reading-list.md) — Külső források: .NET 8 Minimal API, React 19, PostgreSQL RLS, Testcontainers, Zustand, DDD/Clean Architecture (6 téma, 25+ cikk)

## market/
- [COMPETITIVE_ANALYSIS_WOODWORKING_SAAS.md](market/COMPETITIVE_ANALYSIS_WOODWORKING_SAAS.md) — Cabinet Vision ($5000+) vs CutList Plus ($89) vs SpaceOS (freemium), Blue Ocean stratégia, go-to-market
