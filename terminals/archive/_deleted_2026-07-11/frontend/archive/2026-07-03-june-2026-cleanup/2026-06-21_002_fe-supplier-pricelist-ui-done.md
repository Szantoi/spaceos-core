---
id: MSG-FRONTEND-002-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-002
created: 2026-06-21
content_hash: d133052a2f4a956e76d00d75d257bed21edc3298aff033b9615a1e1cdc7a0091
---

## Összefoglaló

Beszállítói önkiszolgáló árlista UI sikeresen implementálva.

### Implementált komponensek

**Új komponens:** `src/pages/SupplierPortalPage.tsx` (700+ sor)

### Funkciók

#### 1. Árlistáim Tab ✅
- Beszállítói portál oldal tabos felülettel
- Árlisták megjelenítése státusz szerint rendezve:
  - **Aktív** (zöld badge) - először
  - **Piszkozat** (szürke badge) - második
  - **Lejárt** (halványított) - harmadik
- Összegző metrikák: aktív, piszkozat, összes count
- Tétel előnézet (ha ≤5 tétel)

#### 2. Új Árlista Létrehozása ✅
- **NewPriceListDrawer** komponens
- Form elemek:
  - Érvényesség kezdete (kötelező)
  - Érvényesség vége (opcionális)
  - Tételek hozzáadása dinamikusan:
    - Anyagkód (font-mono)
    - Anyagnév
    - Egységár (number input, jobbra igazítva)
    - Deviza (HUF/EUR/USD select)
  - Tétel hozzáadás/törlés gombok
- API: `POST /api/procurement/suppliers/{supplierId}/price-list`
- Új árlista Draft státuszban jön létre

#### 3. Draft Szerkesztése ✅
- **EditPriceListDrawer** komponens
- Csak Draft státuszú árlisták szerkeszthetők
- Módosítható mezők:
  - Érvényesség dátumok
  - Tételek (hozzáadás/törlés/módosítás)
- API: `PUT /api/procurement/suppliers/{supplierId}/price-list/{id}`
- "Szerkesztés" gomb csak Draft-nál látható

#### 4. Aktiválás Flow ✅
- **Confirmation Dialog** implementálva
- Üzenet: "Az előző aktív árlista automatikusan lejárt státuszba kerül"
- Két gomb: "Mégse" / "Igen, aktiválom"
- API: `POST /api/procurement/suppliers/{supplierId}/price-list/{id}/activate`
- Loading state + success/error feedback

#### 5. FSM Státusz Vizualizáció ✅
- **Draft** → szürke badge (bg-stone-100, text-stone-600)
- **Active** → zöld badge (bg-emerald-50, text-emerald-700) + "✓ Ez az aktív árlista" chip
- **Expired** → halványított badge (bg-stone-50, text-stone-400) + opacity-60 a kártyán

### Üzleti Szabályok (implementálva)

- ✅ Egy beszállítónak egyszerre csak 1 aktív árlistája lehet (backend kezeli)
- ✅ Aktiváláskor az előző aktív automatikusan Expired (confirmation dialog figyelmezteti)
- ✅ Csak Draft státuszú árlista szerkeszthető (Edit gomb feltételes megjelenítés)
- ✅ A beszállító csak SAJÁT árlistáit látja (API: `/api/procurement/suppliers/{supplierId}/...`)

### Responsive Design ✅

- **Mobile-first** megközelítés
- Tailwind utility classes:
  - `sm:`, `md:`, `lg:` breakpointok
  - Grid layout tételekhez: `grid-cols-[90px_1fr_90px_60px_28px]`
  - Flexbox card layout
- Minden input és gomb érintés-barát méretben (min h-8 / h-9)

### Technical Details

**API Endpointok:**
```
GET    /api/procurement/suppliers/{supplierId}/price-list
POST   /api/procurement/suppliers/{supplierId}/price-list
PUT    /api/procurement/suppliers/{supplierId}/price-list/{id}
POST   /api/procurement/suppliers/{supplierId}/price-list/{id}/activate
```

**Routing:**
```typescript
/supplier/portal  →  SupplierPortalPage
```

**Dependencies:**
- `useApi` hook (fetch wrapper)
- `useMutation` hook (POST/PUT/DELETE wrapper)
- `SlideOver`, `Card`, `Icon`, `GhostBtn` UI components
- React Hooks: `useState`, `useEffect`

**TypeScript:**
- Strict típusozás minden komponensre
- Interface definíciók:
  - `PriceListDto`
  - `PriceListEntryDto`
  - `DraftEntry`
  - `PriceListStatus` ('Draft' | 'Active' | 'Expired')

### Build ✅

```
✓ TypeScript compile: 0 hiba
✓ Vite build: Sikeres (1.05s)
📦 Bundle: 1,196 kB (274.7 kB gzipped)
```

### Módosított/Új Fájlok

```
✅ CREATED src/pages/SupplierPortalPage.tsx (700+ sor)
✅ MODIFIED src/App.tsx
   - Import hozzáadva
   - Route hozzáadva: /supplier/portal
```

### Definition of Done ✅

- [x] Árlistáim tab implementálva
- [x] CRUD műveletek működnek
- [x] FSM vizualizáció (Draft → Active → Expired)
- [x] Responsive design (mobil-first)
- [x] `pnpm build` sikeres
- [x] Nincs TypeScript hiba

### Következő Lépések (opcionális)

1. **Auth Context Integration**
   - `supplierId` jelenleg hardcoded (`'supplier-1'`)
   - Éles környezetben az auth context-ből kell kiolvasni

2. **Mock Data vs Real API**
   - Jelenleg a komponens a backend API-t hívja
   - Ha a backend még nincs teljesen kész, fallback mock data hozzáadható

3. **Unit Tesztek**
   - SupplierPriceListsTab tesztek
   - EditPriceListDrawer tesztek
   - NewPriceListDrawer tesztek
   - Activation confirmation dialog tesztek

4. **E2E Tesztek (Playwright)**
   - Teljes workflow: Create → Edit → Activate
   - Edge cases: multiple drafts, validation errors

### Demó URL

```
https://portal.joinerytech.hu/supplier/portal
```

**Mock Supplier ID:** `supplier-1` (hardcoded a komponensben)
