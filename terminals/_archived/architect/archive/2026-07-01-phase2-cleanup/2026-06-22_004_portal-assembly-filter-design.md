---
id: MSG-ARCHITECT-004
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-22
content_hash: e7abe6ee2d48017b3cfb3c6622dd327e55b273b60452e96acda74fbaa91a5dc5
---

# JoineryTech Portal: Assembly Filter + KPI Cards tervezés

## Kontextus

A JoineryTech Portal prototípusból 6 planning idea érkezett. Ezeket meg kell tervezni az éles portálhoz.
A portál: `/opt/spaceos/frontend/joinerytech-portal/` (React + TypeScript + Tailwind)

## Feladat: 3 feature spec kidolgozása

### 1. Assembly Filter Panel (MAGAS prioritás)

**Eredeti idea (prototípus):**
- `assembly.jsx` → FilterPanel subcomponent
- Kolapszálható kategória szűrők (Keretrendszer, Felület, Vasalat, Egyéb)
- Checkboxes: összes/egyik sem
- Real-time lista frissítés

**Tervezési kérdések:**
1. Melyik oldalon implementáljuk? (ProductionPage? DesignPage? Új oldal?)
2. Milyen kategóriák legyenek? (backend API-ból vagy hardcoded?)
3. State management: zustand store vagy lokális state?
4. Reusable komponens legyen vagy page-specific?

### 2. KPI Cards (KÖZEPES prioritás)

**Eredeti idea:**
- Beszállítói megbízhatóság mutatók
- On-time %, Ár-stabilitás, Minőségi értékelés, Aktív szállítók
- 5 percenként frissül
- Zöld/sárga/piros indikátorok

**Tervezési kérdések:**
1. Melyik oldalra kerüljön? (DashboardPage? SalesPage? ProcurementPage?)
2. Backend API kell hozzá? Vagy mock data elegendő MVP-hez?
3. Komponens struktúra: KPICard + KPIGrid

### 3. Inline Editing (ALACSONYABB prioritás)

**Eredeti idea:**
- Katalógus táblázat sorok inline szerkesztése
- Ár + kedvezmény módosítás
- Mentés/Mégse gombok
- `catalog.edit` permission

**Tervezési kérdések:**
1. Hol van katalógus táblázat? (MasterdataPage? Új CatalogPage?)
2. Backend API szükséges (PUT endpoint)?
3. Permission check hogyan működjön?

---

## Elvárt output

1. **ADR dokumentum**: `/opt/spaceos/docs/adr/ADR-XXX-portal-ui-enhancements.md`
2. **Komponens specifikáció**: melyik fájlok, milyen props, state struktúra
3. **Prioritási sorrend**: melyiket implementáljuk először

## Referenciák

- Planning ideas: `/opt/spaceos/docs/planning/ideas/2026-06-21_00*.md`
- Portal kód: `/opt/spaceos/frontend/joinerytech-portal/src/`
- Meglévő KPI példák: `grep -r "KPI\|kpi" src/pages/` → már van 41 fájlban

---

## Acceptance Criteria

- [ ] ADR dokumentum elkészült
- [ ] 3 feature spec részletesen leírva
- [ ] Implementációs sorrend meghatározva
- [ ] Frontend-nek kiadható spec formátumban
