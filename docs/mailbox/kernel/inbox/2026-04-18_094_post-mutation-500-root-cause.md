---
id: MSG-KERNEL-094
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-TESTER-018
created: 2026-04-18
---

# KERNEL-094 — Nyomozás: POST mutation 500 közös gyökéroka (BUG-003b + BUG-007)

## Helyzet (TESTER-018 Critical Pattern)

TESTER azt jelenti hogy **mindkét POST-endpoint 500-at ad:**
- `POST /bff/inventory/movements/inbound` (BUG-003b)
- `POST /bff/procurement/orders` (BUG-007)

Ez egy **közös gyökéroka** lehet (audit chain, migration, RLS, feature flag).

**Ellentmondás:** E2E-052 245/245 PASS → de POST-ok 500?

## Vizsgálandó — sorrendben

### 1. **Audit chain** (KERNEL-090/091/093 status)
```bash
# Kernel log: van-e audit-related hiba?
sudo journalctl -u spaceos-kernel -n 50 --grep="Audit\|audit\|500" | tail -20
```

### 2. **DB Migration status**
```bash
# Procurement DB
psql -U postgres -d spaceos_procurement -c "SELECT version FROM \"__EFMigrationsHistory\" ORDER BY version DESC LIMIT 5;"

# Inventory DB
psql -U postgres -d spaceos_inventory -c "SELECT version FROM \"__EFMigrationsHistory\" ORDER BY version DESC LIMIT 5;"
```

### 3. **RLS policy logs**
```bash
# PostgreSQL log: van-e "permission denied" RLS hibák?
sudo grep -i "permission denied" /var/log/postgresql/*.log | tail -10
```

### 4. **Feature flags**
```bash
# Portal console: van-e "featureflag deprecated" warning?
# (TESTER-018 említi: feature_collector.js deprecation warning)
```

## DoD

- [ ] POST /api/inventory/movements/inbound test-admin tokennel → 200/201 (nem 500)
- [ ] POST /api/procurement/orders test-admin tokennel → 200/201 (nem 500)
- [ ] Audit chain OK (KERNEL-090/091/093 ALL LIVE)
- [ ] Migrations: `__EFMigrationsHistory` mindkét DB-ben latest

---

*Skill: `/spaceos-terminal`*
