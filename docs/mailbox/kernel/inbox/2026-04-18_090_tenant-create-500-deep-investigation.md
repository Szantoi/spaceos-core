---
id: MSG-KERNEL-090
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-E2E-051-DONE
created: 2026-04-18
---

# KRITIKUS — POST /api/tenants → 500 MÉG MINDIG SIKERTELEN

Az E2E-051 teljes rerun (2026-04-18 13:37) megerősítette: a `7f8fd4c` MinIO DI guard deployolva, de a POST /api/tenants **továbbra is 500 → BFF 502**.

## Érintett tesztek (12 fail, 105 cascade skip)

```
03-tenant-crud     6 fail  → POST /bff/api/tenants → 502 (Kernel 500)
04-facility-crud   5 fail  → tenantId undefined (tenant create fail cascad)
07-role-based-access 1 fail → Admin can create tenant → 502
```

## Amit `7f8fd4c` javított (NEM ez a probléma)

- `40-error-paths`: MinIO DI resolution exception → 13/13 most zöld ✅
- De: az `AuditEventDispatcher`-t igénylő tenant creation más hibával dob 500-at

## Vizsgálandó — E2E terminál diagnózisa alapján

**1. KERNEL-086 audit chain vs. tenant create transaction**

```bash
# Kernel log — POST /api/tenants híváskor mi a stack trace?
journalctl -u spaceos-kernel --since "30 min ago" | grep -A 10 "Exception\|500\|tenant" | head -50
```

**2. Unapplied migration ellenőrzés**

```bash
# A 7f8fd4c deploy előtt/után volt-e migration diff?
cd /opt/SpaceOS.Kernel
dotnet ef migrations list
# vs PostgreSQL:
psql -U spaceos_app -d spaceos -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
```

**3. RLS / GUC context — admin token tenant create context**

- A `TenantSessionInterceptor` admin kontextusban helyesen inicializálja-e a GUC-ot?
- `SET app.tenant_id` — admin POST /api/tenants esetén szükséges-e?

**4. Közvetlen VPS teszt (hitelesített admin tokennel)**

```bash
# Admin token megszerzése (Keycloak)
TOKEN=$(curl -s -X POST "https://auth.doorstar.hu/realms/spaceos/protocol/openid-connect/token" \
  -d "grant_type=client_credentials&client_id=spaceos-kernel&client_secret=..." \
  | jq -r .access_token)

curl -X POST http://127.0.0.1:5000/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Debug Tenant","type":"FurnitureManufacturer","domain":"debug-test"}' \
  -v 2>&1
```

## DoD

- [ ] Stack trace azonosítva és dokumentálva (mi dob 500-at?)
- [ ] Fix commitolva és tesztelve: `dotnet test` ≥1138 pass
- [ ] `POST /api/tenants → 201` VPS-en (curl teszt)
- [ ] Ha migration szükséges: INFRA task kiadva
- [ ] Ha KERNEL-086 audit chain okozza: audit pipeline fix vagy bypass az érintett scope-ban

---

*Skill: `/spaceos-terminal`*
