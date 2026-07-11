---
id: MSG-FRONTEND-002
from: conductor
to: frontend
type: task
priority: high
status: DONE
created: 2026-06-21
model: sonnet
ref: MSG-BACKEND-001
content_hash: bef2d4028c95a807d2c5fda0c010cd9198fab5711071a09d2150eaf4d678db43
---

# FE-PROC-002: Beszállítói önkiszolgáló árlista UI

## Feladat: Beszállítói árlista kezelő felület implementálása

**Prioritás:** HIGH
**Típus:** Feature
**Kapcsolódik:** MSG-BACKEND-001 (Backend API kész)

### Kontextus

A backend implementálta a beszállítói árlista API-t (4 endpoint):
- POST `/api/procurement/suppliers/{supplierId}/price-list` - Új árlista (Draft)
- PUT `/api/procurement/suppliers/{supplierId}/price-list/{id}` - Szerkesztés (csak Draft)
- POST `/api/procurement/suppliers/{supplierId}/price-list/{id}/activate` - Aktiválás
- GET `/api/procurement/suppliers/{supplierId}/price-list` - Listázás

### Feladat

Implementáld a **Beszállítói portál** (`page-supplier.jsx` vagy megfelelő komponens) bővítését:

1. **Árlistáim tab** a beszállítói portálon
   - Aktív árlista megjelenítése (zöld badge)
   - Draft árlisták listája (szürke badge)
   - Expired árlisták (halványítva)

2. **Új árlista létrehozása**
   - Form: érvényesség kezdete/vége, tételek (anyagkód, ár, mennyiségi egység)
   - Mentés Draft-ként

3. **Draft szerkesztése**
   - Tételek hozzáadása/törlése/módosítása
   - Érvényesség módosítása

4. **Aktiválás flow**
   - Confirmation dialog: "Az előző aktív árlista automatikusan lejár"
   - Aktiválás gomb (csak Draft-nál)

### Üzleti szabályok (UI-ban is érvényesíteni)

- Egy beszállítónak egyszerre csak EGY aktív árlistája lehet
- Aktiváláskor az előző aktív automatikusan Expired lesz (backend kezeli)
- Csak Draft státuszú árlista szerkeszthető
- A beszállító csak a SAJÁT árlistáját látja

### Definition of Done

- [ ] Árlistáim tab implementálva
- [ ] CRUD műveletek működnek
- [ ] FSM vizualizáció (Draft → Active → Expired)
- [ ] Responsive design (mobil-first)
- [ ] `pnpm build` sikeres
- [ ] Nincs TypeScript hiba

### Referenciák

- Backend API: `/opt/spaceos/backend/spaceos-modules-procurement/`
- Meglévő beszállítói portál: keress rá a `page-supplier` vagy hasonló komponensre
- PROJECT_STATUS.md 6.2 szekció
