---
id: MSG-BACKEND-020-DONE
from: backend
to: conductor
type: done
status: ARCHIVED
ref: MSG-BACKEND-018
created: 2026-06-22
---

# CRITICAL: Autonomous Endpoint HTTPS 404 Fix — DONE

## Összefoglaló

**ROOT CAUSE azonosítva és fixelve:** Nginx HTTPS konfiguráció hibásan a rossz backend port-ra proxy-zott.

## Probléma azonosítása

### Debug folyamat:

1. **Nginx access log (✅):**
   - HTTPS kérések megérkeztek nginx-hez (200+ kérés)
   - Mind 404-et adtak vissza (21 byte response)

2. **Nginx error log (✅):**
   - Nincs autonomous-specifikus hiba
   - A kérések nem érték el a backend route handler-t

3. **Port mapping felfedezés (🔍 ROOT CAUSE):**
   ```
   Port 3456: knowledge-service (spaceos-nexus)
   Port 3457: datahaven-web (dashboard backend)
   ```

4. **Nginx HTTPS konfig elemzés:**
   ```nginx
   # /etc/nginx/sites-enabled/joinerytech
   server {
       server_name datahaven.joinerytech.hu;
       listen 443 ssl http2;

       location /api/ {
           proxy_pass http://127.0.0.1:3456;  ❌ HIBÁS!
       }
   }
   ```

5. **Autonomous route lokáció:**
   ```
   /opt/spaceos/datahaven-web/src/routes/autonomousRoutes.js
   → Port 3457-en fut (NEM 3456!)
   ```

## ROOT CAUSE

**Nginx HTTPS konfig hibás port-ot használt:**
- HTTPS kérések → port 3456 (knowledge-service)
- Autonomous endpoint → port 3457 (datahaven-web)
- **Mismatch → 404**

**Miért működött localhost HTTP?**
- Közvetlen hívás a 3457-es portra → `http://127.0.0.1:3457/api/autonomous/history`
- Nem nginx-en keresztül → nincs proxy mismatch

## Implementált fix

### 1. Nginx konfig módosítás

**Fájl:** `/etc/nginx/sites-enabled/joinerytech`

**Változtatás:**
```diff
# datahaven.joinerytech.hu server block
- proxy_pass http://127.0.0.1:3456;
+ proxy_pass http://127.0.0.1:3457;
```

**Érintett location block-ok:** 6 db
- `/api/events` (SSE endpoint)
- `/api/` (általános API)
- `/health`
- `/css/`
- `/js/`
- `/` (dashboard frontend)

### 2. Deploy folyamat

```bash
# Backup készítés
sudo cp /etc/nginx/sites-enabled/joinerytech /tmp/joinerytech.bak.20260622-110736

# Fix alkalmazása (awk script)
sudo awk '/server_name datahaven\.joinerytech\.hu/,/^}/ {gsub(/127\.0\.0\.1:3456/, "127.0.0.1:3457")} 1' \
  /etc/nginx/sites-enabled/joinerytech > /tmp/joinerytech.fixed

sudo cp /tmp/joinerytech.fixed /etc/nginx/sites-enabled/joinerytech

# Konfig validálás
sudo nginx -t
# ✅ nginx: configuration file /etc/nginx/nginx.conf test is successful

# Reload
sudo systemctl reload nginx
# ✅ Sikeres
```

## Tesztek

### HTTPS endpoint teszt

```bash
# HTTP status check
curl -I https://datahaven.joinerytech.hu/api/autonomous/history
# HTTP/2 200 ✅

# Response content
curl -s https://datahaven.joinerytech.hu/api/autonomous/history
# {
#   "currentCycle": 20,
#   "nextScheduled": "11:10",
#   "skipRate": "30% (6/20)",
#   "history": [...]
# } ✅
```

### Verifikáció

- [x] HTTPS endpoint működik: 200 OK
- [x] JSON response valid
- [x] Debug log látható backend console-ban (nginx forwarded request)
- [x] Frontend `/autonomous` oldal HTTPS-en adatokat tölt be

## Acceptance Criteria

- [x] HTTPS endpoint működik: `curl https://datahaven.joinerytech.hu/api/autonomous/history` → 200 OK
- [x] Debug log látszik backend console-ban HTTPS kérésnél
- [x] Frontend `/autonomous` oldal HTTPS-en adatokat tölt be
- [x] Root cause dokumentálva DONE outbox-ban

## Változott fájlok

- **Módosított:**
  - `/etc/nginx/sites-enabled/joinerytech` (6 proxy_pass sor: 3456 → 3457)

- **Backup:**
  - `/tmp/joinerytech.bak.20260622-110736` (eredeti konfig mentve)

## Következő lépések

1. **Frontend unblocked** — MSG-FRONTEND-008 folytatható
2. **3. monitoring feature** befejezése frontend oldalon
3. **Nincs további backend munka** ehhez a featuréhez

## Megjegyzések

**Miért történt ez a probléma?**
- A knowledge-service (port 3456) később lett hozzáadva a rendszerhez
- Nginx HTTPS konfig még nem volt frissítve a datahaven-web új portjára (3457)
- Localhost HTTP konfig helyes volt (`datahaven` server block → 3457)

**Tanulság:**
- Multi-service setup-nál explicit port mapping dokumentáció szükséges
- Nginx konfig review minden új service indításakor
- HTTPS és HTTP konfig konzisztencia ellenőrzés

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
