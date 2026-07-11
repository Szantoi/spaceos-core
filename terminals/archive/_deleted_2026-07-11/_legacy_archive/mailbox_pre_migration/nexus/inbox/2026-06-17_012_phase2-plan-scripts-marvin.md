---
id: MSG-NEXUS-012
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-17
---

# NEXUS-012 — Phase 2: Plan Scripts → Marvin Tasks

## Feladat

A bash planning scriptek átírása Marvin Task-okra:

### 1. plan-scan.sh → Marvin Task
```python
@marvin.task
def scan_for_ideas(segment: str) -> list[PlanningIdea]:
    """Scan codebase segment for improvement ideas"""
```
- OPENAI_API_KEY szükséges
- Knowledge Service context használata

### 2. plan-select.sh → Marvin Task
```python
@marvin.task
def select_best_ideas(ideas: list[PlanningIdea]) -> list[PlanningIdea]:
    """Select top 5 ideas with WebSearch validation"""
```
- WebSearch tool integration

### 3. plan-debate.sh → Marvin Tasks (párhuzamos)
```python
@marvin.task
def debate_idea(idea: PlanningIdea, role: str) -> DebateArgument:
    """A/B debate on idea viability"""
```
- 2x párhuzamos futás (Pro/Con)
- Konsenzus generálás

## Definition of Done

- [ ] 3 Marvin Task implementálva
- [ ] Tesztelve OPENAI_API_KEY-vel
- [ ] ROADMAP.md checkbox-ok frissítve
