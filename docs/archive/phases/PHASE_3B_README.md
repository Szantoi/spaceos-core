# SpaceOS — Phase 3B: Escrow GA Foundation

> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ  
> **Tervdokumentum:** `SpaceOS_Phase3B_Architecture_v4.md`  
> **Blokkoló feltétel:** Sprint D Phase 2 DoD teljes  
> **Becsült effort:** 14 fejlesztői nap  
> **Migration sorszám:** 0020–0023  
> **Test baseline:** 1210 pass / 0 fail (2026-04-07)

---

## Mi a cél?

Phase 3B egy kérdésre válaszol:

> *„Ha egy Doorstar megrendelés lezárul és az Escrow kifizeti a gyártót — be tudjuk-e bizonyítani egy esetleges vitában, hogy pontosan mi volt az állapot a kifizetés pillanatában?"*

**Jelenleg: nem.** Nincs snapshot store, a feltöltött bizonyíték (ProofUrl) egy sima HTTP link amit bárki törölhet, és a hash chain genesis értéke hardkódolt konstans.

Phase 3B ezt zárja le. Utána az Escrow feature flag production-ban bekapcsolható.

---

## Mit csinálunk konkrétan?

Négy egymásra épülő réteget adunk a Kernel-hez:

### 1. AggregateSnapshot store

Minden `FlowEpic → CLOSED_DONE` és `FlowMilestone` lezáráskor a rendszer elmenti az aggregate teljes állapotát egy dedikált táblába — hash-sel együtt. Ez teszi lehetővé a temporal query-t: *„mi volt az állapot 2026-03-15 14:32-kor?"*

```
FlowEpic CLOSED_DONE
    → AggregateSnapshot (StateJson + SnapshotHash)
    → SnapshotHash bekerül az AuditEvent chain-be (cross-reference)
```

**Fontos döntés:** az aggregate-et nem közvetlenül szerializáljuk (DDD `private set` → üres JSON lenne), hanem explicit snapshot DTO-t (`FlowEpicSnapshotDto`) — ezt az aggregate állítja elő a `ToSnapshotJson()` metóduson át.

### 2. Outbox Pattern

A snapshot mentés és az Escrow trigger **nem lehet két különálló DB hívás** — ha a folyamat kettő között crashel, inkonzisztens állapotba kerülünk. Az Outbox Pattern megoldja: mindkét művelet ugyanabba a tranzakcióba kerül, egy background worker dolgozza fel.

```
CLOSED_DONE event
    → OutboxEntry INSERT (ugyanaz a tranzakció mint a FSM state change)
    → OutboxWorker (PeriodicTimer, 5s) dolgozza fel
    → SnapshotService.TakeSnapshotAsync()
    → [jövő] Escrow trigger
```

### 3. ProofHash + WORM storage

A `ImplementationSummary.ProofUrl` (egy sima HTTP link) helyett a rendszer a feltöltött fájlt egy **write-once storage-ba** (S3 Object Lock vagy Azure Immutable Blob) menti, és a fájl **SHA-256 hash-ét** tárolja az adatbázisban. A hash az igazság — az URL csak másodlagos.

Expand-contract migrációval csináljuk: Phase 3B-ben a ProofHash nullable jön be, a régi ProofUrl nullable lesz. A `DROP COLUMN "ProofUrl"` egy külön Phase 3B+ migration (0025) lesz, miután minden sor rendelkezik hash-sel.

### 4. Audit minőség (Chain Integrity + Genesis hash)

- `GET /api/audit-events/verify-chain` endpoint — SOC 2 CC7.2 önellenőrzés
- `GET /api/snapshots/{id}?at=` + `/versions` — temporal query API
- Genesis hash konstans eltávolítva → Key Vault (dev: env var)
- `AuditEvents.HashAlgorithm` mező — crypto-agility: SHA-256 → SHA3-256 upgrade path elkészítve

---

## Amit NEM csinálunk Phase 3B-ben

Expliciteten ki van ejtve a scopeból:

| Tétel | Miért nem most | Hova kerül |
|-------|----------------|-----------|
| GDPR pseudonymizáció (P2-3) | Külön skillset, Privacy Notice módosítást igényel | Phase 3D |
| Audit alerting (P2-4) | DevOps/monitoring scope, nem backend | Phase 3D |
| ProofUrl DROP | Expand-contract Phase 2 — csak ha minden sor hash-sel rendelkezik | Phase 3B+ (migration 0025) |
| SHA3-256 migration utility | Séma kész (HashAlgorithm mező), az adatok áthashelése külön sprint | Phase 3B+ |
| RFC 3161 TSA timestamping | Horizon 2 — Escrow GA után | P3-1 |

---

## Architektúra döntések (nem kérdőjelezhetők meg)

```
Kernel FROZEN — nincs új domain concept közvetlenül a Kernelbe
Layer dependency: Domain ← Application ← Infrastructure ← Api
ISnapshotService: internal — nem publikus DI, Api réteg nem látja
OutboxWorker: IServiceScopeFactory (nem direkt IOutboxRepository inject — Captive Dependency)
SnapshotService: ISnapshotable interface + ToSnapshotJson() — nem JsonSerializer.Serialize(aggregate)
ProofStorageKey formátum: {tenantId}/{yyyy/MM/dd}/{guid}_{sanitizedFileName}
RLS worker bypass: UUID '00000000-0000-0000-0000-000000000001' — nem string konstans
```

---

## Új fájlok / változások áttekintése

### Domain (`SpaceOS.Kernel.Domain`)

```
Domain/
├── Entities/
│   ├── AggregateSnapshot.cs          ← új aggregate snapshot entity
│   └── OutboxEntry.cs                ← új outbox entity
├── Enums/
│   ├── AggregateType.cs              ← FlowEpic|FlowMilestone|B2BHandshake|SpaceLayer
│   └── OutboxStatus.cs               ← Pending|Processing|Processed|Dead
├── Events/
│   ├── AggregateSnapshotCreatedEvent.cs
│   ├── ProofAttachedEvent.cs
│   └── OutboxEntryDeadEvent.cs
├── Common/
│   └── ISnapshotable.cs              ← interface: ToSnapshotJson()
└── Interfaces/
    ├── IAggregateSnapshotRepository.cs
    ├── IOutboxRepository.cs
    └── IProofStorageService.cs       ← Upload/Verify/IsAvailable

Módosított:
└── Entities/ImplementationSummary.cs ← ProofHash + ProofStorageKey + ProofStorageProvider
```

### Application (`SpaceOS.Kernel.Application`)

```
Application/
├── Snapshots/
│   ├── ISnapshotService.cs           ← internal interface
│   ├── SnapshotService.cs            ← internal implementation
│   ├── Dtos/
│   │   ├── FlowEpicSnapshotDto.cs
│   │   └── FlowTaskSnapshotDto.cs
│   ├── Handlers/
│   │   └── FlowEpicClosedDoneOutboxHandler.cs  ← FSM event → OutboxEntry
│   └── Specs/
│       ├── SnapshotAtSpecification.cs
│       └── SnapshotVersionsSpecification.cs
├── Outbox/
│   └── IOutboxEventHandler.cs        ← handler registry interfész
├── Queries/
│   ├── GetSnapshotAtQuery.cs + Handler
│   ├── GetSnapshotVersionsQuery.cs + Handler
│   └── VerifyChainQuery.cs + Handler
└── Commands/
    └── AttachProofCommand.cs + Handler
```

### Infrastructure (`SpaceOS.Infrastructure`)

```
Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── AggregateSnapshotConfiguration.cs
│   │   └── OutboxEntryConfiguration.cs
│   └── Migrations/
│       ├── 0020_AggregateSnapshots.cs
│       ├── 0021_OutboxEntries.cs
│       ├── 0022_ImplSummaryProofHash.cs
│       └── 0023_AuditEventsHashAlgorithm.cs
├── Outbox/
│   ├── OutboxRepository.cs           ← FOR UPDATE SKIP LOCKED, system context
│   ├── OutboxWorker.cs               ← BackgroundService, PeriodicTimer, scope factory
│   └── Handlers/
│       └── FlowEpicClosedDoneHandler.cs  ← IOutboxEventHandler impl
├── Storage/
│   ├── LocalProofStorageService.cs   ← dev: filesystem
│   └── S3WormProofStorageService.cs  ← prod: S3 Object Lock
└── Security/
    └── KeyVaultGenesisHashProvider.cs ← prod (dev: ConstantGenesisHashProvider)

Módosított:
└── Repositories/AggregateSnapshotRepository.cs  ← új
```

### Api (`SpaceOS.Kernel.Api`)

```
Api/
└── Endpoints/
    ├── SnapshotEndpoints.cs    ← GET /api/snapshots/{id}?at= + /versions
    ├── VerifyChainEndpoint.cs  ← GET /api/audit-events/verify-chain (AdminOnly)
    └── ProofUploadEndpoint.cs  ← POST /api/tasks/{id}/proof (streaming, MIME whitelist)

Módosított:
└── Program.cs  ← IProofStorageService DI + OutboxWorker + IOutboxEventHandler registry
```

---

## Implementációs sorrend

```
Nap  1:   T-01  AggregateSnapshot entity + ISnapshotable + SnapshotDTOs (FlowEpic, Milestone)
Nap  2:   T-01  Migration 0020 + EF config + IAggregateSnapshotRepository + unit tesztek
Nap  3:   T-02  OutboxEntry + IOutboxEventHandler registry + OutboxRepository
Nap  4:   T-02  Migration 0021 + OutboxWorker (PeriodicTimer + IServiceScopeFactory)
Nap  5:   T-03  SnapshotService + FlowEpicClosedDoneOutboxHandler + integration teszt
Nap  6:   T-04  GetSnapshotAtQuery + GetSnapshotVersionsQuery (Ardalis.Spec)
Nap  7:   T-05  Migration 0022 + IProofStorageService + LocalProofStorageService
Nap  8:   T-05  Proof upload streaming endpoint + AttachProofCommand + S3 provider stub
Nap  9:   T-06  VerifyChain endpoint + ChainVerificationDto (WormStorageAvailable flag)
Nap 10:   T-07  Genesis hash KV (SEC-P3B-06) + Migration 0023 HashAlgorithm
Nap 11–12: Tesztek (unit + integration + E2E, ≥ 45 új teszt)
Nap 13:   EXPLAIN ANALYZE minden endpointon + security gate-ek
Nap 14:   DoD checklist final · buffer / hotfix
```

---

## Golden Rules emlékeztető

Minden handler-re kötelező, Phase 3B-ben sem kivétel:

1. Nincs public setter aggregátumokon
2. Business logic csak Domainben
3. Minden mutáció domain eventet emel
4. `PopDomainEvents()` + `DispatchAsync()` minden mutáló handler végén
5. Lista query csak `Ardalis.Specification`-on át — nyers repo hívás tilos
6. `Result<T>` minden handleren
7. `ConfigureAwait(false)` minden production async híváson
8. `AsNoTracking()` minden read-only repository metóduson

---

## Kritikus hibák amiket NE csinálj

Ezeket a review már megtalálta — ne kövesd el újra:

| Anti-pattern | Helyes megoldás |
|---|---|
| `JsonSerializer.Serialize(aggregate)` — DDD private setter → `{}` | `aggregate.ToSnapshotJson()` — explicit DTO |
| `BackgroundService` + direkt `IRepository` inject | `IServiceScopeFactory` → `CreateAsyncScope()` per batch |
| `_ = task` fire-and-forget | `.FireAndForget(_logger, context)` helper |
| `BuildServiceProvider()` DI-ban | `IConfigureNamedOptions` pattern |
| `Task.Delay` loop a worker-ben | `PeriodicTimer` |
| `FOR UPDATE SKIP LOCKED` RLS tenant context nélkül | System UUID `'00000000-0000-0000-0000-000000000001'` set_config |
| `GenesisHash` C# konstans | `IGenesisHashProvider` — csak `IsDevelopment()` esetén konstans |
| `ProofStorageKey` TenantId prefix nélkül | `{tenantId}/{yyyy/MM/dd}/{guid}_{file}` |

---

## Definition of Done — rövid checklist

Implementáció akkor kész, ha:

- [ ] Migration 0020–0023 fut, minden tábla `spaceos_schema_owner`-é
- [ ] RLS + FORCE RLS minden új táblán
- [ ] `EXPLAIN ANALYZE` minden endpointon — Seq Scan nincs
- [ ] `AggregateSnapshot.Create()` StateJson > 512KB → `DomainException`
- [ ] `OutboxWorker` graceful shutdown `OperationCanceledException`-re
- [ ] `ISnapshotService` nem látható az Api rétegből
- [ ] Proof upload: MIME whitelist érvényesítve; hash szerveren számított
- [ ] `ConstantGenesisHashProvider` csak `IsDevelopment()` esetén regisztrálható
- [ ] `grep -rn "00000...000" --include="*.cs"` → 0 (konstans törölve)
- [ ] 1210 meglévő teszt zöld + ≥ 45 új teszt
- [ ] 0 build warning · 0 vulnerable package

A teljes DoD: `SpaceOS_Phase3B_Architecture_v4.md` Section 10.

---

## Kapcsolódó dokumentumok

| Dokumentum | Tartalom |
|---|---|
| `SpaceOS_Phase3B_Architecture_v4.md` | Teljes tervdokumentum — DDL, domain kód, finding táblák, ERD |
| `SpaceOS_Security_Task_Register.md` | P1-3, P1-4, P1-8, P2-1..P2-6 eredeti specifikáció |
| `SpaceOS_Audit_Research.docx` | Hash chain algoritmus, snapshot trigger logika, C# referencia minták |
| `Codebase_Status_20260407.md` | Aktuális test baseline, deployed stack, nyitott limitációk |
| `SpaceOS_Sprint_D_Phase2_v4.md` | Phase 2 (blokkoló feltétel) tervdokumentuma |
