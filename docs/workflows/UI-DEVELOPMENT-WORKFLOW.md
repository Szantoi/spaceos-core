# UI Development Workflow

> **Hivatalos workflow minden UI fejlesztéshez a JoineryTech portálon**
>
> Létrehozva: 2026-07-13
> Státusz: AKTÍV

---

## Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     UI DEVELOPMENT WORKFLOW                         │
└─────────────────────────────────────────────────────────────────────┘

    ┌──────────────┐      ┌──────────────┐
    │   EXPLORER   │      │   DESIGNER   │
    │              │      │              │
    │ Gap Analysis │      │  Fidelity    │
    │ (JSX vs TSX) │      │   Review     │
    └──────┬───────┘      └──────┬───────┘
           │                     │
           └──────────┬──────────┘
                      │
                      ▼
              ┌───────────────┐
              │   ARCHITECT   │
              │               │
              │ • OpenAPI spec│
              │ • Domain model│
              │ • UI spec     │
              │ • ADR (ha kell)│
              └───────┬───────┘
                      │
           ┌──────────┴──────────┐
           │                     │
           ▼                     ▼
    ┌──────────────┐      ┌──────────────┐
    │   BACKEND    │      │   FRONTEND   │
    │              │      │              │
    │ API impl     │      │ UI impl      │
    │ Domain impl  │      │ Components   │
    └──────┬───────┘      └──────┬───────┘
           │                     │
           └──────────┬──────────┘
                      │
                      ▼
              ┌───────────────┐
              │   DESIGNER    │
              │               │
              │ Final Review  │
              │ QA Sign-off   │
              └───────────────┘
```

---

## Fázisok Részletesen

### 1. FÁZIS: Gap Analysis (Explorer + Designer)

**Cél:** Azonosítani a hiányzó és hiányos UI elemeket

| Terminál | Input | Output |
|----------|-------|--------|
| **Explorer** | JSX design files (110 db) | Gap matrix, effort estimation |
| **Designer** | JSX vs TSX összehasonlítás | Fidelity score, vizuális eltérések |

**Explorer output:**
- `/opt/joinerytech/docs/audit/YYYY-MM-DD_joinerytech-ui-vs-portal-gap-analysis.md`
- Gap matrix: Design → Implementation mapping
- Priority: P1/P2/P3
- Effort: S/M/L/XL

**Designer output:**
- `/opt/joinerytech/docs/audit/YYYY-MM-DD_ui-design-fidelity-review.md`
- Module scores (1-10)
- Design system compliance
- Critical visual issues

---

### 2. FÁZIS: Architect Tervezés

**Cél:** Minden hiányzó modul/feature specifikálása

**⚠️ KÖTELEZŐ: Nincs implementáció Architect tervezés nélkül!**

| Elem típus | Architect output |
|------------|------------------|
| Új modul | OpenAPI spec + Domain model + ADR |
| Új feature | OpenAPI spec + UI wireframe |
| Új API endpoint | OpenAPI spec |
| Új UI komponens | Komponens spec + props definition |
| Domain változás | Domain model update + ADR |

**Architect output lokációk:**
- OpenAPI: `/opt/joinerytech/docs/api/{module}-openapi.yaml`
- Domain: `/opt/joinerytech/docs/domain/{module}-domain-model.md`
- ADR: `/opt/spaceos/docs/architecture/decisions/ADR-XXX-*.md`
- UI spec: `/opt/joinerytech/docs/ui/{component}-spec.md`

---

### 3. FÁZIS: Implementation (Backend + Frontend)

**Cél:** Architect spec alapján implementáció

**Backend:**
- Domain layer (entities, value objects, aggregates)
- Infrastructure layer (DbContext, repositories)
- API layer (controllers, DTOs, validators)
- Tests (unit + integration)

**Frontend:**
- Pages (TSX)
- Components (TSX)
- Hooks (TanStack Query)
- Stores (Zustand)
- Tests

**Acceptance criteria:** Architect spec-nek 100% megfelelés

---

### 4. FÁZIS: Designer Review

**Cél:** Végső vizuális QA

**Checklist:**
- [ ] Design fidelity OK (matches JSX)
- [ ] Design system compliance
- [ ] Responsive OK
- [ ] A11y OK
- [ ] Interactions OK

**Output:** APPROVED vagy CHANGES_REQUESTED

---

## Workflow Triggerek

| Trigger | Akció |
|---------|-------|
| Új feature request | → Explorer gap check |
| Gap analysis DONE | → Architect tervezés |
| Architect spec DONE | → Backend/Frontend dispatch |
| Implementation DONE | → Designer review |
| Designer APPROVED | → DONE, merge ready |
| Designer CHANGES_REQUESTED | → Frontend fix → Designer re-review |

---

## Felelősség Mátrix

| Fázis | Felelős | Konzultál | Informál |
|-------|---------|-----------|----------|
| Gap Analysis | Explorer, Designer | - | Root, Conductor |
| Tervezés | **Architect** | Designer | Root |
| Backend impl | Backend | Architect | Conductor |
| Frontend impl | Frontend | Architect, Designer | Conductor |
| Review | Designer | - | Root, Conductor |

---

## Példa: Új Modul Hozzáadása

**Scenario:** Assembly modul UI hiányzik

```
1. Explorer DONE: "assembly.jsx nincs implementálva, P1, effort: XL"

2. Root → Architect inbox:
   "Tervezd meg az Assembly modul UI-t az assembly.jsx alapján"

3. Architect DONE:
   - docs/api/assembly-openapi.yaml
   - docs/ui/assembly-ui-spec.md
   - ADR-067-assembly-module.md

4. Root → Backend inbox:
   "Implementáld az Assembly API-t (ref: assembly-openapi.yaml)"

5. Root → Frontend inbox:
   "Implementáld az Assembly UI-t (ref: assembly-ui-spec.md)"

6. Backend DONE, Frontend DONE

7. Root → Designer inbox:
   "Review Assembly modul (ref: assembly.jsx vs AssemblyPage.tsx)"

8. Designer DONE: APPROVED

9. Assembly modul COMPLETE ✅
```

---

## Anti-patterns (KERÜLENDŐ)

❌ **NE csináld:**
- Frontend implementáció Architect spec nélkül
- Backend API Architect OpenAPI spec nélkül
- Új komponens Designer konzultáció nélkül
- Skip Designer review
- Implementáció gap analysis nélkül

✅ **MINDIG:**
- Explorer/Designer először → gap-ek azonosítása
- Architect tervezés → spec-ek
- Implementáció → spec alapján
- Designer review → végső QA

---

## Kapcsolódó Dokumentumok

- `/opt/spaceos/config/root-expectations.md` — Root felelősségek
- `/opt/spaceos/docs/WORKFLOW.md` — Általános workflow
- `/opt/joinerytech/docs/joinerytech/*.jsx` — UI design files (110 db)
- `/opt/joinerytech/src/joinerytech-portal/src/` — Portal implementáció

---

_UI Development Workflow — 2026-07-13_
