---
id: MSG-ABSTRACTIONS-006
from: root
to: abstractions
type: task
priority: high
status: READ
ref: —
created: 2026-04-15
---

# MSG-ABSTRACTIONS-006 — Security Review (Q2 Pre-launch)

## Feladat

Futtass teljes biztonsági önellenőrzést a Modules.Abstractions kódbázison.
Ez egy **review feladat** — kódot csak ha kritikus sérülékenységet találsz.

## Ellenőrzési területek

### 1. Authentication & Authorization
- [ ] Minden endpoint `[Authorize]`-zal védett?
- [ ] JWT validáció: `ValidateAudience = true`? `Issuer` validáció?
- [ ] Tenant izolálás: minden query tenant-scope-on belül?

### 2. Row-Level Security (RLS) — spaceos_abstractions séma
- [ ] `ProductTemplates`, `ProductConnections`, `ComponentTypes` táblák RLS policy-val?
- [ ] `app.current_tenant_id` session változó be van-e állítva minden request előtt?
- [ ] Cross-tenant query lehetséges-e bármelyik endpointon?

### 3. Graph Engine (BFS cycle detection)
- [ ] A write-time BFS (R-18 fix, Sprint 5) nem exploitálható DoS-ra? (pl. nagyon mély gráf = O(N²) BFS traversal)
- [ ] A `AddConnection()` input validálva van-e (SourceId / TargetId = valid GUID, tenant-scope)?
- [ ] Körkörös referencia a cycle detection kódjában?

### 4. Input Validation
- [ ] FluentValidation minden Command-on?
- [ ] SQL injection: csak paraméteres EF Core query?
- [ ] Graph node IDs validálva (nem üres GUID)?

### 5. Sensitive Data
- [ ] Stack trace nem szivárog ki a response-ban?
- [ ] PII nem jelenik meg logban?

### 6. OWASP Top 10 rapid check
- [ ] A1 — Broken Access Control: cross-tenant resource → 404?
- [ ] A3 — Injection: EF Core paraméteres query?
- [ ] A5 — Security Misconfiguration: production appsettings tiszta?
- [ ] A4 — Insecure Design: gráf depth limit van-e (DoS védelem)?

## DoD

- [ ] Minden terület ellenőrizve
- [ ] Talált problémák: kritikus / közepes / alacsony szinten kategorizálva
- [ ] Ha kritikus találat: `status: BLOCKED` → azonnali jelzés
