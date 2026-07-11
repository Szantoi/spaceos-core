# Frontend — Domain Memory

> **TTL:** 48h (HOT) | **Frissítve:** 2026-06-30

---

## Sprint Fókusz

### Aktív Feladatok

1. **MSG-FRONTEND-063** — KPI Dashboard Implementation
   - JoineryTech-inspired KPI cards
   - Dark theme, mobile-first
   - Státusz: **IN_PROGRESS**

2. **MSG-FRONTEND-061** — CSS Critical Fix
   - Planning page styling
   - Státusz: **DONE**

---

## Designer Koordináció (ÚJ!)

**2026-06-30-tól:** A Designer terminál koordinálja a UI review-t.

### Workflow

1. Frontend implementál UI-t
2. Frontend DONE → Designer review
3. Designer APPROVED → Feature lezárva
4. Designer REVISION → Frontend javít → újra review

### UX Elvek (kötelező)

- **Mobile-First:** Touch target ≥ 44×44px, thumb zone
- **Single-Screen Focus:** Max 7±2 elem, progresszív felfedés
- **Dark-First:** Design tokenek használata
- **Konzisztencia:** Komponens újrafelhasználás

**Referencia:** `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md`

---

## Datahaven UI Státusz

| Oldal | Státusz | Utolsó frissítés |
|-------|---------|------------------|
| Dashboard | Active dev | 2026-06-30 |
| Kanban | Stable | 2026-06-29 |
| Planning | CSS fixed | 2026-06-30 |
| Projects | Stable | 2026-06-29 |

---

## API Integráció

- **Datahaven API:** `https://datahaven.joinerytech.hu/api/`
- **Auth Token:** `dev-token-spaceos-dashboard-2026`
- **Real-time:** SSE streaming (`/api/terminal/status/stream`)
