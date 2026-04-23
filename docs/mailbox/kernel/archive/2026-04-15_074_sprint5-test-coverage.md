---
id: MSG-KERNEL-074
from: root
to: kernel
type: task
priority: high
status: READ
ref: R-14, R-15
created: 2026-04-15
---

# MSG-KERNEL-074 — Sprint 5: Test Coverage — DB triggerek + Audit chain

## Háttér

Devils-advocate audit (2026-04-15) két kritikus gap-et azonosított:

**R-14:** A Migration 0029 SEC-01 (`prevent_tenant_type_change`) és SEC-02 (`validate_enabled_modules_for_type`) triggerek csak valódi PostgreSQL-en léteznek. Ha bármely integrációs teszt SQLite/EF InMemory-t használ, ezek a biztonsági invariánsok tesztelés nélküliek.

**R-15:** Az audit chain SHA-256 hash integritása nincs CI-ban folyamatosan verifikálva (KERNEL-070 preexisting gap). Golden Rule #3 konvenció által tartott, nem teszttel.

## Feladat

### 1. Provider audit (R-14)

```bash
# Keress minden SQLite / InMemory referenciát az integrációs tesztekben
grep -r "UseInMemoryDatabase\|UseSqlite\|InMemoryDatabase" \
  SpaceOS.Tests/ SpaceOS.IntegrationTests/ --include="*.cs"
```

- Ha van keveredés: migráld Testcontainers alapú PostgreSQL-re
- Adj hozzá dedikált SEC-01 tesztet:
  `UPDATE "Tenants" SET "TenantType" = 'Installer' WHERE ...` → DB kivétel elvárt
- Adj hozzá dedikált SEC-02 tesztet:
  érvénytelen modul kombináció INSERT → DB kivétel elvárt

### 2. Audit chain CI teszt (R-15 = KERNEL-070)

- Hozz létre `AuditChainIntegrityTest`:
  N audit event → visszaolvas → SHA-256 lánc konzisztencia ellenőrzés
- Ha törött lánc → test fail (CI gate)
- Identifikáld a preexisting hash mismatch root cause-át

## DoD

- [ ] `grep` eredmény dokumentálva: hány teszt fut valódi Postgres-en
- [ ] SEC-01 + SEC-02 trigger dedikált tesztek léteznek és zöldek
- [ ] `AuditChainIntegrityTest` létezik és zöld
- [ ] Preexisting hash mismatch root cause azonosítva (vagy BLOCKED üzenet)
- [ ] Tesztszám ≥ 1104 (ne csökkenjen)
- [ ] DONE outbox: eredmények + új tesztszám

