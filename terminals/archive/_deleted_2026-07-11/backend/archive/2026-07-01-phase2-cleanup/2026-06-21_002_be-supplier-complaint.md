---
id: MSG-BACKEND-002
from: conductor
to: backend
type: task
priority: medium
status: READ
created: 2026-06-21
model: sonnet
content_hash: e2dd5082ce5cbcf86db89cb004c40443d5de3ed5bab4d3df67696982ee46217e
---

# BE-SERVICE-001: Beszállítói reklamáció-válasz API

## Feladat: Beszállítói reklamáció válasz endpoint-ok

**Prioritás:** MEDIUM
**Típus:** Feature
**Modul:** spaceos-modules-procurement vagy spaceos-kernel (Service domain)

### Kontextus

A PROJECT_STATUS.md 6.2 backlog szerint a következő lánc-záró feature a **Beszállítói reklamáció-válasz** - a bejövő QA selejt esetén a beszállító válaszolhat a reklamációra a portálon.

### Jelenlegi állapot

Ellenőrizd a meglévő szolgáltatásokban:
- Van-e már reklamáció/complaint entitás?
- Van-e QA selejt flow?
- Hol kapcsolódik a beszállítói portálhoz?

### Szükséges funkciók

1. **Reklamáció-válasz endpoint** - a beszállító válaszolhat
2. **Státusz FSM** - pl. `pending → supplier_response → resolved/rejected`
3. **Beszállító-szűrés** - csak saját reklamációkat látja

### Előzetes lépések

1. Kutasd fel a meglévő reklamáció/service domain-t
2. Készíts rövid tervet az implementációhoz
3. Ha nincs meglévő alap, jelezd BLOCKED státusszal

### Definition of Done

- [ ] Reklamáció válasz endpoint(ok) implementálva
- [ ] Unit tesztek
- [ ] `dotnet test` PASS
- [ ] API dokumentáció

### Referenciák

- PROJECT_STATUS.md 6.2 szekció
- Meglévő service/reklamáció domain keresése szükséges
