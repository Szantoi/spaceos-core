---
id: MSG-FE-031-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-031
created: 2026-05-26
---

# FE-031 DONE — Tervezés (Design world) teljes design implementáció

## Státusz: KÉSZ

Minden Definition of Done kritérium teljesítve.

## Elvégzett munkák

### 1. `DesignWorldPage` — screen-alapú navigáció
A `DesignPage.tsx` teljes refaktora `SalesWorldPage` mintájára:
- `useParams` + `useNavigate` alapú screen routing
- `WorldShell` saját maga kezeli (nem `WorldPage` wrapperből)
- `key={currentScreen}` wrapper a force remounthoz
- Backward compat: `export { DesignWorldPage as DesignPage }`

### 2. Navigálható screen-ek
| URL | Komponens |
|-----|-----------|
| `/w/design` | `DesignDashboard` |
| `/w/design/dash` | `DesignDashboard` |
| `/w/design/editor` | `TemplateEditor` |
| `/w/design/generate` | `MaterialsGenerator` |
| `/w/design/catalog` | `DesignCatalog` |

### 3. `DesignDashboard` (új komponens)
- Saját padding: `px-7 py-6 space-y-6`
- 4 KPI kártya (md:grid-cols-4)
- "Népszerű sablonok" kártya: `PARAM_TEMPLATES` első 3 eleme, TemplateThumb + név/típus/verzió + uses + rating
- "Sablonok megnyitása →" link → `onScreen("editor")` (amber-700)
- "Aktív projektek" kártya: Doorstar (58% amber progress bar) + Bognár (100% emerald progress bar)

### 4. `TemplateEditor`
- Saját wrapper: `px-7 py-6`
- Advanced módban CNC preview panel (bg-stone-900, emerald-300 szöveg, Holzma komment)
- Advanced módban constraint formula megjelenítés: `{c.expr}` az egyes sorokban

### 5. `MaterialsGenerator`
- Saját wrapper: `px-7 py-6`
- Step 0 sablon választóból `T-04` kizárva (`.filter(t => t.id !== 'T-04')`)
- Step 1: "Hozzárendelés rendeléshez" kártya rendelés selector + mennyiség input-tal
- Step 2: "Egyedi hozzáadása" gomb + `extras` state, `allParts = [...resolved, ...extras]`, "egyedi" badge az extra soroknál
- Step 3: Cutting Plan ID (`CP-184-XXX`) + "Megnyitás Gyártás → Szabászat" gomb

### 6. `DesignCatalog`
- Saját wrapper: `px-7 py-6`
- Filter sorrend javítva: cats → flex-1 → "Új tétel" (a design szerint)
- "Anyag katalógus" title eltávolítva a filter sorból

### 7. `App.tsx`
- `DesignPage` import → `DesignWorldPage`
- Design route-ok: `WorldPage` wrapper → `DesignWorldPage` (standalone)

### 8. `TEMPLATES` import extra2-ből eltávolítva
Az új `DesignDashboard` `PARAM_TEMPLATES`-t használ. A régi `TEMPLATES` (extra2) import törlésre került.

## Tesztek
- **243 teszt, 0 fail** (volt 235, +8 új: dashboard KPI, popular templates, active projects, editor screen, CNC preview, constraint formula, generate screen, order card, egyedi gomb, catalog screen)
- `pnpm build` → 0 error, 0 TS warning
- `pnpm lint` → 0 új hiba (pre-existing hibák változatlanok)

## Commit
Pending
