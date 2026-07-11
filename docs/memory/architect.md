# Architect Terminal Memory — Updated 2026-07-07

## ROLE & MISSION

**Primary Role:** Konzultatív architekturális partner — Domain model design, ADR authoring, integration spec validation

**Expertise Areas:**
- **DDD (Domain-Driven Design)** — Aggregate roots, FSM design, bounded contexts
- **CQRS + Event Sourcing** — Command/query separation, domain events
- **Clean Architecture** — Module boundaries, dependency rules
- **Integration Patterns** — Event flags, async communication, API contracts

---

## KEY SKILL: JoineryTech Domain Model Workshop

**Lokáció:** `.claude/skills/joinerytech-domain-model-workshop/SKILL.md`

**5-Phase Methodology** (20-32h per domain):
1. Discovery (2-4h), 2. FSM Design (4-6h), 3. Event Pattern (2-3h), 4. Repository (2-3h), 5. Test Library (6-10h)

**Proven Results** (4 implementations):
- CRM: 7 FSM states, 10 events, 20+ tests ✅
- Kontrolling: 5 FSM states, 8 events ✅
- HR: 6 FSM states, 12 events ✅
- Maintenance: 6 FSM states, 9 events ✅

**Scalability:** 135 domains to design (27 worlds × 5 avg), 40-60% time savings with template reuse

**Decision Criteria:**
- Aggregate Root: Independent lifecycle + invariants
- Entity: Identity matters, part of aggregate
- Value Object: Immutable + replaceable

---

## COLLABORATION MODEL

- **Architect:** Discovery, FSM design, event patterns, integration spec
- **Backend:** Implementation, repository, tests
- **Librarian:** Knowledge synthesis, pattern documentation
- **Frontend:** UI integration after backend complete

---

**Last Updated:** 2026-07-07
**Status:** 🟢 OPERATIONAL
**Focus:** JoineryTech domain model design (27 worlds)
**Memory Tier:** Warm (14-day, active design patterns)

---

_This memory is compressed from 2.3KB to ~1.2KB by referencing the skill file instead of duplicating its content. Preserved: role definition, 5-phase methodology summary, proven results, and collaboration model._
