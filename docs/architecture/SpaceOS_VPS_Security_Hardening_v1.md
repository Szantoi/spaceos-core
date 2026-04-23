# SpaceOS — VPS Security Hardening Spec v1
## Lynis Audit Response · 65/100 → 80+ target

> **Verzió:** v1.0 — 2026-04-23
> **Státusz:** DRAFT — Architect spec, Gábor approval szükséges
> **VPS:** 109.122.222.198 (Debian 13 trixie)
> **Lynis Index:** 65/100 (cél: 80+)
> **Audit riport:** `/tmp/vps-audit-2026-04-23.md`
> **Referencia:** DEPLOYMENT_RUNBOOK.md, SECURITY_PATTERNS.md

---

## 0. Root prioritizálás validálása

### Elfogadott és korrigált sorrend

| Batch | Root eredeti | Architect validálás | Korrekció |
|-------|-------------|--------------------|---------| 
| **1 (KRITIKUS)** | S1-S5: port bind, apt, SSH root | **ELFOGADVA** — de S1 (Docker port) részletes stratégiával, mert Docker megkerüli az UFW-t | Docker bind fix PRIORITÁS #1 |
| **2 (HIGH)** | S6-S10: PG chmod, SSH hardening, Redis, unknown port | **ELFOGADVA** — S8 (SSH) figyelem: VS Code Remote igények | MaxSessions ≥ 5, AllowTcpForwarding marad |
| **3 (MEDIUM)** | S11-S17: auditd, AIDE, rkhunter, protocol | **ELFOGADVA** — batch 3 alacsonyabb sürgősségű, de auditd fontos a compliance-hez | auditd Phase 2-re tolható |

### Kimaradt kritikus elemek

| # | Finding | Batch | Indoklás |
|---|---------|-------|----------|
| **S-NEW-1** | pgAdmin Docker plaintext credentials: `admin@spaceos.hu` / `SpaceOsSecretPassword2026!` — container envben, Docker inspect-tel kiolvasható | 1 | Ha a port fix UTÁN is fut, a credential legalább nem elérhető kívülről |
| **S-NEW-2** | `42523/tcp` ismeretlen port — azonosítani kell mielőtt bármit teszünk | 1 | Hátha malware |

---

## 1. Docker + UFW interakció elemzés (Q2 — Root kérdés)

### A probléma

Docker alapértelmezetten közvetlenül manipulálja az `iptables` NAT és FILTER táblákat. Az UFW (Uncomplicated Firewall) a saját chain-jeit használja, amiket Docker **megkerül**:

```
UFW lánc:    INPUT → ufw-user-input → DROP   ← UFW tiltja a 5432-t
Docker lánc: PREROUTING → DOCKER → FORWARD → spaceos_postgres:5432   ← Docker engedélyezi
```

**Következmény:** `ufw deny 5432` hatástalan ha a Docker container `0.0.0.0:5432`-re bind-ol.

### Megoldási opciók

| Opció | Megoldás | Előny | Hátrány |
|-------|---------|-------|---------|
| **A) docker-compose bind fix** | `"127.0.0.1:5432:5432"` | Egyszerű, megbízható, nincs UFW dependency | Minden compose fájlt kézzel kell javítani |
| B) `iptables: false` daemon.json | Docker nem nyúl iptables-hez | UFW veszi át a kontrollt | Docker networking eltörik: container → internet nem megy |
| C) DOCKER-USER chain | `iptables -I DOCKER-USER -p tcp --dport 5432 -j DROP` | UFW + Docker együttélés | Fragilis, Docker update törhet, rossz dokumentáció |
| D) UFW-Docker plugin | `ufw-docker` 3rd party plugin | Elegáns integráció | 3rd party dependency, nem auditált |

### Döntés: **Opció A** — docker-compose bind fix

**Indoklás:**
- Legegyszerűbb, legkevesebb kockázat
- A VPS-en csak 2 Docker container van publikus port-tal (postgres, pgAdmin)
- Nincs szükség komplex UFW+Docker integrációra
- A Minecraft szerver szándékosan publikus (`19132/udp`) — azt hagyjuk

---

## 2. Keycloak bind (Q3 — Root kérdés)

**Jelenlegi állapot:** Keycloak bare-metal systemd service, `*:8080` — minden interfészen hallgat.

**Fix:** A `keycloak.conf`-ba kell a bind:

```ini
# /opt/keycloak-app/conf/keycloak.conf — HOZZÁADNI:
http-host=127.0.0.1
```

**Miért nem a systemd unit?** A Keycloak Quarkus runtime a `keycloak.conf`-ot olvassa. A systemd unit-ban `KC_HTTP_HOST=127.0.0.1` env var is működne, de a conf fájl a kanonikus megoldás és a Keycloak docs ezt javasolja.

**Ellenőrzés restart után:**
```bash
sudo systemctl restart keycloak
ss -tlnp | grep 8080
# Elvárt: 127.0.0.1:8080 (nem *:8080)
```

**Backward compatibility:** Nginx `proxy_pass http://127.0.0.1:8080/` → változatlan, mert már loopback-re mutat.

---

## 3. SSH hardening (Q4 — Root kérdés)

### VS Code Remote SSH igények

Gábor VS Code Remote-on dolgozik. A VS Code Remote SSH:
- Több SSH session-t nyit (terminal, extension host, port forward)
- `AllowTcpForwarding` kell a port forwarding-hoz (dev server, DB tunnel)
- `MaxSessions` ≥ 5 kell (VS Code 3-4 channel-t nyit per connection)
- `AllowAgentForwarding` hasznos (git push SSH kulccsal)

### Javasolt SSH konfig

```sshd_config
# /etc/ssh/sshd_config.d/99-spaceos-hardening.conf

# Auth hardening
PermitRootLogin prohibit-password      # Root SSH key OK, password NO
MaxAuthTries 3                          # Brute-force csökkentés
PubkeyAuthentication yes
PasswordAuthentication no               # Csak SSH key (ha még nincs → FIGYELEM!)

# Session limits (VS Code Remote kompatibilis)
MaxSessions 5                           # VS Code 3-4 channel → 5 elég
ClientAliveInterval 30                  # Keepalive
ClientAliveCountMax 3                   # 30*3=90s timeout

# Forwarding (VS Code Remote szükséges)
AllowTcpForwarding yes                  # VS Code port forward → kell
AllowAgentForwarding yes                # Git SSH key forward → kell
X11Forwarding no                        # Nincs GUI → tiltható

# Logging
LogLevel VERBOSE                        # Részletesebb auth log

# Port — NEM változtatjuk v1-ben
# Port 22                               # Non-standard port → v2 (Gábor dönt)
```

### Kritikus figyelmeztetés

**`PasswordAuthentication no` ELŐTT ellenőrizni kell:**
1. Gábor SSH kulcsa be van állítva (`~gabor/.ssh/authorized_keys` létezik?)
2. `spaceos-deploy` user SSH kulcsa be van állítva
3. Ha nincs SSH key → először kulcsot kell generálni, UTÁNA tiltani a jelszót
4. **Tesztelni:** Jelszó tiltása előtt nyiss egy ÚJ SSH session-t kulccsal — ha működik, UTÁNA tiltd a jelszót az eredeti session-ben

---

## 4. Backward compatibility ellenőrzés (Q5)

| Service | Port | Bind változik? | Hatás |
|---------|------|----------------|-------|
| Kernel | 5000 | NEM (már 127.0.0.1) | Nincs |
| Orchestrator | 3000 | NEM (már 127.0.0.1) | Nincs |
| Joinery | 5002 | NEM (már 127.0.0.1) | Nincs |
| Abstractions | 5003 | NEM (már 127.0.0.1) | Nincs |
| Cutting | 5005 | NEM (már 127.0.0.1) | Nincs |
| Inventory | 5004 | NEM (már 127.0.0.1) | Nincs |
| Procurement | 5006 | NEM (már 127.0.0.1) | Nincs |
| FreeTier | 5010 | NEM (már 127.0.0.1) | Nincs |
| Redis | 6379 | NEM (már 127.0.0.1) | Nincs |
| PG native | 5433 | NEM (már 127.0.0.1) | Nincs |
| **PG Docker** | **5432** | **IGEN → 127.0.0.1** | **Ellenőrizni: ki csatlakozik 5432-re?** |
| **pgAdmin** | **5050** | **IGEN → 127.0.0.1** | **SSH tunnel szükséges kívülről** |
| **Keycloak** | **8080** | **IGEN → 127.0.0.1** | **Nginx proxy → változatlan** |
| Nginx | 443/80 | NEM | Nincs |
| Minecraft | 19132 | NEM | Szándékos publikus |

### PG Docker 5432 — ki használja?

A natív PostgreSQL port 5433-on van (DEPLOYMENT_RUNBOOK.md). A Docker PG 5432-n van.

**Kérdés Gábornak:** A Docker PostgreSQL (5432) mire szolgál? Az alkalmazások a natív 5433-at használják (kernel.env, joinery.env, stb.). Ha a Docker PG csak dev/backup célú → bind fix biztonságos. Ha valami más service csatlakozik 5432-re kívülről → meg kell tudni mi.

---

## 5. Bedrock MC szerver (Q7 — Root kérdés)

**Port:** `0.0.0.0:19132/udp` — Minecraft Bedrock szerver, Docker container, 8 napja fut.

**Kockázat értékelés:**

| Szempont | Értékelés |
|----------|-----------|
| Protocol | UDP — nincs TCP handshake, amplification attack lehetséges |
| Attack surface | Bedrock server software (itzg/docker-minecraft-bedrock-server) — közismert, karbantartott |
| Adatvesztés kockázat | 0 — nem SpaceOS adat, nem érint üzleti logikát |
| SpaceOS hatás | A szerver CPU/RAM-ot fogyaszt, de izolált Docker container-ben fut |
| DDoS vektor | UDP reflection/amplification: egy támadó `19132/udp`-t abuse-olhatja válaszok erősítésére |

**Javaslat:** 
- **v1 (most):** Elfogadott kockázat — a Minecraft nem SpaceOS kritikus infrastruktúra
- **v2 (ha DDoS ér):** UFW rate limit `19132/udp`-re, vagy a Minecraft-ot külön VPS-re költöztetni
- **Fontos:** A Minecraft szerver NE futtasson semmilyen SpaceOS-hoz tartozó Docker network-öt — izoláció ellenőrizendő

---

## 6. INFRA Task Spec — Batch 1 (KRITIKUS)

### S1 — Docker PostgreSQL bind fix

**Kockázat:** 🔴 KRITIKUS — DB közvetlenül elérhető az internetről
**Fájl:** `/opt/spaceos/spaceos-kernel/docker-compose.yml`

```bash
# 1. Backup
sudo cp /opt/spaceos/spaceos-kernel/docker-compose.yml \
        /opt/spaceos/spaceos-kernel/docker-compose.yml.bak-$(date +%Y%m%d-%H%M%S)

# 2. Edit: ports "5432:5432" → "127.0.0.1:5432:5432"
# KERESENDŐ:
#   ports:
#     - "5432:5432"
# CSERÉLENDŐ:
#   ports:
#     - "127.0.0.1:5432:5432"

# 3. Restart
cd /opt/spaceos/spaceos-kernel
sudo docker compose down
sudo docker compose up -d

# 4. Verify
ss -tlnp | grep 5432
# ELVÁRT: 127.0.0.1:5432 (NEM 0.0.0.0:5432)

# 5. Smoke test — alkalmazások működnek
curl -s http://127.0.0.1:5000/healthz   # Kernel
curl -s http://127.0.0.1:5002/healthz   # Joinery
curl -s http://127.0.0.1:5005/healthz   # Cutting
```

**Rollback:**
```bash
sudo cp /opt/spaceos/spaceos-kernel/docker-compose.yml.bak-* \
        /opt/spaceos/spaceos-kernel/docker-compose.yml
cd /opt/spaceos/spaceos-kernel && sudo docker compose down && sudo docker compose up -d
```

### S2 — Docker pgAdmin bind fix

**Kockázat:** 🔴 KRITIKUS — Admin UI + plaintext credentials elérhető az internetről
**Fájl:** Ugyanaz a compose fájl, VAGY külön ha pgAdmin más compose-ban van

```bash
# 1. Keresendő a pgAdmin service block:
#   ports:
#     - "5050:80"
# Cserélendő:
#   ports:
#     - "127.0.0.1:5050:80"

# 2. Restart (compose down/up — lásd S1)

# 3. Verify
ss -tlnp | grep 5050
# ELVÁRT: 127.0.0.1:5050 (NEM 0.0.0.0:5050)

# 4. Ezentúl SSH tunnel-lel érhető el:
# ssh -L 5050:127.0.0.1:5050 user@109.122.222.198
# → böngészőben: http://localhost:5050
```

**Rollback:** compose backup visszamásolása (lásd S1).

### S3 — Keycloak loopback bind

**Kockázat:** 🟡 HIGH — Auth szerver elérhető nginx megkerülésével
**Fájl:** `/opt/keycloak-app/conf/keycloak.conf`

```bash
# 1. Backup
sudo cp /opt/keycloak-app/conf/keycloak.conf \
        /opt/keycloak-app/conf/keycloak.conf.bak-$(date +%Y%m%d-%H%M%S)

# 2. Hozzáadni a keycloak.conf-hoz:
# http-host=127.0.0.1

# 3. Rebuild + restart
sudo /opt/keycloak-app/bin/kc.sh build
sudo systemctl restart keycloak

# 4. Verify
ss -tlnp | grep 8080
# ELVÁRT: 127.0.0.1:8080 (NEM *:8080)

# 5. Smoke test — Keycloak nginx-en keresztül
curl -s https://joinerytech.hu/auth/realms/spaceos/.well-known/openid-configuration | head -1
# ELVÁRT: JSON válasz (nem timeout, nem error)
```

**Rollback:**
```bash
sudo cp /opt/keycloak-app/conf/keycloak.conf.bak-* \
        /opt/keycloak-app/conf/keycloak.conf
sudo /opt/keycloak-app/bin/kc.sh build
sudo systemctl restart keycloak
```

### S4 — Security packages frissítés

**Kockázat:** 🔴 KRITIKUS — openssh-server, openssl, libssl3t64 CVE-k
**Effort:** ~5 perc (apt, automatikus)

```bash
# 1. Lista ellenőrzés
sudo apt update
apt list --upgradable 2>/dev/null | grep -E "openssh|openssl|libssl|bind9"

# 2. Security upgrade
sudo apt upgrade -y

# 3. Service restart (ha kernel/ssh frissült)
# OpenSSH: meglévő session NEM szakad meg, de az sshd-t újra kell indítani
sudo systemctl restart sshd

# 4. Verify
ssh -V   # Új verzió
openssl version  # Új verzió
```

**Rollback:** `apt` downgrade lehetséges de nem triviális — ezért **ELŐTTE snapshot készítés** ajánlott (ha a VPS provider támogatja).

**FIGYELEM:** Az `apt upgrade` a Docker csomagokat is frissíti (docker-ce 29.3.0 → 29.4.1). Ez container restart-ot **NEM** okoz, de a Docker daemon rövid időre leáll → a containerek automatikusan újraindulnak. **Sorrend:** Először S1-S2 (bind fix), UTÁNA S4 (apt upgrade) — így a restart után a Docker containerek már a helyes bind-dal indulnak.

### S5 — SSH PermitRootLogin fix

**Kockázat:** 🟡 HIGH — root login brute-force
**Fájl:** `/etc/ssh/sshd_config.d/99-spaceos-hardening.conf` (új fájl)

```bash
# 1. ELŐFELTÉTEL: SSH key ellenőrzés!
ls -la /root/.ssh/authorized_keys 2>/dev/null
ls -la /home/gabor/.ssh/authorized_keys 2>/dev/null
ls -la /home/spaceos-deploy/.ssh/authorized_keys 2>/dev/null
# Ha BÁRMELYIK hiányzik → STOP, először kulcsot kell beállítani!

# 2. Konfig fájl létrehozása
sudo tee /etc/ssh/sshd_config.d/99-spaceos-hardening.conf <<'EOF'
# SpaceOS SSH Hardening — 2026-04-23
PermitRootLogin prohibit-password
MaxAuthTries 3
MaxSessions 5
ClientAliveInterval 30
ClientAliveCountMax 3
X11Forwarding no
LogLevel VERBOSE
EOF

# 3. Konfig teszt (NE indítsd újra validálás nélkül!)
sudo sshd -t
# ELVÁRT: nincs output (= nincs hiba)

# 4. Restart
sudo systemctl restart sshd

# 5. TESZTELÉS — ÚJ terminálból!
# Nyiss egy ÚJ SSH session-t mielőtt bezárod az aktuálist!
# Ha az új session nem működik → a régi session-ben rollback
```

**Rollback:**
```bash
sudo rm /etc/ssh/sshd_config.d/99-spaceos-hardening.conf
sudo systemctl restart sshd
```

**Megjegyzés:** `PasswordAuthentication no` NEM szerepel Batch 1-ben! Ezt csak UTÁN kell beállítani, miután ellenőrizve van hogy minden user SSH key-jel tud csatlakozni. **Ez Batch 2 (S8) feladata.**

---

## 7. INFRA Task Spec — Batch 2 (HIGH)

### S6 — PostgreSQL config chmod

```bash
sudo chmod 600 /etc/postgresql/17/main/pg_ctl.conf
sudo chmod 600 /etc/postgresql/17/main/postgresql.conf
sudo chmod 600 /etc/postgresql/17/main/start.conf
sudo chown postgres:postgres /etc/postgresql/17/main/{pg_ctl.conf,postgresql.conf,start.conf}

# Verify
ls -la /etc/postgresql/17/main/*.conf
# ELVÁRT: -rw------- postgres postgres
```

**Rollback:** `sudo chmod 644 /etc/postgresql/17/main/{pg_ctl.conf,postgresql.conf,start.conf}`

### S7 — Redis CONFIG command rename

**Fájl:** `/etc/redis/redis.conf`

```bash
# 1. Backup
sudo cp /etc/redis/redis.conf /etc/redis/redis.conf.bak-$(date +%Y%m%d-%H%M%S)

# 2. Hozzáadni:
# rename-command CONFIG ""
# Megjegyzés: üres string = letiltva; ha kellenék → random stringre: rename-command CONFIG "SPACEOS_CONFIG_29f8a"

# 3. Restart
sudo systemctl restart redis-server

# 4. Verify
redis-cli CONFIG GET maxmemory 2>&1
# ELVÁRT: "ERR unknown command 'CONFIG'" (vagy hasonló)
```

**Rollback:** backup visszamásolása + restart.

**FIGYELEM:** Ha bármilyen alkalmazás `CONFIG` parancsot használ Redis-en (pl. StackExchange.Redis `ConfigurationOptions.ConfigCheckInterval`) → az el fog törni. Ellenőrizni kell a Kernel és FreeTier Redis klienseket.

### S8 — SSH password auth tiltás (HA SSH key OK)

```bash
# ELŐFELTÉTEL: S5 után, SSH key-ek ellenőrizve!
# 1. Teszt: nyiss session-t SSH key-jel (nem jelszóval)
ssh -o PreferredAuthentications=publickey gabor@109.122.222.198
# Ha SIKERÜL → folytatható
# Ha NEM → STOP, kulcsot kell beállítani

# 2. Hozzáadni a /etc/ssh/sshd_config.d/99-spaceos-hardening.conf-hoz:
# PasswordAuthentication no

# 3. sshd -t && systemctl restart sshd

# 4. Tesztelés ÚJ session-ből (régi nyitva marad!)
```

### S9 — Ismeretlen port 42523 azonosítása

```bash
# Ki hallgat rajta?
ss -tlnp | grep 42523
# VAGY:
sudo lsof -i :42523
sudo fuser 42523/tcp

# Ha azonosított és nem szükséges → kill + tiltás
# Ha szükséges → dokumentálni
```

### S10 — Unused kernel protocols tiltása

```bash
sudo tee /etc/modprobe.d/spaceos-hardening.conf <<'EOF'
# SpaceOS — unused protocol disable (Lynis NETW-3200)
install dccp /bin/false
install sctp /bin/false
install rds /bin/false
install tipc /bin/false
EOF
```

**Rollback:** `sudo rm /etc/modprobe.d/spaceos-hardening.conf`

---

## 8. INFRA Task Spec — Batch 3 (MEDIUM, Phase 2)

### S11 — auditd

```bash
sudo apt install -y auditd
sudo systemctl enable --now auditd

# SpaceOS-specifikus audit szabályok:
sudo tee /etc/audit/rules.d/spaceos.rules <<'EOF'
# Monitor /etc/spaceos/ env fájlok
-w /etc/spaceos/ -p wa -k spaceos_env
# Monitor SSH config
-w /etc/ssh/sshd_config -p wa -k sshd_config
-w /etc/ssh/sshd_config.d/ -p wa -k sshd_config
# Monitor Docker compose files
-w /opt/spaceos/spaceos-kernel/docker-compose.yml -p wa -k docker_compose
# Monitor sudoers
-w /etc/sudoers -p wa -k sudoers
-w /etc/sudoers.d/ -p wa -k sudoers
EOF
sudo systemctl restart auditd
```

### S12 — fail2ban jail.local

```bash
sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
# Edit jail.local:
# [sshd]
# enabled = true
# maxretry = 3
# bantime = 3600
# findtime = 600
sudo systemctl restart fail2ban
```

### S13 — AIDE / rkhunter (alacsony prioritás)

```bash
sudo apt install -y aide rkhunter
sudo aideinit    # Baseline generálás (~10 perc)
sudo rkhunter --update
sudo rkhunter --propupd
```

### S14 — umask, libpam-tmpdir, needrestart

```bash
# /etc/login.defs: UMASK 027 (077 túl szigorú, 022 túl laza)
sudo sed -i 's/^UMASK.*/UMASK\t\t027/' /etc/login.defs

sudo apt install -y libpam-tmpdir needrestart debsums apt-show-versions
```

---

## 9. Végrehajtási sorrend és smoke test mátrix

### Batch 1 végrehajtási sorrend (SZIGORÚAN TARTANDÓ)

```
S1 (Docker PG bind) 
  → S2 (Docker pgAdmin bind) 
  → S3 (Keycloak bind) 
  → SMOKE TEST A 
  → S4 (apt upgrade — Docker daemon restart!) 
  → SMOKE TEST B 
  → S5 (SSH hardening) 
  → SMOKE TEST C
```

### Smoke Test A — Port bind-ok után

```bash
# 1. Port ellenőrzés
ss -tlnp | grep -E "5432|5050|8080"
# ELVÁRT:
#   127.0.0.1:5432  (PG Docker)
#   127.0.0.1:5050  (pgAdmin)
#   127.0.0.1:8080  (Keycloak)

# 2. SpaceOS service-ek élnek
for port in 5000 5002 5003 5004 5005 5006 5010 3000; do
  echo -n "Port $port: "
  curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:$port/healthz
  echo
done
# ELVÁRT: 200 mindenhol (vagy a service-specifikus healthy response)

# 3. Keycloak nginx-en keresztül
curl -s https://joinerytech.hu/auth/realms/spaceos/.well-known/openid-configuration | python3 -m json.tool | head -3

# 4. E2E auth test (ha van kéznél)
# POST login → token kapás → API hívás
```

### Smoke Test B — apt upgrade után

```bash
# 1. Docker containerek futnak
docker ps --format "table {{.Names}}\t{{.Status}}"
# ELVÁRT: spaceos_postgres UP, spaceos_pgadmin UP, bedrock-mc-production UP

# 2. Szolgáltatások
sudo systemctl is-active spaceos-{kernel,joinery,abstractions,cutting,inventory,procurement}
sudo systemctl is-active keycloak

# 3. OpenSSH verzió
ssh -V
```

### Smoke Test C — SSH hardening után

```bash
# ÚJ terminálból:
ssh gabor@109.122.222.198
# ELVÁRT: sikeres bejelentkezés SSH kulccsal

# Root login teszt (ha kell):
ssh root@109.122.222.198
# ELVÁRT: jelszóval NEM megy; kulccsal IGEN (prohibit-password)
```

---

## 10. Eredmény becslés

### Lynis index javulás

| Fix | Lynis pont becslés |
|-----|-------------------|
| Port bind (S1-S3) | +3-4 |
| apt upgrade (S4) | +4-5 (23 csomag) |
| SSH hardening (S5, S8) | +3-4 |
| PG chmod (S6) | +1-2 |
| Redis CONFIG rename (S7) | +1 |
| Protocol disable (S10) | +1 |
| **Batch 1+2 össz** | **+13-17 → 78-82/100** |
| auditd + AIDE + rkhunter (Batch 3) | +3-5 → 81-87/100 |

### Effort

| Batch | Effort | Kockázat |
|-------|--------|---------|
| Batch 1 (S1-S5) | ~30 perc | Alacsony (rollback parancsok vannak) |
| Batch 2 (S6-S10) | ~40 perc | Közepes (Redis CONFIG + SSH password) |
| Batch 3 (S11-S14) | ~1.5 óra | Alacsony (új csomagok, nem módosít meglévőt) |

---

## 11. Nyitott kérdések Gábornak

| # | Kérdés | Hatás |
|---|--------|-------|
| 1 | A Docker PostgreSQL (port 5432) mire szolgál? Az alkalmazások a natív 5433-at használják. | Ha nincs külső kliens → bind fix biztonságos |
| 2 | SSH: van-e SSH key beállítva minden user-nek? (`gabor`, `root`, `spaceos-deploy`) | S5/S8 előfeltétel |
| 3 | SSH port váltás (22 → custom): akarod-e v1-ben? | Lynis javasolja, de nem kritikus |
| 4 | Minecraft szerver: külön VPS-re költöztetés opció? | CPU/RAM isolation |
| 5 | VPS provider snapshot lehetőség apt upgrade előtt? | Safety net |
