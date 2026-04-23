---
id: MSG-KERNEL-079
from: root
to: kernel
type: task
priority: high
status: READ
ref: —
created: 2026-04-15
---

# MSG-KERNEL-079 — Security Review (Q2 Pre-launch)

## Feladat

Futtass teljes biztonsági önellenőrzést a Kernel kódbázison. Ez egy **review feladat**
— kódot csak ha kritikus sérülékenységet találsz.

## Ellenőrzési területek

### 1. Authentication & Authorization
- [ ] Minden endpoint `[Authorize]`-zal védett? Vannak-e nem szándékosan nyilvános endpointok?
- [ ] JWT validáció: `Issuer`, `Audience`, `RS256` — helyes konfig?
- [ ] `TenantSessionInterceptor` — minden request kap tenant-t, soha nem kerüli meg az RLS-t?
- [ ] `ClaimsTenantResolver` — null tenant esetén mi történik? (500 vagy 401?)

### 2. Row-Level Security (RLS)
- [ ] Minden multi-tenant tábla rendelkezik RLS policy-val?
- [ ] `AuditEvents`, `Tenants`, `Facilities`, `Nodes`, `FlowEpics`, `Stages` — mind lefedett?
- [ ] Az `app.current_tenant_id` session változó mindig be van állítva a query előtt?
- [ ] Cross-tenant query lehetséges-e bármelyik endpointon?

### 3. Input Validation
- [ ] FluentValidation minden command-on? Van-e megkerülhető validáció?
- [ ] SQL injection: csak paraméteres query (EF Core)? Van-e raw SQL string concat?
- [ ] Path traversal: fájl-alapú operációknál (ha van) van-e input sanitizáció?

### 4. Audit & Immutability
- [ ] Az `AuditEvent`-ek nem módosíthatók (UPDATE tiltva)?
- [ ] A hash chain bypass lehetséges-e (közvetlen DB insert, dispatcher megkerülése)?
- [ ] SEC-01/SEC-02 triggerek a prod DB-ben élnek?

### 5. Sensitive Data
- [ ] Jelszó/token nem kerül logba?
- [ ] `ExternalAuthToken` mező titkosítva van-e (vagy csak DB-ben tárolt plaintext)?
- [ ] Stack trace nem szivárog ki API response-ban?

### 6. OWASP Top 10 rapid check
- [ ] A1 — Broken Access Control: cross-tenant 404 (nem 403)?
- [ ] A2 — Cryptographic Failures: SHA-256 + RS256, nincs MD5/SHA1?
- [ ] A3 — Injection: EF paraméteres query mindenhol?
- [ ] A5 — Security Misconfiguration: `appsettings.Production.json` nem tartalmaz dev secret-et?
- [ ] A9 — Logging: nincs PII (személyes adat) a logokban?

## DoD

- [ ] Minden terület ellenőrizve
- [ ] Talált problémák: kritikus (azonnali fix) / közepes (következő sprint) / alacsony (backlog)
- [ ] Ha kritikus találat: `status: BLOCKED` outbox + azonnali jelzés

## Megjegyzés

Ez nem penetration test — statikus kódelemzés + logika review. Ha egy területen
nem tudsz meggyőzően véleményt alkotni, jelezd (ne találgass).
