---
id: MSG-FRONTEND-007
from: conductor
to: frontend
type: task
priority: medium
status: READ
model: sonnet
ref: IDEA-20260623-003
created: 2026-06-23
content_hash: 53288bcba95f3c1ec565497604afd739a5c2ce5495bbcea762a412760552f963
---

# Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge

## Kontextus

A `catalog-world-view.jsx` katalógus nézetben a termékképek nagyobb adatfogyasztást okoznak mobilon. Ez a feladat a képek lazy-load optimalizálását és fallback kezelését implementálja.

**Forrás idea:** `/opt/spaceos/docs/planning/ideas/2026-06-23_003_3-katalógus-termékkép-lazy-load-optimali.md`

## Feladat (1-2 óra)

### 1. Lazy-loading implementáció

Módosítsd a `catalog-world-view.jsx` komponenst:

```jsx
// Termék kép komponens lazy-load attribútummal
<img
  src={product.imageUrl}
  alt={product.name}
  loading="lazy"  // <-- natív HTML lazy-load
  className="..."
  onError={(e) => e.target.style.display = 'none'}  // fallback kezelés
/>
```

### 2. Loading skeleton placeholder

Adj hozzá egy Tailwind-alapú loading skeleton-t amely a kép betöltése közben látszik:

```jsx
<div className="relative">
  <div className="absolute inset-0 bg-slate-200 animate-pulse rounded-md" />
  <img
    src={product.imageUrl}
    loading="lazy"
    onLoad={(e) => e.target.previousSibling.remove()}  // skeleton eltüntetés
    className="relative z-10"
  />
</div>
```

### 3. "Nincs kép" fallback badge

Termékek amelyeknek nincs képe (vagy nem tölthető be):

```jsx
{!product.imageUrl && (
  <div className="w-full h-32 bg-slate-100 flex items-center justify-center rounded-md">
    <span className="text-slate-400 text-sm">Nincs kép</span>
  </div>
)}
```

## Érintett fájlok

- `catalog-world-view.jsx` (fő módosítás)
- Opcionálisan: `app-main.jsx` ha globális placeholder komponens kell

## Definition of Done

- [ ] Termékképek `loading="lazy"` attribútummal
- [ ] Loading skeleton megjelenik betöltés közben
- [ ] "Nincs kép" badge jelenik meg ha nincs `imageUrl`
- [ ] Mobil nézet ellenőrzése (DevTools throttling)
- [ ] Kód formázva (prettier)
- [ ] Build 0 hiba, 0 warning

## Tesztelési útmutató

1. DevTools Network tab → Fast 3G throttling
2. Scroll le a katalógus listán
3. Ellenőrizd hogy a képek csak akkor töltődnek be amikor viewport-ba kerülnek
4. Ellenőrizd a skeleton megjelenését
5. Tesztelj egy terméket ami nincs képpel (`imageUrl: null`)

---

**Prioritás:** medium
**Becsült idő:** 1-2 óra
**Architektúra review:** NEM szükséges (egyszerű UI optimalizáció)
