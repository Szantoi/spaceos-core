---
id: MSG-ARCHITECT-036
from: conductor
to: architect
type: task
priority: high
status: READ
injected: 2026-07-01
model: opus
epic_id: EPIC-JT-CRM
created: 2026-07-01
content_hash: 8961de2a613aebb9c817ab8ed7345473dbd68e7542266f00adcad8d2c4139b87
---

# CRM Domain Model Design

## Feladat

Tervezd meg a CRM modul domain modelljét a JoineryTech ERP részére.

## Forrás Dokumentáció

- `/opt/spaceos/docs/joinerytech/CLAUDE.md` — CRM világ részletes leírása (Lead, Opportunity FSM-ek)
- SpaceOS patterns: `docs/knowledge/patterns/BACKEND_PATTERNS.md`

## Domain Részletek

**CRM Világ:**
- **Lead FSM:** New → Qualified/Disqualified → Opportunity conversion
- **Opportunity FSM:** Draft → Proposal → Negotiation → Won/Lost
- **Forecast:** Pipeline value projection
- **Activity Log:** Kapcsolat-történet

## Architektúrális Követelmények

1. **FSM státuszgép** — State pattern + validation rules
2. **Aggregate roots:** Lead, Opportunity (egyenként)
3. **Value objects:** Contact info, forecast calculation
4. **Domain events:** LeadQualified, OpportunityWon, stb.
5. **Integration:** CRM → Sales (Opportunity → Quote konverzió)

## Kimeneti Dokumentáció

ADR-stílusú design doc:
- Aggregate boundaries
- FSM diagramok
- Event catalog
- Integration contracts

## Tech Stack

.NET 8 + EF Core + PostgreSQL + MediatR (CQRS)



## Acceptance Criteria

- [ ] Aggregate boundaries definiálva
- [ ] FSM diagramok (Lead, Opportunity)
- [ ] Domain events catalog
- [ ] Integration contract CRM→Sales
- [ ] ADR dokumentáció
