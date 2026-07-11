# Outbox Tudás Szintézis — 2026-07

**Forrás:** 1097 outbox üzenet (2026-06-21 → 2026-07-10)
**Szintetizálta:** Librarian terminál
**Dátum:** 2026-07-10

---

## 1. Backend Implementation Patterns

### 1.1 JoineryTech Module Development Workflow

**Forrás:** MSG-BACKEND-188/189/190/191 (EHS Week 1-4)

**4 hetes fejlesztési ciklus:**

| Hét | Layer | Komponensek |
|-----|-------|-------------|
| Week 1 | Domain | Aggregates, Events, FSM, Value Objects |
| Week 2 | Application | CQRS Handlers, Commands, Queries, MediatR |
| Week 3 | Infrastructure | DbContext, RLS, Repositories, Migrations |
| Week 4 | API | Minimal API endpoints, Integration Tests |

**Bevált minta:** Minden hét végén DONE outbox + Architect review.

### 1.2 RLS Implementation Pattern

**Forrás:** MSG-BACKEND-190 (EHS Week 3)

```csharp
// ITenantContext abstraction — clean separation
public interface ITenantContext { Guid TenantId { get; } }

// PostgreSQL session variables via interceptor
SELECT ehs.set_tenant_context('{tenantId}');

// Explicit tenant filtering on all repository methods
Task<T> GetByIdAsync(Guid tenantId, Guid id);
```

**Kulcs döntések:**
- Silent failure — gracefully handles missing RLS function (pre-migration state)
- All indexes include `tenant_id` for RLS performance
- Explicit tenant filtering — no implicit global filters

### 1.3 Owned Entities Mapping

**Forrás:** MSG-BACKEND-190 (EHS Week 3)

**Pattern:** Owned entities → separate tables (nem JSON column)

| Relationship | Table | Benefit |
|--------------|-------|---------|
| 0-1 | `incident_investigations` | SQL querying enabled |
| 0-n | `incident_corrective_actions` | Collection management |
| 0-n | `risk_controls` | Join-based filtering |

**Rationale:** Avoids large JSON columns, enables SQL querying of child entities.

### 1.4 Computed Property Pattern

**Forrás:** MSG-BACKEND-190 (TrainingRecord.Status)

**Pattern:** Status NOT stored in database, calculated on-the-fly:

```csharp
public TrainingStatus Status => ExpiresAt switch
{
    null => TrainingStatus.Valid,
    var exp when exp <= DateTime.UtcNow => TrainingStatus.Expired,
    var exp when exp <= DateTime.UtcNow.AddDays(30) => TrainingStatus.Expiring,
    _ => TrainingStatus.Valid
};
```

**Benefit:** Avoids stale status data, single source of truth.
**Trade-off:** Repository filtering applied client-side after query.

---

## 2. Architecture Review Patterns

### 2.1 Phase 1 MCP Tools Review

**Forrás:** MSG-ARCHITECT-071 (2026-07-07)

**Security kritikus pontok azonosítva:**
1. **Path Traversal** — Component Scaffold `output_dir` whitelist validation
2. **Cycle Detection** — Dependency Resolver circular deps infinite loop

**Approve With Changes workflow:**
- 2 critical issues → must fix before production
- 3 recommended improvements → non-blocking
- Implementation order recommendation included

### 2.2 ADR Alignment Verification

**Pattern:** Minden új tool esetén ADR alignment check:
- ADR-041 (Graph-Based Workflow)
- ADR-049 (Dual Session Architecture)
- ADR-050 (Code Generator Toolchain)

---

## 3. Conductor Coordination Patterns

### 3.1 Multi-Track Dispatch

**Forrás:** Conductor outbox üzenetek

**Pattern:** Párhuzamos A/B/C track-ek → calendar time csökkentés

```
Track A: CRM Week 4 API (Backend)
Track B: Kontrolling Frontend (Frontend)
Track C: EHS Domain (Backend — parallel)
```

**Dependency blocking:** MSG-031 blocked on MSG-030 Phase 3.

### 3.2 Focus Queue Management

**MCP Tools:**
- `get_focus_queue` — aktív és várakozó feladatok
- `set_active_task` — fókusz váltás
- `add_focus_item` — queue bővítés

---

## 4. Frontend Integration Patterns

### 4.1 OpenAPI-First Development

**Forrás:** MSG-FRONTEND-* (CRM, Kontrolling integration)

**Workflow:**
1. Backend → OpenAPI spec (Week 0)
2. Orval → API hooks generálás
3. Frontend → UI implementation with generated hooks
4. Designer → Visual review

**ROI:** $4k→$14k estimated savings per module.

### 4.2 CSS Variables Enforcement

**Forrás:** Designer review outbox

**Pattern:** Designer REJECT hard-coded colors → 100% dark mode compliance

```css
/* ❌ REJECT */
background: #0b1220;

/* ✅ APPROVE */
background: var(--bg-primary);
```

---

## 5. Nightwatch Pipeline Patterns

### 5.1 Watch Module Architecture

**Forrás:** Root outbox (MSG-ROOT-* infrastructure fixes)

| Module | Function |
|--------|----------|
| `watchPriority` | Root + Conductor ALWAYS running |
| `watchDone` | DONE message → reviewer.sh |
| `watchStuck` | Stuck session detection (>15 min idle) |
| `watchInbox` | UNREAD inbox → terminal wake |
| `watchQueue` | Focus queue dispatch |
| `watchResponse` | Response routing → SSE |

### 5.2 Response Routing Pattern

**Probléma:** `type: response` outbox messages did not trigger SSE events

**Megoldás:** `watchResponse.ts` module:
1. Monitors outbox for `type: response`
2. Emits SSE event (`response:routed`)
3. If target session running → tmux nudge

---

## 6. Risk & Mitigation Patterns

### 6.1 Common Vulnerabilities

| Vulnerability | Module | Mitigation |
|---------------|--------|------------|
| AutoMapper GHSA-rvv3-g6hj-g44x | EHS | Upgrade to 13.0.2+ |
| Path Traversal | Component Scaffold | ALLOWED_ROOTS whitelist |
| Circular Dependencies | EPICS.yaml | dagValidator.ts cycle detection |

### 6.2 Test Coverage Gaps

**Pattern:** Tests deferred to Week 4 per scope (infrastructure priority)

**Mitigation workflow:**
1. Week 1-3: Implementation without tests
2. Week 4: 30-40 repository integration tests (Testcontainers)
3. Week 4: API integration tests (E2E smoke)

---

## 7. Key Metrics

| Metric | Value | Source |
|--------|-------|--------|
| Total Outbox Messages | 1097 | File scan |
| Backend DONE | 102 | Terminal outbox |
| Architect Reviews | 44 | Terminal outbox |
| Conductor Coordination | 238 | Terminal outbox |
| BLOCKED Messages | 36 | Filename pattern |
| Escalations | 43 | Filename pattern |

---

## 8. Recommended Knowledge Documents

Based on pattern frequency and value:

| Suggested Doc | Content | Priority |
|---------------|---------|----------|
| `JOINERYTECH_MODULE_WORKFLOW.md` | Week 1-4 development cycle | HIGH |
| `RLS_IMPLEMENTATION_GUIDE.md` | ITenantContext + interceptor | HIGH |
| `OPENAPI_FIRST_WORKFLOW.md` | Contract-first development | MEDIUM |
| `NIGHTWATCH_ARCHITECTURE.md` | 6 watch modules | MEDIUM |

---

**Archival Status:** Ez a szintézis az 1097 outbox üzenet tudásának összefoglalója. Az outbox üzenetek archiválhatók a `terminals/*/archive/` mappákba.

_Generated by Librarian Terminal — 2026-07-10_
