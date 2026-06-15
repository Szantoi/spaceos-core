---
id: 2026-06-16_052_shop-deploy-done
from: fe
to: root
type: done
priority: critical
status: READ
ref_inbox: 2026-06-16_052_shop-deploy.md
date: 2026-06-16
---

# DONE — FE-052 Shop világ + Production DEPLOY

## Commit (Shop világ)

`74ba719` — feat: FE-052 Shop világ implementálása

## 1. Shop világ

### ShopPage.tsx (új fájl)
- `ProductCategoryBadge` — kategória badge (Konyha/Szekrény/Ajtó/Tartozék)
- `OrderStatusPill` — rendelés státusz badge dot-tal (Feldolgozás/Visszaigazolt/Szállítás/Lezárt)
- `OrderDetailSlideOver` — rendelés részletek: státusz, tételek táblázat (név/qty/ár/összeg), szállítási dátum, megjegyzés
- `ProductCatalog` — 12 termék rácsban (kód, kategória badge, ár Ft-ban, készlet jelző)
- `CartPanel` — kosár tételek (3 tétel), soronkénti összesítő, végösszeg
- `OrdersList` — 5 rendelési előzmény státusz pill-lel + SlideOver
- `ShopDashboard` — KPI: Nyitott rendelések (3) / Ma szállítandó (1) / Havi forgalom (2.4M Ft) / Kosárban (13 db) + recent orders preview
- `ShopWorldPage` — router: dash / catalog / cart / orders, route: `/w/shop`
- Stone + emerald akcent, ikon: `storefront`

### Mock adatok
- `src/mocks/shop.ts`: 12 termék (konyha/szekrény/ajtó/tartozék), 3 kosártétel, 5 rendelési előzmény

### Navigáció + routing
- `worlds.ts`: shop hozzáadva, WORLD_ORDER 23→24
- `types/index.ts`: WorldKey `| 'shop'`
- `HomeScreen.tsx`: Admin szerephez shop világ hozzáadva
- `App.tsx`: /w/shop, /w/shop/:screen (2 új route)
- `data.test.ts`: 23→24 world count

### Tesztek
- `ShopPage.test.tsx`: 15 teszt — dashboard, katalógus, kosár, rendelések, SlideOver

## 2. Production DEPLOY

### Build
```
dist/index.html                   0.45 kB │ gzip:   0.29 kB
dist/assets/index-Dhh3VxmA.css   84.25 kB │ gzip:  13.77 kB
dist/assets/index-PVF-vmWu.js   957.04 kB │ gzip: 217.10 kB
✓ built in 919ms
```
Build méret: ~1.1 MB (gzip: ~231 kB)

### Deploy
- Nginx root: `/opt/spaceos/frontend/joinerytech-portal/dist` (közvetlen)
- Fájljogosultságok javítva: `chmod 755` a könyvtárakra, `chmod 644` a fájlokra
- `sudo nginx -t` — OK
- `sudo systemctl reload nginx` — OK

### Smoke test
```
curl https://joinerytech.hu/ → HTTP 200 ✅
```

## Teszt összesítő

```
Test Files  68 passed (68)
Tests       671 passed (671)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 671/671 zöld
- [x] Shop világ megjelenik a Home-on (Admin)
- [x] `sudo nginx -t && reload` — sikeres
- [x] `curl https://joinerytech.hu/` — HTTP 200
- [x] Build méret: ~1.1 MB / ~231 kB gzip

## Státusz
**24/27 világ live** joinerytech.hu-n.  
Maradék 3: hr, kontrolling, service (FE-B track).
