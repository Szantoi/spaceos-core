---
created: 2026-04-20
status: DONE
completed: 2026-04-20
assignee: Gábor (VPS operator, root@spaceos)
---

# FreeTier Infra — Manuális setup (root@spaceos)

## Miért kell ez manuálisan?

Az INFRA terminál (gabor user) nem tud `apt-get`, `psql` (postgres userként), vagy `certbot` parancsokat futtatni, mert a VPS sudoers globális `use_pty` beállítása blokkolja a non-interactive sudo hívásokat. A parancsokat root-ként kell futtatni.

**Te már root@spaceos vagy** — sudo nem kell, csak másold be az alábbi blokkokat.

---

## Elvégzendő feladatok

### 1. Redis telepítés és konfiguráció

**Miért:** A FreeTier API Redis-t használ session nonce tárolásra és rate limiting számlálásra. Jelszóval védett, csak loopback-en hallgat.

```bash
apt-get install -y redis-server
grep -q "^requirepass" /etc/redis/redis.conf \
  || echo "requirepass JrkRme6TcZkssj83lbXFgEkCAkht0hS" >> /etc/redis/redis.conf
grep -q "^maxmemory " /etc/redis/redis.conf \
  || echo "maxmemory 256mb" >> /etc/redis/redis.conf
grep -q "^maxmemory-policy" /etc/redis/redis.conf \
  || echo "maxmemory-policy allkeys-lru" >> /etc/redis/redis.conf
systemctl enable redis-server
systemctl restart redis-server
redis-cli -a JrkRme6TcZkssj83lbXFgEkCAkht0hS ping
```

**Elvárt eredmény:** `PONG`

---

### 2. PostgreSQL — DB + user + role létrehozása

**Miért:** A FreeTier API saját `spaceos_freetier` adatbázisban tárol (szeparált a többi modultól). A `spaceos_freetier_share_reader` role least-privilege olvasási jogot kap a megosztott workspace nézetekhez (ADR-038 D-20 döntés).

```bash
psql -U postgres -p 5433 <<'SQL'
CREATE DATABASE spaceos_freetier;
CREATE USER spaceos_freetier WITH PASSWORD 'RJDptF2Crg9hWCCfiJYYARtIVEceMll';
GRANT ALL PRIVILEGES ON DATABASE spaceos_freetier TO spaceos_freetier;
\c spaceos_freetier
GRANT ALL ON SCHEMA public TO spaceos_freetier;
CREATE ROLE spaceos_freetier_share_reader;
SQL
```

**Verify:**
```bash
PGPASSWORD=RJDptF2Crg9hWCCfiJYYARtIVEceMll psql -U spaceos_freetier -h 127.0.0.1 -p 5433 -d spaceos_freetier -c "SELECT current_database();"
```

**Elvárt eredmény:** `spaceos_freetier`

---

### 3. Let's Encrypt SAN bővítés

**Miért:** Az nginx vhost `freetier.joinerytech.hu`-ra van konfigurálva, de a jelenlegi TLS cert valószínűleg csak `joinerytech.hu`-t tartalmaz. HTTPS nélkül a böngésző elutasítja a kapcsolatot.

```bash
# Ellenőrzés — szerepel-e már a cert-ben:
openssl x509 -in /etc/letsencrypt/live/joinerytech.hu/cert.pem -noout -text \
  | grep -A3 "Subject Alternative Name"
```

Ha `freetier.joinerytech.hu` **nem szerepel** a kimenetben:

```bash
certbot --expand -d joinerytech.hu -d freetier.joinerytech.hu \
  --nginx --non-interactive --agree-tos
```

**Elvárt eredmény:** `Successfully deployed certificate` vagy a domain már szerepel a cert-ben.

---

### 4. freetier.env jogosultság fix

**Miért:** A `/etc/spaceos/freetier.env` Redis jelszót és DB connection string-et tartalmaz — csak a `spaceos` service usernek szabad olvasnia.

```bash
chmod 640 /etc/spaceos/freetier.env
chown root:spaceos /etc/spaceos/freetier.env
ls -la /etc/spaceos/freetier.env
```

**Elvárt eredmény:** `-rw-r----- 1 root spaceos`

---

## Jelszavak (freetier.env-ben is megvannak)

| Szolgáltatás | Jelszó |
|---|---|
| Redis | `JrkRme6TcZkssj83lbXFgEkCAkht0hS` |
| PostgreSQL spaceos_freetier user | `RJDptF2Crg9hWCCfiJYYARtIVEceMll` |

---

## Eredmények (2026-04-20)

| # | Feladat | Státusz | Megjegyzés |
|---|---|---|---|
| 1 | Redis telepítés + konfig | DONE | `PONG` — jelszó, maxmemory 256mb, allkeys-lru beállítva |
| 2 | PostgreSQL DB + user + role | DONE | `spaceos_freetier` DB, user, `spaceos_freetier_share_reader` role létrehozva |
| 3 | Let's Encrypt SAN bővítés | DONE | `freetier.joinerytech.hu` hozzáadva, cert lejárat: 2026-07-19, auto-renew aktív |
| 4 | freetier.env jogosultság | DONE | `-rw-r----- root:spaceos` (640) |

---

## Elvégzés után

Jelezd Rootnak — küldöm az INFRA DONE visszaigazolást és a FREETIER terminál folytathatja a Nap 3.5 migration feladattal.
