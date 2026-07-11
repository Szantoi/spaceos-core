---
id: MSG-EXPLORER-014
from: root
to: explorer
type: task
priority: high
status: READ
injected: 2026-07-01
model: sonnet
created: 2026-07-01
content_hash: 74e1512fe24dbae06f8cdd2587f145be0de9b2a2f889d0f827af6dd819077790
---

# Memória és Feladat Fájlok Audit

## Feladat

Végezz átfogó kutatást a memória fájlokban és a feladat (task) fájlokban:

### 1. Memória fájlok áttekintése
```
docs/memory/*.md
```
- Mi a jelenlegi állapot?
- Vannak elavult bejegyzések?
- Mely információk relevánsak még?
- Mely információk archiválandók?

### 2. Task fájlok áttekintése
```
docs/tasks/new/       ← tervdok kész, terminálnak még nem kiadva
docs/tasks/active/    ← inbox elment, terminál dolgozik rajta
docs/tasks/archive/   ← DONE + elfogadott, lezárt
```
- Vannak `new/`-ban elavult vagy kiadott task-ok?
- Vannak `active/`-ban befejezett task-ok (mozgatandók `archive/`-ba)?
- Konzisztens a státusz a tényleges állapottal?

### 3. Outbox áttekintés
```
terminals/*/outbox/*.md
```
- Hány DONE/BLOCKED van feldolgozatlanul?
- Mely outbox-ok archiválandók?

### 4. Inbox archiválás jelöltek
```
terminals/*/inbox/*.md (status: READ)
```
- Mely READ státuszú inbox-ok archiválandók?

## Output

Készíts összefoglaló riportot:
1. **Memória állapot** — terminálonként
2. **Task státusz mátrix** — new/active/archive számok
3. **Archiválási javaslatok** — konkrét fájllisták
4. **Elavult információk** — törlésre vagy frissítésre jelöltek

Az eredményt küld el a Librarian-nak feldolgozásra (outbox DONE).

## Acceptance Criteria

- [ ] Minden memória fájl áttekintve
- [ ] Task fájl státuszok ellenőrizve
- [ ] Archiválási lista elkészítve
- [ ] Összefoglaló riport írva
