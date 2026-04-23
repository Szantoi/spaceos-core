---
id: MSG-PROCUREMENT-008
from: root
to: procurement
type: task
priority: medium
status: READ
ref: MSG-PORTAL-001-DONE
created: 2026-04-18
---

# Szállítók — E-mail és Telefon mezők nem jelennek meg (BUG-001)

## Root cause

A Portal `CreateSupplierModal` onSubmit handlere HELYES — `email` és `phone` mezők szerepelnek a POST request body-ban. A hiba a Procurement backend oldalán van.

## Vizsgálandó

1. `POST /procurement/suppliers` handler — menti-e az `email` és `phone` mezőket az adatbázisba?
2. `GET /procurement/suppliers` lista response — tartalmazza-e az `email` és `phone` mezőket?
3. Ha az entitás/DTO hiányos: addigi az `email` és `phone` mezők hozzáadása szükséges

## Tesztelési adat

```json
{
  "name": "Teszt Szállító Kft.",
  "email": "info@teszt-szallito.hu",
  "phone": "+36 1 234 5678"
}
```

**Tapasztalt:** POST után a listában `email` és `phone` oszlop `—` (üres).

## DoD

- [ ] `POST /procurement/suppliers` body-ban lévő `email` + `phone` mező mentődik DB-be
- [ ] `GET /procurement/suppliers` response tartalmazza az `email` + `phone` mezőket
- [ ] Szállítók listában az E-mail és Telefon megjelenik mentés után (manuális reteszt)
- [ ] `dotnet build` → 0 error · `dotnet test` → ≥51 zöld

---

*Skill: `/spaceos-terminal`*
