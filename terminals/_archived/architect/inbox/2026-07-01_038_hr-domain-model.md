---
id: MSG-ARCHITECT-038
from: conductor
to: architect
type: task
priority: high
status: READ
injected: 2026-07-01
model: opus
epic_id: EPIC-JT-HR
created: 2026-07-01
content_hash: c1c9b00584dca10569b106c23708b30c568e5a95054839ac658ab94f690b1ed6
---

# HR Domain Model

## Feladat

Tervezd meg a HR modul domain modelljét a JoineryTech ERP részére.

## Forrás Dokumentáció

- `/opt/spaceos/docs/joinerytech/CLAUDE.md` — HR világ részletes leírása (Dolgozó, Kapacitás, Távollét FSM)

## Domain Részletek

**HR Világ:**
- **Dolgozói Törzs:** Skills, shift patterns, daily capacity
- **Távollét-kérelem FSM:** Pending → Approved/Rejected
- **Munkaóra-napló:** Napi munkaidő rögzítés
- **Kapacitás-számítás:** Available capacity aggregáció

## Architektúrális Követelmények

1. **Aggregate roots:** Employee, Absence
2. **FSM:** Absence request státuszgép
3. **Capacity Engine:** Daily availability calculation
4. **Integration:** HR → EHS (munkavédelmi compliance), HR → Production (kapacitás)

## Kimeneti Dokumentáció

ADR-stílusú design doc:
- Aggregate boundaries
- FSM diagram (Absence)
- Capacity calculation logic
- Integration contracts (HR→EHS, HR→Production)

## Tech Stack

.NET 8 + EF Core + PostgreSQL



## Acceptance Criteria

- [ ] Aggregate boundaries (Employee, Absence)
- [ ] FSM diagram (Absence request)
- [ ] Capacity calculation logic
- [ ] Integration contracts HR→EHS, HR→Production
- [ ] ADR dokumentáció
