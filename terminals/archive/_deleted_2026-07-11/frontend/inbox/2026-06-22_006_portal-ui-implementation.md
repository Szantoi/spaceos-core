---
id: MSG-FRONTEND-006
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-ARCHITECT-004
created: 2026-06-22
content_hash: 8c3f600789313a1b36c07c5149126e0b53bc67e53e2357febd77c08ef6ce44b5
---

# JoineryTech Portal UI Implementation (ADR-047)

Az Architect elkészítette a részletes specifikációt. Implementáld az első 2 feature-t.

## Referencia

**ADR dokumentum:** `/opt/spaceos/docs/adr/ADR-047_portal-ui-enhancements.md`

---

## Feladat 1: Assembly Filter Panel (MAGAS prioritás)

### Létrehozandó fájlok

```
src/components/filters/
├── FilterPanel.tsx          # Reusable filter container
├── FilterCategory.tsx       # Collapse-able category + checkboxes
└── index.ts                 # Export
```

### Módosítandó

- `src/pages/ProductionPage.tsx` — FilterPanel integráció a Cutting tab-ra

### Key implementációs pontok

1. **Lokális useState** a filter state-hez (nem zustand)
2. **Collapse animation** Tailwind `max-h-0` / `max-h-96` transition-nel
3. **Checkbox styling** a meglévő portal theme szerint (teal-600)
4. **Hardcoded kategóriák** MVP-hez: Keretrendszer, Felület, Vasalat, Egyéb

### Props interface (ADR-047-ből)

```typescript
interface FilterPanelProps {
  categories: FilterCategory[];
  selected: Record<string, string[]>;
  onChange: (categoryKey: string, values: string[]) => void;
  collapsed?: boolean;
}
```

---

## Feladat 2: KPI Cards (KÖZEPES prioritás)

### Módosítandó

- `src/pages/ProcurementPage.tsx` — KPI grid hozzáadása a lap tetejére

### Meglévő komponens használata

```typescript
import { KpiCard } from '../components/ui'
```

### 4 KPI implementálása (mock data)

| KPI | Value | Unit | Típus |
|---|---|---|---|
| On-time szállítás | 94 | % | delta + spark |
| Ár-stabilitás | 2.3 | % | delta + spark |
| Minőségi értékelés | 4.6 | ★ | delta + spark |
| Aktív szállítók | 12 | - | breakdowns |

### Grid layout

```tsx
<div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-5">
  {kpis.map(k => <KpiCard key={k.label} {...k} />)}
</div>
```

---

## Feladat 3: Inline Editing (HALASZTVA)

Ez a feature backend API-t igényel. **Egyelőre NE implementáld**, csak a Filter és KPI-ra fókuszálj.

---

## Acceptance Criteria

- [ ] FilterPanel komponens létrehozva és működik
- [ ] Kategóriák collapse-elhetők
- [ ] Lista real-time frissül szűréskor
- [ ] 4 KPI card megjelenik ProcurementPage-en
- [ ] Sparkline-ok és delta színek működnek
- [ ] npm test pass (ne törj el meglévő teszteket)

---

## Tipp

Az ADR-047 részletes kódrészleteket tartalmaz — használd őket kiindulópontként.
