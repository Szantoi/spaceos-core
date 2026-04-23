# Infra Terminal — Hidegindítási Kontextus

> VPS: `109.122.222.198`
> OS: Ubuntu/Debian
> User: `spaceos-deploy` (passwordless sudo a service restart parancsokhoz)

---

## Felelősség

- Systemd service-ek (Kernel, Joinery, Abstractions, Cutting, Inventory, Procurement)
- PM2 (Orchestrator)
- Nginx (TLS, brand routing, reverse proxy)
- Keycloak (bare-metal systemd)
- PostgreSQL (port 5433)
- Env fájlok kezelése (`/etc/spaceos/*.env`)
- Binary deploy + rollback

---

## Jelenlegi állapot (2026-04-17)

| Service | Status | Utolsó deploy |
|---------|--------|---------------|
| spaceos-kernel | active | commit `694bc56` (MapInboundClaims fix) |
| spaceos-joinery | active | 2026-04-09 (stable) |
| spaceos-abstractions | active | 2026-04-12 (M01-03 security fixes) |
| spaceos-cutting | active | 2026-04-17 (connection affinity fix) |
| spaceos-inventory | active | 2026-04-16 (interceptor) |
| spaceos-procurement | active | 2026-04-16 (interceptor) |
| spaceos-orchestrator (PM2) | online | commit `4a96e3c` (abstractions proxy) |
| keycloak | active | hostname fix, Script Mapper v2 |
| nginx | active | CSP + auth proxy |

---

## Keycloak quick ref

```bash
# Restart keycloak
sudo systemctl restart keycloak
sudo journalctl -u keycloak -n 50

# Script Mapper update
cp new.jar /opt/keycloak-app/providers/spaceos-tenants-mapper.jar
/opt/keycloak-app/bin/kc.sh build
sudo systemctl restart keycloak

# Admin token (master realm)
curl -X POST http://localhost:8080/auth/realms/master/protocol/openid-connect/token \
  -d "client_id=admin-cli&grant_type=password&username=admin&password=$(cat /opt/spaceos/keycloak/.env | grep KC_ADMIN_PASSWORD | cut -d= -f2)"

# Realm export
/opt/keycloak-app/bin/kc.sh export --file /tmp/realm-export.json --realm spaceos
cp /tmp/realm-export.json /opt/spaceos/keycloak/realm-export.json
```

**Keycloak config:** `/opt/keycloak-app/conf/keycloak.conf`

---

## Nginx kulcs lokációk

```
/etc/nginx/sites-available/joinerytech.hu
/etc/nginx/sites-enabled/joinerytech.hu
```

```bash
sudo nginx -t  # konfig teszt
sudo systemctl reload nginx
```

---

## PostgreSQL quick ref

```bash
psql -U spaceos -h localhost -p 5433 -d spaceos

# Migration history check
psql -U spaceos -p 5433 -d spaceos -c \
  'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;'

# GUC ellenőrzés
psql -U spaceos -p 5433 -d spaceos -c "SHOW app.current_tenant_id;"
```

---

## Nyitott feladatok (INFRA scope)

1. `kernel.env` jogosultság 644 → 640 (titok tartalmaz)
2. Összes modul DB `ALTER DATABASE SET "app.current_tenant_id"` megvan-e?
3. E2E `.gitignore` ellenőrzés (`.env` ne kerüljön git-be)
4. `publish.bak-*` mappák cleanup (régi backup-ok törlése)

---

## E2E env (`/opt/spaceos/e2e/.env`)

```ini
KC_URL=http://localhost:8080/auth
KC_TEST_CLIENT_SECRET=ET48o6KTW0IQPoMJCYMWyXZSAMHBipdn
E2E_TEST_PASSWORD=SpaceOS-Test-2026!
KC_TOKEN_URL=http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/token
```

**VIGYÁZAT:** Ez fájl git-be nem kerülhet! `.gitignore` ellenőrzés szükséges.
