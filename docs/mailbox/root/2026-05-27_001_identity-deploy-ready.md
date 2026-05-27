---
id: MSG-ROOT-001
from: root
to: operator
type: task
priority: high
status: READ
created: 2026-05-27
---

# Identity modul — VPS deploy (P0-1 lezárult)

P0-1 lezárult, Identity implementáció kész (63 teszt). A VPS deploy manuálisan elvégzendő.

## 1. dotnet-ef 8.x tool telepítés

```bash
dotnet tool update --global dotnet-ef --version 8.0.11
dotnet ef --version  # → 8.0.11
```

## 2. PostgreSQL — identity DB és role

```bash
sudo -u postgres psql <<'SQL'
CREATE DATABASE spaceos_identity;
CREATE ROLE identity_app WITH LOGIN PASSWORD 'ERŐS_JELSZÓ_IDE';
GRANT CONNECT ON DATABASE spaceos_identity TO identity_app;
SQL
```

## 3. Migration futtatás

```bash
cd /opt/spaceos/backend/spaceos-modules-identity
dotnet ef database update \
  --project Identity.Infrastructure \
  --startup-project Identity.Api \
  --connection "Host=127.0.0.1;Database=spaceos_identity;Username=identity_app;Password=JELSZÓ"
```

## 4. Keycloak — `spaceos-identity-service` client

KC admin konzolon (`https://joinerytech.hu/auth/admin`):
- Clients → Create → Client ID: `spaceos-identity-service`
- Client authentication: ON, Service accounts: ON
- Service account roles → assign: `manage-users` (realm management)
- Credentials → copy Client Secret

## 5. .env fájl

```bash
cat > /opt/spaceos/backend/spaceos-modules-identity/.env <<EOF
NODE_ENV=production
PORT=5008
JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=identity-api
DB_CONNECTION=Host=127.0.0.1;Database=spaceos_identity;Username=identity_app;Password=JELSZÓ
REDIS_CONNECTION=127.0.0.1:6379
KC_BASE_URL=http://localhost:8080/auth
KC_REALM=spaceos
KC_CLIENT_ID=spaceos-identity-service
KC_CLIENT_SECRET=KEYCLOAK_SECRET
EOF
chmod 640 /opt/spaceos/backend/spaceos-modules-identity/.env
chown root:spaceos /opt/spaceos/backend/spaceos-modules-identity/.env
```

## 6. Build + publish

```bash
cd /opt/spaceos/backend/spaceos-modules-identity
dotnet publish Identity.Api -c Release -o publish/
```

## 7. systemd service

```bash
sudo cp /etc/systemd/system/spaceos-kernel.service \
        /etc/systemd/system/spaceos-modules-identity.service
# Szerkeszd: Description, WorkingDirectory, ExecStart → publish/Identity.Api
sudo systemctl daemon-reload
sudo systemctl enable spaceos-modules-identity
sudo systemctl start spaceos-modules-identity
sudo systemctl status spaceos-modules-identity
```

## 8. nginx route hozzáadás

```nginx
location /identity/ {
    proxy_pass         http://127.0.0.1:5008/identity/;
    proxy_set_header   Host $host;
    proxy_set_header   X-Real-IP $remote_addr;
}
```

```bash
sudo nginx -t && sudo systemctl reload nginx
```

## 9. Ellenőrzés

```bash
curl https://joinerytech.hu/identity/health
# → {"status":"Healthy"}
```

## JWT_AUTHORITY beállítás a többi modulban (P0-1 deploy)

```bash
# Cutting, Inventory, Procurement .env-jébe:
echo "JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos" >> /opt/spaceos/backend/spaceos-modules-cutting/.env
echo "JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos" >> /opt/spaceos/backend/spaceos-modules-inventory/.env
echo "JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos" >> /opt/spaceos/backend/spaceos-modules-procurement/.env
# majd restart mindháromnak
sudo systemctl restart spaceos-cutting-svc spaceos-modules-inventory spaceos-modules-procurement
```
