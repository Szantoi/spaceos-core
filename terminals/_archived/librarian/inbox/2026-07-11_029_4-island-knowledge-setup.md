---
id: MSG-LIBRARIAN-029
from: root
to: librarian
type: task
priority: high
status: READ
model: sonnet
created: 2026-07-11
content_hash: 7cb1d3feba3f7aec34f07de663c8497a291a20ed6ed13a781dca63ea0e67bd25
---

# 4-Sziget Tudástár Kialakítás

## Kontextus

A SpaceOS 4 független szigetre lett bontva. Minden szigetnek saját tudástárra van szüksége.

## Feladat

Kategorizáld a meglévő `/opt/spaceos/docs/knowledge/` tartalmát és oszd szét a szigetek között:

### 1. Nexus (`/opt/nexus/docs/knowledge/`)
- MCP protokoll dokumentáció
- Knowledge-service architektúra
- Pipeline szkriptek dokumentáció
- TypeScript patterns

**Forrás:** `docs/knowledge/patterns/MCP_*`, pipeline docs

### 2. JoineryTech (`/opt/joinerytech/docs/knowledge/`)
- 7 modul domain modellek (CRM, HR, EHS, stb.)
- .NET 8 patterns
- React 18 patterns
- Faipar domain tudás

**Forrás:** `docs/joinerytech/`, ADR-054 → ADR-058

### 3. Doorstar (`/opt/doorstar/docs/knowledge/`)
- 6-STAGE production workflow
- Cabinet-VPS kommunikáció
- Ajtógyártás domain

**Forrás:** EPIC-DOORSTAR-SOFTLAUNCH docs, Cabinet governance

### 4. SpaceOS (`/opt/spaceos/docs/knowledge/`)
- 4-sziget architektúra
- Federation protokoll
- Orchestration patterns

## Várt Eredmény

Minden szigeten:
```
docs/knowledge/
├── INDEX.md
├── architecture/
├── patterns/
├── domain/
└── deployment/
```

## Referencia Dokumentumok

- `/opt/spaceos/docs/architecture/4-ISLAND-ARCHITECTURE.md`
- `/opt/spaceos/docs/tasks/active/4-ISLAND-KNOWLEDGE-SETUP.md`

## Határidő

2026-07-12 (1 nap)
