# Memory Index

This directory contains persistent memory files for the SpaceOS.Kernel project (SpaceOS Test Writer agent).

## Files

| File | Type | Description |
|------|------|-------------|
| `project_test_runner.md` | project | Use `dotnet exec <dll>` not `dotnet test` — VSTest fails due to missing assembly in this environment. |
| `project_test_infrastructure.md` | project | ApiFactory, DatabaseSeedHelper, assertion style (xUnit Assert.*), PagedList shape, TradeType enum values. |
| `project_query_validator_bug.md` | project | All 5 list query validators are `internal sealed` — never registered by AddValidatorsFromAssembly — 422 pagination validation silently skipped end-to-end. |
| `project_intent_schema_validator.md` | project | IntentDataSchemaValidator is internal static — tested directly; per-TradeType required properties and error message patterns. |
| `project_msg_k016_patterns.md` | project | MSG-K016: CloseFlowEpicCommandHandler takes 5 deps (repo, snapshotRepo, outboxRepo, uow, dispatcher). UploadProof handler takes only IImmutableStorage. ftp:// is a valid absolute URI — do not use as negative InlineData for ProofUrl validator. |
| `project_msg_k017_patterns.md` | project | MSG-K017: OccurredAt reflection trick for AuditEvent, CountAsync call-index mock pattern for AnomalyDetector, VerifyChainQueryValidator DateRange error name. 462 total tests after. |
| `project_msg_k018_patterns.md` | project | MSG-K018: UserProfile.Erase() idempotency, Pseudonymizer null guard, ReHashChain empty chain, AuditEventDispatcher 8-param ctor, HashAlgorithm property. 495 total tests after. |
| `project_msg_k021_patterns.md` | project | MSG-K021: NodeManifest/SyncSignal throw ArgumentException (not DomainException), B2BHandshake 4 nullable Sprint C init-props. 401 unit tests after. |
| `project_msg_k022_patterns.md` | project | MSG-K022: FlowTask/Milestone/Project/Program/OfflineSyncQueueItem in SpaceOS.Modules.FlowManagement. Tests in Kernel.Tests/Entities/Modules/. 452 unit tests after. |
| `project_msg_k024_k025_patterns.md` | project | MSG-K024/K025: RegisterNode/Heartbeat/GetManifest/ReceiveSyncSignal handler deps, IAsyncDisposable mock pattern, WorkflowPhase import, GetBackoffDelay cap. 491 unit tests after. |
| `project_msg_kc01_patterns.md` | project | MSG-KC01: TenantSessionInterceptor Keycloak claim parsing, JwksHealthCheck unit test pattern, OnChallenge content-type quirk, RealmRoles mapping helper. 933 total tests after. |
| `project_msg_k054_patterns.md` | project | MSG-K054: Stage event field names, ToApiResult→200 (not 201), DomainException→400 (not 422), pg_advisory_lock SQLite incompatibility, RBAC roles. 1004 total passing after. |
| `project_msg_k054_a9fix_patterns.md` | project | MSG-K054 A9 fix: 14 StageRegistry handler companion unit tests, constructor patterns, IStageChainValidator throws (not Result.Error), 1068 total after. |
