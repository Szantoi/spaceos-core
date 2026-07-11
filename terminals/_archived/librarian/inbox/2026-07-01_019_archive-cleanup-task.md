---
id: MSG-LIBRARIAN-019
from: root
to: librarian
type: task
priority: high
status: READ
model: sonnet
ref: MSG-EXPLORER-014
created: 2026-07-01
content_hash: 4d2e21b47292e21eabec08c2ed2578d73e6c20436999438597a7cfb497f263e2
---

# Dokumentáció és Archiválás Feladat

## Kontextus

Az Explorer terminál (MSG-EXPLORER-014) átfogó audit-ot végez a memória és task fájlokon.
A te feladatod az audit eredményeinek feldolgozása és az archiválás végrehajtása.

## Feladatok

### 1. Várakozz az Explorer riportra
- Figyeld az Explorer outbox-át
- Amint megérkezik a DONE üzenet, dolgozd fel az eredményeket

### 2. Archiválás végrehajtása

**Inbox archiválás:**
```bash
# READ státuszú, 7+ napos inbox-ok mozgatása archive-ba
terminals/<terminal>/inbox/*.md → terminals/<terminal>/archive/
```

**Outbox archiválás:**
```bash
# Feldolgozott outbox-ok mozgatása
terminals/<terminal>/outbox/*.md → terminals/<terminal>/archive/
```

**Task fájl archiválás:**
```bash
# Befejezett task-ok mozgatása
docs/tasks/active/*.md → docs/tasks/archive/
```

### 3. Memória szinkronizálás

Frissítsd a memória fájlokat:
- Távolítsd el az elavult bejegyzéseket
- Add hozzá az új, releváns információkat
- Tartsd meg a struktúrát

### 4. Knowledge Base frissítés

Ha az audit új tudáselemeket talál:
- Szintetizáld a docs/knowledge/ mappába
- Frissítsd az INDEX.md-t

## Output

1. **Archiválási log** — mely fájlok lettek mozgatva
2. **Memória változások** — mi változott
3. **Knowledge frissítések** — új/módosított dokumentumok

## Acceptance Criteria

- [ ] Explorer riport feldolgozva
- [ ] Inbox/outbox archiválás végrehajtva
- [ ] Task fájlok rendezve
- [ ] Memória fájlok frissítve
- [ ] Knowledge base naprakész
