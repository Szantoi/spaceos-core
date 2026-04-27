---
id: MSG-CUTTING-043-DONE
from: cutting
to: root
type: done
status: READ
---

## Összefoglaló

Phase 4 Track A implementálva: két új projekt létrehozva és a solution-hoz adva.

### Új projektek

**`src/SpaceOS.Modules.Cutting.Execution.Domain/`**
- `CuttingExecution` aggregate — 7-state FSM (Scheduled → Started → InProgress → Completed / Cancelled / Failed)
- 7 Value Object: `WorkerEventHmac`, `CompletionProof` (3 level), `ProgressEventId`, `OffcutEvent`, `WorkerAssignment`, `ScheduleWindow`, `ExecutionKey`
- 3 owned Entity: `ProgressEvent`, `OffcutReport`, `MilestoneSubscription`
- 12 domain event (sealed records, `IDomainEvent + INotification`)
- 3 policy interface port: `IWorkerSecurityPolicy`, `ICuttingProofPolicy`, `ICuttingProgressPolicy`
- `ICuttingExecutionRepository` interface
- `IMilestonePredicate` + 4 built-in: `PanelCompletionPredicate`, `TimeWindowPredicate`, `QualityCheckPredicate`, `WorkerConsentPredicate`
- 9 Ardalis.Specification: `CuttingExecutionByIdSpec`, `ActiveExecutionsByTenantSpec`, `ExecutionsBySheetSpec`, `ExecutionsByMachineAndDateSpec`, `ExecutionsByHandshakeEpicSpec`, `PendingMilestonesSpec`, `ExecutionsByConsentScopeSpec`, `ConsentAffectedPhotoCountSpec`, `ExecutionKeyByExecutionSpec`
- 7 enum: `CuttingExecutionStatus`, `ProofLevel`, `ProgressEventKind`, `MilestoneKind`, `CancelReason`, `ConsentScope`, `MilestoneStatus`

**`src/SpaceOS.Modules.Cutting.Execution.Application/`**
- 8 Command + Handler + Validator: Schedule, Start, RecordProgress, RecordOffcut, Complete, Cancel, EvaluateMilestones, WithdrawWorkerConsent
- 6 Query + Handler: GetExecution, ListExecutions, GetProgress, GetMilestones, GetCompletionProof, GetWorkerConsent
- 6 Event Handler: ProgressRecorded, ExecutionCompleted, CompletionProofCommitted, MilestoneReached, PanelCompleted, OffcutReported
- 5 port interface: `ICuttingExecutionRealtimePublisher`, `ICuttingHashChainSink`, `IStageRegistry`, `IOffcutNotificationSink`, `ICuttingAuditLogger`
- `IConsentWithdrawalRepository` + `ConsentWithdrawal` entity
- `PredicateFactoryV1` (JSON-driven factory)
- 6 DTO record

### Módosított fájlok
- `CuttingExecution.cs` (Phase 3 stub): `[Obsolete]` attribútum hozzáadva
- `ICuttingRepository.cs`: `#pragma warning disable CS0618` a stub referenciáknál
- `CuttingDbContext.cs`, `CuttingExecutionConfiguration.cs`, `CuttingRepository.cs`: pragma suppress
- `CuttingExecutionTests.cs`, `CuttingRepositoryTests.cs`: pragma suppress
- `SpaceOS.Modules.Cutting.Tests.csproj`: 2 új ProjectReference + Ardalis.Specification
- `SpaceOS.Modules.Cutting.sln`: 2 új projekt hozzáadva

## Tesztek

**408 / 408 PASS** (volt: 303 — 105 új teszt)

Új tesztosztályok (`tests/Execution/`):
- `CuttingExecutionFsmTests.cs` — 28 teszt (FSM átmenetek, edge case-ek)
- `ValueObjectTests.cs` — 27 teszt (összes VO)
- `MilestonePredicateTests.cs` — 19 teszt (mind 4 predicate + factory + evaluate integration)
- `DomainEventTests.cs` — 10 teszt (domain event dispatch, PopDomainEvents clearance)
- `CommandHandlerTests.cs` — 18 teszt (minden handler success + failure path)
- `SpecificationTests.cs` — 9 teszt (mind 9 spec WhereExpression ellenőrzése inline list-en)

## Security review

- `[Obsolete]` attribútum nem töri el a meglévő kódot (csak warning, pragma-val suppressálva az örök helyszíneken)
- `WorkerEventHmac.FixedTimeEquals` — `CryptographicOperations.FixedTimeEquals` használ timing-safe összehasonlítást
- `ExecutionKey.Generate()` — `RandomNumberGenerator.GetBytes(32)` (AES-256, CSPRNG)
- Összes VO `Result<T>` factory — nem throw, nem silent swallow
- FSM tranzíciók Result<T>-t adnak vissza, nem throw-olnak

## Kockázatok / kérdések

Nincs. Phase 4 Track B (Infrastructure + API) következhet.
