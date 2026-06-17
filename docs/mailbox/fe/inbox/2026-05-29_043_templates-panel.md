---
id: MSG-FE-043
from: root
to: fe
type: task
priority: high
status: READ
created: 2026-05-29
---

# FE-043 — Settings TemplatesPanel

## Háttér

Az Abstractions modul (`/abstractions/`, port 5003) futó API-val rendelkezik.
A Settings oldalon jelenleg nincs TemplatesPanel — ez a feladat azt adja hozzá.

## API

```
GET  /abstractions/api/modules/templates          → lista (paginált)
GET  /abstractions/api/modules/templates/{id}     → részletek
```

**Lista response (várható mezők):** `id`, `name`, `tradeType`, paraméterszám, slot-szám, aktív/inaktív

## Feladat

### TemplatesPanel — új Settings panel

**Fájl:** `src/components/settings/TemplatesPanel.tsx` (új)

| Elem | Leírás |
|---|---|
| Sablon lista táblázat | Név, típus (`tradeType`), paraméterszám, aktív/inaktív badge |
| Sablon részletek | SlideOver: paraméterek táblázat (kulcs, érték, leírás), előnézet / graph JSON |
| Mock fallback | Ha API nem válaszol → TEMPLATES_FALLBACK mock tömb |

### Settings integrálás

A `TemplatesPanel`-t add hozzá a Settings oldal tab-sorához (ha van) vagy önálló szekcióként.
Ha a Settings lap nem tab-alapú, adj hozzá egy "Sablonok" szekciót.

## Build + test gate

- `pnpm build` → 0 TS hiba
- `pnpm test` → minden zöld, min. +8 új teszt

## DONE kritériumok

- [ ] `TemplatesPanel` lista + részletek SlideOver (API + mock fallback)
- [ ] Settings oldalba integrálva
- [ ] `pnpm build` → 0 hiba
- [ ] `pnpm test` → minden zöld (+8 legalább)
- [ ] Outbox DONE commit hash-sel
