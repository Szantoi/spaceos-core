---
id: MSG-ARCHITECT-004-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-004
created: 2026-06-22
completed: 2026-06-22
content_hash: 83fcca921d1442c86397c61e62398f2d3e26a778b90e197fb014821f8f62e702
---

# MSG-ARCHITECT-004 — DONE

## Summary

JoineryTech Portal UI Enhancements tervezés elkészült.

## Eredmények

### 1. ADR-047 dokumentum
`/opt/spaceos/docs/adr/ADR-047_portal-ui-enhancements.md` - Részletes architekturális döntési dokumentum 3 feature-ről.

### 2. Implementációs sorrend (prioritás)
1. **Assembly Filter Panel** (MAGAS) - ProductionPage-re, lokális React state, reusable FilterPanel komponens
2. **KPI Cards** (KÖZEPES) - ProcurementPage-re, meglévő KpiCard komponens használata, mock data MVP-hez
3. **Inline Editing** (ALACSONYABB) - MasterdataPage-re, permission check szükséges, backend API igény

### 3. Komponens specifikációk
- **FilterPanel**: `src/components/filters/FilterPanel.tsx` - collapse-able kategóriák, checkbox gyorsválasztás
- **KPI Grid**: Meglévő KpiCard (spark, delta, breakdowns) - 4 beszállítói metrika
- **EditableRow**: `src/components/table/EditableRow.tsx` - row-level edit mode, Mentés/Mégse

### 4. Döntések
- Lokális useState > zustand (egyszerűség)
- Mock data MVP-hez > API (gyors implementáció)
- Row edit button > double-click (explicit UX)

### Acceptance Criteria teljesítve
- [x] ADR dokumentum elkészült (ADR-047)
- [x] 3 feature spec részletesen leírva
- [x] Implementációs sorrend meghatározva
- [x] Frontend-nek kiadható spec formátumban

## Files Changed

- docs/adr/ADR-047_portal-ui-enhancements.md

---

**Timestamp:** 2026-06-22T05:28:37.791Z
