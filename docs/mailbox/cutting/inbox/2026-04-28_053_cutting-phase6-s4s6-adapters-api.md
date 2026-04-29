---
id: MSG-CUTTING-053
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-052-DONE
created: 2026-04-28
---

# CUTTING-053 — Phase 6 S4-S6: Adapters + API + Hardening (Day 7–13)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CUTTING-052 ✅ (861 teszt, S1-S3 Foundation + Framework + Resolver)
> **Ez az UTOLSÓ Phase 6 task!**
> **Használhatsz sub-agent-eket** ha szükséges

---

## S4 — 3 Adapter NuGet + BuiltinCuttingProvider (~2 nap)

### BuiltinCuttingProvider (BE-02)

A meglévő `CuttingProviderService`-t NEM törölni — delegate a `BuiltinCuttingProvider`-re:

```csharp
public sealed class BuiltinCuttingProvider : IExternalCuttingAdapter
{
    // Wrap-olja a meglévő Phase 1-5 nesting + execution logikát
    // ICuttingProvider compatibility adapter
}
```

### OptiCutAdapter + OptiCutFormatConverter

- `IAdapterFormatConverter<NestingInput, OptiCutXml>` — pure, no I/O
- XXE-hardened XML parsing (SEC-02: `DtdProcessing.Prohibit`)
- `FileExchangeTransport` használata

### CutRiteAdapter + CliWrapperTransport

- Wine subprocess (BoundedSubprocessRunner)
- CutRite CLI format converter

### ManualAdapter

- Submit-only, delegates BuiltinCuttingProvider

---

## S5 — API + Integration tesztek (~2 nap)

### 4 Admin endpoint

```
POST /api/cutting/adapters/config             — CreateOrUpdateConfig
GET  /api/cutting/adapters/config             — GetConfig (per-tenant)
POST /api/cutting/adapters/config/test        — TestAdapter (dry-run)
GET  /api/cutting/adapters/health             — GetAdapterHealth
```

### MediatR pipeline (BE-08)

- ValidationBehavior → command handlers
- Idempotency middleware (BE-13)

### OpenAPI snapshot (BE-10)

### Tesztek (70+)

- Adapter implementations: Builtin delegate, OptiCut XML, CutRite CLI, Manual
- API endpoints: auth, validation, config CRUD, health
- Integration: full flow config → resolve → adapt → submit

---

## S6 — Hardening + Ops (~2.5 nap)

- pg_cron retention (adapter_call_audit partition cleanup, 90 nap)
- `spaceos_cutting_retention` role
- SBOM generation
- EXPLAIN ANALYZE az adapter_call_audit partitioned query-kre
- Staging smoke test

---

## Definition of Done

- [ ] BuiltinCuttingProvider (BE-02 delegation)
- [ ] OptiCutAdapter + XXE-hardened converter (SEC-02)
- [ ] CutRiteAdapter + Wine CLI
- [ ] ManualAdapter
- [ ] 4 admin endpoint
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 931 pass (861 + 70 új)
- [ ] Outbox DONE
