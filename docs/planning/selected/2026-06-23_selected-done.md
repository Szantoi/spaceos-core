---
created: 2026-06-23
selected_by: sonnet
status: pending_debate
top_count: 3
---
```

# SpaceOS Planning — Kiválasztott fejlesztési irányok

## TOP 1: Assembly Szerelési Sorrend Drag-and-Drop Átrendezés UI

**Miért top:** 
Ez a funkció közvetlenül a gyártócsarnokban dolgozó munkások hatékonyságát növeli. A szerelési sorrend flexibilis átrendezése kritikus, mert a valós gyártásban gyakran változnak a körülmények (hiányzó alkatrész, gépkiesés, dolgozó kompetencia). A Doorstar-nál ez azt jelenti, hogy ha egy bútor összeszerelésénél kiderül, hogy egy munkalépéshez hiányzik az anyag, a szerelő át tudja rendezni a sorrendet és folytathatja más részegységgel. Ez csökkenti az állásidőt és növeli a termelékenységet.

**Webes minták:**
- **Odoo MRP modul**: A munkarendelés műveleteinek átrendezése egyszerű lista-alapú drag-and-drop interfészen történik. A sorrend változását azonnal menti és az előrejelzett átfutási időt újraszámolja.
- **Monday.com Manufacturing**: Board viewban a taskok drag-and-drop módosítása visuális feedback-kel, "drop zone" indikátorokkal
- **dnd-kit React library**: Industry standard, accessibility-támogatással, touch-device kompatibilis, kis bundle size
- **Pattern**: Vertical list + drag handle icon + visual elevation on drag + optimistic UI update

**Javasolt megközelítés:**
1. `@dnd-kit/core` és `@dnd-kit/sortable` integrálása az assembly.jsx-be
2. Műveletek listájának átstrukturálása sortable container-ré
3. Drag handle ikon minden művelet mellett (6 pont ikon, csak érintésnél aktív mobilon)
4. Sorrend változás → app-store.jsx `updateAssemblyOrder(workOrderId, newSequence)` hívás
5. Optimistic UI: azonnal frissül, majd backend szinkron
6. Toast notification: "Szerelési sorrend mentve"
7. Mobil geszturák: long-press to activate drag (zsúfolt listákhoz)

---

## TOP 2: Dinamikus Katalógus Filter-panel szinkronizálása a localStorage Store-val

**Miért top:**
A Doorstar termelésirányítók naponta többször keresnek katalógusban (anyagok, alkatrészek, szerelvények). Ha minden oldal-frissítésnél elvesznek a szűrők, az jelentős időpazarlás. Egy asztalosiparban a katalógus akár 5000+ tételt tartalmazhat, és a szűrők (anyagminőség, méretválaszték, készletállapot) nélkül használhatatlan. A localStorage-alapú perzisztencia lehetővé teszi, hogy a dolgozó ott folytassa a keresést, ahol abbahagyta - akár órák múlva is.

**Webes minták:**
- **Odoo Purchase/Inventory**: Filter preferences mentése session-alapon, visszatöltés sidebar-ból
- **SAP Fiori**: "My Views" funkció - felhasználói szűrők mentése preset-ekként
- **Amazon B2B**: Filter state URL query params-ban (shareable, bookmarkable)
- **Pattern**: Implicit autosave (onChange) + "Clear filters" button + visual indicator ha aktív filter van

**Javasolt megközelítés:**
1. app-store.jsx új metódusok:
   ```javascript
   saveCatalogFilters(filters) // localStorage.setItem('spaceos_catalog_filters', JSON.stringify(filters))
   loadCatalogFilters() // return JSON.parse(localStorage.getItem(...)) || defaultFilters
   clearCatalogFilters() // localStorage.removeItem + reset UI
   ```
2. catalog-manager.jsx `useEffect` hook-on mount: load and apply filters
3. Filter onChange → debounced save (300ms delay, ne minden keypress-re írjon)
4. Visual badge a nav-ban: "3 aktív szűrő" ha van mentett filter
5. "Szűrők törlése" gomb → egyértelmű visszaállítás
6. Verziókezelés: localStorage-ba mentett objektum tartalmaz `version: 1` field-et, ha változik a szűrő struktúra, invalid cache-t eldobja

---

## TOP 3: Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge

**Miért top:**
A Doorstar gyártócsarnokában gyakran gyenge a WiFi lefedettség, és mobilnetről dolgoznak. Egy 200 terméket tartalmazó katalóguslap 50+ MB adatforgalmat jelenthet, ha minden kép azonnal betöltődik. A lazy loading + placeholder radikálisan csökkenti az初 oldalbetöltési időt és az adatforgalmat. Az asztalosipari alkatrészekről gyakran nincs fotó (szabványos csavarok, furatok, stb.) - a "Nincs kép" badge egyértelművé teszi, hogy ez szándékos, nem betöltési hiba.

**Webes minták:**
- **Shopify Product Lists**: Progressive image loading + low-quality placeholder (LQIP) blur effect
- **Unsplash**: Skeleton loader + fade-in transition amikor kép betölt
- **Standard pattern**: 
  - `<img loading="lazy" />` (native browser support)
  - Intersection Observer API fallback régebbi böngészőkhöz
  - Blurhash vagy SVG placeholder (aspect-ratio megőrzéssel)
  - "No image" state: grey background + icon + text

**Javasolt megközelítés:**
1. catalog-world-view.jsx komponens update:
   ```jsx
   <img 
     loading="lazy" 
     src={product.imageUrl || '/placeholder.svg'}
     className="w-full h-48 object-cover bg-gray-100"
     onError={(e) => e.target.src = '/placeholder.svg'}
   />
   ```
2. Skeleton loader komponens létrehozása:
   ```jsx
   <div className="animate-pulse bg-gray-200 w-full h-48 rounded" />
   ```
3. Image state kezelés: `loading | loaded | error`
4. "Nincs kép" badge: 
   ```jsx
   {!product.imageUrl && (
     <div className="absolute top-2 right-2 bg-gray-500 text-white px-2 py-1 rounded text-xs">
       Nincs kép
     </div>
   )}
   ```
5. Progressive enhancement: első 6 kép eager load (above the fold), többi lazy
6. Prefetch hint a következő oldalhoz (pagination esetén)

---

## Elvetett ötletek (és miért)

- **"Zöld" / "Sárga" / "Piros" készletjelzők**: Túl fragmentált ötlet, nincs kontextus. Valószínűleg egy nagyobb készletkezelési feature része kell legyen, önmagában nem értelmezhető fejlesztési egység.

- **Általános színkódolás koncepciók (004, 005, 006)**: Ezek design token-ek vagy style guide elemek, nem önálló funkciók. A manufacturing domain-ben a készletállapot vizualizáció fontos, de ezt egy komplex "Készletnyilvántartás és anyagigény-tervezés" feature részeként kell megvalósítani, amely magában foglal:
  - MRP (Material Requirements Planning) kalkulációt
  - Lead time számítást
  - Safety stock menedzsmentet
  - Beszállítói rendelés automatizálást
  
  Ezek a színkódok önmagukban csak vizuális elemek üzleti logika nélkül, így nem prioritásként kezelhetők a jelenlegi planning ciklusban.