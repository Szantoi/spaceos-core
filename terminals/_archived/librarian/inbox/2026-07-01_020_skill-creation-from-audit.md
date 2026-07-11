---
id: MSG-LIBRARIAN-020
from: root
to: librarian
type: task
priority: medium
status: INJECTED
injected: 2026-07-03
model: sonnet
ref: MSG-LIBRARIAN-001-DONE
created: 2026-07-01
content_hash: 7d47c8e40c0661bd1aba7bde4a0f761acf3e887126a546879ad805bfad5430c1
---

# Skill-ek Létrehozása az Audit Eredményekből

## Kontextus

Az Explorer audit (MSG-EXPLORER-014-DONE) és a te 3-fázisú archiválásod (MSG-LIBRARIAN-001-DONE) során több értékes, ismételhető folyamat került azonosításra.

Ezeket skill-ekké kell alakítani a `~/.claude/skills/` mappában.

## Feladatok

### 1. Memory Cleanup Skill (`memory-cleanup`)

**Forrás:** Az archiválási munkafolyamat amit végrehajtottál

**Tartalom:**
- Stale memory template azonosítás (>30 nap, <500 byte)
- Duplicate consolidation workflow (pl. orch.md → orchestrator.md)
- Archive struktura létrehozás
- Reversibility (minden fájl megőrzése archive-ban)

**Lokáció:** `~/.claude/skills/memory-cleanup/SKILL.md`

### 2. Inbox Archival Skill (`inbox-archival`)

**Forrás:** Phase 2 archiválási folyamat

**Tartalom:**
- READ státuszú, 7+ napos inbox-ok azonosítása
- Terminálonkénti archive mappa struktúra
- Batch archival script minta
- Policy javaslatok (30 nap ajánlott a 7 helyett)

**Lokáció:** `~/.claude/skills/inbox-archival/SKILL.md`

### 3. Terminal Audit Skill (`terminal-audit`)

**Forrás:** Explorer audit methodology

**Tartalom:**
- Memory fájl állapot ellenőrzés (size, date, content)
- Task státusz mátrix generálás (new/active/archive)
- Outbox/Inbox metrika gyűjtés
- Archival candidate lista készítés
- 3-fázisú cleanup terv sablon

**Lokáció:** `~/.claude/skills/terminal-audit/SKILL.md`

### 4. Knowledge Pattern Document (opcionális)

Ha a skill-ek mellett pattern dokumentáció is hasznos lenne:
- `docs/knowledge/patterns/MEMORY_CLEANUP_PATTERN.md`
- `docs/knowledge/patterns/ARCHIVAL_PROCEDURES.md`

## Output

1. **3 skill fájl** létrehozva `~/.claude/skills/`-ben
2. **DONE outbox** a skill-ek listájával
3. **Opcionális:** Knowledge pattern docs

## Acceptance Criteria

- [ ] memory-cleanup skill létrehozva és tesztelhető
- [ ] inbox-archival skill létrehozva és tesztelhető
- [ ] terminal-audit skill létrehozva és tesztelhető
- [ ] Skill-ek az archiválási tapasztalatokra épülnek
- [ ] Skill-ek újrahasználhatók jövőbeli audit/cleanup munkákhoz

## Megjegyzések

- A skill-ek a SpaceOS skill formátumot kövessék (lásd `.claude/skills/` meglévő példákat)
- Minden skill-ben legyen "When to use" és "Step-by-step" szekció
- A monitor terminál dokumentáció (`knowledge/patterns/MONITOR_TERMINAL.md`) külön feladatként is jöhet

