---
id: MSG-JOINERY-004
from: root
to: joinery
type: task
priority: high
status: READ
ref: —
created: 2026-04-15
---

# MSG-JOINERY-004 — Security Review (Q2 Pre-launch)

## Feladat

Futtass teljes biztonsági önellenőrzést a Modules.Joinery kódbázison.
Ez egy **review feladat** — kódot csak ha kritikus sérülékenységet találsz.

## Ellenőrzési területek

### 1. Authentication & Authorization
- [ ] Minden endpoint `[Authorize]`-zal védett?
- [ ] JWT validáció azonos a Kernel konfiggal (`Issuer`, `Audience`, RS256)?
- [ ] Tenant izolálás: minden query tenant-scope-on belül?

### 2. Row-Level Security (RLS) — spaceos_joinery séma
- [ ] `DoorOrders`, `DoorOrderItems`, `CuttingSheets`, `ProductionSheets` táblák rendelkeznek RLS policy-val?
- [ ] `app.current_tenant_id` session változó be van-e állítva minden request előtt?
- [ ] Cross-tenant query lehetséges-e bármelyik endpointon?

### 3. PDF Generálás
- [ ] A PDF gyártásilap tartalmaz-e user által injektálható adatot (pl. `CustomerName`, `Notes`)?
- [ ] HTML template → PDF konverzión átmegy-e user input szanitizálás nélkül? (HTML injection / XSS a PDF-ben)
- [ ] A PDF fájl ideiglenes tárolása és törlése biztonságos-e?

### 4. Input Validation
- [ ] FluentValidation minden Command-on?
- [ ] Dimenzió értékek (szélesség, magasság) validálva vannak-e (min/max, pozitív, egész)?
- [ ] SQL injection: csak paraméteres EF Core query?

### 5. Sensitive Data
- [ ] Gyártói árak, kalkulációs adatok nem kerülnek ki más tenant-nak?
- [ ] Stack trace nem szivárog ki a response-ban?
- [ ] PII (ügyfél adatok) log-ban nem jelenik meg?

### 6. OrchestratorClient
- [ ] Az upstream hívások timeout-tal vannak konfigurálva?
- [ ] Upstream hibák helyes státuszkódot adnak vissza (nem 200)?

### 7. OWASP Top 10 rapid check
- [ ] A1 — Broken Access Control: cross-tenant resource → 404?
- [ ] A3 — Injection: EF Core paraméteres query, nincs string concat?
- [ ] A4 — Insecure Design: manufacturing data nem elérhető nem-gyártó role-nak?
- [ ] A5 — Security Misconfiguration: production appsettings tiszta?

## DoD

- [ ] Minden terület ellenőrizve
- [ ] Talált problémák: kritikus / közepes / alacsony szinten kategorizálva
- [ ] Ha kritikus találat: `status: BLOCKED` → azonnali jelzés
