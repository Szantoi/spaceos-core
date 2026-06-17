# SpaceOS Platform Extension Programme
## Sprint C — Abstractions · Federation · Database · Security · Backend

> **Version:** 4.0 | **Date:** April 2026 | **Status:** DRAFT → BACKEND REVIEW  
> **Architect:** Szántói Gábor  
> **Skills applied:** `/senior-pm` `/database-designer` `/database-schema-designer` `/senior-security` `/senior-backend`

---

## Document Version History

| Ver | Date | Skills | Changes |
|-----|------|--------|---------|
| v1.0 | Apr 2026 | `/senior-pm` | Charter, WSJF, ADR-001..003, backlog, RACI, DoD |
| v2.0 | Apr 2026 | `/database-designer` `/database-schema-designer` | + ERD, DDL migrations 0004–0007, index strategy, RLS, DB-per-Tenant |
| v3.0 | Apr 2026 | `/senior-security` | + Threat model SEC-01..13, ADR-004, HMAC chain, View-per-scope, Node auth, Golden Rule #11, DoD +9 security gates |
| **v4.0** | **Apr 2026** | **`/senior-backend`** | **+ BE-01..15 backend findings, ADR-005, ADR-003 amended (transaction boundary + chain serialization), A-19..22 interfaces, is_online generated column, idempotency key, SIP versioning, LOD endpoint, EF Core config, Golden Rule #12, DoD +8 backend gates, Risk Register +4** |

---

## 1. Executive Summary

Sprint C establishes the foundational contract layer (Abstractions), federated data architecture, complete database schema, security baseline, and backend engineering contracts for the SpaceOS multi-brand, multi-tenant platform. v4.0 integrates the backend engineering review identifying 3 additional CRITICAL findings around data integrity.

> ⚠️ **7 CRITICAL Findings — Deployment Blocked Until All Resolved**  
> SEC-01 SSRF · SEC-02 Plaintext tokens · SEC-03 RLS gaps · SEC-04 Config injection · BE-01 Hash chain race · BE-02 Transaction boundary · BE-03 Idempotency

### Finding Summary

| ID | Severity | Domain | Finding |
|----|----------|--------|---------|
| SEC-01 | 🔴 CRITICAL | Security | NodeManifest.server_url — SSRF vector, no URL validation |
| SEC-02 | 🔴 CRITICAL | Security | ExternalAuthToken plaintext in DB — Key Vault required |
| SEC-03 | 🔴 CRITICAL | Security | DELIVERY_ONLY + SPEC_SHARED RLS not enforced at DB level |
| SEC-04 | 🔴 CRITICAL | Security | set_config() string concatenation — SQL config injection |
| BE-01 | 🔴 CRITICAL | Backend | Hash chain race condition — parallel FSM writes corrupt chain |
| BE-02 | 🔴 CRITICAL | Backend | FSM transition + SyncSignal not in same transaction boundary |
| BE-03 | 🔴 CRITICAL | Backend | SyncSignal no idempotency key — retry causes duplicate chain entries |
| SEC-05 | 🟠 HIGH | Security | Plain SHA-256 chain admin-rewritable — HMAC-SHA256 required |
| SEC-06 | 🟠 HIGH | Security | FULL_TRANSPARENT NULL parent_company_id privilege escalation |
| SEC-07 | 🟠 HIGH | Security | HandshakeAnchor JSONB no CHECK constraint |
| SEC-08 | 🟠 HIGH | Security | SIP Node-to-Node auth not specified |
| BE-04 | 🟠 HIGH | Backend | NodeManifest CRUD API endpoints not specified |
| BE-05 | 🟠 HIGH | Backend | is_online boolean write amplification — 2880 writes/day/Node |
| BE-06 | 🟠 HIGH | Backend | SIP protocol versioning not defined |
| BE-07 | 🟠 HIGH | Backend | LOD federated fetch endpoint not specified |
| BE-08 | 🟠 HIGH | Backend | EF Core version/concurrency token config missing |
| BE-09 | 🟠 HIGH | Backend | JSONB EF Core value converters missing |
| BE-10 | 🟠 HIGH | Backend | PostgreSQL enum EF Core mapping not specified |
| BE-11 | 🟠 HIGH | Backend | Modules DbContext ADR missing — cross-context FK undefined |
| BE-12 | 🟡 MEDIUM | Backend | GenesisHash defined in two places — single source required |
| BE-13 | 🟡 MEDIUM | Backend | 4 interfaces missing from Abstractions backlog |
| BE-14 | 🟡 MEDIUM | Backend | Purge job mechanism not decided |
| BE-15 | 🟡 MEDIUM | Backend | NodeManifestValidator static class violates Golden Rule #2 |

---

## 2. WSJF Prioritisation (v4.0)

> WSJF = (Business Value + Time Criticality + Risk Reduction) / Job Size  
> 🔴 MUST = deployment blocker

| # | Work Item | BV | TC | RR | Size | WSJF | Status |
|---|-----------|----|----|----|----|------|--------|
| C-01 | SEC-01 SSRF mitigation | 10 | 10 | 10 | 2 | 15.0 | 🔴 MUST |
| C-02 | SEC-02 Key Vault / column encryption | 10 | 10 | 10 | 3 | 10.0 | 🔴 MUST |
| C-03 | SEC-03 View-per-scope RLS | 10 | 10 | 10 | 3 | 10.0 | 🔴 MUST |
| C-04 | SEC-04 Parameterized set_config | 10 | 10 | 10 | 1 | 30.0 | 🔴 MUST |
| C-05 | BE-01 FOR UPDATE hash chain lock | 10 | 10 | 10 | 1 | 30.0 | 🔴 MUST |
| C-06 | BE-02 Transaction boundary + ADR-003 amend | 10 | 10 | 10 | 2 | 15.0 | 🔴 MUST |
| C-07 | BE-03 SyncSignal idempotency key | 10 | 10 | 10 | 2 | 15.0 | 🔴 MUST |
| C-08 | Modules.Abstractions A-01..A-22 | 10 | 9 | 10 | 3 | 9.7 | 🟢 GREEN |
| C-09 | ADR-005 Modules DbContext | 9 | 9 | 9 | 2 | 13.5 | 🟡 AMBER |
| C-10 | BE-04 NodeManifest API endpoints | 9 | 10 | 9 | 2 | 14.0 | 🟠 HIGH |
| C-11 | BE-05 is_online generated column | 7 | 8 | 7 | 1 | 22.0 | 🟠 HIGH |
| C-12 | BE-06 SIP versioning header | 8 | 8 | 8 | 1 | 24.0 | 🟠 HIGH |
| C-13 | BE-07 LOD federated fetch endpoint spec | 8 | 9 | 8 | 2 | 12.5 | 🟠 HIGH |
| C-14 | BE-08/09/10 EF Core config specs | 9 | 9 | 9 | 2 | 13.5 | 🟠 HIGH |
| C-15 | DB Migrations 0004–0010 + indexes | 10 | 10 | 10 | 4 | 7.5 | 🟡 AMBER |
| C-16 | SEC-05..10 remaining HIGH security | 8 | 8 | 9 | 4 | 6.3 | 🟠 HIGH |
| C-17 | Golden Rules #9/#10/#11/#12 | 7 | 8 | 7 | 1 | 22.0 | 🟢 GREEN |
| C-18 | Domain Glossary + documentation | 6 | 7 | 5 | 1 | 18.0 | 🟢 GREEN |

---

## 3. Architecture Decision Records

### ADR-001 — Kernel Frozen Boundary

| Field | Value |
|-------|-------|
| Status | ACCEPTED |
| Decision | Kernel is FROZEN. FlowEpic is the atom. FlowTask / Milestone / Project / Program live in Modules.FlowManagement. Kernel stores only: TenantId, FacilityId, FlowEpicId + FSM State + HandshakeId + LastSyncHash + NodeManifest. |
| DB Impact | New Kernel tables: node_manifests, sync_signals. B2BHandshakes gains 4 nullable columns (expand phase). |
| v4 Amendment | NodeManifestValidator refactored from static class to INodeUrlValidator interface (BE-15). Validation logic moves to NodeManifest domain entity as private invariant. |

---

### ADR-002 — Actor Graph & Handshake Visibility Model

| Field | Value |
|-------|-------|
| Status | ACCEPTED |
| Decision | Full graph — any actor initiates Handshake with any other. HandshakeVisibilityScope: PUBLIC_MILESTONE \| DELIVERY_ONLY \| SPEC_SHARED \| FULL_TRANSPARENT. SelfTask valid (Initiator == Responsible). |
| v3 Amendment | DELIVERY_ONLY + SPEC_SHARED enforced via View-per-scope at DB level. FULL_TRANSPARENT requires IS NOT NULL guard on parent_company_id. HandshakeAnchor JSONB has CHECK constraint. |
| v4 Amendment | HandshakeAnchor serialized via System.Text.Json (TypeNameHandling-free). EF Core value converter specified. JSONB columns never use Newtonsoft.Json TypeNameHandling. |

---

### ADR-003 — Offline-First, SIP Sync Protocol & Transaction Boundary *(AMENDED v4.0)*

| Field | Value |
|-------|-------|
| Status | ACCEPTED — AMENDED v4.0 |
| Core Decision | Every Node operates autonomously. Kernel = registry only. SyncSignal is the only data flowing to Kernel. |
| v4 Transaction Boundary | FSM transition + SyncSignal creation MUST execute in a single UnitOfWork transaction. Commit order: (1) FlowEpic state, (2) SyncSignal.Create() with FOR UPDATE lock, (3) AuditEvent.Create(), (4) UnitOfWork.SaveChangesAsync(). PopDomainEvents() + DispatchAsync() only after successful commit. |
| v4 Hash Chain Serialization | GetLastHashAsync() MUST use SELECT ... FOR UPDATE within the same transaction. Per-Tenant chain is intentional — proves cross-Epic event ordering for Escrow legal evidence. Merkle tree deferred to Horizon 4. |
| v4 Idempotency | Every SyncSignal carries client-generated client_signal_id (UUID). DB has UNIQUE(tenant_id, client_signal_id). INSERT uses ON CONFLICT DO NOTHING. |
| v4 SIP Versioning | All SIP HTTP calls MUST include: `SpaceOS-SIP-Version: 1.0`. Kernel rejects unknown versions with 400. |
| v4 Node Auth | Node JWT: iss=spaceos-kernel, sub=tenant_id, aud=spaceos-sip, TTL=72h, node_url claim. Auto-rotation. |
| v3 Amendments | HMAC-SHA256 with Key Vault secret. TLS 1.3 minimum. OfflineQueueService expires_at TTL 30 days. |

---

### ADR-004 — Encryption at Rest

| Field | Value |
|-------|-------|
| Status | ACCEPTED |
| Decision | (1) TDE at tablespace level for Szint 2/3. (2) AES-256-GCM application-layer column encryption for external_auth_token, contract_hash, implementation_summary. (3) Backup: gpg AES-256. (4) AuthorAuthId replaced with HMAC pseudonym. |
| Key Rotation | 90-day automatic via Key Vault. Old key retained 30 days. |

---

### ADR-005 — Modules DbContext Architecture *(NEW v4.0)*

| Field | Value |
|-------|-------|
| Status | ACCEPTED |
| Context | Two schemas: public (Kernel, AppDbContext) and modules (FlowManagement). EF Core cannot span a FK across two DbContext instances. FlowTask.epic_kernel_id references Kernel FlowEpic but lives in modules schema. |
| Decision | (1) Modules.FlowManagement gets own ModulesDbContext in SpaceOS.Infrastructure. (2) epic_kernel_id stored as UUID — no EF navigation property. (3) Application validates epic_kernel_id via IFlowEpicExistsQuery before creating FlowTask. (4) No cross-context FK at DB level — referential integrity at application layer. |
| Consequence | ModulesDbContext uses same PostgreSQL connection with `SET search_path TO modules, public`. Migrations use separate IMigrationsAssembly. |
| Rejected | (1) Single AppDbContext — violates module isolation. (2) Separate database per module — operational overhead for Szint 1. |

---

## 4. Domain Model

### 4.1 Work Hierarchy

| Level | Entity | Owner | DB Location |
|-------|--------|-------|-------------|
| Programme | FlowProgram | Modules.FlowManagement | Node local DB — modules schema |
| Project | FlowProject | Modules.FlowManagement | Node local DB — modules schema |
| Milestone | FlowMilestone | Modules.FlowManagement | Node local DB — modules schema |
| **Epic ← ATOM** | **FlowEpic** | **SpaceOS.Kernel** | **Kernel DB — public schema** |
| Task | FlowTask | Modules.FlowManagement | Node local DB — modules schema |

### 4.2 TenantType & HandshakeVisibilityScope

**TenantType enum:** `Manufacturer` (Cabinet/Door/Window) · `Supplier` · `Dealer` · `Installer` · `Designer` · `Client`

**HandshakeVisibilityScope enum:**

| Scope | What crosses the boundary |
|-------|--------------------------|
| `PUBLIC_MILESTONE` | FSM state only: DONE / NOT DONE / DELAYED |
| `DELIVERY_ONLY` | Shipment data: what, when, where. No price, no margin. |
| `SPEC_SHARED` | Dimensions + material specification. No price. |
| `FULL_TRANSPARENT` | Full data — only intra-company between two units. |

### 4.3 Updated: sync_signals with Idempotency + FOR UPDATE

```sql
CREATE TABLE IF NOT EXISTS sync_signals (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    epic_id             UUID NOT NULL REFERENCES flow_epics(id),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    new_state           TEXT NOT NULL,
    state_hash          VARCHAR(64) NOT NULL,   -- HMAC-SHA256
    previous_hash       VARCHAR(64) NOT NULL DEFAULT
                        '0000000000000000000000000000000000000000000000000000000000000000',
    client_signal_id    UUID NOT NULL,           -- NEW v4: idempotency key
    is_synced_to_kernel BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '30 days',
    occurred_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_sync_signals_client_id
        UNIQUE (tenant_id, client_signal_id)     -- idempotent upsert
);

-- GetLastHashAsync — serialized with FOR UPDATE (BE-01 fix)
SELECT state_hash FROM sync_signals
WHERE  tenant_id = @tenantId
ORDER  BY occurred_at DESC
LIMIT  1
FOR UPDATE;
```

### 4.4 Updated: node_manifests — is_online as Generated Column

```sql
CREATE TABLE IF NOT EXISTS node_manifests (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    server_url           TEXT NOT NULL,   -- SSRF-validated on insert/update
    public_api_version   TEXT NOT NULL DEFAULT '1.0',
    last_heartbeat_at    TIMESTAMPTZ,
    -- is_online is COMPUTED — eliminates 2880 writes/day/Node (BE-05)
    is_online            BOOLEAN GENERATED ALWAYS AS (
        last_heartbeat_at > NOW() - INTERVAL '2 minutes'
    ) STORED,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version              INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT uq_node_manifests_tenant UNIQUE (tenant_id)
);

-- Heartbeat: only one column updated
UPDATE node_manifests SET last_heartbeat_at = NOW() WHERE tenant_id = @id;
```

---

## 5. Sprint C — Backlog

### 5.1 Kernel.Domain Additions

| ID | Deliverable | Detail | Layer | Est. |
|----|-------------|--------|-------|------|
| K-01 | NodeManifest | ServerUrl, ApiVersion, last_heartbeat_at, is_online (generated) | Kernel.Domain | S |
| K-02 | SyncSignal | EpicId + FSM State + HMAC Hash + UpdatedAt + client_signal_id | Kernel.Domain | S |
| K-03 | B2BHandshake extension | + InitiatorAnchor (jsonb+CHECK), ResponsibleAnchor, VisibilityScope, ContractHash | Kernel.Domain | M |

### 5.2 Modules.Abstractions (A-01..A-22)

| ID | Interface | Detail | Folder |
|----|-----------|--------|--------|
| A-01 | IFlowNode | Common base for all flow levels | Abstractions/ |
| A-02 | HandshakeAnchor | VO — Epic/Project/Program anchor | Abstractions/Handshake/ |
| A-03 | HandshakeVisibilityScope | Enum — 4 values | Abstractions/Handshake/ |
| A-04 | HandshakeType | Enum — 8 values incl. SelfTask | Abstractions/Handshake/ |
| A-05 | HandshakeState | Enum — FSM states | Abstractions/Handshake/ |
| A-06 | IHandshake | Full contract interface | Abstractions/Handshake/ |
| A-07 | ITenantProfile | Base actor interface | Abstractions/Actors/ |
| A-08 | TenantType | Enum — 6 values | Abstractions/Actors/ |
| A-09 | ManufacturerType | Enum — Cabinet/Door/Window + B2B model | Abstractions/Actors/ |
| A-10 | ISyncable | Offline-first contract | Abstractions/Sync/ |
| A-11 | INodeManifest | Node self-description + MaxGuestLod | Abstractions/Sync/ |
| A-12 | IParametricProduct | Gyártható termék alap | Abstractions/Products/ |
| A-13 | ICutListCalculator | Strategy — Cabinet/Door/Window | Abstractions/Products/ |
| A-14 | IHardwareResolver | Vasalat lista strategy | Abstractions/Products/ |
| A-15 | IPricingResolver | Supplier ár + Dealer margin | Abstractions/Products/ |
| A-16 | IProductCatalog | Dealer + Manufacturer kínálat | Abstractions/Catalog/ |
| A-17 | ISupplierCatalog | Anyag, ár, készlet | Abstractions/Catalog/ |
| A-18 | IMaterialSpec | Lap típus, vastagság, felület | Abstractions/Catalog/ |
| **A-19** | **INodeUrlValidator** | **Result Validate(string serverUrl) — SSRF validation (replaces static class BE-15)** | **Abstractions/Actors/** |
| **A-20** | **IColumnEncryptionService** | **string Encrypt(string) / string Decrypt(string)** | **Abstractions/Crypto/** |
| **A-21** | **IKeyVaultService** | **Task\<byte[]\> GetSigningKeyAsync / GetEncryptionKeyAsync** | **Abstractions/Crypto/** |
| **A-22** | **INodeAuthService** | **Task\<string\> IssueNodeJwtAsync / Task\<Result\> ValidateNodeJwtAsync** | **Abstractions/Sync/** |

### 5.3 Modules.FlowManagement

| ID | Deliverable | Detail | Est. |
|----|-------------|--------|------|
| F-01 | FlowTask | Aggregate — lokális atom, Kernel NEM látja | M |
| F-02 | FlowMilestone | Aggregate — checkpoint | M |
| F-03 | FlowProject | Aggregate — Epic container | M |
| F-04 | FlowProgram | Aggregate — Project container | M |
| F-05 | FlowNodeResolver | Melyik ID melyik szint | S |
| F-06 | OfflineQueueService | SyncSignal pufferelés + idempotency key | L |

### 5.4 Database Deliverables

| ID | Deliverable | Content | Est. |
|----|-------------|---------|------|
| DB-01 | Migration 0004 | node_manifests + sync_signals + handshake_visibility_scope enum | M |
| DB-02 | Migration 0005 | B2BHandshakes expand (4 nullable columns) | S |
| DB-03 | Migration 0006 | flow_tasks / milestones / projects / programs (modules schema) | L |
| DB-04 | Migration 0007 | offline_sync_queue + expires_at + purge index | S |
| DB-05 | Migration 0008 | View-per-scope: v_handshake_delivery + v_handshake_spec + security_barrier | L |
| DB-06 | Migration 0009 | JSONB CHECK constraints on both anchor columns | S |
| DB-07 | Migration 0010 | ModulesDbContext migration assembly + search_path config | M |
| DB-08 | Index strategy | 7 Kernel + 5 Modules indexes | M |
| DB-09 | RLS policies | 4 policies per scope + tenant isolation | L |
| DB-10 | DB-per-Tenant config | TenantConnectionResolver Szint 1/2/3 | M |

### 5.5 Security Deliverables (S-01..S-13)

> 🔴 **S-01..S-04 are CRITICAL blockers. Must complete before Sprint C closes.**

| ID | Deliverable | Layer | Est. |
|----|-------------|-------|------|
| S-01 | NodeManifestValidator → INodeUrlValidator | Modules.Abstractions | M |
| S-02 | ColumnEncryptionService (AES-256-GCM) | Infrastructure | L |
| S-03 | View-per-scope RLS (Migration 0008) | DB Migration | L |
| S-04 | Parameterized set_config middleware | Infrastructure | S |
| S-05 | HMAC hash chain (SyncSignalHasher) | Infrastructure | M |
| S-06 | RLS NULL guard on FULL_TRANSPARENT | DB Migration | XS |
| S-07 | JSONB CHECK constraints (Migration 0009) | DB Migration | S |
| S-08 | SIP Node JWT spec + INodeAuthService | Kernel.Api | M |
| S-09 | Queue TTL + purge (Migration 0007 amendment) | DB Migration | S |
| S-10 | ADR-004 implementation (TDE + column enc) | Infrastructure | L |
| S-11 | SyncSignal rate limit (50 req/min/tenant) | Kernel.Api | S |
| S-12 | PII pseudonymization (AuthorAuthId → HMAC) | Domain + Infra | M |
| S-13 | LodLevel policy (INodeManifest.MaxGuestLod) | Abstractions | S |

### 5.6 NodeManifest API Endpoints (BE-04)

| Method | Route | Purpose | Auth | Rate Limit |
|--------|-------|---------|------|------------|
| POST | `/api/nodes/register` | Register Node. Validates server_url. Issues Node JWT. | AdminPolicy | 10 req/min/tenant |
| PUT | `/api/nodes/heartbeat` | Update last_heartbeat_at only. is_online is computed. | NodeJwt | 120 req/min/tenant |
| GET | `/api/nodes/{tenantId}/manifest` | Fetch public manifest for federation. | ReadPolicy | 60 req/min |
| GET | `/api/layers/{id}?lod={level}` | Federated layer fetch with LOD enforcement. | NodeJwt | 60 req/min/tenant |
| POST | `/api/sync/signal` | Receive SyncSignal. Idempotent. FOR UPDATE chain lock. | NodeJwt | 50 req/min/tenant (sliding) |

---

## 6. Golden Rules — Ratified in Sprint C

**GOLDEN RULE #9 — Data Sovereignty**
> A Node saját adatai fizikailag el vannak választva. A Kernel csak létezésről és FSM státuszról tud. Tartalom soha nem kerül a Kernelbe — csak HMAC hash. A Handshake-en keresztül a másik fél kizárólag a HandshakeVisibilityScope által engedélyezett adatokat láthatja — View-per-scope pattern DB szinten enforceolva.

**GOLDEN RULE #10 — Offline First**
> Minden Node képes önállóan üzemelni kapcsolat nélkül. A sync eventual consistency. Az OfflineQueueService (30 napos TTL, idempotency key) garantálja, hogy semmi el nem veszik. A Kernel nem single point of failure.

**GOLDEN RULE #11 — Security by Default**
> Minden Node-to-Node kommunikáció TLS 1.3 + signed Node JWT. Minden érzékeny DB oszlop AES-256-GCM titkosítva. Minden URL SSRF-validált. Minden RLS scope DB szinten enforceolt. Minden hash HMAC-SHA256 Key Vault secret-tel.

**GOLDEN RULE #12 — Transactional Integrity**
> FlowEpic FSM átmenet + SyncSignal + AuditEvent egyetlen UnitOfWork tranzakcióban él vagy hal. A hash chain GetLastHashAsync FOR UPDATE-tel serialized. PopDomainEvents() + DispatchAsync() csak sikeres commit után. Nincs részleges state — sem az adatbázisban, sem a chain-ben.

---

## 7. Risk Register (v4.0)

| ID | Risk | P | I | Score | Category | Response | Mitigation |
|----|------|---|---|-------|----------|----------|------------|
| R-01 | Interface contract instability | 2 | 5 | 12.0 | Technical | Mitigate | Semver v1.0.0 lock |
| R-02 | B2BHandshake expand breaks 357 tests | 3 | 4 | 14.4 | Technical | Mitigate | Nullable expand phase |
| R-03 | RLS false positive blocks valid query | 2 | 5 | 14.0 | Technical | Mitigate | Non-superuser test suite |
| R-04 | Key Vault unavailable at startup | 2 | 5 | 14.0 | Technical | Mitigate | Health check gate + dev fallback |
| R-05 | HMAC key rotation breaks chain | 3 | 5 | **18.0** | Security | Mitigate | Old key retained 30d, overlap window |
| R-06 | FOR UPDATE lock timeout at scale | 1 | 3 | 4.2 | Performance | Accept | < 5ms at Doorstar scale. Merkle tree Horizon 4. |
| R-07 | ModulesDbContext migration ordering error | 2 | 4 | 11.2 | Technical | Mitigate | Explicit migration dependency order |
| R-08 | epic_kernel_id dangling reference | 2 | 4 | 11.2 | Technical | Mitigate | IFlowEpicExistsQuery guard before FlowTask create |
| R-09 | SIP version negotiation rejects valid upgrade | 1 | 3 | 4.2 | Technical | Accept | Accept-SIP-Version header for negotiation |
| R-10 | is_online GENERATED ALWAYS on older PG | 1 | 3 | 4.2 | Technical | Accept | Requires PostgreSQL 12+. Kernel runs PG16. |

---

## 8. RACI Matrix

| Activity | Architect | Claude Code | Reviewer | Stakeholder |
|----------|-----------|-------------|----------|-------------|
| Interface design (A-01..A-22) | R/A | C | C | I |
| Security interface design (S-01..13) | R/A | C | C | I |
| DB schema + migrations | R/A | C | C | I |
| Index + RLS + Views | R/A | C | C | I |
| BE-01..03 transaction + concurrency design | R/A | C | C | I |
| ADR-001..005 ratification | C | C | C | A |
| Code generation | A | R | — | I |
| dotnet build + test pass | A | R | C | I |
| RLS non-superuser test | A | R | C | I |
| Concurrency unit test (BE-01) | A | R | C | I |
| Golden Rules #9/#10/#11/#12 sign-off | R/A | — | — | A |
| Sprint C → Sprint D handoff | R/A | — | C | I |

> R = Responsible · A = Accountable · C = Consulted · I = Informed

---

## 9. Definition of Done — Sprint C (v4.0)

### Architecture & Code

- [ ] All Abstractions interfaces A-01..A-22 compile: 0 errors, 0 warnings — `dotnet build`
- [ ] No circular dependency: Abstractions ↛ FlowManagement — Dependency graph
- [ ] B2BHandshake extension does not break 357 existing tests — `dotnet test`
- [ ] ADR-001..ADR-005 all status = ACCEPTED — Architect sign-off
- [ ] Golden Rules #9, #10, #11, #12 in root CLAUDE.md — `git diff`
- [ ] No TODO/FIXME in any committed file — `grep -r TODO`
- [ ] All public interfaces have XML doc comments — Code review

### Database

- [ ] Migrations 0004..0010 run cleanly on PostgreSQL 16 — `dotnet ef database update`
- [ ] All FK columns have B-tree indexes — `\d+ table`
- [ ] sync_signals has UNIQUE(tenant_id, client_signal_id) — `\d+ sync_signals`
- [ ] node_manifests.is_online is GENERATED ALWAYS — no manual writes — `\d+ node_manifests`
- [ ] JSONB CHECK constraints on both anchor columns — INSERT invalid JSON → error
- [ ] RLS non-superuser test: 0 false positives, 0 false negatives — psql test script
- [ ] View-per-scope: security_barrier=true on both views — `\d+ view`
- [ ] ModulesDbContext migrations use 'modules' schema — pg_tables check

### Backend *(NEW — v4.0)*

- [ ] TransitionFlowEpicCommandHandler: FSM + SyncSignal + AuditEvent in single UoW — Integration test: DB rollback on SyncSignal failure
- [ ] GetLastHashAsync uses FOR UPDATE — concurrent test: 2 parallel transitions → sequential chain — Concurrency unit test
- [ ] SyncSignal idempotency: duplicate client_signal_id → 200 OK, no second row in DB — Integration test
- [ ] `/api/nodes/register` rejects private IP ranges and HTTP URLs — Unit test (12 attack vectors)
- [ ] `SpaceOS-SIP-Version` header missing → 400 on `/api/sync/*` and `/api/nodes/*` — Integration test
- [ ] NodeManifestValidator is INodeUrlValidator — not static class — Code review
- [ ] SyncConstants.GenesisHash: single source in C# and migration SQL — `grep -r '000...000'`
- [ ] HandshakeAnchor uses System.Text.Json — zero Newtonsoft.Json references — `grep -r Newtonsoft.Json Modules`

### Security *(MANDATORY — deployment blocked if any unchecked)*

- [ ] NodeManifestValidator rejects private IPs, HTTP, non-443/8443 ports — Unit test (12 attack vectors)
- [ ] ExternalAuthToken never stored plaintext — query DB directly — Integration test
- [ ] set_config: Guid.TryParse guard + parameterized Dapper call — Code review
- [ ] SyncSignalHasher uses HMACSHA256 — `grep -r 'SHA256.HashData'` returns 0
- [ ] FULL_TRANSPARENT RLS: NULL parent_company_id cannot cross tenants — RLS security test
- [ ] Node JWT: iss/aud/sub/node_url/exp all validated on every SyncSignal — Integration test
- [ ] TLS 1.0/1.1 disabled on all Node endpoints — `nmap --script ssl-enum-ciphers`
- [ ] Rate limit: 51st SyncSignal/min → HTTP 429 — Load test
- [ ] Rate limit: 51st node registration/min → HTTP 429 — Load test

---

## 10. Module & DB Dependency Graph

```
SpaceOS.Kernel.Domain              ← FROZEN (atom: FlowEpic)
  │  Tables (public schema):
  │    tenants, facilities, flow_epics,
  │    b2b_handshakes, node_manifests, sync_signals  ← NEW v3/v4
  │
  └── SpaceOS.Modules.Abstractions    ← interfaces + VOs, zero logic (A-01..A-22)
        │
        ├── SpaceOS.Modules.FlowManagement
        │     Tables (modules schema):
        │       flow_programs, flow_projects, flow_milestones,
        │       flow_tasks, offline_sync_queue  ← all NEW
        │
        ├── SpaceOS.Modules.Joinery          (Sprint D)
        ├── SpaceOS.Modules.Handshake         (Sprint D)
        ├── SpaceOS.Modules.Client            (Horizon 2)
        └── SpaceOS.Modules.InteriorDesign    (Horizon 2)

RULE: Modules may reference Kernel entity IDs. Never Kernel types.
RULE: No module references another module — only Abstractions.
RULE: Kernel DB = public schema. Node DB = modules schema.
RULE: epic_kernel_id is UUID only — no EF navigation property to FlowEpic.
```

---

## 11. Next Steps — Post Sprint C

| Sprint | Label | Scope | Unblocked by |
|--------|-------|-------|-------------|
| D | Joinery Framework | Modules.Joinery: ICutListCalculator impls. Cabinet + Door. Unit tests. | Sprint C — ALL DoD criteria green |
| E | Tool Registry | Orchestrator: real Kernel API calls. calculate_cabinet LLM tool. SIP v1.0. | Sprint D — Joinery interface |
| F | JoineryTech Portal | Turborepo: apps/joinerytech. Dark mode. CutList. BFF clone. | Sprint D — Joinery API |
| G | Doorstar Onboarding | DoorstarProfile. Szint 2 DB. Key Vault provisioned. Node registered. Demo. | Sprint F — Portal |
| H | Client Framework | Modules.Client: ProjectIntent, ApprovalWorkflow, EscrowTrigger. | Sprint C ADR-002 |
| I | External Pen Test | Third-party pentest: SIP protocol, RLS, Node JWT, SSRF. | Sprint G — Doorstar live |
| J | Merkle Tree (optional) | Per-Epic chains + periodic cross-Epic anchor. Only if scale demands. | Horizon 4 |

---

## 19. Security Threat Model — STRIDE Analysis

> S = Spoofing · T = Tampering · R = Repudiation · I = Information Disclosure · D = Denial of Service · E = Elevation of Privilege

### SEC-01 🔴 CRITICAL — SSRF: NodeManifest.server_url

| Field | Value |
|-------|-------|
| STRIDE | Information Disclosure + Server-Side Request Forgery |
| Attack | Malicious Tenant registers server_url pointing to `169.254.169.254`, localhost admin APIs, or `file://` URIs. |
| Impact | Full compromise of host cloud metadata → credential theft, lateral movement. |

```csharp
// SpaceOS.Modules.Abstractions/Actors/INodeUrlValidator.cs
public interface INodeUrlValidator
{
    Result Validate(string serverUrl);
}

// Implementation:
private static readonly string[] BlockedCidrs =
    ["169.254.", "10.", "192.168.", "172.16.", "127.", "0.", "::1"];

public Result Validate(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return Result.Error("Invalid URL format");
    if (uri.Scheme != "https")
        return Result.Error("HTTPS required");
    if (BlockedCidrs.Any(cidr => uri.Host.StartsWith(cidr)))
        return Result.Error("Private IP range blocked");
    var port = uri.Port == -1 ? 443 : uri.Port;
    if (port != 443 && port != 8443)
        return Result.Error("Only ports 443 and 8443 permitted");
    return Result.Success();
}
```

### SEC-02 🔴 CRITICAL — Plaintext ExternalAuthToken

```csharp
// AES-256-GCM envelope encryption — Infrastructure layer
public string Encrypt(string plaintext)
{
    var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
    RandomNumberGenerator.Fill(nonce);
    using var aes = new AesGcm(_dataEncryptionKey, AesGcm.TagByteSizes.MaxSize);
    var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];
    var tag = new byte[AesGcm.TagByteSizes.MaxSize];
    aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);
    return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
}
```

### SEC-03 🔴 CRITICAL — RLS Incomplete: View-per-scope

```sql
-- Migration 0008: security_barrier views enforce column-level visibility at DB
CREATE VIEW v_handshake_delivery AS
    SELECT id, guest_tenant_id, delegated_at, visibility_scope, contract_hash
    FROM b2b_handshakes
    WHERE visibility_scope = 'DELIVERY_ONLY';

CREATE VIEW v_handshake_spec AS
    SELECT id, guest_tenant_id, delegated_at, visibility_scope,
           contract_hash, responsible_anchor
    FROM b2b_handshakes
    WHERE visibility_scope = 'SPEC_SHARED';

ALTER VIEW v_handshake_delivery SET (security_barrier = true);
ALTER VIEW v_handshake_spec     SET (security_barrier = true);
```

### SEC-04 🔴 CRITICAL — set_config() Injection

```csharp
// ❌ DANGEROUS
await conn.ExecuteAsync($"SELECT set_config('app.current_tenant_id', '{tenantId}', true)");

// ✅ CORRECT
if (!Guid.TryParse(tenantIdString, out var tenantGuid))
    throw new SecurityException("Invalid tenant ID format in JWT claim");

await conn.ExecuteAsync(
    "SELECT set_config('app.current_tenant_id', @id::text, true)",
    new { id = tenantGuid.ToString("D") });
```

### SEC-05 🟠 HIGH — HMAC-SHA256 Hash Chain

```csharp
// HMAC-SHA256 — tamper PROOF (vs plain SHA-256 which is only tamper EVIDENT)
var signingKey = await _keyVault.GetSigningKeyAsync(tenantId, ct).ConfigureAwait(false);
var chainInput = $"{previousHash}:{payloadJson}:{occurredAt:O}";
using var hmac = new HMACSHA256(signingKey);
var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(chainInput));
return Convert.ToHexString(bytes).ToLowerInvariant();
```

### SEC-06 🟠 HIGH — FULL_TRANSPARENT NULL Bypass

```sql
-- Fixed: explicit IS NOT NULL guard
AND t1.parent_company_id IS NOT NULL
AND t1.parent_company_id = t2.parent_company_id
```

### SEC-07 🟠 HIGH — JSONB CHECK Constraint

```sql
ALTER TABLE b2b_handshakes
    ADD CONSTRAINT chk_initiator_anchor_schema CHECK (
        initiator_anchor IS NULL OR (
            jsonb_typeof(initiator_anchor) = 'object'
            AND initiator_anchor ? 'anchor_type'
            AND initiator_anchor ? 'tenant_id'
            AND initiator_anchor ? 'flow_node_id'
            AND (initiator_anchor->>'anchor_type') IN ('Epic','Project','Program')
            AND (initiator_anchor->>'tenant_id') ~ '^[0-9a-f-]{36}$'
        )
    );
```

### SEC-08 🟠 HIGH — Node JWT Specification

```json
{
  "iss": "spaceos-kernel",
  "sub": "<tenant_id>",
  "aud": "spaceos-sip",
  "iat": 1714000000,
  "exp": 1714259200,
  "node_url": "https://...",
  "tier": 2
}
```

---

## 20. Backend Engineering Review — BE-01..15

### BE-01 🔴 CRITICAL — Hash Chain Race Condition

| Field | Value |
|-------|-------|
| Category | Concurrency / Data Integrity |
| Problem | Two parallel FSM transitions on the same Tenant both read the same previous_hash before either writes. Chain becomes inconsistent silently. |
| Why Transaction Alone Doesn't Fix It | Transaction prevents partial writes but not concurrent reads. Both transactions can read the same 'last hash' before either commits. |
| Fix | `SELECT ... FOR UPDATE` within the UnitOfWork transaction. Second writer blocks until first commits. Lock hold time < 5ms at Doorstar scale. |

```csharp
// SyncSignalRepository.GetLastHashAsync — v4.0
public async Task<string?> GetLastHashAsync(TenantId tenantId, CancellationToken ct)
{
    // Must be called within an open transaction (UnitOfWork.Begin())
    return await _context.SyncSignals
        .FromSqlRaw("""
            SELECT * FROM sync_signals
            WHERE  tenant_id = {0}
            ORDER  BY occurred_at DESC
            LIMIT  1
            FOR UPDATE
            """,
            tenantId.Value)
        .AsNoTracking()
        .Select(s => s.StateHash)
        .FirstOrDefaultAsync(ct)
        .ConfigureAwait(false);
}
```

### BE-02 🔴 CRITICAL — FSM + SyncSignal Transaction Boundary

```csharp
// TransitionFlowEpicCommandHandler — v4.0
public async Task<Result> Handle(TransitionFlowEpicCommand cmd, CancellationToken ct)
{
    await using var tx = await _unitOfWork.BeginTransactionAsync(ct).ConfigureAwait(false);

    var epic = await _epicRepo.GetByIdAsync(cmd.EpicId, ct).ConfigureAwait(false);
    if (epic is null) return Result.NotFound();

    epic.Transition(cmd.NewState);

    // FOR UPDATE inside same transaction — serialized
    var prevHash = await _syncRepo.GetLastHashAsync(epic.TenantId, ct).ConfigureAwait(false)
                   ?? SyncConstants.GenesisHash;
    var signal = SyncSignal.Create(epic, prevHash, cmd.ClientSignalId);

    _epicRepo.Update(epic);
    _syncRepo.Add(signal);
    // AuditEvent added by DomainEventDispatcher via interceptor

    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    await tx.CommitAsync(ct).ConfigureAwait(false);

    // Only AFTER commit
    await _dispatcher.DispatchAsync(epic.PopDomainEvents(), ct).ConfigureAwait(false);

    return Result.Success();
}
```

### BE-03 🔴 CRITICAL — SyncSignal Idempotency

```sql
INSERT INTO sync_signals
    (id, epic_id, tenant_id, new_state, state_hash, previous_hash,
     client_signal_id, occurred_at)
VALUES
    (@id, @epicId, @tenantId, @state, @hash, @prevHash, @clientSignalId, @occurredAt)
ON CONFLICT (tenant_id, client_signal_id)
DO NOTHING;   -- retry is silently discarded
```

### BE-05 🟠 HIGH — is_online Write Amplification

**Problem:** Every heartbeat updates a boolean — 2880 writes/day/Node.  
**Fix:** GENERATED ALWAYS computed column (see §4.4). Heartbeat updates only `last_heartbeat_at`.

### BE-06 🟠 HIGH — SIP Versioning

```csharp
app.Use(async (ctx, next) => {
    var version = ctx.Request.Headers["SpaceOS-SIP-Version"].FirstOrDefault();
    if (ctx.Request.Path.StartsWithSegments("/api/sync") ||
        ctx.Request.Path.StartsWithSegments("/api/nodes"))
    {
        if (version != "1.0")
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new {
                error = "SIP version not supported",
                supported = new[] { "1.0" }
            });
            return;
        }
    }
    await next();
});
```

### BE-08 🟠 HIGH — EF Core Concurrency Token

```csharp
builder.Property(x => x.Version)
       .HasColumnName("version")
       .IsConcurrencyToken()  // throws DbUpdateConcurrencyException on stale write
       .HasDefaultValue(1);

builder.HasQueryFilter(x => x.DeletedAt == null);  // soft delete

builder.Property(x => x.ImplementationSummary)
       .HasColumnType("jsonb")
       .HasConversion(
           v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
           v => JsonSerializer.Deserialize<ImplementationSummary>(v, JsonSerializerOptions.Default));
```

### BE-10 🟠 HIGH — PostgreSQL Enum EF Core Mapping

```csharp
// In AppDbContext.OnModelCreating:
modelBuilder.HasPostgresEnum<HandshakeVisibilityScope>("handshake_visibility_scope");

// Entity configuration:
builder.Property(x => x.VisibilityScope)
       .HasColumnType("handshake_visibility_scope");
```

### BE-11 🟠 HIGH — Modules DbContext (resolved by ADR-005)

```csharp
public class ModulesDbContext : DbContext
{
    public DbSet<FlowTask>      FlowTasks      => Set<FlowTask>();
    public DbSet<FlowProject>   FlowProjects   => Set<FlowProject>();
    public DbSet<FlowProgram>   FlowPrograms   => Set<FlowProgram>();
    public DbSet<FlowMilestone> FlowMilestones => Set<FlowMilestone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("modules");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModulesDbContext).Assembly,
            t => t.Namespace?.Contains("Modules") == true);
    }
}

// epic_kernel_id — UUID only, NO navigation property
// Validated at Application layer:
// var epicExists = await _epicExistsQuery.ExistsAsync(cmd.EpicKernelId, ct);
```

### BE-12 🟡 MEDIUM — GenesisHash Single Source

```csharp
// SpaceOS.Modules.Abstractions/Sync/SyncConstants.cs
public static class SyncConstants
{
    public const string GenesisHash =
        "0000000000000000000000000000000000000000000000000000000000000000";

    public const string SipVersion      = "1.0";
    public const int    NodeJwtTtlHours = 72;
    public const int    OfflineQueueTtlDays = 30;
}
```

### BE-14 🟡 MEDIUM — Purge Job Mechanism Decision

| Option | Pro | Con |
|--------|-----|-----|
| `pg_cron` | DB-szintű, no app dependency | Szint 3 On-Premise-en nem mindig elérhető |
| `IHostedService` (.NET) | Minden Node-on fut | Ha Node le van állítva, nem fut |
| Hangfire | Persistent job store | Külső dependency |

> **Decision needed before Sprint D.** Recommendation: `IHostedService` for Szint 1/2, `pg_cron` optional for Szint 3.

---

*SpaceOS Platform Extension Programme · Sprint C Charter v4.0 · April 2026 · CONFIDENTIAL*
