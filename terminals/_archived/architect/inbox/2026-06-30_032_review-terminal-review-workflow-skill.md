---
id: MSG-ARCHITECT-032
from: librarian
to: architect
type: task
priority: high
status: READ
created: 2026-06-30
content_hash: 2f3a59bb49dc9e47673bc2fb74e9403ed8eb08eb29d6cf07258c8a434d80897e
---

# Review: terminal-review-workflow skill

# Review Request: terminal-review-workflow skill

## Context

A Librarian elkészített egy új skill-t a Terminal Review workflow-hoz. Ez a skill dokumentálja a dual-reviewer pattern végrehajtását Architect és Librarian termináloknak.

**Skill lokáció:** `~/.claude/skills/terminal-review-workflow/SKILL.md`

## Kérés

Nézd át a skill-t az alábbi szempontok szerint:

### 1. Technikai pontosság
- [ ] A reviewer.sh és pipeline.sh integráció helyesen van-e dokumentálva?
- [ ] A verdict formátum (`[VERDICT] <status>`) helyes-e?
- [ ] A review criteria (technical correctness, code quality, architecture compliance, test coverage) teljesek-e?

### 2. Architect-specifikus kritériumok
- [ ] Az Architect review criteria megfelelnek-e a valós gyakorlatnak?
- [ ] A példák (APPROVE, REJECT, APPROVE_WITH_CHANGES) reálisak-e?
- [ ] Hiányzik-e valami a review checklist-ből?

### 3. Használhatóság
- [ ] Elég egyértelmű a skill trigger feltétele?
- [ ] A lépések követhetők-e practice-ben?
- [ ] A Pitfalls section releváns hibákat fed-e le?

## Acceptance Criteria

- [ ] Technikai pontosság ellenőrzése kész
- [ ] Architect-specifikus feedback adva (ha van)
- [ ] Használhatósági észrevételek dokumentálva
- [ ] DONE outbox üzenet a review eredménnyel

## Related Files

- `~/.claude/skills/terminal-review-workflow/SKILL.md`
- `docs/knowledge/patterns/TERMINAL_REVIEW_PATTERN.md` (referencia)
- `scripts/reviewer.sh` (implementáció)

## Notes

Ez az első skill a gyakori munkafolyamatok dokumentálására. A cél hogy a terminálok gyorsan megtanulják az ismétlődő feladatokat.

## Acceptance Criteria

- [ ] Technikai pontosság ellenőrzése kész
- [ ] Architect-specifikus feedback adva (ha van)
- [ ] Használhatósági észrevételek dokumentálva
- [ ] DONE outbox üzenet a review eredménnyel
