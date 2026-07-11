---
id: MSG-FRONTEND-025-BLOCKED
from: frontend
to: conductor
type: blocked
status: READ
ref: MSG-FRONTEND-007
created: 2026-06-23
content_hash: 9ff9dbb069e84350b6a72b5d06f4b1523ebe1df4fd88b93fcd08234cebdb0c8c
---

# Katalógus Lazy-load Optimalizáció - BLOCKED

## Blocker

A feladat leírás egy **nem létező fájlra** hivatkozik: `catalog-world-view.jsx`

### Projekt fájlkeresés eredménye

```bash
find /opt/spaceos/frontend/joinerytech-portal -name "*catalog*world*" -o -name "*world*view*"
# Nincs találat
```

**Létező catalog komponensek:**
- `src/components/catalog/CatalogPanel.tsx` — táblázatos nézet (NINCS képmegjelenítés)
- `src/components/catalog/VirtualizedCatalogGrid/index.tsx` — virtualizált grid (ÚJ, MSG-FRONTEND-023)
- `src/components/catalog/CatalogFilterBar/*` — filter komponensek (ÚJ, MSG-FRONTEND-023)

## A kért funkciók már implementálva vannak!

A MSG-FRONTEND-007 által kért lazy-load funkciók **már implementálva vannak** az előző feladatban (MSG-FRONTEND-023: Week 1 Catalog Filter MVP).

### VirtualizedCatalogGrid/index.tsx — CatalogCard komponens

**1. ✅ Lazy-loading implementáció**
```tsx
// src/components/catalog/VirtualizedCatalogGrid/index.tsx:194
<img
  src={`https://via.placeholder.com/300x200?text=${encodeURIComponent(item.name)}`}
  alt={item.name}
  className={`w-full h-full object-cover ${imageLoaded ? 'opacity-100' : 'opacity-0'}`}
  onLoad={() => setImageLoaded(true)}
  onError={() => setImageError(true)}
  loading="lazy"  // ✅ Natív HTML lazy-load
/>
```

**2. ✅ Loading skeleton placeholder**
```tsx
// src/components/catalog/VirtualizedCatalogGrid/index.tsx:189-192
{!imageLoaded && !imageError && (
  <div className="absolute inset-0 flex items-center justify-center">
    <div className="animate-pulse w-full h-full bg-gray-200" />
  </div>
)}
```

**3. ✅ "Nincs kép" fallback badge**
```tsx
// src/components/catalog/VirtualizedCatalogGrid/index.tsx:181-187
{imageError ? (
  <svg
    className="w-12 h-12 text-gray-400"
    fill="none"
    stroke="currentColor"
    viewBox="0 0 24 24"
  >
    {/* Placeholder SVG icon */}
  </svg>
) : ( /* ... */ )}
```

## Kérdések a Conductor-nak

### 1. Fájl létrehozás szükséges?

A `catalog-world-view.jsx` komponens nem létezik. Kérdések:
- **Kell-e új komponenst létrehozni** ezzel a névvel?
- Vagy a feladat a **VirtualizedCatalogGrid komponensre** vonatkozott (ami már kész)?

### 2. CatalogPanel.tsx módosítás?

A `CatalogPanel.tsx` jelenleg **táblázatos nézet** inline szerkesztéssel, nincs benne képmegjelenítés.

Ha ott is kell lazy-load képkezelés:
- Hozzá kell adni `imageUrl` mezőt a Product típushoz?
- Új oszlop a táblázatba képpel?

### 3. Idea forrás pontosítás

Az idea automatikusan generálódott a "JoineryTech prototípusból". Lehet hogy egy korábbi prototípus fájlstruktúrára utal ami nem egyezik a jelenlegi projekttel?

## Javasolt megoldások

### Option A: Feladat DONE (már implementálva)

A VirtualizedCatalogGrid komponens már tartalmazza a kért funkciókat. MSG-FRONTEND-007 → DONE.

### Option B: Új komponens létrehozása

Ha a `catalog-world-view.jsx` egy külön komponens kell legyen:
- Adjatok spec-et az új komponens funkcionalitásáról
- Vagy klónozzam a VirtualizedCatalogGrid-et új néven?

### Option C: CatalogPanel.tsx kiterjesztése

Ha a meglévő CatalogPanel.tsx-be kell képeket integrálni:
- Adjatok spec-et a táblázat + kép kombinációról

## Blocker feloldásához szükséges

1. **Conductor döntés:** Melyik komponensre vonatkozik a feladat?
2. **Fájlnév pontosítás:** `catalog-world-view.jsx` vagy más komponens?
3. **Alternatíva:** Ha a VirtualizedCatalogGrid elég, akkor DONE jelzés

## Jelenlegi státusz

- ✅ VirtualizedCatalogGrid lazy-load **implementálva**
- ✅ Loading skeleton **implementálva**
- ✅ Fallback badge **implementálva**
- ❌ `catalog-world-view.jsx` **nem létezik**

---

**Frontend terminál**
2026-06-23
