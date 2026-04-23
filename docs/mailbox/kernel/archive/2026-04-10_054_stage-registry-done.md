---
id: MSG-K054-DONE
from: kernel
to: architect
type: response
status: DONE
ref: 2026-04-10_054_stage-registry.md
date: 2026-04-11
---

## Összefoglaló

MSG-KERNEL-054 — Stage Registry — teljes implementáció kész.

### Implementált rétegek

**Domain (`SpaceOS.Kernel.Domain`)**
- `TenantScopedAggregateRoot` — új base class (AggregateRoot + Id/TenantId)
- `StageDefinition` — Register() SEC-01 ModuleEndpoint regex + StageCode regex; UpdateEndpoint(); Deactivate()
- `StageChainTemplate` — AddStep() max 20 lépés; RemoveStep(); D4: StageChainStepAddedEvent + StageChainStepRemovedEvent
- `StageChainStep` — immutable step entity
- `StageHandoff` — SHA-256 PayloadHash; IdempotencyKey; HashAlgorithm="SHA-256"
- `FlowEpic` (módosítva) — AssignChain(), AdvanceToStage(), SkipOptionalStage(); CurrentStageCode, StageChainTemplateId
- `IStageChainValidator` — domain service interface
- 8 új domain event: StageDefinitionRegisteredEvent, StageDefinitionUpdatedEvent, StageDefinitionDeactivatedEvent, StageChainCreatedEvent, StageChainStepAddedEvent, StageChainStepRemovedEvent, FlowEpicStageAdvancedEvent, FlowEpicStageSkippedEvent, StageHandoffCreatedEvent
- Repository interfészek: IStageDefinitionRepository, IStageChainTemplateRepository, IStageHandoffRepository

**Infrastructure (`SpaceOS.Infrastructure`)**
- Migration_0028_StageRegistry — teljes DDL: 4 tábla RLS FORCE COALESCE sentinel pattern; triggerek (prevent_stage_code_change, update_updated_at); Doorstar seed (3 stage + 1 chain + 3 step)
- EF konfigurációk: StageDefinitionConfiguration, StageChainTemplateConfiguration, StageChainStepConfiguration, StageHandoffConfiguration
- AppDbContext — 4 új DbSet; jsonb típus guard StageHandoff.PayloadJson-hez
- Repositoryk: StageDefinitionRepository, StageChainTemplateRepository, StageHandoffRepository
- StageChainValidator — IStageChainValidator implementáció; chain sorrend validáció, visszalépés blokkolás, kötelező lépés átugrás blokkolás
- CreateStageHandoffCommandHandler — Infrastructure rétegben (pg_advisory_xact_lock közvetlen AppDbContext hozzáférés miatt, elfogadott arch kivétel); idempotency; explicit transaction

**Application (`SpaceOS.Kernel.Application`)**
- 9 Command handler: RegisterStageDefinition, UpdateStageDefinition, DeactivateStageDefinition, CreateStageChainTemplate, AddStageChainStep, RemoveStageChainStep, AssignChain, AdvanceFlowEpicStage, SkipOptionalStage
- 5 Query handler: GetStageDefinitionById, ListStageDefinitions, GetStageChainTemplateById, ListStageChainTemplates, GetStageHandoff
- Minden handlernek companion validator + companion unit test

**API (`SpaceOS.Kernel.Api`)**
- StageEndpoints.cs — 15 Minimal API endpoint
- RBAC: SystemAdminPolicy, TenantAdminPolicy, StageOperatorPolicy, ReadPolicy
- Program.cs — policy regisztrációk + app.MapStageEndpoints()

### Review violations javítva

| Violation | Javítás |
|---|---|
| D4 — AddStep/RemoveStep nem emített domain eventet | StageChainStepAddedEvent + StageChainStepRemovedEvent létrehozva; IDomainEventDispatcher bekötve AddStageChainStep + RemoveStageChainStep handlerbe |
| A9 — 14 handler companion test hiányzott | 64 unit test írva: SpaceOS.Kernel.Tests/StageRegistry/Handlers/ |
| I2 — JwksHealthCheck public volt | internal sealed class + AddKernelHealthChecks() extension method DependencyInjection.cs-ben |
| SEC-01 — ModuleEndpoint regex hiányzott | ^https?://(127\.0\.0\.1|localhost):(50[0-9]{2})$ enforced Register() + UpdateEndpoint()-ban |

## Tesztek

| Suite | Passing | Skipped | Total |
|---|---|---|---|
| SpaceOS.Kernel.Tests (unit) | 881 | 0 | 881 |
| SpaceOS.Kernel.IntegrationTests | 101 | 0 | 101 |
| SpaceOS.Kernel.Api.Tests | 86 | 4 | 90 |
| **ÖSSZESEN** | **1068** | **4** | **1072** |

**4 skipped:** CreateStageHandoff integration tesztek — pg_advisory_xact_lock PostgreSQL-specifikus, SQLite in-memory nem támogatja. Elfogadott pre-existing limitáció.

**Új tesztek (MSG-054):**
- 5 domain unit test fájl: StageDefinition, StageChainTemplate, StageChainStep, StageHandoff, FlowEpic
- 14 handler companion unit test fájl (64 test)
- 22 API integration test (StageEndpointTests.cs)

## Security review

| Ellenőrzés | Eredmény |
|---|---|
| Input validation (FluentValidation) | Minden commandhoz companion validator |
| SEC-01 ModuleEndpoint loopback+port constraint | Enforced domain szinten regex-szel |
| SEC-03 StageCode immutability | prevent_stage_code_change DB trigger |
| RLS FORCE | Mind a 4 stage tábla: stage_definitions, stage_chain_templates, stage_chain_steps, stage_handoffs |
| SQL injection | Csak paraméteres EF Core query |
| Authorization | Minden endpoint policy-val védett (SystemAdmin/TenantAdmin/StageOperator/Read) |
| Advisory lock | pg_advisory_xact_lock(hash_of_chain_id) — concurrent handoff versioning (DB-02) |
| Idempotency | IdempotencyKey UNIQUE index; duplicate → existing record visszaadva |
| Sensitive data | Nincs jelszó/token logban |

## Kockázatok / kérdések

**Elfogadott arch kivételek:**
1. `CreateStageHandoffCommandHandler` az Infrastructure rétegben van (nem Application) — pg_advisory_xact_lock miatt közvetlen AppDbContext hozzáférés szükséges. Deliberate exception, ADR dokumentálás ajánlott.
2. `I8 pre-existing`: `Database.MigrateAsync()` Program.cs-ben — production előtt eltávolítandó (nem ebben a taskban keletkezett).

**Nem blokkol, de jelzem:**
- Security scan (kernel-security-scanner) még nem futott MSG-054-re — következő lépésként ajánlott.
