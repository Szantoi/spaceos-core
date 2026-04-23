# SpaceOS — Deployment Runbook

> VPS: `109.122.222.198` · Nginx (HTTPS) → Orchestrator (PM2) → Kernel (systemd)
> Domainek: `joinerytech.hu` · `asztalostech.hu`
> Referencia: [MSG-INFRA-KC01-DONE], [MSG-INFRA-056-DONE], [MSG-INFRA-061-DONE], [MSG-INFRA-062-DONE], [MSG-INFRA-063-DONE]

---

## Port térkép

| Service | Port | Protokoll | Elérhetőség |
|---------|------|-----------|-------------|
| Kernel | 5000 | HTTP | loopback-only (127.0.0.1) |
| Orchestrator | 3000 | HTTP | loopback-only (PM2) |
| Joinery | 5002 | HTTP | loopback-only |
| Abstractions | 5003 | HTTP | loopback-only |
| Inventory | 5004 | HTTP | loopback-only |
| Cutting | 5005 | HTTP | loopback-only |
| Procurement | 5006 | HTTP | loopback-only |
| Keycloak | 8080 | HTTP | loopback (via Nginx /auth/) |
| PostgreSQL | 5433 | TCP | loopback-only (NEM 5432!) |
| Nginx | 443 / 80 | HTTPS/HTTP | publikus |

**Megjegyzés:** Kernel port 5000 — az `appsettings.Production.json`-ben `"Urls": "http://127.0.0.1:5000"` van, ez felülírja az `ASPNETCORE_URLS` env var-t. [MSG-INFRA-062-DONE]

---

## Systemd service nevek

| Service | Unit neve | EnvironmentFile |
|---------|-----------|-----------------|
| Kernel | `spaceos-kernel` | `/etc/spaceos/kernel.env` |
| Joinery | `spaceos-joinery` | `/etc/spaceos/joinery.env` |
| Abstractions | `spaceos-abstractions` | `/etc/spaceos/abstractions.env` |
| Inventory | `spaceos-inventory` | `/etc/spaceos/inventory.env` |
| Cutting | `spaceos-cutting` | `/etc/spaceos/cutting.env` |
| Procurement | `spaceos-procurement` | `/etc/spaceos/procurement.env` |
| Keycloak | `keycloak` | `/opt/spaceos/keycloak/.env` |

**Hasznos parancsok:**
```bash
sudo systemctl status spaceos-kernel
sudo systemctl restart spaceos-kernel
sudo journalctl -u spaceos-kernel -n 50
sudo systemctl is-active spaceos-{kernel,joinery,abstractions,cutting,inventory,procurement}
```

---

## Orchestrator (PM2)

```bash
pm2 status
pm2 restart spaceos-orchestrator
pm2 logs spaceos-orchestrator --lines 20
```

**Working directory:** `/opt/spaceos/spaceos.orchestrator`
**env fájl:** `/etc/spaceos/orchestrator.env`

---

## Publish path és deploy minta

Minden .NET service publish path-ja:
```
/opt/spaceos/[repo-neve]/publish/
```

### Deploy lépések (.NET service)

```bash
# 1. Repo frissítés
cd /opt/spaceos/[REPO]
git fetch && git checkout develop && git pull

# 2. Build
dotnet publish [ProjectName].Api/[ProjectName].Api.csproj -c Release -o /tmp/[service]-publish

# 3. Backup
sudo cp -r /opt/spaceos/[repo]/publish /opt/spaceos/[repo]/publish.bak-$(date +%Y%m%d-%H%M%S)

# 4. Stop + clear + deploy
sudo systemctl stop spaceos-[service]
sudo rm -rf /opt/spaceos/[repo]/publish/*            # ← FONTOS: rm -rf ELŐTTE!
sudo cp -r /tmp/[service]-publish/. /opt/spaceos/[repo]/publish/
sudo chown -R spaceos-deploy:spaceos-deploy /opt/spaceos/[repo]/publish/

# 5. Start + verify
sudo systemctl start spaceos-[service]
curl http://127.0.0.1:[PORT]/healthz
```

**KRITIKUS GOTCHA:** `rm -rf publish/*` **kötelező** inkrementális build előtt! Nélküle régi DLL-ek maradhatnak → `StackExchangeRedis DLL mismatch` típusú hibák. [MSG-INFRA-062-DONE]

---

## Env fájlok tartalma

### `/etc/spaceos/kernel.env`

```ini
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=spaceos;Username=spaceos;Password=spaceos_db_pass
ASPNETCORE_URLS=http://127.0.0.1:5001   # no-op (appsettings felülírja 5000-re)
ASPNETCORE_ENVIRONMENT=Development       # Rate limit + Crypto fallback
Crypto__SigningKey=<base64>              # Dev: c3BhY2Vvcy12cHMtZGV2LXNpZ25pbmcta2V5LTIwMjY=
RateLimit__WritePerMinute=1000           # E2E-hez magas; prod: 20
RateLimit__SyncSignalPerMin=500
JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=kernel-api
```

### `/etc/spaceos/joinery.env`

```ini
ConnectionStrings__JoineryDb=Host=127.0.0.1;Port=5433;Database=spaceos_joinery;Username=spaceos;Password=spaceos_db_pass
ASPNETCORE_URLS=http://127.0.0.1:5002
Jwt__Authority=https://joinerytech.hu/auth/realms/spaceos
Jwt__Audience=kernel-api
```

### `/etc/spaceos/abstractions.env`

```ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5003
ABSTRACTIONS_BASE_URL=http://127.0.0.1:5003
ConnectionStrings__AbstractionsDb=Host=127.0.0.1;Port=5433;Database=spaceos_abstractions;Username=spaceos;Password=spaceos_db_pass
Jwt__Authority=http://127.0.0.1:8080/realms/spaceos
Jwt__Audience=spaceos-orchestrator-bff
```

Jogosultság: **640 root:spaceos** (szigorúbb, mint a kernel.env!)

### `/etc/spaceos/cutting.env`

```ini
ASPNETCORE_URLS=http://127.0.0.1:5005
ConnectionStrings__CuttingDb=Host=127.0.0.1;Port=5433;Database=spaceos_cutting;Username=spaceos;Password=spaceos_db_pass
Jwt__Authority=https://joinerytech.hu/auth/realms/spaceos
InventoryService__BaseUrl=http://127.0.0.1:5004
```

---

## GenesisHash env deploy-invariáns megoldás

**Probléma:** A `GenesisHash` az első audit event hash-e — ha minden deploy-nál változik, az audit chain megszakad.

**Megoldás:** `GenesisHash` értékét env var-ban rögzíteni: [MSG-INFRA-096 pattern]
```ini
# kernel.env
AuditChain__GenesisHash=<hash-érték>
```

Ez deploy-invariáns: a hash értéke nem változik rebuild-ek között.

---

## Keycloak deployment

**Runtime:** Bare-metal systemd, NEM Docker (CPU x86-64-v2 inkompatibilitás a VPS-en). [MSG-INFRA-KC01-DONE]

**Elérhetőség:** `http://localhost:8080` (SSH tunnel szükséges admin UI-hoz)

**Konfig fájl:** `/opt/keycloak-app/conf/keycloak.conf`

```ini
hostname=joinerytech.hu
hostname-strict=false
hostname-strict-https=true
hostname-backchannel-dynamic=true
http-relative-path=/auth
proxy=edge
db=postgres
db-url=jdbc:postgresql://127.0.0.1:5433/spaceos_keycloak
db-username=spaceos_keycloak_user
features=scripts
```

**Admin CLI:**
```bash
# Admin token kérés (master realm)
curl -X POST http://localhost:8080/auth/realms/master/protocol/openid-connect/token \
  -d "client_id=admin-cli&grant_type=password&username=admin&password=<PASS>" \
  | jq -r '.access_token' > /tmp/kc_admin_token

# Realm config lekérés
curl -H "Authorization: Bearer $(cat /tmp/kc_admin_token)" \
  http://localhost:8080/auth/admin/realms/spaceos | jq .

# Realm config módosítás (pl. token lifespan)
curl -X PUT -H "Authorization: Bearer $(cat /tmp/kc_admin_token)" \
  -H "Content-Type: application/json" \
  http://localhost:8080/auth/admin/realms/spaceos \
  -d '{"accessTokenLifespan": 300}'
```

**Script Mapper JAR deploy:**
```bash
cp /opt/keycloak-app/providers/spaceos-tenants-mapper.jar \
   /opt/keycloak-app/providers/spaceos-tenants-mapper.jar.bak-$(date +%Y%m%d-%H%M%S)
# ... új JAR másolása ...
/opt/keycloak-app/bin/kc.sh build
sudo systemctl restart keycloak
```

**Realm export:**
```bash
/opt/keycloak-app/bin/kc.sh export \
  --file /tmp/realm-export.json \
  --realm spaceos
cp /tmp/realm-export.json /opt/spaceos/keycloak/realm-export.json
```

**Keycloak clients:**
- `portal-app`: Public, PKCE S256, Standard Flow ON
- `kernel-api`: Bearer-only
- `test-runner`: Confidential, Direct Access Grant ON (E2E only)

---

## Nginx konfig kulcspontok

```nginx
# Domain-based brand routing
server {
    server_name joinerytech.hu;
    add_header X-SpaceOS-Brand "joinerytech";

    location / {
        root /opt/spaceos/design-portal/apps/joinerytech/dist/;
        try_files $uri $uri/ /index.html;
    }

    location /bff/ {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header X-SpaceOS-Brand "joinerytech";
    }

    location /auth/ {
        proxy_pass http://127.0.0.1:8080/;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto https;
    }

    location /auth/admin/ {
        allow 127.0.0.1;
        deny all;
        proxy_pass http://127.0.0.1:8080/admin/;
    }
}
```

**CSP connect-src fix (Portal):** `connect-src 'self' https://joinerytech.hu/auth/;` szükséges a Keycloak OIDC hívásokhoz. [MSG-PORTAL SEC fix]

---

## E2E env vars

```bash
# /opt/spaceos/e2e/.env
KC_URL=http://localhost:8080/auth
KC_TEST_CLIENT_SECRET=ET48o6KTW0IQPoMJCYMWyXZSAMHBipdn
E2E_TEST_PASSWORD=SpaceOS-Test-2026!
KC_TOKEN_URL=http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/token
```

**Keycloak seed profil felhasználók:**
- `test-admin` / `SpaceOS-Test-2026!` — Admin role, `doorstar-kft` group
- `designer` / `SpaceOS-Test-2026!` — Designer role, `doorstar-kft` group
- `designer-rbac`, `designer-read` — RBAC tesztekhez

**Doorstar tenant ID:** `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

---

## Rollback terv

```bash
sudo systemctl stop spaceos-[service]
sudo rm -rf /opt/spaceos/[repo]/publish/*
sudo cp -r /opt/spaceos/[repo]/publish.bak-[TIMESTAMP]/. /opt/spaceos/[repo]/publish/
sudo systemctl start spaceos-[service]
```

Rollback idő: < 10 másodperc.

---

## Sudoers konfig (`deploy-spaceos` user)

A `spaceos-deploy` user passwordless sudo jogosultsággal rendelkezik a service restart parancsokhoz.

```sudoers
spaceos-deploy ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart spaceos-*,
                                    /usr/bin/systemctl stop spaceos-*,
                                    /usr/bin/systemctl start spaceos-*,
                                    /usr/bin/cp -r /tmp/* /opt/spaceos/*/publish/*,
                                    /usr/bin/rm -rf /opt/spaceos/*/publish/*
```
