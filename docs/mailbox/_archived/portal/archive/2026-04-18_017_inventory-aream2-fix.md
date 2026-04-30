---
id: MSG-PORTAL-017
from: root
to: portal
type: task
priority: high
status: READ
ref: DEBUG-001-followup
created: 2026-04-18
---

# PORTAL-017 — InventoryPage `areaM2.toFixed()` crash fix

## Bug

```
TypeError: Cannot read properties of undefined (reading 'toFixed')
    at InventoryPage-Cezkqy69.js:1:7176
```

**Root cause:** `stock.areaM2`, `o.areaM2`, `row.areaM2` értékek `undefined`-ok lehetnek az API válaszban (null vagy hiányzó mező), de a kód közvetlenül `.toFixed(2)`-t hív rajtuk.

## Fájl

`apps/joinerytech/src/features/inventory/InventoryPage.tsx`

## Fix — 3 sor

**107. sor** (stock terület):
```tsx
// volt:
{stock.areaM2.toFixed(2)}
// legyen:
{stock.areaM2?.toFixed(2) ?? '0.00'}
```

**131. sor** (offcut terület):
```tsx
// volt:
<span className="text-sm text-gray-500">{o.areaM2.toFixed(2)} m²</span>
// legyen:
<span className="text-sm text-gray-500">{o.areaM2?.toFixed(2) ?? '0.00'} m²</span>
```

**163. sor** (trend táblázat):
```tsx
// volt:
<td className="px-4 py-3 text-right text-gray-700">{row.areaM2.toFixed(2)}</td>
// legyen:
<td className="px-4 py-3 text-right text-gray-700">{row.areaM2?.toFixed(2) ?? '0.00'}</td>
```

## Tesztek

- `InventoryPage.test.tsx` meglévő tesztjei zöldek maradjanak
- Ha nincs teszt az `areaM2 = undefined` esetre → adj hozzá egyet (stock mock-ban `areaM2: undefined`)

## Pipeline

```
1. npm run build → 0 TS error
2. npm test → minden zöld
3. OUTBOX DONE → root + INFRA deploy
```

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
