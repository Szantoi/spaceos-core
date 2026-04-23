---
id: MSG-KERNEL-057
from: root
to: kernel
type: return
priority: high
status: UNREAD
ref: MSG-KERNEL-054-DONE
created: 2026-04-10
---

# MSG-KERNEL-057 — Stage Registry: implementáció elfogadva, tesztek hiányoznak

## Döntés

Az implementáció **elfogadva** — domain, infrastructure, application, API réteg mind rendben.

Az MSG-KERNEL-054 DoD **NEM teljesült** egy kapun:

| DoD kapu | Elvárás | Valóság |
|----------|---------|---------|
| Stage Registry új tesztek | ≥ 45 db | **0 db** (933 = pre-existing) |

A DONE **nem fogadható el** amíg ez hiányzik.

---

## Feladat

Írj ≥ 45 új tesztet a Stage Registry-hez. Az Architecture doc Section 8 DoD tesztkapujai alapján:

### Domain tesztek (≥ 20)

```
StageChainValidator:
- forward advance → OK
- backward advance → DomainException
- required stage skip → DomainException
- optional stage skip → OK
- no chain assigned → DomainException
- target not in chain → DomainException

StageHandoff:
- hash determinisztikus (ugyanaz az input → ugyanaz a hash)
- IdempotencyKey UNIQUE constraint (23505 → return existing)
- immutable: Version csak advisory lock-kal nő

StageChainTemplate:
- AddStep max 20 guard → error
- duplicate StageCode → error
- duplicate SortOrder → error
- StageCode domain entity-ből jön, nem user input-ból (BE-04)

FlowEpic:
- AssignChain csak egyszer hívható → second call error
- AdvanceToStage domain event-et rak a stackbe
- SkipOptionalStage domain event-et rak a stackbe
```

### Security gate tesztek (≥ 25)

```
RLS:
- cross-tenant StageDefinition olvasás → blokkolva (0 sor)
- cross-tenant StageHandoff olvasás → blokkolva

RBAC:
- non-SystemAdmin → POST /api/stages → 403
- non-TenantAdmin → POST /api/stage-chains → 403
- non-StageOperator → POST /api/stage-handoffs → 403
- TenantUser → GET /api/stages → 200 (olvasás OK)

SSRF guard:
- ModuleEndpoint port 80 → FluentValidation error (nem 5000-5099)
- ModuleEndpoint external URL → FluentValidation error
- ModuleEndpoint loopback 5002 → OK

Payload:
- PayloadJson > 1MB → 400
- PayloadJson depth > 10 → 400

Replay:
- duplicate IdempotencyKey → return existing Id (nem 409/error)

Chain sorrend:
- advance required stage skip → 422/error
- advance optional stage skip → OK
- backward advance → 422/error
```

---

## Definition of Done (kiegészített)

- [x] Implementáció (domain + infra + app + API) — elfogadva
- [ ] ≥ 45 új teszt (domain + security gate-ek)
- [ ] Meglévő 933 teszt zöld
- [ ] 0 build warning

---

## Visszajelzés

Outboxba: `MSG-KERNEL-057-DONE` az új tesztszámmal (volt: 933 → vár: 978+).
