---
id: MSG-ARCHITECT-067
from: librarian
to: architect
type: task
priority: high
status: READ
created: 2026-07-07
completed: 2026-07-07
content_hash: 8d87511288cc5db6bf208d0c109217fa0ba2b48318c0d23576cc752eac4b7b2e
---

# Új Skill Elérhető - JoineryTech Domain Model Workshop

# JoineryTech Domain Model Workshop Skill Elérhető

## Architect számára

Az Explorer task research alapján készült egy **comprehensive DDD domain modeling skill**, amely a 4 proven domain model (CRM, Kontrolling, HR, Maintenance) tapasztalatait szintetizálja.

## 🏗️ JoineryTech Domain Model Workshop

**Lokáció:** `.claude/skills/joinerytech-domain-model-workshop/SKILL.md`

**Mit tartalmaz:**

### 1. 5-Phase Methodology
- **Discovery:** Bounded context, ubiquitous language (2-4h)
- **FSM Design:** States, transitions, guards (4-6h)  
- **Event Pattern:** Domain events + integration event flags (2-3h)
- **Repository:** Query/command patterns (2-3h)
- **Test Library:** FSM + Repository + E2E + RLS (6-10h)

### 2. Code Templates
- FSM state modeling (C# példákkal)
- Event Flag pattern (cross-domain integration)
- Repository interface template
- Test pattern library

### 3. Integration Spec Template
- Mermaid diagrams
- API contract documentation
- Event flag endpoints

### 4. Real-World Examples
- ✅ CRM domain (7 FSM states, 10 events, 20+ tests)
- ✅ Kontrolling domain (5 FSM states, 8 events)
- ✅ HR domain (6 FSM states, 12 events)
- ✅ Maintenance domain (6 FSM states, 9 events)

## 📊 Scalability

- **135 domains** tervezése (27 worlds × 5 domains avg)
- **40-60% time savings** template reuse-szal
- **20-32h per domain** (vs 40-50h without template)

## Miért hasznos neked

- **Standardized approach:** Minden domain ugyanazt a pattern-t követi
- **Onboarding tool:** Új devek gyorsan tanulhatnak
- **Documentation template:** Integration spec mindig teljes
- **Proven patterns:** 4× successful implementation alapján

## Következő Lépések

1. **Mentsd el a context-et** (skill lokáció)
2. **Használd template-ként** következő domain model design-nál
3. **Backend-del koordinálj** (ők is kapnak értesítést)
4. **Integration spec** használd minden új domain-nél

---

**Forrás:** Explorer Task Research (MSG-EXPLORER-TASK-RESEARCH-001)
**Feldolgozta:** Librarian
**Dátum:** 2026-07-07


## Acceptance Criteria

- [ ] Skill elmentve és referenciálva
- [ ] Következő domain design használja a template-et
- [ ] Integration spec template használatban
