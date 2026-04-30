---
id: MSG-PORTAL-013-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-010
created: 2026-04-19
---

## Összefoglaló

PORTAL-010 — Inventory Bevételezés UI Refactor: 3 UX fejlesztés (anyag szeparáció, auto m², dátum validáció).

## Módosítások

### 1. Anyag típus + Vastagság szeparáció

**Új constants** (`types/index.ts`):
- `MaterialTypeBase`: 'MDF' | 'Forgacslap' | 'ABSEl'
- `ThicknessType`: 16 | 18 | 22
- `MATERIAL_TYPE_BASES`, `MATERIAL_TYPE_BASE_LABELS`
- `THICKNESS_VALUES`

**Form**: Anyag típus + Vastagság külön dropdown-ok

### 2. Hossz × Szélesség + Auto m²

**Új constants** (`types/index.ts`):
- `LengthType`: 800 | 1000 | 1200 | 1500 | 2000 | 2500 | 3000
- `WidthType`: 300 | 400 | 500 | 600 | 750
- `LENGTH_VALUES`, `WIDTH_VALUES`

**Form**: Hossz + Szélesség dropdown-ok
- `useWatch` hook: length, width, panelCount figyelés
- `useEffect`: auto m² kalkuláció `(hossz/1000) × (szélesség/1000) × lapszám`
- areaM2 input: readOnly, szürke háttér (auto-filled)

### 3. Dátum validáció

**Form**:
- `<input type="date" max={today}` HTML constraint
- Zod schema: `.refine((date) => new Date(date) <= new Date(), '...')` backend validáció

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `apps/joinerytech/src/types/index.ts` | Material + Thickness + Length + Width constants |
| `apps/joinerytech/src/features/inventory/RecordInboundModal.tsx` | Form refactor: szeparáció, auto m², dátum |
| `apps/joinerytech/src/features/inventory/RecordInboundModal.test.tsx` | Test update: új form layout |

## Tesztek

323 / 323 zöld ✓

## Security review

- Input validation: Zod schema, HTML input constraints
- Auto-calculation: frontend js, backend validáció (±5% eltérés)

## Megjegyzés

Backend már támogatja a szétválasztott materialType + thickness felépítést (`RecordInboundRequest` interface).

## Commit

`git add apps/joinerytech/src/types/index.ts apps/joinerytech/src/features/inventory/RecordInboundModal.tsx apps/joinerytech/src/features/inventory/RecordInboundModal.test.tsx && git commit -m "refactor(inventory): bevételezés UI — anyag + vastagság szeparáció, auto m², dátum validáció (PORTAL-010)"`
