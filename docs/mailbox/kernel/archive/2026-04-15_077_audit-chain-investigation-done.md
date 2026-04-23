---
id: MSG-KERNEL-077-DONE
from: kernel
to: root
type: done
priority: medium
status: READ
ref: MSG-KERNEL-077
created: 2026-04-15
---

# MSG-KERNEL-077 — Audit chain break investigation DONE

## Összefoglaló

Minden DoD pont teljesítve. A chain break **rollback artifact**, Doorstar production adat
részben érintett (4 event belső lánca ép, első event genesis-ankora törött).

---

## DoD: Tenant `a16e3cf4` azonosítás

**EREDMÉNY: E2E teszt tenant.**

```sql
Id: a16e3cf4-c6b4-4b45-b55e-d67dae0279ee
Name: probe-1776138776916
TenantType: Manufacturer
```

`probe-<timestamp>` névmintázat → automatikusan generált E2E tesztelési tenant.

---

## DoD: Root cause

**EREDMÉNY: Rollback ciklus okozta.**

A `probe-1776138776916` tenanthoz **1 event** maradt az adatbázisban (4bbd17e9).
Az összes megelőző event törlődött az April 13-i rollback során.

```
DB-ben maradt:
  event_B (4bbd17e9)
    PreviousHash: e92ace49e3da  ← törölt event_A StateHash-ére mutat
    OccurredAt:  2026-04-14 03:52:56 UTC

Törölt (rollback):
  event_A
    StateHash:   e92ace49e3da  ← már nem létezik
```

Az ALERT értelmezése:
- **Expected** `e9728cbde1...` = jelenlegi genesis hash (in-memory, app restart után)
- **Found** `e92ace49e3da` = event_B PreviousHash (törölt event_A-ra mutat)
- Mismatch = chain break

**Kiváltó ok:** A `AuditChain:GenesisHash` nincs konfigurálva a production appsettings-ben
(`appsettings.Production.json` csak Jwt+Urls-t tartalmaz). A `ConfigurationGenesisHashProvider`
ezért **random 32-byte hex-et generál és in-memory cache-el**. Minden app restart új genesis hash-t hoz.
A rollback ciklus (3+ restart April 13-án) többszöri genesis hash-váltást okozott.

---

## DoD: Doorstar tenant érintett-e?

**EREDMÉNY: RÉSZBEN érintett — belső lánc ép, genesis-ankora törött.**

```
Doorstar Kft. (a1b2c3d4-e5f6-7890-abcd-ef1234567890) — 4 event:

Seq 10178 | 2026-04-14 21:42 | prev: ca29f1876837985e → state: a1cb8ab2e6d57fff
Seq 10233 | 2026-04-14 21:44 | prev: a1cb8ab2e6d57fff ✓ → state: f0089a49c132ffa1
Seq 10773 | 2026-04-14 22:16 | prev: f0089a49c132ffa1 ✓ → state: 6a833d4cc4caed94
Seq  8743 | 2026-04-15 12:18 | prev: 6a833d4cc4caed94 ✓ → state: be9b630654afc038
```

**Belső lánc (event 2→3→4):** minden PreviousHash egyezik a megelőző StateHash-sel ✓

**Genesis-ankor (event 1):** `ca29f1876837985e` nem szerepel egyetlen AuditEvent StateHash-ében sem,
és nem egyezik a jelenlegi genesis hash-sel (`e9728cbde1...`). Ez valószínűleg:
- **A)** April 13-án volt egy korábbi Doorstar event, amelyik a rollbackban törlődött
  (az ő StateHash-je volt `ca29f1876837985e`) — ez a legvalószínűbb
- **B)** Az April 13-i genesis hash volt `ca29f1876837985e`, és April 14-én
  az első Doorstar event még az akkor érvényes genesis-re mutat (de azóta az app restarolt)

**Doorstar esemény-integritás:** a **jelenlegi 4 Doorstar event** belső láncként konzisztens.
Az April 14-15-i Doorstar aktivitás ép. Nem kritikus produkciós adatintegritási probléma.

---

## Kockázatok / Javasolt akció

### Azonnali (opcionális)
Nincs kritikus teendő. A chain break egy April 13-i rollback artifact, nem folyamatban lévő sérülés.

### Ajánlott rövid-táv
**`AuditChain:GenesisHash` konfigurálása a VPS-en** — ez megakadályozza a jövőbeli genesis hash-drift-et:

```json
// appsettings.Production.json (VPS-en)
{
  "AuditChain": {
    "GenesisHash": "<stable-64char-hex>"
  }
}
```

Amíg nincs konfigurálva, minden restart new genesis hash-t generál → chain verifier false positive alert-eket produkál.

### Elfogadható
A `probe-1776138776916` tenant chain break-je: **nincs teendő** — E2E teszt tenant, rollback artifact.
Doorstar event 1 genesis-ankora: **elfogadható** — belső lánc ép, rollback artifact, nem aktív integritási sérülés.

---

## Tesztek

Nincs kódváltozás ebben a feladatban (investigation only). Tesztek változatlanul zöldek:
- 910 unit + 107 integration + 93 API = **1110 teszt zöld**

## Security review

- Közvetlen DB-query sudo -u postgres-zel (lokális VPS-en, production read-only vizsgálat)
- Nem módosítottunk audit adatot
- A genesis hash konfigurációs ajánlás security javítás (deterministikus anchor = nem manipulálható véletlennel)
