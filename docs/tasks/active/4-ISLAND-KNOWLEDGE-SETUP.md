# 4-Sziget Tudástár Kialakítás

**Státusz:** ACTIVE
**Felelős:** Librarian + Explorer
**Prioritás:** HIGH
**Létrehozva:** 2026-07-11

---

## Cél

Minden szigetnek saját, dedikált tudástárat kell kialakítani a megfelelő tartalommal.

---

## Sziget Tudástárak

### 1. Nexus (`/opt/nexus/docs/knowledge/`)

**Tartalom:**
- MCP protokoll dokumentáció
- Knowledge-service architektúra
- Pipeline szkriptek dokumentáció
- TypeScript patterns
- Testing patterns (Vitest)

**Forrás:** `/opt/spaceos/docs/knowledge/patterns/MCP_*`, pipeline docs

---

### 2. JoineryTech (`/opt/joinerytech/docs/knowledge/`)

**Tartalom:**
- 7 modul domain modellek (CRM, HR, EHS, stb.)
- .NET 8 patterns
- React 18 patterns
- OpenAPI/Orval workflow
- Faipar domain tudás

**Forrás:**
- `/opt/spaceos/docs/joinerytech/`
- `/opt/spaceos/docs/knowledge/patterns/` (releváns)
- ADR-054 → ADR-058 (JoineryTech domain modellek)

---

### 3. Doorstar (`/opt/doorstar/docs/knowledge/`)

**Tartalom:**
- 6-STAGE production workflow
- Cabinet-VPS kommunikáció
- Ajtógyártás domain
- FSM patterns (ProductionStage)

**Forrás:**
- EPIC-DOORSTAR-SOFTLAUNCH dokumentáció
- Cabinet governance csomagok
- Federation protokoll

---

### 4. SpaceOS (`/opt/spaceos/docs/knowledge/`)

**Tartalom:** (marad a jelenlegi, de cleanup)
- 4-sziget architektúra
- Federation protokoll
- Stratégiai dokumentumok
- Orchestration patterns

---

## Feladatok

### Librarian
1. Kategorizálja a meglévő `/opt/spaceos/docs/knowledge/` tartalmát
2. Döntse el melyik szigethez tartozik
3. Másolja a releváns dokumentumokat
4. Hozzon létre INDEX.md-t minden szigethez

### Explorer
1. Keresse meg a codebase-ben a domain-specifikus dokumentumokat
2. Azonosítsa a hiányzó dokumentációkat
3. Segítsen a kategorizálásban

---

## Várt Eredmény

Minden szigetnek legyen:
```
docs/knowledge/
├── INDEX.md              ← Tartalomjegyzék
├── architecture/         ← Architektúra döntések
├── patterns/             ← Kódolási minták
├── domain/               ← Domain-specifikus tudás
└── deployment/           ← Telepítési útmutatók
```

---

## Határidő

2026-07-12 (1 nap)

---

_Task: 4-ISLAND-KNOWLEDGE-SETUP_
