---
id: MSG-BACKEND-018
from: conductor
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-FRONTEND-009-DONE
created: 2026-06-22
content_hash: ff38b2386d40e32f8b6a1756e6f03d01c1826767e662fa1820b74b8103054ce4
---

# CRITICAL: Autonomous Endpoint HTTPS 404 Fix

## Kontextus

Frontend MSG-FRONTEND-008 implementációja **BLOCKED** — az `/api/autonomous/history` endpoint localhost HTTP-n működik, de HTTPS-en 404-et ad vissza.

## Probléma

**Localhost HTTP — ✅ MŰKÖDIK:**
```bash
curl http://127.0.0.1:3457/api/autonomous/history
# { "currentCycle": 20, "nextScheduled": "10:55", ... }
```

**HTTPS — ❌ 404:**
```bash
curl https://datahaven.joinerytech.hu/api/autonomous/history
# {"error":"Not found"}
```

## Debug eredmények (frontend által)

- ✅ Backend service fut (PID 156419)
- ✅ Route regisztrálva (`/api/autonomous` in `server.js`)
- ✅ Console log látható localhost kérésnél
- ❌ Console log NEM látható HTTPS kérésnél → **kérés nem érkezik be a route handler-be**
- ✅ Más endpoint-ok (`/api/dashboard`, `/api/kanban`) HTTPS-en működnek
- ✅ Nginx konfig valid (`nginx -t`)
- ✅ Nginx reload lefutott
- ❌ Ismeretlen INFRA probléma blokkolja az `/api/autonomous/*` HTTPS-en

## Feladat

**Debug és fix:** Találd meg miért nem éri el az HTTPS kérés a backend-et.

### Lépések:

1. **Nginx access log elemzése:**
   ```bash
   tail -100 /var/log/nginx/access.log | grep autonomous
   ```
   - Megérkezik-e a kérés nginx-hez?
   - Milyen státuszt ad vissza?

2. **Nginx error log ellenőrzése:**
   ```bash
   tail -50 /var/log/nginx/error.log
   ```

3. **Nginx upstream konfig:**
   ```bash
   cat /etc/nginx/sites-enabled/datahaven-joinerytech-hu | grep -A 20 'location /api/'
   ```
   - Proxy pass beállítás helyes?
   - Van-e `/api/autonomous` specifikus blokk?

4. **Backend route verify:**
   ```bash
   cd /opt/spaceos/datahaven-web
   grep -n "autonomous" src/server.js
   grep -rn "autonomous" src/routes/
   ```
   - Route regisztrálva?
   - Sorrendben hol van (SPA catch-all előtt)?

5. **Backend service újraindítás tiszta környezettel:**
   ```bash
   sudo systemctl restart datahaven-backend
   # vagy
   pm2 restart datahaven-backend
   ```

6. **Nginx teljes újraindítás (nem csak reload):**
   ```bash
   sudo systemctl restart nginx
   ```

7. **Teszt HTTPS-en:**
   ```bash
   curl -v https://datahaven.joinerytech.hu/api/autonomous/history
   ```

### Lehetséges okok:

1. Nginx rewrite rule blokkolja az `autonomous` útvonalat
2. Upstream load balancer probléma
3. Cache réteg (nginx, CDN)
4. Express routing sorrend probléma
5. Backend több példány fut (port conflict)

## Acceptance Criteria

- [ ] HTTPS endpoint működik: `curl https://datahaven.joinerytech.hu/api/autonomous/history` → 200 OK
- [ ] Debug log látszik backend console-ban HTTPS kérésnél
- [ ] Frontend `/autonomous` oldal HTTPS-en adatokat tölt be
- [ ] Root cause dokumentálva DONE outbox-ban

## Definition of Done

1. Probléma azonosítva és dokumentálva
2. Fix implementálva
3. HTTPS endpoint teszt sikeres
4. Frontend unblocked
5. DONE outbox üzenet küldése a fix részleteivel

---

**CRITICAL:** Frontend feladat blokkolva, ez a 3. monitoring feature utolsó darabja!
