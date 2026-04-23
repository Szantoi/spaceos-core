# SpaceOS — Modules.Joinery v2 Implementation Kickoff

> **Dátum:** 2026-04-09
> **Tervdokumentum:** `SpaceOS_Modules_Joinery_v2_Architecture_v4.md`
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ → IMPLEMENTÁCIÓ INDÍTVA
> **Effort:** ~16 fejlesztői nap · 6 track · 3 repo

---

## Claude Code Agent utasítás

> Implementáld a Modules.Joinery v2 tervdokumentum szerint a következő feladatokat:
>
> **Track A (Nap 1-3):** Migration J-0002, Domain bővítés (DoorOrderStatus FSM, CuttingListSnapshot, VO-k, events), EF Core config (ConcurrencyCheck)
>
> **Track B (Nap 4-6):** JoineryOutboxWorker (FOR UPDATE SKIP LOCKED), IOrchestratorClient (3× retry, 10s timeout), SaveCalculationResultCommand (optimistic concurrency + IsLatest logic)
>
> **Track C (Nap 7-8):** Abstractions: POST /api/templates/{name}/calculate (tenant check). Orchestrator: /internal/* prefix + X-SpaceOS-Internal guard
>
> **Track D (Nap 9-11):** QuestPDF IProductionSheetGenerator, GetProductionSheetQuery (lazy cache), SEC-05 response headers, DoorOrderRevertedEventHandler (cache + file invalidáció)
>
> **Track E (Nap 12-13):** FAF_T seed (~15 slot, ~20 connection, 7 param), FAF_Ü klón, BFAJ klón. ITemplateValidator.Validate() PASS mindháromra
>
> **Track F (Nap 14-16):** ≥40 új teszt (109 meglévő zöld marad). Unit + integration + security gates
>
> **DoD checklist:** SpaceOS_Modules_Joinery_v2_Architecture_v4.md#10
>
> **Blokkoló gate-ek:** Migration J-0002, SEC-01 (/internal/ guard), SEC-02 (SKIP LOCKED)
>
> **Minden feladat után futtasd:** `dotnet test && dotnet build`

---

## Végrehajtási sorrend

| Nap | Feladat | Track | Repo | Függőség |
|-----|---------|-------|------|----------|
| 1 | Migration J-0002 DDL + RLS + indexek | A-DB | joinery | — |
| 2 | Domain: DoorOrderStatus + VO-k + events + CuttingListSnapshot | A-Domain | joinery | — |
| 3 | EF Core: JoineryDbContext bővítés + ConcurrencyCheck config | A-Infra | joinery | Nap 1-2 |
| 4 | JoineryOutboxWorker + JoineryOutboxEntry + OutboxCleanupJob | B-Outbox | joinery | Nap 1 |
| 5 | IOrchestratorClient (HTTP + retry) + SubmitDoorOrderCommandHandler | B-Bridge | joinery | Nap 4 |
| 6 | SaveCalculationResultCommand + concurrency + IsLatest | B-Bridge | joinery | Nap 3-4 |
| 7 | Abstractions: POST /api/templates/{name}/calculate + tenant check | C-Abs | abstractions | — |
| 8 | Orchestrator: /internal/ prefix + guard + proxy routes | C-Orc | orchestrator | Nap 7 |
| 9 | IProductionSheetGenerator (QuestPDF) + PDF layout | D-PDF | joinery | Nap 3 |
| 10 | GetProductionSheetQuery + lazy cache + response headers | D-PDF | joinery | Nap 9 |
| 11 | DoorOrderRevertedEventHandler + RevertToDraft handler | D-PDF | joinery | Nap 10 |
| 12 | FAF_T seed — pattern kialakítás | E-Seed | abstractions | Nap 7 |
| 13 | FAF_Ü + BFAJ seed + ITemplateValidator tesztek | E-Seed | abstractions | Nap 12 |
| 14-16 | Tesztek: ≥40 új (unit + integration + E2E snapshot) | F-Test | joinery | Nap 1-13 |

---

## Repo-specifikus utasítások

### spaceos-modules-joinery (Track A, B, D, F)

```bash
cd /opt/spaceos/modules-joinery
git checkout develop
# Track A
dotnet ef migrations add JoineryV2_CuttingListSnapshot --project SpaceOS.Modules.Joinery.Infrastructure
# Approved package:
dotnet add SpaceOS.Modules.Joinery.Infrastructure package QuestPDF --version "2024.12.*"
```

**Új fájlok:**
- `Domain/Entities/CuttingListSnapshot.cs`
- `Domain/ValueObjects/CuttingListLine.cs`, `CncInstruction.cs`, `ProcessStep.cs`
- `Domain/Events/DoorOrderSubmitted.cs`, `DoorOrderCalculated.cs`, `DoorOrderCalculationFailed.cs`, `DoorOrderReverted.cs`
- `Infrastructure/Outbox/JoineryOutboxWorker.cs`, `JoineryOutboxEntry.cs`, `JoineryOutboxCleanupJob.cs`
- `Infrastructure/Http/OrchestratorClient.cs`
- `Infrastructure/Pdf/ProductionSheetGenerator.cs`
- `Application/Commands/SubmitDoorOrderCommand.cs` + Handler + Validator
- `Application/Commands/SaveCalculationResultCommand.cs` + Handler
- `Application/Commands/RevertDoorOrderCommand.cs` + Handler
- `Application/Queries/GetProductionSheetQuery.cs` + Handler

**Módosított fájlok:**
- `Domain/Aggregates/DoorOrder.cs` — Submit(), MarkCalculating(), MarkCalculated(), MarkCalculationFailed(), RevertToDraft(), Version property
- `Domain/Enums/DoorOrderStatus.cs` — +3 enum (Calculating, Calculated, CalculationFailed)
- `Infrastructure/Persistence/JoineryDbContext.cs` — új entity-k + ConcurrencyCheck
- `Api/Program.cs` — új endpoint-ok

### spaceos-modules-abstractions (Track C, E)

```bash
cd /opt/spaceos/modules-abstractions
git checkout develop
```

**Új fájlok:**
- `Api/Endpoints/CalculateByNameEndpoint.cs` — POST /api/templates/{name}/calculate
- `Infrastructure/Seeding/DoorstarProductTemplateSeed.cs` — FAF_T, FAF_Ü, BFAJ

### spaceos-orchestrator (Track C)

```bash
cd /opt/spaceos/orchestrator
git checkout develop
```

**Módosított fájlok:**
- `src/middleware/internalGuard.ts` — `/internal/*` prefix + `X-SpaceOS-Internal` header check
- `src/routes/index.ts` — `/internal/abstractions/calculate` + `/internal/joinery/results` proxy
- `src/routes/joinery.ts` — `/bff/joinery/orders/:id/submit`, `/sheet`, `/revert` proxy

---

## Security gates (deployment blocker)

| Gate | Finding | Ellenőrzés |
|------|---------|-----------|
| SEC-01 | Internal endpoint guard | `curl -s https://joinerytech.hu/bff/internal/abstractions/calculate` → 403 |
| SEC-02 | FOR UPDATE SKIP LOCKED | Párhuzamos OutboxWorker → nincs duplikált snapshot |
| SEC-03 | RevertToDraft cache invalidáció | Revert → PDF fájl törölve + cache rekord törölve |
| SEC-04 | Abstractions tenant check | TenantA template → TenantB calculate → 403/404 |
| SEC-05 | PDF response headers | `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: private, no-store` |
| SEC-06 | ContentHash incl. TenantId | Azonos input + más TenantId → más hash |
| RLS | Cross-tenant isolation | TenantA snapshot → TenantB query → 0 result |

---

## Kockázatok

| Kockázat | P | H | Mitigáció |
|----------|---|---|-----------|
| QuestPDF Community licenc ($1M limit) | Low | Med | Professional $699/yr budget |
| FAF_T seed hibás offsetek | Med | High | Doorstar manuális validáció: 3 teszt rendelés |
| OutboxWorker deadlock sok item-nél | Low | Med | SKIP LOCKED + max 10 batch |
| Abstractions API nem elérhető | Low | High | Phase A+B DONE, csak 1 új endpoint |

---

## Sorrend a Keycloak KC01-03-hoz képest

```
Most:    Joinery v2 (16 nap) ─────────────────────────────→ Doorstar demo
Utána:   KC01 (4 nap) → KC02 (2 nap) → KC03 (3 nap) ───→ Doorstar prod auth
```

A két track független repo-kon fut — ha kapacitás van, KC01 párhuzamosítható Track C-vel.
