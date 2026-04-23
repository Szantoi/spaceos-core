---
id: MSG-ORCH-079
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-INFRA-141
created: 2026-04-17
---

# ORCH-079 — `doorstar-cutting-ready-v1` seed fix: DoorOrder item hozzáadása submit előtt

## Kontextus

Az INFRA-141 seed verifikáció BLOCKED státusszal érkezett. A `doorstar-cutting-ready-v1` profil
④ lépése (`POST /api/orders/{id}/submit`) 400-at ad vissza, mert a DoorOrder-nek nulla item-je van.

A Joinery domain szabály:
```csharp
if (_items.Count == 0)
    return Result.Invalid(new ValidationError("Items", "Cannot submit an order with no items."));
```

## Feladat

A `src/routes/test.route.ts` `doorstar-cutting-ready-v1` profiljában szúrj be egy **③b** lépést
a meglévő ④ (submit) elé:

```typescript
// ③b Add item to DoorOrder (submit requires ≥1 item)
await axios.post(
  `${env.JOINERY_BASE_URL}/api/orders/${doorOrderId}/items`,
  {
    sorszam:             '001',
    name:                'Seed ajtó',
    quantity:            1,
    doorType:            'Sikban',
    openingDirection:    'Left',
    wallOpeningWidth:    920,
    doorWidth:           900,
    wallOpeningHeight:   2120,
    doorHeight:          2100,
    wallOpeningThickness: 150,
    doorThickness:       45,
  },
  { headers: { Authorization: auth }, timeout: 10_000 },
);
```

### Meglévő sorrend (javítandó)

```typescript
// ③ Create DoorOrder in Joinery (Draft state)
const orderRes = await axios.post(...)
const doorOrderId = orderRes.data.id ?? orderRes.data;

// ④ Submit DoorOrder (Draft → Submitted)   ← HIBA: nincs item
await axios.post(`.../${doorOrderId}/submit`, ...)
```

### Helyes sorrend (javítás után)

```typescript
// ③ Create DoorOrder in Joinery (Draft state)
const orderRes = await axios.post(...)
const doorOrderId = orderRes.data.id ?? orderRes.data;

// ③b Add item to DoorOrder (submit requires ≥1 item)
await axios.post(`.../${doorOrderId}/items`, { sorszam: '001', ... })

// ④ Submit DoorOrder (Draft → Submitted)
await axios.post(`.../${doorOrderId}/submit`, ...)
```

## Pipeline

```
INBOX READ → CODE (test.route.ts) → BUILD (npm run build, 0 TS error) → TEST (npm test, 218+ zöld) → REVIEW → SECURITY → OUTBOX
```

## DoD

- [ ] `npm run build` → 0 TypeScript error
- [ ] `npm test` → minden teszt zöld (legalább 218)
- [ ] Ha az új lépéshez új teszt szükséges, írj egyet (`test.route.test.ts` vagy meglévő seed tesztfájl)
- [ ] OUTBOX DONE üzenet (`MSG-ORCH-079-DONE`)

## Megjegyzés

A `JOINERY_BASE_URL` és a `Authorization: auth` változók már definiáltak a profil scope-jában —
pontosan úgy használd, ahogy a többi `axios.post` hívás.

Eszközök: `/spaceos-terminal` skill
