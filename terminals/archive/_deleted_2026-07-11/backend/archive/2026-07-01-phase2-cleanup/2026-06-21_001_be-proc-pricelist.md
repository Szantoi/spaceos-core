---
id: MSG-BACKEND-001
from: conductor
to: backend
type: task
priority: medium
status: READ
created: 2026-06-21
model: sonnet
content_hash: 7951034fbb996b1ead736313f45c24afb42d9e2a544fc1d44501dce1c3c9c79a
---

# BE-PROC-001: Beszállítói önkiszolgáló árlista API

## Feladat: Beszállítói önkiszolgáló árlista endpoint-ok

**Prioritás:** MEDIUM
**Típus:** Feature
**Modul:** spaceos-modules-procurement

### Kontextus

A JoineryTech design terv (PROJECT_STATUS.md 6.2) szerint a következő lánc-záró feature a **Beszállítói önkiszolgáló árlista** - a beszállító maga karbantarthatja az árlistáját a portálon.

### Jelenlegi állapot

A Procurement modulban már létezik:
- `SupplierPriceDto.cs`
- `GetSupplierPricesQuery.cs` + Handler
- `PriceListStatus.cs` enum (Draft/Active/Expired)

### Szükséges új endpoint-ok

1. **POST /procurement/api/suppliers/{supplierId}/price-list** - Új árlista létrehozása (Draft státuszban)
2. **PUT /procurement/api/suppliers/{supplierId}/price-list/{id}** - Árlista szerkesztése (csak Draft állapotban)
3. **POST /procurement/api/suppliers/{supplierId}/price-list/{id}/activate** - Árlista aktiválása (Draft → Active)
4. **GET /procurement/api/suppliers/{supplierId}/price-list** - Beszállító árlistái

### Üzleti szabályok

- Egy beszállítónak egyszerre csak EGY aktív árlistája lehet
- Aktiváláskor az előző aktív árlista automatikusan Expired lesz
- A beszállító csak a SAJÁT árlistáját kezelheti (tenant + supplier ID ellenőrzés)

### Definition of Done

- [ ] 4 új endpoint implementálva
- [ ] Unit tesztek (min. 80% coverage az új kódra)
- [ ] Integration tesztek az FSM átmenetekre
- [ ] Swagger dokumentáció
- [ ] `dotnet test` PASS

### Referenciák

- `/opt/spaceos/backend/spaceos-modules-procurement/`
- `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` (6.2 szekció)
