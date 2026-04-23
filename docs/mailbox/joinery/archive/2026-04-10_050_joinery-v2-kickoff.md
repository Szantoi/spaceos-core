---
id: MSG-JOINERY-050
from: root
to: joinery
type: task
priority: high
status: READ
created: 2026-04-10
ref: docs/archive/SpaceOS_Joinery_v2_Claude_Code_Package.md
---

# MSG-JOINERY-050: Modules.Joinery v2 — Kickoff

## Feladat

Implementáld a Modules.Joinery v2-t a Claude Code Package szerint:
`docs/archive/SpaceOS_Joinery_v2_Claude_Code_Package.md`

## Végrehajtási sorrend (Track A → F)

| Track | Nap | Feladat | Repo |
|---|---|---|---|
| A-DB | 1 | Migration J-0002 DDL + RLS + indexek | joinery |
| A-Domain | 2 | DoorOrderStatus FSM + VO-k + events + CuttingListSnapshot | joinery |
| A-Infra | 3 | EF Core: JoineryDbContext + ConcurrencyCheck | joinery |
| B-Outbox | 4 | JoineryOutboxWorker + OutboxCleanupJob | joinery |
| B-Bridge | 5 | IOrchestratorClient (HTTP + 3× retry) + SubmitDoorOrderCommandHandler | joinery |
| B-Bridge | 6 | SaveCalculationResultCommand + optimistic concurrency + IsLatest | joinery |
| C-Abs | 7 | POST /api/templates/{name}/calculate + tenant check | abstractions |
| C-Orc | 8 | /internal/ prefix + X-SpaceOS-Internal guard + proxy routes | orchestrator |
| D-PDF | 9 | IProductionSheetGenerator (QuestPDF) + PDF layout | joinery |
| D-PDF | 10 | GetProductionSheetQuery + lazy cache + SEC-05 headers | joinery |
| D-PDF | 11 | DoorOrderRevertedEventHandler + RevertToDraft handler | joinery |
| E-Seed | 12 | FAF_T seed (~15 slot, ~20 connection, 7 param) | abstractions |
| E-Seed | 13 | FAF_Ü + BFAJ klón + ITemplateValidator PASS mindháromra | abstractions |
| F-Tests | 14-16 | ≥40 új teszt (meglévő 109 zöld marad) | joinery |

## Blokkoló gate-ek (ezek nélkül nem folytatható)

1. **Migration J-0002** — minden track alapja
2. **SEC-01** — `/internal/` guard (C-Orc track előtt)
3. **SEC-02** — SKIP LOCKED (B-Outbox trackben)

## Baseline tesztek (ezek nem törhetnek el)

| Repo | Zöld tesztek |
|---|---|
| joinery | 109 |
| abstractions | 61 |
| orchestrator | 163 |
| kernel | 933 |

## Minden track után kötelező

```bash
dotnet test && dotnet build
```

## DoD

- [ ] Migration J-0002 fut, rollback is működik
- [ ] DoorOrderStatus FSM tesztelve (összes átmenet)
- [ ] JoineryOutboxWorker SKIP LOCKED bizonyított
- [ ] PDF generálás működik (QuestPDF)
- [ ] FAF_T + FAF_Ü + BFAJ seed PASS ITemplateValidator-on
- [ ] ≥40 új teszt, baseline zöld
- [ ] `dotnet build` 0 error, 0 warning

## Válasz

Outbox üzenet: `docs/mailbox/joinery/outbox/2026-04-10_050_joinery-v2-kickoff-done.md`
