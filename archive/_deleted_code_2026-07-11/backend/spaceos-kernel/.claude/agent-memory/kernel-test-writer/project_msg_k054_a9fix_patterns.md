---
name: MSG-K054 A9 companion test patterns
description: StageRegistry handler companion unit tests — placement, constructor patterns, IStageChainValidator mock behavior
type: project
---

StageRegistry handler companion tests were written as an A9 violation fix. 14 handlers tested, 64 new tests added.

**Location:** `SpaceOS.Kernel.Tests/StageRegistry/Handlers/` — new subdirectory (not flat under Application/).

**Handler constructor patterns:**
- `RegisterStageDefinitionCommandHandler(IStageDefinitionRepository, IUnitOfWork, IDomainEventDispatcher)`
- `UpdateStageDefinitionCommandHandler(IStageDefinitionRepository, IUnitOfWork, IDomainEventDispatcher)`
- `DeactivateStageDefinitionCommandHandler(IStageDefinitionRepository, IUnitOfWork, IDomainEventDispatcher)`
- `CreateStageChainTemplateCommandHandler(IStageChainTemplateRepository, IUnitOfWork, IDomainEventDispatcher)`
- `AddStageChainStepCommandHandler(IStageChainTemplateRepository, IStageDefinitionRepository, IUnitOfWork, IDomainEventDispatcher)`
- `RemoveStageChainStepCommandHandler(IStageChainTemplateRepository, IUnitOfWork, IDomainEventDispatcher)`
- `AssignChainCommandHandler(IFlowEpicRepository, IStageChainTemplateRepository, IUnitOfWork, IDomainEventDispatcher)`
- `AdvanceFlowEpicStageCommandHandler(IFlowEpicRepository, IStageChainTemplateRepository, IStageChainValidator, IUnitOfWork, IDomainEventDispatcher)`
- `SkipOptionalStageCommandHandler(IFlowEpicRepository, IUnitOfWork, IDomainEventDispatcher)`
- Query handlers: single repository constructor arg.

**IStageChainValidator behavior:** `ValidateAdvance` is void and throws `DomainException` on invalid transitions. The handler does NOT catch — it propagates. Use `Assert.ThrowsAsync<DomainException>` for the failure test, not `ResultStatus.Error`.

**FlowEpic with chain for Advance/Skip tests:**
```csharp
var epic = FlowEpic.Create("Test Epic", TestFacilityId, TestTenantId);
epic.AssignChain(chainId, "stage_a");
epic.PopDomainEvents();
```

**StageChainTemplate with step (for RemoveStep test):**
```csharp
var template = StageChainTemplate.Create(TenantId, "Standard Chain");
var definition = StageDefinition.Register(TenantId, "review_step", "Review Step", "http://127.0.0.1:5000");
template.AddStep(definition, 1);
template.PopDomainEvents();
```

**AddStageChainStepCommandHandler uses `GetByIdWithStepsAsync` for chain, `GetByIdAsync` for definition.**

**`AdvanceFlowEpicStageCommandHandler` — EpicHasNoChainAssigned returns `ResultStatus.Error` (not NotFound).**

**StageHandoff.Create params:** `(tenantId, flowEpicId, sourceStage, targetStage, nextVersion, idempotencyKey, payloadJson, sourceActorId, targetActorId, handshakeId?)` — HashAlgorithm is always "SHA-256".

**Total tests after A9 fix:** 1068 passing (881 unit + 101 integration + 86 api), 4 skipped (pre-existing in Api.Tests).
