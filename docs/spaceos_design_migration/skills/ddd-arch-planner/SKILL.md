---
name: ddd-arch-planner
description: >
  Multi-review architecture planning skill for DDD / Clean Architecture .NET projects.
  Produces a versioned, implementation-ready architecture document through a structured
  review pipeline (database → security → backend), culminating in a Claude Code–ready
  implementation plan with Definition of Done, migration gates, and security debt tracking.

  ACTIVATE at the END of an architecture design session when the user asks for:
  "implementációs terv", "Claude Code-nak szánt terv", "implementation plan",
  "generáld le a tervet", "készítsd el a tervdokumentumot", "következő sprint architektúra",
  "architecture phase plan", "next phase architecture", "design + review + DoD",
  or any variant of "make it ready for implementation" / "következő fázis terve".

  Do NOT activate for single-question architecture answers — only for the full
  multi-review pipeline that produces a shippable implementation document.
---

# DDD Architecture Planner

Structured multi-review pipeline that turns an architecture discussion into a
**versioned, implementation-ready document** for Claude Code execution.

---

## 1. When to use this skill

You are in the **right place** when ALL of these are true:
- A new backend feature / phase / module is being designed
- The stack is .NET (Clean Architecture + DDD + CQRS) with PostgreSQL
- The output will be handed to Claude Code (or another agent) for implementation
- The user wants a DoD checklist, migration plan, and security gate list

You are in the **wrong place** if:
- The user wants a quick answer to a single design question → answer directly
- The stack is not .NET / DDD → adapt or use `/senior-architect` instead

---

## 2. The pipeline

```
Phase 0 — Context load
  └─ Read project knowledge, existing architecture docs, security debt register

Phase 1 — v1 Draft
  └─ Domain model · DB schema · API surface · Value Objects
     Output: {document}_v1.md

Phase 2 — Database review
  📂 Load: references/sub-database-designer.md + references/sub-database-schema-designer.md
  └─ Findings → v2 delta table
     Output: {document}_v2.md

Phase 3 — Security review
  📂 Load: references/sub-senior-security.md
  └─ Findings → v3 delta table (CRITICAL / HIGH / MEDIUM)
     Output: {document}_v3.md

Phase 4 — Backend review
  📂 Load: references/sub-senior-backend.md
  [optional — trigger if >= 1 HIGH finding remains after v3]
  └─ Findings → v4 delta table
     Output: {document}_v4.md

Phase 5 — Final document assembly
  └─ Cumulative Finding Summary · DoD checklist · Security debt table · Roadmap · Status banner
```

**Sub-skill betöltési szabály:**
- Minden fázis elején olvasd be a megjelölt referencia fájlt
- A fájl tartalmát alkalmazd a jelenlegi tervdokumentumra mint analitikai lens
- Ne töltsd be az összes sub-skill fájlt egyszerre — csak a fázis-specifikusakat
- Finding ID formátum: `[PREFIX]-NN · 🔴/🟠/🟡 · [terület] · [probléma] · [javítás]`
  - DB review prefix: `DB-NN`
  - Security review prefix: `SEC-NN`
  - Backend review prefix: `BE-NN`

**Rule:** Each phase produces a delta table before rewriting. Never silently absorb findings.

---

## 3. Document structure (every version)

```markdown
# {Project} — {Phase Name}
## {Subtitle}

> Verzió: vN.N — {date}
> Státusz: {DRAFT | REVIEW | IMPLEMENTÁCIÓRA KÉSZ}
> Blokkoló feltétel: {previous phase DoD}
> Kumulált review: {skill list} → vN

---
## 1. Kumulált Finding Összesítő (v1 → vN)
## 2. Domain modell
## 3. DB schema (DDL + ERD mermaid)
## 4. API surface
## 5. EF Core konfiguráció
## 6. Definition of Done
   ### Migration gates
   ### Domain gates
   ### API + validation gates
   ### Security gates
   ### Összesített (existing tests green + new test count target)
## 7. Security adósság státusz
## 8. Mi jön utána (roadmap táblázat)
```

---

## 4. Finding table format

Every review phase appends a new row to the Kumulált Finding Összesítő:

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|------------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | Nc + Nh + Nm | ... | +N nap |
| v2 → `/senior-security` → v3 | Nc + Nh + Nm | ... | +N nap |
| v3 → `/senior-backend` → v4 | Nc + Nh + Nm | ... | +N nap |
| **Összesen** | **totals** | | **N fejlesztői nap** |

Individual findings use this format (append after summary table):

| ID | Súly | Terület | Probléma | vN javítás |
|----|------|---------|----------|------------|

Severity icons: 🔴 CRITICAL · 🟠 HIGH · 🟡 MEDIUM · 🟢 LOW

---

## 5. Definition of Done checklist rules

- Every DoD item is a `- [ ]` checkbox
- Group by: Migration gates · Domain gates · API gates · Security gates · Összesített
- Security gates are **deployment blockers** — mark them explicitly
- Összesített must always include:
  - `- [ ] Meglévő {N} teszt zöld`
  - `- [ ] {Phase} új tesztek: >= {N} db`
  - `- [ ] 0 build warning`
  - `- [ ] ConfigureAwait(false) minden production async call-ban`
  - `- [ ] dotnet list package --vulnerable → 0 high/critical`

---

## 6. Security debt table format

| ID | Tétel | Previous phase | This phase | Marad |
|----|-------|---------------|------------|-------|
| P1-3 | AggregateSnapshot | ❌ | ✅ T-xx | — |
| P1-4 | Outbox Pattern | ❌ | ❌ | Phase N+1 |

---

## 7. Status banner rules

**DRAFT** — v1 only, no reviews completed
**REVIEW** — at least one review pass done, findings pending absorption
**IMPLEMENTÁCIÓRA KÉSZ** — all findings absorbed, DoD complete, no open CRITICAL/HIGH

Final line of every completed document:

```
*{Project} · {Phase} vN.N · {skill list} reviewed · {date}*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — N finding beépítve, minden döntés lezárva*
```

---

## 8. Claude Code handoff section

When the user requests the **Claude Code implementation plan**, append this section:

```markdown
---
## 9. Claude Code implementációs csomag

### Végrehajtási sorrend

| Nap | Feladat | Track | Függőség |
|-----|---------|-------|----------|
| 1   | ...     | A     | —        |

### Agent utasítás

> "Implementáld a {Phase} tervdokumentum szerint a következő feladatokat:
> Track A: {list}
> Track B: {list}
> DoD checklist: {document}#{section}
> Blokkoló gate-ek: {migration list}
> Minden feladat után futtasd: dotnet test && dotnet build"

### Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|-------------|-------|-----------|
```

---

## 9. Golden rules (enforce in every review pass)

1. No public setters on aggregates
2. Business logic in Domain only — never in handlers
3. Every mutation raises a domain event
4. `PopDomainEvents()` + `DispatchAsync()` at end of every mutating handler
5. Every list query through `Ardalis.Specification`
6. `Result<T>` return on every handler
7. `ConfigureAwait(false)` on every production async call
8. `AsNoTracking()` on every read-only method
9. No `BuildServiceProvider()` in DI setup
10. No eager-loaded navigation properties on aggregate roots
11. Security gates are deployment blockers — not afterthoughts
12. `EXPLAIN ANALYZE` on every new query endpoint — no Seq Scan

---

## 10. Adapting for non-SpaceOS projects

If project context is not already loaded, ask for:
- Solution structure (project names, layer names)
- ORM + DB (EF Core version, PostgreSQL / SQL Server)
- Approved package list
- Existing test count
- Frozen architectural decisions
- Open security debt items

Then proceed with the same pipeline — structure and review pattern are universal.

---

## Reference files

- `references/review-checklist.md` — 38-rule review checklist used in each pass
- `references/finding-severity.md` — severity classification guide
