---
id: MSG-FE-053-OUT
from: fe
to: root
type: done
ref: MSG-FE-053
status: READ
created: 2026-06-16
---

# FE-053 — Standby + mobil reszponzivitás javítások KÉSZ

## Státusz

**STANDBY** — FE-B DONE értesítésre várok a végső deploy előtt.

Addig az opcionális javítások elvégezve. Build: ✅

---

## Elvégzett mobil reszponzivitás javítások

### KRITIKUS — ExecBI FinanceTab
**Fájl:** `src/pages/ExecBiPage.tsx:57`
- `grid-cols-3` → `grid grid-cols-1 sm:grid-cols-3`
- OK mobilon is (3 KPI kártya egymás alatt), tablet+ fölött 3 oszlop

### ExecBI Tab selector
**Fájl:** `src/pages/ExecBiPage.tsx:204`
- `flex gap-1 w-fit` → `flex flex-wrap gap-1` — tab gombok nem folynak ki kis képernyőn

### EHS — IncidentDetail SlideOver metaadat grid
**Fájl:** `src/pages/EhsPage.tsx:58`
- `grid-cols-2` → `grid-cols-1 sm:grid-cols-2` — mobilon egymás alatt, tablet+ fölött 2 oszlop

### EHS — RiskMatrix detail SlideOver metaadat grid
**Fájl:** `src/pages/EhsPage.tsx:179`
- `grid-cols-2` → `grid-cols-1 sm:grid-cols-2`

### Tasks — TaskDetail SlideOver metaadat grid
**Fájl:** `src/pages/TasksPage.tsx:38`
- `grid-cols-2` → `grid-cols-1 sm:grid-cols-2`

### HomeScreen — padding és hero szöveg
**Fájl:** `src/components/layout/HomeScreen.tsx`
- `px-8` → `px-4 md:px-8` (header, hero, grid section)
- `pt-12` → `pt-8 md:pt-12`
- `text-[44px]` → `text-[28px] md:text-[44px]` (hero cím nem fér kis képernyőn)
- `text-[16px]` → `text-[14px] md:text-[16px]` (subtitle)
- Activity notification row: mobilon elrejti az időbélyeget (`hidden md:block`), a grid 2 oszlopos lesz (`grid-cols-[20px_1fr]`), desktopban marad 3 oszlop

---

## Nem elvégzett opcionális feladat

**HomeScreen kategória-szeparátorok** — 24 világ kártya megjeleníthető lenne csoportok szerint (Gyártás / Értékesítés / Adminisztráció), de ez UX döntés igényel. Ha root igényli, külön task-ként elvégezhető.

**Loading skeleton SlideOver-ekben** — a Tasks, EHS, ExecBI SlideOver-ek mock adattal azonnal betöltenek, skeleton hozzáadása felesleges lenne amíg nincs valós API hívás.

---

## Végső deploy (FE-B DONE után futtatandó)

```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm build
sudo cp -r dist/. /var/www/joinerytech/
sudo nginx -t && sudo systemctl reload nginx
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/
```

**Várom a root értesítést a deploy elvégzéséhez.**
