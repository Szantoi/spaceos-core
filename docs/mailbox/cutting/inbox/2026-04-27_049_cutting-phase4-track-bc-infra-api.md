---
id: MSG-CUTTING-049
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-043-DONE
created: 2026-04-27
---

# CUTTING-049 — Phase 4 Track B+C: Infrastructure + Persistence + API (Nap 13–19)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md` — Section 6, 7, 8
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CUTTING-048 ✅ (408 teszt, Domain + Application kész) + KERNEL-103 ✅ (Outbox Extension DEPLOYED)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track B: Infrastructure + Persistence (~7 nap)

### EF Core + Migrations

- `CuttingExecutionDbContext` (vagy a meglévő CuttingDbContext bővítés)
- EF configurations: CuttingExecution, ProgressEvent, OffcutReport, MilestoneSubscription, ConsentWithdrawal, StaffAuditLog
- 3 migration (spec §6): 
  - `C_0004_ExecutionAggregate` — CuttingExecutions tábla + ProgressEvents + OffcutReports + MilestoneSubscriptions
  - `C_0005_SecretsSchema` — `spaceos_cutting_secrets` schema + ExecutionKeys tábla + role isolation (A4-14)
  - `C_0006_ConsentWithdrawals` — ConsentWithdrawals tábla + StaffAuditLog
- RLS FORCE minden új táblán + COALESCE(NULLIF(...)) pattern (Cutting Phase 3 tanulság!)
- Append-only trigger: `ProgressEvents` UPDATE+DELETE BLOCK (A4-6, DB-01)
- UNIQUE partial index: aktív Execution per Sheet (A4-2)

### Crypto stack (A4-9, A4-16)

- `TwoSlotMasterKekProvider` — PRIMARY+PREVIOUS KEK (SEC-01)
- `KekRewrapBackgroundService` — per-batch scope (A4-21, BE-A02)
- `PerExecutionKeyVault` — AES-256 per-execution key, Erase() crypto-shredding
- `CryptographicOperations.ZeroMemory` secret zeroization (SEC-14)

### Outbox integration (A4-20)

- `OutboxInterceptor` regisztráció a CuttingDbContext-re (Kernel Outbox re-use)
- SaveChanges → outbox append egy tranzakcióban (BE-A08)

### Adapters

- `IOffcutReturnAdapter` — Inventory shared-tx (A4-13, BE-A05 connection assertion)
- Sidecar client: `IHttpClientFactory` named "sidecar" + SPKI pin (BE-A04)

---

## Track C: API + Realtime (~5 nap)

### Minimal API endpoints

- 8 command endpoint (POST): Schedule, Start, RecordProgress, RecordOffcut, Complete, Cancel, EvaluateMilestones, WithdrawWorkerConsent
- 6 query endpoint (GET): GetExecution, ListExecutions, GetProgress, GetMilestones, GetCompletionProof, GetWorkerConsent
- Worker consent endpoints
- Handshake cross-tenant endpoint (ETag rate-limit)

### SignalR (A4-5)

- `ExecutionHub` — in-tenant real-time progress
- `IExecutionAccessChecker` (SEC-15: group authorization)

### Auth + Security

- JWT algorithm allowlist `["RS256"]` (SEC-05)
- `InProcessAdapterAssertion` startup guard (A4-19)

---

## Tesztek (125+)

**Infrastructure (60+):** RLS isolation, crypto round-trip, advisory lock, Inventory atomic, outbox-tx, append-only trigger
**API (30+):** endpoint routing, auth, SignalR hub, handshake rate-limit
**Integration (35+):** full flow Schedule→Start→Progress→Complete, crypto-shredding E2E

---

## Definition of Done

- [ ] EF configurations + 3 migration (COALESCE pattern!)
- [ ] RLS FORCE + append-only trigger
- [ ] Crypto: TwoSlotMasterKekProvider + PerExecutionKeyVault + KekRewrap
- [ ] OutboxInterceptor (Kernel re-use)
- [ ] API endpoints (14 endpoint) + SignalR ExecutionHub
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 533 pass (408 előző + 125 új)
- [ ] Outbox DONE
