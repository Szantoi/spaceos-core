---
id: MSG-KERNEL-089
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-E2E-050-DONE
created: 2026-04-18
---

# KRITIKUS REGRESSZIÓ — POST /api/tenants → 500

Az E2E terminál `MSG-E2E-050` futtatásán (2026-04-18) felfedezte:

```
POST http://127.0.0.1:5000/api/tenants
→ HTTP 500 "An unexpected error occurred."
```

**E2E-049 (2026-04-17) 233/233 zöld volt.** A regresszió Sprint 4 deploy (INFRA-149) után keletkezett.

**Érintett tesztek: 13 fail**
- `03-tenant-crud` (6 test)
- `04-facility-crud` (5 test)
- `07-role-based-access` (1 test)
- `40-error-paths` (1 test)

## Lehetséges okok

### 1. KERNEL-086 audit chain (commit 82a849a)

A Sprint 4-ben deployolt `KERNEL-086` módosított a tenant create pipeline-ban. Ellenőrizendő:
- `TenantEndpoints.cs` — változott-e a `POST /api/tenants` handler?
- Az audit event writer dobhat-e kivételt a tenant create közben?

### 2. Migration inkompatibilitás

Ellenőrizd: `dotnet ef migrations list` vs `SELECT * FROM "__EFMigrationsHistory"` (PostgreSQL) — azonos-e a sor szám?

### 3. RLS / GUC context

A `TenantSessionInterceptor` futás közben hibát dob-e admin kontextusban?

## Diagnosztika

```bash
# Kernel log — utolsó hibák
journalctl -u spaceos-kernel --since "1 hour ago" | grep -i "error\|exception\|500" | tail -20

# Vagy közvetlen próba (kernel belső portján):
curl -X POST http://127.0.0.1:5000/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"name":"Debug Tenant","type":"FurnitureManufacturer","domain":"debug-test"}' \
  -v 2>&1 | tail -30
```

## DoD

- [ ] `POST /api/tenants` → **201** VPS-en
- [ ] Root cause azonosítva és dokumentálva
- [ ] `dotnet test` → ≥1138 zöld
- [ ] Ha migration szükséges: INFRA task kiadva

---

*Skill: `/spaceos-terminal`*
