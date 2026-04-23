---
id: BATCH-0-CLEANUP
title: Batch 0 legacy fail-ek javítása (4 teszt)
status: new
priority: low
assignee: ~
epic: batch0-cleanup
blocked_by: ~ (Batch 3 nem blokkolja)
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/e2e/outbox/2026-04-13_013_rerun-b270ccf-done.md
---

## Kontextus

Az E2E suite-ban 4 teszt folyamatosan fail — ezek a `c62f1d7` commit ELŐTTI állapotban
is fennálltak. **Nem regresszió**, hanem örökség a korai implementációból (Batch 0).

A Batch 2 baseline: **147 pass / 4 fail / 0 skipped** (151 összesen).
Ezeket a fail-eket külön sprintbe érdemes zárni — Batch 3-at nem blokkolják.

## A 4 fail részletezése

| Tesztfájl | Teszt neve | Várt kód | Szimptóma | Valószínű ok |
|---|---|---|---|---|
| `05-flowepic-lifecycle` | PUT /close — transition Delivery → ClosedDone | 200 | FSM edge case | FlowEpic close validation (ProofUrl/Hash kötelező) |
| `15-nodes-sync` | POST /bff/nodes/register — register node | 201 | 500 | NodeEndpoints belső hiba, ismert Batch 0 bug |
| `24-tenant-summary` | FlowEpicCount increments after create | — | count nem nő | Summary aggregáció timing vagy tenant-scope hiba |
| `24-tenant-summary` | ActiveWorkstationCount increments after WS activated | — | count nem nő | WorkStation aktiválás → summary frissítés késés |

## Ajánlott bontás

### CLEANUP-01: FlowEpic /close fix (05-flowepic-lifecycle)

**Érintett:** Kernel terminál
- FlowEpicEndpoints: `PUT /{id}/close` FSM guard
- CloseFlowEpicCommandValidator: ProofUrl + ProofHash kötelező-e Delivery nélkül?
- E2E teszt adja-e a `proofUrl`/`proofHash` mezőket?

### CLEANUP-02: Node register 500 fix (15-nodes-sync)

**Érintett:** Kernel terminál
- NodeEndpoints: `POST /api/nodes/register`
- 500 = nem ValidationProblem → belső kivétel
- Logból azonosítható

### CLEANUP-03: TenantSummary count fix (24-tenant-summary ×2)

**Érintett:** Kernel terminál
- Dashboard vagy TenantSummary aggregate lekérdezés
- FlowEpicCount + ActiveWorkstationCount nem frissül időben
- Lehet eventual consistency (event handler async) vagy RLS szűrési hiba

## Prioritás és ütemezés

- **Batch 3 nem blokkolja** — a 36/37/34 chain tesztek zöld maradnak e nélkül
- **Ajánlott ütemezés:** Batch 3 lezárása után, külön sprintben (CLEANUP-01..03)
- **Doorstar Q2-t nem blokkolja** — ha a happy path (proof chain) zöld

## Kiadási feltétel

Root aktiválja és terminálhoz rendeli, miután Batch 3 összes tesztje (E2E-014, 015, 016)
elfogadásra kerül. Addig `new/` státuszban marad.
