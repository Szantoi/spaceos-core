---
id: MSG-ROOT-002
from: root
to: operator
type: task
priority: high
status: UNREAD
created: 2026-05-28
ref: MSG-KERNEL-107-DONE, MSG-JOINERY-055-DONE
---

# ADR-039 — Kombinált VPS deploy (Kernel + Joinery)

Mindkét ADR-039 blokkoló endpoint elkészült. A Sales modul elindulásának feltétele a deploy.

---

## 1. `SpaceOS:InternalSecret` beállítása

Ez a közös shared secret — **mindkét service-ben azonos értéknek kell lennie**, és a Sales modulban is ezt fogják konfigurálni.

```bash
# Generálj egy erős secret-et (egyszer, ezt mentsd el!)
INTERNAL_SECRET=$(openssl rand -hex 32)
echo "SpaceOS__InternalSecret=$INTERNAL_SECRET"
```

### Kernel env fájl (`/etc/spaceos/kernel.env`)

```bash
sudo bash -c "echo 'SpaceOS__InternalSecret=<GENERATED_SECRET>' >> /etc/spaceos/kernel.env"
```

### Joinery env fájl

```bash
# Ellenőrizd a joinery env fájl nevét:
ls /etc/spaceos/ | grep joinery
# Ha /etc/spaceos/joinery.env:
sudo bash -c "echo 'SpaceOS__InternalSecret=<GENERATED_SECRET>' >> /etc/spaceos/joinery.env"
```

---

## 2. Kernel — Migration 0031 + redeploy

A Migration 0031 (`AddInternalAccessAuditLog`) a `app.Database.MigrateAsync()` startup-on **nem** fut automatikusan a Kernel-nél (explicit kezelés van). Kézzel alkalmazandó.

```bash
cd /opt/spaceos/backend/spaceos-kernel

# Build + publish
dotnet publish Kernel.Api -c Release -o /tmp/kernel-publish/
sudo cp -r /tmp/kernel-publish/. /opt/spaceos/backend/spaceos-kernel/publish/

# Migration (dotnet-ef 8.x kell — telepítve van)
dotnet ef database update \
  --project SpaceOS.Infrastructure \
  --startup-project Kernel.Api \
  --connection "$(grep ConnectionStrings__DefaultConnection /etc/spaceos/kernel.env | cut -d= -f2-)"

# Restart
sudo systemctl restart spaceos-kernel
sudo systemctl status spaceos-kernel
```

### Ellenőrzés

```bash
# Tábla létrejött?
sudo -u postgres psql -p 5433 -d spaceos -c "\dt" | grep InternalAccess
# → public | InternalAccessAuditLog | table | spaceos_app
```

---

## 3. Joinery — Migration J-003 + redeploy

A Migration J-003 (`J003_SalesIntegrationReceiver`) **automatikusan fut startup-kor** (`MigrateAsync()`).

```bash
cd /opt/spaceos/backend/spaceos-modules-joinery

# Build + publish
dotnet publish Joinery.Api -c Release -o /tmp/joinery-publish/
sudo cp -r /tmp/joinery-publish/. /opt/spaceos/backend/spaceos-modules-joinery/publish/

# Restart (migration automatikusan lefut)
sudo systemctl restart spaceos-modules-joinery
sudo systemctl status spaceos-modules-joinery
```

### Ellenőrzés

```bash
sudo journalctl -u spaceos-modules-joinery -n 30 | grep -i "migration\|migrat"
# → "Applying migration '20260528000001_J003_SalesIntegrationReceiver'"

sudo -u postgres psql -p 5433 -d spaceos_joinery -c "\dt" | grep DoorOrderConvertedLines
```

---

## 4. nginx — `/api/internal/*` nem exponált kifelé

Ellenőrizd, hogy az nginx konfig nem irányít ki loopback-only route-ot:

```bash
grep -n "internal" /etc/nginx/sites-available/joinerytech
# NEM szabad ilyen sor: location /api/internal/ { proxy_pass ...}
# (ha nincs ilyen sor, helyes!)
```

Ha mégis lenne, töröld az adott `location` blokkot és `sudo nginx -t && sudo systemctl reload nginx`.

---

## 5. Smoke test

```bash
# Kernel internal endpoint — loopbackon belül elérhető?
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-SpaceOS-Internal: <GENERATED_SECRET>" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  http://127.0.0.1:5000/api/internal/tenants/63ef28b6-a43b-4d3f-a076-759a47911559
# → 200

# Kívülről NEM elérhető?
curl -s -o /dev/null -w "%{http_code}" \
  https://joinerytech.hu/api/internal/tenants/63ef28b6-a43b-4d3f-a076-759a47911559
# → 404 vagy 502 (nginx nem irányítja ki)
```

---

## 6. Deploy után jelezd

Ha minden zöld, a Sales modul implementációja elindulhat (MSG-SALES-001 már kiadva).
