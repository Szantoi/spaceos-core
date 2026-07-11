---
id: MSG-ARCHITECT-037
from: conductor
to: architect
type: task
priority: high
status: READ
injected: 2026-07-01
model: opus
epic_id: EPIC-JT-CTRL
created: 2026-07-01
content_hash: 6f1a42751f591cbc76242549e94b3a8530fb757c679933c2ccc271fcb77cc9ed
---

# Kontrolling Domain Model

## Feladat

Tervezd meg a Kontrolling modul domain modelljét a JoineryTech ERP részére.

## Forrás Dokumentáció

- `/opt/spaceos/docs/joinerytech/CLAUDE.md` — Kontrolling világ részletes leírása (Fedezet, EAC)

## Domain Részletek

**Kontrolling Világ:**
- **Fedezet-kalkuláció:** Árbevétel - Költségek (anyag, munka, rezsi)
- **EAC (Estimate at Completion):** Várható végösszeg számítás
- **Projekt-költségkövetés:** Tervezett vs. tényleges
- **Rezsi-elosztás:** Fix költségek allokációja

## Architektúrális Követelmények

1. **Calculation Engine** — Immutable value objects
2. **Aggregate root:** CostCalculation
3. **Integration:** Kontrolling ← Production data (anyag, munkaóra)
4. **Reporting:** Dashboard aggregációk

## Kimeneti Dokumentáció

ADR-stílusú design doc:
- Calculation model
- Cost allocation rules
- Aggregation patterns

## Tech Stack

.NET 8 + PostgreSQL + calculation engine (C# domain logic)



## Acceptance Criteria

- [ ] Calculation engine modell
- [ ] Cost allocation rules
- [ ] Integration contract Kontrolling←Production
- [ ] ADR dokumentáció
