# Backend Terminal Memory — Updated 2026-07-10

## ROLE & IDENTITY

**Primary Mission:** SpaceOS Backend Development — .NET 8, Node.js, PostgreSQL

### Responsibilities
- Kernel, Orchestrator, Joinery module development
- JoineryTech module implementation (CRM, Kontrolling, HR, Maintenance, QA, DMS, EHS)
- API endpoints, CQRS handlers, domain aggregates
- Infrastructure layer (EF Core, Testcontainers)

---

## RECENT COMPLETED TASKS (2026-07-08)

### MSG-BACKEND-192: Memory Management MCP Tools
- **Status:** ✅ DONE outbox written (9.1KB)
- **Deliverables:** 3 MCP tools (`memory_health_report`, `compress_memory`, `extract_patterns`)
- **Implementation:** TypeScript, ~350 lines, integration with knowledge-service memory store

### MSG-BACKEND-193: Session Starter Infrastructure Bug ⚡ **CRITICAL FIX**
- **Status:** ✅ DONE outbox written (8.0KB)
- **Problem:** `watchInbox.ts` sending bash commands instead of Claude prompts → sessions stuck
- **Root Cause:** `tmux send-keys` sent `[INBOX]` messages to bash shell, not Claude
- **Solution:** Replaced with MCP API (`/api/session/inject`, `/api/session/start`)
- **Impact:** Fixes Mode #4 autonomous operation, systemic fix for all 7 terminals
- **Files Changed:** `watchInbox.ts` (233→208 lines), integration test (186 lines), documentation

### MSG-BACKEND-194: Doorstar Production Implementation Plan 🎯 **REAL CLIENT**
- **Status:** ✅ DONE outbox written (26KB)
- **Client:** Doorstar Kft. (production deadline 2026-09-30)
- **Deliverables:** OpenAPI contract (7 endpoints), DDD task breakdown, timeline (5-6 days)
- **Architecture:** Layer 2 DRIVER, FSM (6 STAGE workflow), event-driven (Cutting integration)
- **6 STAGE:** Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható

### Live Bug Demonstration (2026-07-08 → 2026-07-10)
**Inbox Watcher Bug Reproduced:**
- 150+ nudge messages received for MSG-BACKEND-192 (already DONE)
- Demonstrates exact bug fixed in MSG-BACKEND-193
- Fix deployed to `watchInbox.ts`, awaiting nightwatch deployment
- Pattern: nudge every 10 minutes, cooldown ignored (state persistence issue)

**Pipeline Status:**
- All 3 DONE outboxes await reviewer.sh → pipeline.sh processing
- Inbox messages still UNREAD (will be marked READ by pipeline)

---

## JOINERYTECH STATUS (2026-07-08)

### Weekly Progress Pattern
| Week | Layer | Components |
|------|-------|------------|
| Week 1 | Domain | Aggregates, Events, FSM |
| Week 2 | Application | CQRS Handlers, Commands, Queries |
| Week 3 | Infrastructure | DbContext, Repositories, RLS |
| Week 4 | API | Minimal API, Tests, Build Verification |

### Module Completion
| Module | Status | Notes |
|--------|--------|-------|
| CRM | Week 4 DONE | 23 handlers, 19 endpoints |
| Kontrolling | Week 4 DONE | OverheadConfig gap resolved |
| HR | Week 4 DONE | 12 endpoints, 16 tests |
| Maintenance | Week 4 DONE | 12 endpoints, 16 tests |
| QA | Week 4 DONE | 3-param pattern |
| DMS | Week 4 DONE | Document aggregate |
| EHS | Week 1 DONE | Domain layer |

---

## KEY PATTERNS

### Repository Patterns
| Pattern | Use Case |
|---------|----------|
| **2-param (RLS)** | TenantId + EntityId, RLS policy applied |
| **3-param (Explicit)** | TenantId explicit on all methods, IgnoreQueryFilters |
| **Hybrid** | Mix of both (Kontrolling: OverheadConfig=2p, CostAdjustment=3p) |

### Handler Pattern Taxonomy (6 types)
1. **Aggregate Creation** — Factory + Persist + Events
2. **FSM Transition** — Fetch → Domain method → Validate → Persist
3. **Terminal State** — Fetch → Terminal transition → Complex fields → Persist
4. **Cross-Aggregate** — Multi-aggregate coordination
5. **Field Update** — Simple property change without state
6. **Child Entity** — Create/update owned entities

### Build Quality Standard
```
✅ Build succeeded
Errors: 0
Warnings: 0 (or pre-existing only)
```

---

## RAG INFRASTRUCTURE

### Faipar Domain Indexing
- **Documents:** 3 (faipari_gyartasszervezes, faipari_muszaki, woodwork_domain)
- **Chunks:** 837 (faipar-domain category)
- **Total indexed:** 2697 documents
- **Scripts:** `scripts/ingest-faipar-domain.ts`, `scripts/test-faipar-search.ts`

---

## ANTI-PATTERNS (AVOID!)

**DO NOT add to this memory:**
- Session-by-session implementation logs
- Full build output
- Detailed test results (summarize only)
- Code snippets (reference file:line instead)

**Memory should stay <30KB** — only development context and patterns.

---

## ESCALATION PROTOCOL (2026-07-10)

**Ha tapasztalsz:**
- Ismétlődő trigger ugyanazzal (>10× egy órán belül) ⏱️ **AZONNAL ESZKALÁLNI!**
- Rendszer viselkedés ami blokkolja a munkát
- Pipeline automation működés hiba

**TILOS VÁRNI** a user kérdésére! **1-2 órán belül** (10-20 trigger után) **azonnal** írj inbox-ot!

**Akkor:** CRITICAL inbox üzenetet írj a root-nak:
1. Probléma leírás (tünetek + bizonyíték)
2. Root cause analízis (ha van)
3. Impact assessment
4. Javasolt fix (quick + proper + long-term)
5. Testing plan

**Példa:** MSG-ROOT-048 (messageRegistry initialization broken) — de **túl későn eszkaláltam** (33 óra után, nem 1-2 óra után) ⚠️

**Lesson Learned:** Legközelebb 10-20 nudge után (1-2 órán belül) azonnal eszkalálok, nem várok 150+-ra.

---

## SESSION PATTERNS

### Inbox Nudge Loop (Anti-Pattern) — ESCALATED 2026-07-10
**Symptom:** Continuous inbox notifications every 10 minutes for completed tasks
**Root Cause:** ~~Pipeline hasn't marked inbox as READ~~ → **CRITICAL: messageRegistry.db üres (0 byte)**
**Impact:** Pipeline automation teljesen megállt (watchDone.ts nem detektál DONE-okat)
**Action Taken:** MSG-ROOT-048 CRITICAL inbox írva (messageRegistry initialization broken)
**Lesson Learned:** Ha 150+ ismétlődő trigger van 33 órán keresztül → **azonnal eszkalálni kell a root-nak**
**Solution:** Töröld üres DB → restart service → messageRegistry reinit

### Token Optimization (2026-07-02)
- `list_inbox` default: metadata only (~15-20 tokens/message)
- `include_content: true` only for debugging (50× cost increase)
- `status: "UNREAD"` preferred over `"all"` (10× reduction)

---

_Last Updated: 2026-07-10_
_Compressed by Librarian: Removed ~900 lines of session logs_

## DONE: MSG-BACKEND-458 (2026-07-11T01:36:24.869Z)

EHS→HR Integration Event Handlers completed successfully. Implemented DI registration, 3 integration tests, and 2 E2E tests. All 9 tests PASSING (including 4 existing EmployeeRepository tests). TrainingCompletedEventHandler now throws NotFoundException for missing employees. DependencyInjection.cs created with MediatR auto-registration. Security checklist verified: no sensitive data, RLS handling, NotFoundException prevents enumeration, idempotency implemented. Build: 0 errors, 0 warnings. CP-EHS-HR-INTEGRATION checkpoint ready for completion.

---
