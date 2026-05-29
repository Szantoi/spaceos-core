---
id: MSG-FE-044-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-29
ref: MSG-FE-044
commit: 526fa4c
---

# FE-044 DONE — DesignPage paraméter wizard API bekötés

## Elvégzett munkák

### Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/pages/DesignPage.tsx` | Új típusok + ApiParamWizard komponens + TemplateEditor picker kibővítés |
| `src/pages/__tests__/DesignPage.test.tsx` | +10 új teszt (API wizard, mock fallback, calculate, error state) |

## Funkcionális összefoglaló

### Új típusok

- `ApiTemplateDetail` — template detail API válasz (id, name, tradeType, version, isActive, parameters, graphJson, slots)
- `CalculateResult` — számítás eredmény (parts[], summary, egyéb mezők)

### ApiParamWizard komponens

- `GET ${API_BASE.abstractions}/api/modules/templates/{id}` → dinamikus paraméterek betöltése
- Paraméterek: kulcs (mono) + szám input, PUT `onBlur` per-param mentés
- "Számítás indítása" gomb → `POST /templates/{id}/calculate` → eredmény megjelenítés
  - Ha `parts[]` van: táblázat (Alkatrész, Szélesség, Magasság, Vastagság, Db)
  - Ha nincs parts: JSON pre-block (dark emerald)
- "Vágólista előnézet" gomb (calculate után) → `GET /templates/{id}/cutting-list` → JSON preview
- Loading és error state (ha detail nem töltődik be)

### TemplateEditor kibővítés

- `GET /abstractions/api/modules/templates?pageSize=50` → API sablonok listája (array vagy `{items, totalCount}`)
- Picker: API sablonok (teal, "API" badge) + PARAM_TEMPLATES mock sablonok (amber)
- Sablon típusa szerint kondicionális rendering:
  - API sablon kiválasztva → `<ApiParamWizard>` jelenik meg
  - Mock sablon kiválasztva → meglévő komplex editor (vars, parts tree, CNC preview, stb.)
- "Egyszerű/Haladó fx" mode toggle csak mock sablon esetén látható

### Mock fallback

- Ha API nem válaszol: csak a `PARAM_TEMPLATES` mock sablonok láthatók (API badge nélkül)
- API detail hiba esetén: "nem töltődött be" hibaüzenet az ApiParamWizard-ban

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 391 passed, 0 failed  (+10 új teszt, előtte 381)
```

## DONE kritériumok

- [x] Sablon picker API-ból tölt (mock fallback)
- [x] Paraméter wizard dinamikus (API template definíció alapján)
- [x] Paraméter mentés (`PUT` on blur) + számítás (`POST /calculate`) bekötve
- [x] Opcionális vágólista preview (`GET /cutting-list`) megvalósítva
- [x] `pnpm build` → 0 hiba
- [x] `pnpm test` → minden zöld (+10 új teszt)
- [x] Outbox DONE commit: `526fa4c`
