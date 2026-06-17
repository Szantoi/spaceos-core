# Finding Severity Guide — Frontend Architecture

## Severity classification

| Súly | Ikon | Definíció | Deploy blocker? |
|------|------|-----------|-----------------|
| **CRITICAL** | 🔴 | Biztonsági rés, adatszivárgás, auth bypass, enabledModules megkerülés | ✅ IGEN — nem deployolható |
| **HIGH** | 🟠 | Funkcionalitás-sértés, performance budget túllépés, hiányzó ErrorBoundary, accessibility P1 | ✅ IGEN — nem deployolható |
| **MEDIUM** | 🟡 | Karbantarthatósági probléma, hiányzó teszt, konzisztencia-sértés, UX regression | ❌ NEM — de sprint-en belül javítandó |
| **LOW** | 🟢 | Kosmetikai, naming, optimization opportunity, nice-to-have | ❌ NEM — backlog-ba kerül |

## Frontend-specifikus osztályozás

### 🔴 CRITICAL — mindig

- JWT / refresh token localStorage-ban
- `dangerouslySetInnerHTML` sanitizálás nélkül
- `enabledModules` JWT-ből decode-olva (nem BFF verified)
- PIN login brute force védelem nélkül
- Cross-tenant adat megjelenítés
- `new Function()` / `eval()` user inputtal sandbox nélkül

### 🟠 HIGH — mindig

- Hiányzó `<ErrorBoundary>` lazy-loaded world-ön
- Hiányzó route guard (nem-engedélyezett world renderelődik)
- Flash of unauthorized content
- Bundle size > 200KB gzip (entry)
- World chunk > 80KB gzip
- Lighthouse Performance < 70
- Missing timeout BFF proxy-n
- 4+ szintű prop drilling auth/tenant adattal

### 🟡 MEDIUM — kontextustól függ

- Hiányzó `React.memo` 100+ elem listán
- Inkonzisztens TanStack Query key convention
- Hiányzó ARIA label icon-only gombon
- Missing MSW handler (teszt gap)
- Console.log production build-ben
- Hiányzó Cache-Control referencia adatokon

### 🟢 LOW — backlog

- Naming convention eltérés
- Felesleges re-render (nem látható performance hatás)
- Missing `key` prop warning (ha nincs bug)
- Inconsistent import order
