---
id: MSG-ARCH-013-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-010
created: 2026-04-27
---

## Összefoglalás

A `SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md` (61KB, v4 FINAL) + `PHASE_4_README.md` áttanulmányozva és validálva. A tervdok **implementációra kész** — 26 finding beépítve (4 review-n átment), nyitott döntés nincs.

**Egy korábban nem azonosított blokkoló van:** a Kernel `OutboxMessages` séma **inkompatibilis** a spec által feltételezett sémával. Ez Kernel migration + domain model bővítést igényel a Phase 4 implementáció ELŐTT.

---

## 1. Axióma lefedettség — A4-1 → A4-21

**Verdikt: TELJES LEFEDÉS ✅**

| Axióma | Tartalom | Status |
|--------|----------|--------|
| A4-1 | CuttingSheet immutable, Execution sosem módosítja | ✅ Spec §4.2 explicit |
| A4-2 | Sheet → 0..1 aktív Execution | ✅ DB UNIQUE PARTIAL constraint |
| A4-3 | Két különálló FSM | ✅ Sheet.Status ≠ Execution.Status, külön aggregate |
| A4-4 | Mérföldkő rugalmas, befejezés szigorú | ✅ Stage Registry + 7/7 panel completion |
| A4-5 | In-tenant real-time, cross-tenant pull-only | ✅ SignalR in-tenant, ETag-elt pull kifelé |
| A4-6 | Append-only progress events | ✅ DB-trigger UPDATE+DELETE BLOCK (DB-01) |
| A4-7 | Idempotens progress POST | ✅ UUID v7 EventId + DB UNIQUE |
| A4-8 | Completion proof minimum kötelező | ✅ Level 0 (HashOnly) always enforced |
| A4-9 | Crypto-shredding GDPR Art. 17 | ✅ AES-256 per-execution kulcs, ErasedAt mező |
| A4-10 | Cross-tenant policy max(issuer, executor) | ✅ Schedule handler guard |
| A4-11 | Hash-chain integration | ✅ Per-tenant chain, P1-8 LIVE re-use |
| A4-12 | Approved package list érintetlen | ✅ Python sidecar izolált, nem Kernel dep |
| A4-13 | Atomicity over eventual consistency | ✅ Cutting+Inventory shared-tx (be-A05 assertion) |
| A4-14 | Secrets schema-isolation | ✅ Külön spaceos_cutting_secrets schema + role |
| A4-15 | Aggregate fizikai DELETE tilos | ✅ DB-trigger BLOCK + Cancel() FSM-átmenet |
| A4-16 | Két-slot KEK lifecycle | ✅ PRIMARY+PREVIOUS, soha single-key window |
| A4-17 | Per-event worker HMAC | ✅ FixedTimeEquals, WorkerEventHmac VO |
| A4-18 | Async consent withdrawal | ✅ HTTP 202 + ConsentWithdrawalProcessor |
| A4-19 | In-process adapter only | ✅ AssemblyLoadContext startup assertion |
| A4-20 | Outbox-pattern domain event delivery | ✅ Kernel outbox re-use — **de lásd 3. szekció: Kernel séma mismatch BLOCKER** |
| A4-21 | Per-batch DbContext scope BackgroundService-ben | ✅ CreateAsyncScope() per iteration, ChangeTracker.Clear() |

---

## 2. Phase 3 kompatibilitás

**Verdikt: KOMPATIBILIS — de a Phase 3 CuttingExecution stub-ot FELVÁLTJA ✅**

A meglévő `CuttingExecution` aggregate (Phase 3) egy 64-soros stub:
- `Plan/Start/Complete/Fail` FSM
- `throw` alapú hibakezelés (nem `Result<T>`)
- 0 VO, 0 security policy, 0 HMAC, minimális üzleti logika

**Phase 4 viselkedése:**
- A Phase 3 `CuttingExecution` aggregate **teljesen lecserélődik** a Phase 4 verziójával (`Schedule/Start/RecordProgress/RecordOffcut/Complete/Cancel/EvaluateMilestones` FSM, `Result<T>` mindenhol)
- Ez **NEM breaking** a Phase 3 tesztekre — a 303 meglévő teszt a `CuttingSheet`, `CuttingJob`, `CuttingPlan` aggregátokat teszteli; a Phase 3 `CuttingExecution` tesztek értelemszerűen újraírandók (a stub-hoz írt 0-3 teszt várhatóan)
- A Phase 3 `CuttingSheet.Status` FSM **érintetlen marad** (A4-1, A4-3 explicit garantálja)

**Phase 3 CuttingExecution stub eltüntetési stratégia:**
Az új `SpaceOS.Modules.Cutting.Execution.Domain` projektben kerül az új aggregate — a Phase 3-as stub a `SpaceOS.Modules.Cutting.Domain/Aggregates/CuttingExecution.cs` marad mint "legacy placeholder" amíg az Execution projekt el nem készül. A két fájl nem ütközik (különböző namespace: `SpaceOS.Modules.Cutting.Domain` vs `SpaceOS.Modules.Cutting.Execution.Domain`). A Phase 3-as stub explicit `[Obsolete]` attribútummal jelölendő az Inbox #1-ben.

---

## 3. Kernel dependency — BLOKKOLÓ PROBLÉMA

**Verdikt: KERNEL OUTBOX SÉMA INKOMPATIBILIS — Kernel PR KELL ELŐSZÖR ⚠️**

A spec A4-20 és BE-A01 alapján a Phase 4 az **existing Kernel `OutboxMessages` táblát re-use-olja**, és ezt a táblát "Phase 3B LIVE"-ként kezeli. **Ez részben helytelen feltételezés.**

### Mit talált a codebase audit

**LIVE Kernel `OutboxMessages` séma (valóság):**
```
Id          (Guid)
Type        (varchar 200)
Payload     (string)
CreatedAt   (DateTimeOffset)
ProcessedAt (DateTimeOffset?) ← binary pending/done
TenantId    (Guid)
```

**Phase 4 spec által feltételezett séma (`§6.4`):**
```
Id                   (UUID v7)
TenantId
BatchId              ← HIÁNYZIK (SEC-09 batch ordering!)
BatchSequenceNumber  ← HIÁNYZIK (SEC-09 0-based index!)
AggregateId          ← HIÁNYZIK
AggregateType        ← HIÁNYZIK
EventType            ← HIÁNYZIK
PayloadJson          ← létezik "Payload"-ként
OccurredAt           ← létezik "CreatedAt"-ként
DispatchedAt         ← létezik "ProcessedAt"-ként
Status (smallint)    ← HIÁNYZIK (van ProcessedAt null/not-null de nem 3-állapotú enum)
Attempts             ← HIÁNYZIK
LastError            ← HIÁNYZIK
```

**Kritikus hiányok:**
| Mező | Miért kritikus | SEC/BE gate |
|------|----------------|-------------|
| `BatchId` | SEC-09: BATCH-1 gate — hash-chain verifier `BatchId+SeqNum` UNIQUE index-et feltételez | DEPLOYMENT BLOCKER |
| `BatchSequenceNumber` | SEC-09: event-batch ordering reproducibility | DEPLOYMENT BLOCKER |
| `AggregateId` / `AggregateType` | Fan-out routing, `OutboxDispatcher` nem tudja melyik aggregate-hez tartozik | BLOCKER |
| `EventType` | Fan-out: SignalR / hash-chain / Handshake különböző event-típusokra szűr | BLOCKER |
| `Status` enum | `OutboxDispatcher` `Status = 1 (Pending)` feltétel szerint polloz — jelenleg `ProcessedAt IS NULL` | BLOCKER |
| `Attempts` / `LastError` | OUT-2 gate: idempotens retry tracking | Fontos, de nem blokkoló |

**OutboxBackgroundWorker státusz:**
A `SpaceOS.Infrastructure/Outbox/OutboxBackgroundWorker.cs` kommentje szerint:
> *"the real integration bus dispatch will be added when the external message broker infrastructure is provisioned"*

Ez azt jelenti az `OutboxDispatcher` **NEM tartalmaz**:
- SignalR fan-out logikát
- Hash-chain sink-et
- Handshake mirror outbox írást

A Phase 4 `§7.7` flow-diagramban ezek "Phase 3B LIVE"-ként vannak feltüntetve — de valójában **implementálandók** a Kernel Outbox Extension PR-ban.

### Kernel Outbox Extension PR tartalom (BLOCKER — Phase 4 előtt kell)

| Változtatás | Típus | Breaking? |
|-------------|-------|-----------|
| `OutboxMessage` domain model bővítés: `BatchId`, `BatchSequenceNumber`, `AggregateId`, `AggregateType`, `EventType`, `Status` enum, `Attempts`, `LastError` | Additív + séma bővítés | **NEM** — meglévő `Type`/`Payload`/`CreatedAt`/`ProcessedAt` mezők megmaradnak |
| `OutboxMessages` migration: új oszlopok + `IX_Batch` index + `Status` default `1` | Additív | **NEM** |
| `OutboxBackgroundWorker` / `OutboxDispatcher` bővítés: `ISignalROutboxFanOut` + `IHashChainOutboxSink` interface injektálás, fan-out loop | Additív | **NEM** |
| `IOutboxWriter` interface: `AppendAsync(OutboxMessage)` változatlan — csak a model bővül | Additív | **NEM** |

**Becsült effort:** 1.5–2.0 nap — nem triviális (domain model, migration, `OutboxDispatcher` teljes fan-out implementáció), de jól körülhatárolt.

### FlowEpic MicroAssembly migration státusz

A `20260426130223_FlowEpic_Scope_MicroAssembly` migration **LÉTEZIK** a Kernel codebase-ben. A Cabinet 0.2 Inbox #1-ben várt Kernel PR tehát már megtörtént — ez **nem blokkolja** a Cutting Phase 4-et sem.

---

## 4. Security komplexitás értékelés

**Verdikt: ALAPOS, PRODUCTION-READY SPEC ✅**

| Elem | Spec coverage |
|------|---------------|
| KEK két-slot lifecycle | ✅ PRIMARY+PREVIOUS koegzisztencia, `KekRewrapBackgroundService` |
| HMAC per-event | ✅ `WorkerEventHmac` VO + `FixedTimeEquals` IWorkerSecurityPolicy |
| Crypto-shredding | ✅ `PerExecutionKeyVault.EraseAsync` + `CryptographicOperations.ZeroMemory` |
| mTLS + SPKI pin | ✅ `SidecarSpkiPinValidator`, `IHttpClientFactory` named client handler pool |
| Worker consent async withdrawal | ✅ HTTP 202 + `ConsentWithdrawalProcessor` per-batch scope, retroactive photo re-blur |
| Secrets schema isolation | ✅ `spaceos_cutting_secrets` + `spaceos_keyvault_role` NOINHERIT NOLOGIN |
| Append-only triggers | ✅ DB-trigger BLOCK UPDATE+DELETE on ProgressEvents |
| Advisory lock | ✅ `xxHash64` deterministic 63-bit `pg_advisory_xact_lock` |
| Cross-tenant rate-limit | ✅ Redis + `HandshakeRateLimitMiddleware` |
| JWT algorithm allowlist | ✅ `ValidAlgorithms = ["RS256"]` |

**Kiemelt kockázat (spec §13.3-ban is azonosítva):**
- A Phase 3 `CuttingExecution` stub-ban a `CuttingExecutionCompletedEvent` és `WasteRecordedEvent` `RaiseDomainEvent` hívásokkal kerülnek be a Phase 3 outbox-ba. Ha a Kernel Outbox Extension PR **backward-compatible** marad (a meglévő `Type`/`Payload` mezők megmaradnak), ezek a Phase 3-as eventek elvesznek az `OutboxDispatcher` upgrade után — ezt a Kernel PR tesztjei explicit fedniük kell.

---

## 5. Implementációs sorrend (track-okra bontva)

A spec §13.1 ütemterv **konzisztens és helyes** azzal a kiegészítéssel, hogy egy **előkészítő Inbox #0 (Kernel)** szükséges.

### Inbox #0: Kernel Outbox Extension (BLOCKER)

**Scope: 1.5–2.0 nap**
- `OutboxMessage` domain model bővítés: `BatchId`, `BatchSequenceNumber`, `AggregateId`, `AggregateType`, `EventType`, `Status` enum, `Attempts`, `LastError`
- Migration: additív (default `Status = 1`, `Attempts = 0`, nullable `BatchId` és `AggregateId` a meglévő Phase 3 rekordokhoz való kompatibilitásért)
- `OutboxBackgroundWorker` bővítés: `ISignalROutboxFanOut`, `IHashChainOutboxSink`, `IHandshakeMirrorOutboxWriter` interface-ek injektálása + fan-out loop
- `IOutboxWriter` frissítve: az `AppendAsync` az új mezőkkel
- Integration tesztek: SEC-09 batch ordering reproducibility, OUT-1 (rollback → outbox üres), OUT-2 (restart idempotency)

**DoD:** Kernel deploy, `OutboxMessages` migration applied, `OutboxDispatcher` SignalR fan-out LIVE tesztelve ≥1 Cutting Phase 3 event-tel.

### Inbox #1: Phase 4 Domain + Application (Track A)

**Scope: ~7 nap (spec §13.1 Nap 1–13)**
- `SpaceOS.Modules.Cutting.Execution.Domain` projekt: CuttingExecution aggregate (Schedule/Start/RecordProgress/RecordOffcut/Complete/Cancel/EvaluateMilestones), 8 VO, 12 domain event, 4 IMilestonePredicate, 3 Policy interface, 9 Specification (BE-A03)
- `SpaceOS.Modules.Workers.Consent` projekt: worker enrollment + ConsentWithdrawalProcessor (per-batch scope A4-21)
- `SpaceOS.Modules.Cutting.Execution.Application` projekt: 8 command+handler+validator, 6 query, 5 event handler, PredicateFactoryV1
- Phase 3 `CuttingExecution` stub `[Obsolete]` jelölése
- ≥95 unit teszt

**DoD:** Domain 0 public setter, FSM Result<T> mindenhol, 9 specification Ardalis.Specification, ≥95 unit teszt zöld.

### Inbox #2: Phase 4 Infrastructure + Persistence (Track B)

**Scope: ~7 nap (spec §13.1 párhuzam Track A-val)**
- 3 migration (C-0004, C-0005, C-0006) — idempotens DDL
- EF Core configurations, `CuttingExecutionDbContext`, `CuttingExecutionRepository` (AsSplitQuery BE-A10)
- `OutboxInterceptor` regisztráció (Kernel outbox re-use — Inbox #0 után)
- Crypto: TwoSlotMasterKekProvider, KekRewrapBackgroundService (per-batch scope), PerExecutionKeyVault, SecretZeroization
- Sidecar: SidecarImageHardeningClient (IHttpClientFactory), SpkiPinValidator, RequestSigner
- Auth + DI guards: JwtAlgorithmAllowlist, InProcessAdapterAssertion, InventoryAdapterConnectionAssertion (BE-A05)
- ≥60 integration teszt (RLS, hash-chain, crypto-shredding, advisory lock concurrency, Inventory atomic, outbox-tx)

**DoD:** 3 migration applied, RLS FORCE all 5 tables, secrets schema isolated, CONN-1/CONN-2 assertion passes, ≥60 integration teszt zöld.

### Inbox #3: Phase 4 API + Realtime + Tests + Sidecar (Track C)

**Scope: ~5 nap (spec §13.1 Nap 16–19)**
- `SpaceOS.Modules.Cutting.Execution.Api`: Minimal API endpoints (8 command + 6 query + Worker consent + Handshake), SignalR ExecutionHub (IExecutionAccessChecker SEC-15), HandshakeRateLimitMiddleware
- 3 OpenAPI snapshot fájl (BE-A11)
- Python sidecar: FastAPI + insightface ONNX + libmagic MIME + cgroup process isolation + HMAC body-sig
- ≥30 API teszt, ≥10 SignalR teszt, ≥15 Python sidecar teszt
- DoD checklist §9.1–9.9 verifikálása

**DoD:** ≥210 új Phase 4 teszt zöld, meglévő 303 Cutting + ~4023 platform zöld, 3 OpenAPI snapshot committed, sidecar healthcheck zöld.

---

## 6. Effort validálás

**Verdikt: 19 nap + 1.5–2.0 nap Kernel overhead = ~20.5–21 nap REÁLIS ✅**

| Komponens | Nap | Megjegyzés |
|-----------|-----|------------|
| **Kernel Outbox Extension** | **1.5–2.0** | **Nem volt a spec-ben — de blokkoló** |
| Domain + Application (Track A) | ~7.0 | 9 specification, 8 command, 5 event handler |
| Infrastructure + Persistence (Track B) | ~7.0 | 3 migration, crypto stack, sidecar client |
| API + Realtime + Tests + Sidecar (Track C) | ~5.0 | Python sidecar deploy is Track C-ben |
| **Total (becsült)** | **~20.5–21.0** | |

**Fő kockázatok (spec §13.3 kiegészítve):**
1. **Kernel Outbox Extension backward-compat** — Phase 3 eventeket nem szabad elveszíteni az upgrade alatt (explicit tesztelendő)
2. **IOffcutReturnAdapter shared-tx** — Cutting + Inventory shared connection integration teszt bonyolult, `InventoryAdapterIntegrationFixture` külön fixture igényel
3. **Python sidecar deployment** — új service-target a VPS-en, systemd unit + healthcheck integráció

---

## 7. Cross-module dependency összesítő

| Dependency | Típus | Blokkoló? |
|-----------|-------|-----------|
| **Kernel Outbox Extension** (séma + fan-out) | Kernel PR — additív | **IGEN** — Phase 4 Track B OutboxInterceptor regisztráció ezt feltételezi |
| **Modules.Inventory** (`IOffcutReturnAdapter`) | Meglévő interface, shared-tx pattern | **NEM** — de `InventoryAdapterConnectionAssertion` startup-time ellenőrzi |
| **Kernel hash-chain** (P1-8 LIVE) | Re-use — IHashChainService | **NEM** — LIVE |
| **FlowEpic MicroAssembly** (Cabinet 0.2 Kernel PR) | DEPLOYED (migration `20260426130223_FlowEpic_Scope_MicroAssembly`) | **NEM** — már mergelve |
| Phase 3 Cutting (303 teszt) | Zöld kell maradjon | **NEM breaking** — de CuttingExecution stub `[Obsolete]` jelölandő |

---

## 8. Észrevételek (nem blokkolók)

| # | Észrevétel | Hatás |
|---|-----------|-------|
| 1 | A `BuildServiceProvider()` startup-warmup (BE-A05) dokumentált kivétel — a kommentár a spec §7.10-ben indokolt. A `grep`-gate a `§9.9`-ben "1 dokumentált találat"-ot vár. ✅ Helyes megközelítés. | — |
| 2 | A `HMAC backward-compat` kockázat (LapMester migration window, 30 nap, `WorkerHmacRequired` default `false`) jól kezelt a spec §13.3-ban. | — |
| 3 | A spec §6.4-ben az `OutboxMessages` tábla DDL-je a Phase 4 `outbox_messages` (lowercase) névkonvenciót használja — a valós Kernel tábla neve `"OutboxMessages"` (Pascal case, EF Core convention). A Cutting `CuttingExecutionDbContext` `AddInterceptors(OutboxInterceptor)` a Kernel meglévő táblára ír — a névkonvenciót a Kernel EF config definiálja, a Phase 4 agent-nek ezt nem kell felülírnia. | Kis kavarodás-kockázat az agent-nek |
| 4 | Redis single-node failover (P4-4 partial) — Phase 5-re halasztva. Doorstar pilot-hoz elegendő. | — |
| 5 | A sidecar `SpaceOS.Sidecar.ImageHardening` Python service VPS-en a meglévő `/opt/spaceos/` deploy pattern-t követi — a `DEPLOYMENT_RUNBOOK.md`-t a Track C agent frissítse a sidecar systemd unit-tal. | Deploy dokumentáció |
