# SpaceOS — Sprint D · Phase 1: Infrastructure Hardening

**Verzió:** v1.4 · 2026-04-06  
**Státusz:** TERVEZÉS  
**Sprint előzmény:** Sprint C — CLOSED_DONE · 1027 teszt · Full stack deployed  
**Domainek:** joinerytech.hu · asztalostech.hu — azonos VPS, SAN tanúsítvány  
**Becsült időtartam:** 2 fejlesztői nap  
**Érintett rétegek:** DevOps · Nginx · Node.js (PM2) · .NET (systemd) · GitHub Actions · PostgreSQL

---

## 1. Sprint célja

A Sprint C lezárása után a SpaceOS platform minden rétege telepítve és tesztelve van, de az infrastruktúra nem production-grade. A `/senior-security` review 12 megállapítást azonosított (3 kritikus, 5 magas). A `/senior-backend` review 9 további implementációs problémát tárt fel (2 kritikus, 4 magas).

Ez a sprint az összes kritikus és magas prioritású hiányosságot megszünteti:

- **HTTP → HTTPS + TLS hardening + security headerek** — titkosítatlan forgalom és böngésző-szintű védelem hiánya
- **Nincs process supervisor** — szerver-összeomlás manuális beavatkozást igényel
- **Nincs CI/CD + dependency audit** — kézi deploy, nincs automatikus regressziós kapu
- **Nincs brand kontextus az audit naplóban** — visszamenőleg nem pótolható adatvesztés (joinerytech.hu vs asztalostech.hu)
- **Kernel port nyitva** — a Kernel API az Orchestrator megkerülésével közvetlenül elérhető (SEC-08)
- **CI/CD deploy jogosultság túl tág** — kompromittált Actions workflow az egész szervert átveheti (SEC-04)

---

## 2. Feladatok áttekintés

| # | Feladat | Komponens | Prioritás | Becsült idő |
|---|---|---|---|---|
| T-01 | SSL/TLS + hardening + security headerek | Nginx | **P0 — blocker** | 3–4 óra |
| T-02 | PM2 process manager — Orchestrator | Node.js | P1 — magas | 1–2 óra |
| T-03 | systemd service — Kernel API (hardened) | .NET 8 | P1 — magas | 1–2 óra |
| T-04 | GitHub Actions CI — Kernel | spaceos-kernel | P2 — közepes | 2 óra |
| T-05 | GitHub Actions CI — Orchestrator | spaceos-orchestrator | P2 — közepes | 1 óra |
| T-06 | GitHub Actions CI — Design Portal | spaceos-design-portal | P2 — közepes | 1 óra |
| T-07 | SourceBrand audit mező — schema + allowlist + hash chain | Nginx · Orchestrator · Kernel · PG | **P0 — blocker** | 2–3 óra |
| T-08 | Port 5000 lezárása — Kernel loopback-only | UFW + appsettings | **P0 — blocker** | 30 perc |
| T-09 | CI/CD deploy user — korlátozott SSH jogok | VPS + GitHub Secrets | P1 — magas | 1 óra |

**Implementációs sorrend:** T-08 → T-07 → T-01 → T-02 → T-03 → T-09 → T-04–06

---

## 3. Részletes feladatleírások

### T-01 · SSL/TLS — certbot + Nginx (két domain)

**Megvásárolt domainek:** joinerytech.hu · asztalostech.hu — azonos VPS IP, egyetlen SAN tanúsítvány.

#### DNS előfeltétel ellenőrzés

```bash
dig joinerytech.hu +short    # → VPS IP
dig asztalostech.hu +short   # → VPS IP
dig www.joinerytech.hu +short
dig www.asztalostech.hu +short
```

> DNS propagáció 1–48 óra — certbot csak ezután futtatható.

#### Certbot — SAN tanúsítvány

```bash
sudo apt update && sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx \
  -d joinerytech.hu -d www.joinerytech.hu \
  -d asztalostech.hu -d www.asztalostech.hu
```

#### TLS hardening (SEC-02)

```nginx
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:...;
ssl_prefer_server_ciphers off;
ssl_session_cache shared:SSL:10m;
ssl_session_timeout 1d;
ssl_stapling on;
ssl_stapling_verify on;
```

#### HTTP security headerek (SEC-03, SEC-12)

```nginx
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
add_header X-Frame-Options DENY always;
add_header X-Content-Type-Options nosniff always;
add_header Referrer-Policy strict-origin-when-cross-origin always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
add_header Content-Security-Policy "default-src 'self'; connect-src 'self' https://api.anthropic.com; frame-ancestors 'none';" always;
```

#### Brand header injection (T-07 előkészítés)

```nginx
# joinerytech.hu server blokkban:
proxy_set_header X-SpaceOS-Brand "joinerytech";

# asztalostech.hu server blokkban:
proxy_set_header X-SpaceOS-Brand "asztalostech";
```

#### Nginx konfiguráció végállapot

| Server blokk | Konfiguráció |
|---|---|
| `:80 — minden domain` | Redirect: `return 301 https://$host$request_uri` |
| `:443 — joinerytech.hu` | SAN cert · TLS hardening · security headerek · `X-SpaceOS-Brand: joinerytech` |
| `:443 — asztalostech.hu` | Ugyanaz a cert · `X-SpaceOS-Brand: asztalostech` |
| Auto-renewal | `certbot renew --dry-run` — mindkét domaint lefedi |

#### Multi-brand előkészítés

| Domain | Ma (Phase 1) | Phase 4 után |
|---|---|---|
| joinerytech.hu | `/opt/spaceos/design-portal/dist/` | `/opt/spaceos/joinerytech/dist/` |
| asztalostech.hu | `/opt/spaceos/design-portal/dist/` | `/opt/spaceos/asztalostech/dist/` |

#### Definition of Done

- [ ] `curl -I https://joinerytech.hu` → 200 + `Strict-Transport-Security` header
- [ ] `curl -I https://asztalostech.hu` → 200
- [ ] `curl -I http://joinerytech.hu` → 301
- [ ] `curl -I http://asztalostech.hu` → 301
- [ ] SSL Labs: A+ mindkét domainre
- [ ] TLS 1.0/1.1 kapcsolat → connection refused
- [ ] `certbot renew --dry-run` → hibátlan

---

### T-02 · PM2 — Orchestrator process manager

#### ecosystem.config.js

```js
module.exports = {
  apps: [{
    name: 'spaceos-orchestrator',
    script: 'dist/index.js',
    cwd: '/opt/spaceos/spaceos.orchestrator',
    instances: 1,
    autorestart: true,
    watch: false,
    max_memory_restart: '512M',
    pmx: false,               // SEC-11: web dashboard kikapcsolva
    merge_logs: true,
    log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
    error_file: '/var/log/spaceos/orchestrator-error.log',
    out_file:   '/var/log/spaceos/orchestrator-out.log',
    env: { NODE_ENV: 'production', PORT: 3000 }
  }]
};
```

> **SEC-05:** Express morgan log formátum — `Authorization` header explicit kizárva a logból.

#### Telepítés

```bash
npm install -g pm2
pm2 start ecosystem.config.js
pm2 save
sudo pm2 startup
```

#### Definition of Done

- [ ] `pm2 list` → spaceos-orchestrator: online
- [ ] `pm2 kill` → 5 másodpercen belül újraindul
- [ ] Szerver reboot után automatikusan fut
- [ ] Logfájlok íródnak `/var/log/spaceos/`-ba

---

### T-03 · systemd service — Kernel API (.NET 8)

**Unit fájl:** `/etc/systemd/system/spaceos-kernel.service`

```ini
[Unit]
Description=SpaceOS Kernel API (.NET 8)
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=notify
User=spaceos
WorkingDirectory=/opt/spaceos/spaceos-kernel/publish
ExecStart=/usr/bin/dotnet /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll
EnvironmentFile=/etc/spaceos/kernel.env

Restart=always
RestartSec=5
TimeoutStopSec=30          # BE-08: graceful shutdown

NoNewPrivileges=true       # SEC-06
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=/opt/spaceos/spaceos-kernel/publish /var/log/spaceos

[Install]
WantedBy=multi-user.target
```

#### EnvironmentFile jogosultság (BE-07)

```bash
sudo mkdir -p /etc/spaceos
sudo touch /etc/spaceos/kernel.env
sudo chown root:spaceos /etc/spaceos/kernel.env
sudo chmod 640 /etc/spaceos/kernel.env   # root írhat, spaceos olvashat, más senki
```

#### Aktiválás

```bash
sudo systemctl daemon-reload
sudo systemctl enable spaceos-kernel
sudo systemctl start spaceos-kernel
```

#### Definition of Done

- [ ] `systemctl status spaceos-kernel` → active (running)
- [ ] `curl http://127.0.0.1:5000/healthz` → 200 OK
- [ ] Szerver reboot után automatikusan fut
- [ ] `/etc/spaceos/kernel.env` jogosultság: 640

---

### T-04 / T-05 / T-06 · GitHub Actions CI

Mindhárom repóban azonos elvű pipeline fut. **Két job:** `test` (minden push + PR) és `deploy` (csak `main`).

#### Pipeline lépések

| Lépés | Kernel (.NET) | Orchestrator (Node) | Portal (React) |
|---|---|---|---|
| Checkout | `actions/checkout@v4` | `actions/checkout@v4` | `actions/checkout@v4` |
| Runtime | `setup-dotnet@v4 · 8.x` | `setup-node@v4 · 22` | `setup-node@v4 · 22` |
| Install | `dotnet restore` | `npm ci` | `npm ci` |
| Build | `dotnet build -c Release` | `npm run build` | `npm run build` |
| Test | `dotnet test` | `npm test` | `npm test` |
| **Vuln scan (SEC-07)** | `dotnet list --vulnerable` | `npm audit --audit-level=high` | `npm audit --audit-level=high` |
| **Migration (BE-05)** | `dotnet ef database update` | — | — |
| **Publish (BE-03)** | `dotnet publish -c Release -o ./publish` | — | — |
| Deploy (main) | SSH → publish → `systemctl restart` | SSH → `pm2 restart` | SCP `dist/` → VPS |

> **BE-05:** Migration mindig a publish/restart ELŐTT fut.  
> **BE-03:** `dotnet publish` explicit `-o ./publish` output path kötelező.

---

## 4. T-07 · SourceBrand — Adatmodell és implementáció

> Az adat visszamenőleg nem pótolható. Ha most kihagyjuk, a korábbi rekordok örökre anonim maradnak brand szempontból.

### 4.1 AuditEvent tábla — teljes schema

| Mező | Típus | NULL? | Index | Leírás |
|---|---|---|---|---|
| Id | uuid | NOT NULL | PK | COMB GUID |
| TenantId | uuid | NOT NULL | IX | Kompozit: (TenantId, OccurredAt) |
| EventType | varchar(100) | NOT NULL | IX | Szűrhető |
| ActorId | varchar(100) | NULL | — | GDPR: csak GUID |
| SourceIp | varchar(45) | NULL | — | IPv4/IPv6 |
| **SourceBrand** | **varchar(50)** | **NULL** | **IX (partial)** | **ÚJ — 'joinerytech' / 'asztalostech' / null** |
| Payload | jsonb | NOT NULL | — | jsonb: GIN index lehetséges |
| PreviousHash | varchar(64) | NOT NULL | — | SHA-256 hex |
| StateHash | varchar(64) | NOT NULL | — | SHA-256 hex — SourceBrand-et tartalmazza |
| OccurredAt | timestamptz | NOT NULL | IX | Mindig UTC |

### 4.2 Index stratégia

| Index | Oszlopok | Típus |
|---|---|---|
| IX_AuditEvents_Tenant_Time | (TenantId, OccurredAt) | Meglévő |
| IX_AuditEvents_EventType | (EventType) | Meglévő |
| **IX_AuditEvents_SourceBrand** | **(SourceBrand) WHERE NOT NULL** | **ÚJ — partial** |

### 4.3 Hash chain — egységes algoritmus (BE-01 döntés)

Nincs éles adat, csak tesztrekordok → egységes algoritmus, nincs dual-mode.

```
chainInput = previousHash + ":" + payloadJson + ":" + occurredAt(ISO8601) + ":" + (sourceBrand ?? "")
stateHash  = SHA256(UTF8(chainInput))
```

> Migration előtt: `TRUNCATE TABLE "AuditEvents"` (dev/staging) — tesztadatok törlése.

### 4.4 Migration DDL — 0011_AddSourceBrandToAuditEvents (BE-02)

> **KRITIKUS:** `CREATE INDEX CONCURRENTLY` nem futhat EF Core tranzakcióban. `suppressTransaction: true` kötelező.

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "SourceBrand",
        table: "AuditEvents",
        type: "character varying(50)",
        maxLength: 50,
        nullable: true);

    // suppressTransaction: true — KÖTELEZŐ CONCURRENTLY-hez
    migrationBuilder.Sql(
        """
        CREATE INDEX CONCURRENTLY "IX_AuditEvents_SourceBrand"
        ON "AuditEvents" ("SourceBrand")
        WHERE "SourceBrand" IS NOT NULL;
        """,
        suppressTransaction: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_AuditEvents_SourceBrand"";",
        suppressTransaction: true);

    migrationBuilder.DropColumn(name: "SourceBrand", table: "AuditEvents");
}
```

### 4.5 Header pipeline — Nginx → Orchestrator → Kernel

| Réteg | Változás | Részlet |
|---|---|---|
| Nginx | `proxy_set_header` | joinerytech.hu: `X-SpaceOS-Brand: joinerytech` · asztalostech.hu: `X-SpaceOS-Brand: asztalostech` |
| Orchestrator | Header forwarding (BE-06) | `x-spaceos-brand` explicit hozzáadva a forwarded headers listához |
| Kernel | Allowlist validáció **(SEC-01)** | Csak `"joinerytech"` és `"asztalostech"` tárolódik — ismeretlen érték → `null` |
| Kernel | Hash számítás | `sourceBrand ?? ""` bekerül a chainInput-ba |

```csharp
// AuditEventDispatcher.cs — SEC-01 allowlist
private static readonly HashSet<string> AllowedBrands =
    new(StringComparer.OrdinalIgnoreCase) { "joinerytech", "asztalostech" };

var raw = httpContext.Request.Headers["X-SpaceOS-Brand"]
    .FirstOrDefault()?.Trim().ToLowerInvariant();

var sourceBrand = AllowedBrands.Contains(raw) ? raw : null;
```

### 4.6 Definition of Done — T-07

- [ ] `dotnet ef database update` → 0 error
- [ ] `AuditEvents` táblában `SourceBrand` oszlop létezik
- [ ] joinerytech.hu művelet → `SourceBrand = 'joinerytech'`
- [ ] asztalostech.hu művelet → `SourceBrand = 'asztalostech'`
- [ ] Közvetlen API hívás → `SourceBrand = null`
- [ ] `verify-chain` → `IsValid: true` új rekordokra
- [ ] 3 unit teszt zöld: joinerytech / asztalostech / unknown → null ág
- [ ] `X-SpaceOS-Brand: hacker` → `SourceBrand = null` (allowlist teszt)

---

## 5. T-08 · Port 5000 lezárása — Kernel loopback-only (SEC-08)

```json
// appsettings.Production.json
{
  "Urls": "http://127.0.0.1:5000"
}
```

```bash
sudo ufw deny 5000
sudo ufw status
```

#### Definition of Done

- [ ] `curl -m 3 http://VPS_IP:5000/healthz` → connection refused
- [ ] `curl http://127.0.0.1:5000/healthz` (VPS-ről) → 200 OK
- [ ] Orchestrator → Kernel kommunikáció zavartalanul működik

---

## 6. T-09 · CI/CD deploy user — korlátozott SSH jogok (SEC-04)

```bash
# Deploy user
sudo adduser deploy-spaceos --disabled-password --gecos ''
sudo mkdir -p /home/deploy-spaceos/.ssh
chmod 700 /home/deploy-spaceos/.ssh

# Sudoers — BE-04: csak ez a két parancs engedélyezett
echo 'deploy-spaceos ALL=(root) NOPASSWD: /bin/systemctl restart spaceos-kernel' \
  | sudo tee /etc/sudoers.d/spaceos-deploy
echo 'deploy-spaceos ALL=(root) NOPASSWD: /bin/systemctl restart spaceos-orchestrator' \
  | sudo tee -a /etc/sudoers.d/spaceos-deploy
sudo visudo -c    # szintaxis ellenőrzés
```

#### Forced command — authorized_keys

```
command="cd /opt/spaceos/spaceos-kernel && git pull && dotnet publish -c Release -o ./publish && sudo systemctl restart spaceos-kernel",no-pty ssh-ed25519 AAAA...
```

#### GitHub Secrets

| Secret | Érték |
|---|---|
| `VPS_HOST` | 109.122.222.198 |
| `VPS_DEPLOY_USER` | deploy-spaceos |
| `VPS_DEPLOY_KEY` | privát SSH kulcs tartalma |

#### Definition of Done

- [ ] deploy-spaceos user létezik
- [ ] GitHub Actions deploy job deploy-spaceos-ként fut (nem root)
- [ ] `sudo -l -U deploy-spaceos` → csak systemctl restart engedélyezett
- [ ] Forced command aktív — egyéb SSH parancs → permission denied

---

## 7. Kockázatregiszter

| # | Kockázat | Valószínűség | Hatás | Kezelés |
|---|---|---|---|---|
| R-01 | DNS propagáció nem fejeződött be → certbot sikertelen | Alacsony | T-01 késik | `dig joinerytech.hu` ellenőrzés certbot előtt |
| R-02 | PM2 startup script nem generálódik (root jogok hiánya) | Alacsony | T-02 részleges | `sudo pm2 startup` megoldja |
| R-03 | CI deploy SSH kulcs konfiguráció hiányzik | Közepes | Deploy job nem fut | GitHub Secrets: VPS_HOST, VPS_DEPLOY_USER, VPS_DEPLOY_KEY |
| R-04 | PostgreSQL service name eltér → Kernel nem indul reboot után | Alacsony | T-03 blokkol | `systemctl list-units \| grep postgres` |
| R-05 | TLS 1.0/1.1 kliens megtagadja a kapcsolatot | Alacsony | Régi böngészők | Ipari SaaS célközönségnél nem releváns |
| R-06 | deploy-spaceos forced command gátolja a migrate futtatását | Közepes | DB migration kézi lépés marad | Migration manual step — automatizálás Sprint E-re halasztva |

---

## 8. Sprint Definition of Done

### SSL/TLS (T-01)
- [ ] `curl -I https://joinerytech.hu` → 200 + `Strict-Transport-Security`
- [ ] `curl -I https://asztalostech.hu` → 200
- [ ] HTTP → HTTPS redirect mindkét domainre → 301
- [ ] SSL Labs → A+ mindkét domainre
- [ ] TLS 1.0/1.1 → connection refused

### Process Management (T-02, T-03)
- [ ] `pm2 list` → spaceos-orchestrator: online
- [ ] `pm2 kill` → auto-restart 5 másodpercen belül
- [ ] `systemctl status spaceos-kernel` → active (running)
- [ ] Szerver reboot → mindkét service automatikusan elindul
- [ ] `/etc/spaceos/kernel.env` → chmod 640

### SourceBrand (T-07)
- [ ] Migration: 0 error
- [ ] `AuditEvents.SourceBrand` oszlop létezik
- [ ] joinerytech.hu → `SourceBrand = 'joinerytech'`
- [ ] asztalostech.hu → `SourceBrand = 'asztalostech'`
- [ ] Közvetlen API → `SourceBrand = null`
- [ ] `verify-chain` → `IsValid: true`
- [ ] 3 unit teszt zöld (joinerytech / asztalostech / null)
- [ ] Ismeretlen brand érték → `null` (allowlist véd)

### Security (T-08, T-09)
- [ ] `curl -m 3 http://109.122.222.198:5000` → connection refused
- [ ] deploy-spaceos user létezik, sudo csak systemctl restart-ra
- [ ] GitHub Actions deploy job deploy-spaceos-ként fut

### CI/CD (T-04–T-06)
- [ ] PR mindhárom repóban → Actions zöld (test + vuln scan)
- [ ] Main push → deploy job fut
- [ ] `npm audit` → 0 high/critical
- [ ] `dotnet list --vulnerable` → 0 high/critical

---

## 9. Mi jön ezután — Sprint D Phase 2+

| Fázis | Név | Tartalom |
|---|---|---|
| Phase 2 | Tool Registry — valós Kernel integráció | AI chat valódi Kernel adatokat kérdez le. Első use case: FlowEpic lekérdezése chat-ből |
| Phase 3 | Modules.Joinery MVP | Első iparági üzleti logika: ajtóméretek, anyaglista, vágási terv. Doorstar-integráció alapja |
| Phase 4 | Multi-brand architektúra | Turborepo monorepo. JoineryTech és BakeryOS párhuzamos brand skin-ek közös kernelre |

> A Modules.Joinery tervezése architecture-first elvek szerint zajlik — először a domain modell (aggregátumok, Value Object-ek, Driver interfészek), csak utána kód.

---

*SpaceOS · Belső fejlesztési dokumentum · 2026*
