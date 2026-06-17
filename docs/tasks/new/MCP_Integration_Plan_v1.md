# SpaceOS MCP Role & Knowledge Integration Plan

> **Verzió:** v2 · **Dátum:** 2026-06-16
> **Státusz:** REVIEW — v2 infra review kész (nincs DB impakt)
> **Forrás:** MSG-ARCH-008 (Root kérés)
> **Megvalósító:** ROOT (CLAUDE.md szerkesztés) + LIBRARIAN (knowledge import)

---

## 1. Role → CLAUDE.md Mapping

### 1.1 Orchestrator → Root CLAUDE.md

**Forrás:** `orchestrator.role.md`

A Root CLAUDE.md-ből hiányzó elemek, amiket a referencia role hozzáad:

#### Context hygiene szabályok (ÚJ szekció)

```markdown
## Context hygiene

- Ha a session context 60%+ → kötelező kontextus vágás (összefoglalás + irreleváns részek ejtése)
- Root kizárólag dokumentált forrásból dolgozik — ha hiányzik az info, NE találgass, hanem delegálj (Architect, Tester, vagy a releváns terminál)
- State tracking checklist minden session végén:
  - [ ] `docs/tasks/README.md` naprakész
  - [ ] `Codebase_Status.md` tükrözi a változásokat
  - [ ] Dependency konfliktus nincs aktív feladatok között
```

**Értékelés:** KÖZEPES érték — a Root CLAUDE.md már tartalmaz session ritual-t, de a context hygiene explicit szabályozása hiányzik. A 60% threshold hasznos guideline.

#### Kommunikációs minták (enrichment)

```markdown
## Kommunikációs minták

- **Persona Pattern:** Minden inbox üzenetben explicit terminál megnevezés ("Te vagy a Kernel backend fejlesztő...")
- **Context Slicing:** Inbox üzenet csak releváns kontextust tartalmaz — nem az egész Codebase_Status
- **Audience Pattern:** Sonnet terminálnak technikai spec, Haiku terminálnak tömör utasítás
```

**Értékelés:** MAGAS érték — a `model:` mező már van, de a kommunikációs minták explicit rögzítése javítja a terminál hatékonyságot.

---

### 1.2 Architect → Architect CLAUDE.md

**Forrás:** `architect.role.md`

#### Döntési keretrendszer (ÚJ szekció)

```markdown
## Döntési keretrendszer

- Minden architekturális kérdésnél: **minimum 3 alternatíva** vizsgálata
- **Chain of Thought** pattern: lépésről lépésre logikus levezetés, nem intuitív döntés
- **Fact Summary** pattern: ADR és outbox üzenetben tömör, konkluzív megfogalmazás
- Trade-off explicit rögzítése: "Amit nyerünk: X. Amit veszítünk: Y."

## Quality checklist (minden output előtt)

- [ ] Megoldás illeszkedik a projekt céljaira (vision + 5 Golden Rule)
- [ ] Nem sért zárolt ADR döntést (DESIGN_MEMORY.md)
- [ ] ADR dokumentáció szükségessége mérlegelve
- [ ] Security és performance impakt értékelve
```

**Értékelés:** MAGAS érték — az Architect CLAUDE.md jelenleg csak a session ritual-t és a kommunikációt tartalmazza, a döntési keretrendszer hiányzik.

---

### 1.3 Backend Developer → Kernel/Joinery/Cutting/stb. CLAUDE.md-k

**Forrás:** `backend_developer.role.md`

#### Implementációs checklist (ÚJ szekció — minden backend terminálhoz)

```markdown
## Backend implementációs checklist

Minden feature/bugfix végén, DONE outbox előtt:

- [ ] Entity creation factory method-dal (nem publikus constructor)
- [ ] Setter-ek private-ok
- [ ] Domain validation implementálva (nem controller-ben)
- [ ] Controller/endpoint csak DTO-t ad vissza (entity soha)
- [ ] Unit test üzleti logikára
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` minden zöld
```

#### QA Handoff kritérium (ÚJ — jelenleg nincs dokumentálva!)

```markdown
## QA Handoff

A TESTER terminált ROOT hívja be ha a feladat:
- Üzleti validációs logikát tartalmaz (pl. rendelés állapotgép, ár kalkuláció)
- Pénzügyi számítást végez
- Workflow / FSM state machine-t érint
- A task explicit jelzi: "QA needed: Yes"

Egyszerű CRUD endpoint-ok NEM igényelnek QA-t, kivéve ha explicit kérve van.
```

**Értékelés:** MAGAS érték — a QA Handoff kritérium teljesen hiányzik a SpaceOS workflow-ból. Ez a legnagyobb gap.

---

### 1.4 Frontend Developer → FE/FE2/Portal CLAUDE.md

**Forrás:** `frontend_developer.role.md`

#### FE implementációs checklist (ÚJ szekció)

```markdown
## Frontend implementációs checklist

- [ ] Funkcionális komponens (nem class-based)
- [ ] Props interface teljesen tipizált (nincs `any`)
- [ ] Loading és error state kezelve
- [ ] Styling Tailwind class-okkal (nem inline style)
- [ ] Mock fallback ha API nem elérhető
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` minden zöld
```

#### QA Handoff (FE-specifikus)

```markdown
## FE QA Handoff

TESTER bevonás ha:
- Form validáció (komplex, multi-step)
- Interaktív workflow (drag-drop, multi-select + submit)
- Cross-page navigáció állapotmegőrzéssel

Egyszerű megjelenítő komponensek NEM igényelnek QA-t.
```

**Értékelés:** KÖZEPES érték — a FE CLAUDE.md-k már tartalmaznak build/test gate-et, de a tipizálás és QA handoff szabályok hiányoznak.

---

### 1.5 QA Tester → Tester CLAUDE.md

**Forrás:** `qa_tester.role.md`

#### Tesztelési keretrendszer (ÚJ szekció)

```markdown
## Tesztelési alapelvek

- **Zero Trust:** Semmit nem feltételezünk — minden állítás verifikálandó
- **Destructive Testing:** "Hogyan tudom eltörni?" — boundary conditions, null/empty input, max értékek
- **User-Centric:** Interface viselkedést tesztelünk, nem belső implementációt

## Verifikációs checklist

- [ ] Happy path működik
- [ ] Hibaüzenetek érthetőek (nem technikai error)
- [ ] Edge case-ek tesztelve (null, üres, határérték)
- [ ] Regresszió: meglévő funkciók nem törtek el
- [ ] Performance elfogadható (nem lassabb mint előtte)

## Bug report formátum

- **Reprodukciós lépések:** 1-2-3 számozott lista
- **Elvárt viselkedés:** mit kellene csinálnia
- **Tényleges viselkedés:** mit csinál helyette
- **Screenshot/log:** ha releváns
```

**Értékelés:** MAGAS érték — a Tester CLAUDE.md jelenleg minimális, a strukturált tesztelési keretrendszer és bug report formátum nagy javulás.

---

## 2. Engineering Knowledge Import

### Import döntési mátrix

| Fájl | Döntés | Indoklás |
|---|---|---|
| `backend_dotnet.knowledge.md` | **ADAPTÁLÁS** | SpaceOS .NET 8 (nem 10), PostgreSQL (nem SQLite), EF Core 8. A Clean Architecture elvek azonosak, de a specifikus path-ok és package verziók eltérnek. |
| `database_efcore.knowledge.md` | **KIHAGYÁS** | A `docs/knowledge/patterns/DATABASE_PATTERNS.md` már tartalmazza SpaceOS-specifikusan (RLS, DbConnectionInterceptor, Testcontainers). Duplikáció lenne. |
| `efcore_installation.knowledge.md` | **KIHAGYÁS** | A `docs/knowledge/deployment/KNOWN_GOTCHAS.md` már tartalmazza az EF Core gotcha-kat. |
| `frontend_react.knowledge.md` | **ADAPTÁLÁS** | SpaceOS React 19 (nem 18), Tailwind 4 (nem vanilla CSS), Vite 5+. A komponens pattern-ek relevánsak, de styling és API hookok eltérnek. |
| `testing_backend_dotnet.knowledge.md` | **ÁTVÉTEL** | SpaceOS is xUnit + FluentAssertions. A tesztelési minták (AAA, naming, Theory) közvetlenül alkalmazhatók. Testcontainers kiegészítés kell. |
| `testing_frontend_react.knowledge.md` | **ÁTVÉTEL** | SpaceOS is Vitest + React Testing Library. A behavior-driven tesztelési elvek és selector preferenciák azonosak. |
| `testing_strategy.knowledge.md` | **ÁTVÉTEL** | Testing pyramid, coverage checklist, QA handoff szabályok — ezek hiányoznak a SpaceOS docs/knowledge/-ből. |

### Import célmappa

```
docs/knowledge/engineering/       ← ÚJ könyvtár
  BACKEND_PATTERNS.md             ← backend_dotnet adaptálva (SpaceOS .NET 8, PostgreSQL)
  FRONTEND_PATTERNS.md            ← frontend_react adaptálva (React 19, Tailwind 4)
  TESTING_BACKEND.md              ← testing_backend_dotnet átvéve + Testcontainers kiegészítés
  TESTING_FRONTEND.md             ← testing_frontend_react átvéve
  TESTING_STRATEGY.md             ← testing_strategy átvéve + QA Handoff szabályok
```

### Adaptálási delta (backend)

| Referencia | SpaceOS |
|---|---|
| .NET 10 | .NET 8 |
| SQLite in-memory teszt | Testcontainers PostgreSQL |
| `Project.Create()` | `Entity.Create()` + domain event |
| NSubstitute | Moq |
| `JoineryTech.Flow` namespace | `SpaceOS.Modules.*` namespace |

### Adaptálási delta (frontend)

| Referencia | SpaceOS |
|---|---|
| React 18 | React 19 |
| Vanilla CSS | Tailwind CSS 4 |
| TanStack Query | `useApi` hook (custom) |
| Feature-based structure | Page-based structure (27 világ) |
| OpenAPI client generation | Manual API types |

---

## 3. Implementációs sorrend

| Sorrend | Feladat | Ki végzi | Becsült idő |
|---|---|---|---|
| 1 | CLAUDE.md kiegészítések beépítése | ROOT | 1 session |
| 2 | `docs/knowledge/engineering/` mappa + 5 fájl | LIBRARIAN | 1 session |
| 3 | `docs/knowledge/INDEX.md` frissítése | LIBRARIAN | ^^ ugyanaz |
| 4 | Terminál CLAUDE.md-k tesztelése (Haiku dry run) | ROOT | 0.5 session |

**Teljes:** ~2-3 session, nincs blocker.

---

## 4. v2 Infra Review

Nincs DB impakt — ez dokumentáció-szintű változás (CLAUDE.md + knowledge md fájlok). Nincs migration, nincs schema módosítás, nincs deployment szükséglet.

| ID | Súly | Terület | Probléma | Státusz |
|---|---|---|---|---|
| INF-P1 | LOW | Fájlméret | 5 új knowledge fájl → docs/knowledge/ méret ~+50 KB | ACCEPTED — triviális |
| INF-P2 | LOW | Kompatibilitás | CLAUDE.md kiegészítések backward compatible? | ACCEPTED — additive only, nem törli a meglévőt |

---

## 5. Ami NEM része ennek a tervnek

- **Role fájlok közvetlen másolása** — adaptálás kell, nem 1:1 copy
- **MCP Server tool registration** — az MSG-ARCH-005/007 RAG spec hatásköre
- **CLAUDE.md teljes átírása** — csak kiegészítő blokkok, a meglévő struktúra megmarad
- **Engineering knowledge automatikus frissítése** — a LIBRARIAN manuálisan kezeli
