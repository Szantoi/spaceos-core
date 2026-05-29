---
id: MSG-FE-043-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-29
ref: MSG-FE-043
commit: 2e748b0
---

# FE-043 DONE — Settings TemplatesPanel API integráció

## Elvégzett munkák

### Módosított / új fájlok

| Fájl | Változás |
|---|---|
| `src/components/settings/TemplatesPanel.tsx` | Teljes újraírás: API + mock fallback + táblázat + részletek SlideOver |
| `src/pages/SettingsPage.tsx` | "Sablonok" tab hozzáadva (`templates` key) |
| `src/components/settings/__tests__/TemplatesPanel.test.tsx` | +11 új teszt |

## Funkcionális összefoglaló

### TemplatesPanel

- `GET ${API_BASE.abstractions}/api/modules/templates` → lista; paginált (`{items, totalCount}`) vagy tömb válasz is elfogadva
- Ha API nem válaszol (503/hiba): `TEMPLATES_FALLBACK` (5 rekord: 2 ajtó, 2 szekrény, 1 ablak)
- Táblázat: Név, Típus badge (Ajtó/Szekrény/Ablak/Egyedi), Paraméterek szám, Slotok szám, Aktív/Inaktív badge
- Sor kattintásra `TemplateDetailSlideOver` nyílik

### TemplateDetailSlideOver

- `GET ${API_BASE.abstractions}/api/modules/templates/{id}` → részletek; fallback `getFallbackDetail(id)`
- Státusz + típus badge + ID (mono)
- **Paraméterek táblázat:** Kulcs (mono), Érték (teal mono), Leírás
- **Graph JSON előnézet:** ha `graphJson` mező nem null — dark `<pre>` block
- Statisztika: Paraméterszám + Slotok száma kártya
- Footer: Bezárás + Példányosítás gomb

### Settings integráció

- `SettingsPage` tab sor: "Sablonok" tab hozzáadva (`templates` key), katalógus után
- `TemplatesPanel` közvetlenül renderelve (nem `CatalogPanel` alá beágyazva)

> Megjegyzés: `CatalogPanel` továbbra is tartalmazza a saját Sablonok tabját (meglévő kártyás mock UI) — ez változatlan maradt, nem törtük el.

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 381 passed, 0 failed  (+11 új teszt, előtte 370)
```

## DONE kritériumok

- [x] `TemplatesPanel` lista + részletek SlideOver (API + mock fallback)
- [x] Settings oldalba integrálva ("Sablonok" tab)
- [x] `pnpm build` → 0 hiba
- [x] `pnpm test` → minden zöld (+11 új teszt)
- [x] Outbox DONE commit: `2e748b0`
