---
processed: 2026-06-30
id: MSG-LIBRARIAN-013
from: root
to: librarian
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
content_hash: 5309cc27502766293df3f922ccb2df8ff16bceedc2ff4c9301a92066298a1470
---

# Feladat: Gyakori Munkafolyamatok Skill-ekké Alakítása

A rendszerben sok ismétlődő munkafolyamat van, amit skill-ként kell dokumentálni, hogy a terminálok gyorsan használhassák anélkül, hogy mindent újra fel kellene olvasniuk.

## Cél

Nézd át a következő forrásokat és azonosítsd a skill-re érett munkafolyamatokat:

1. **MEMORY.md fájlok** - `terminals/*/MEMORY.md`
2. **Outbox DONE üzenetek** - `terminals/*/outbox/` (utolsó 2 hét)
3. **Knowledge dokumentumok** - `docs/knowledge/patterns/`
4. **Meglévő skill-ek** - `~/.claude/skills/` (ne duplikálj!)

## Skill Prioritások

### P1 - Kritikus (azonnal kell)
- **Terminal Review workflow** - Architect+Librarian DONE értékelés
- **Inbox/Outbox kezelés** - üzenet írás, státusz frissítés, frontmatter
- **Session management** - tmux session indítás, nudge, kill

### P2 - Fontos (ezen a héten)
- **MCP tool használat** - gyakori MCP hívások mintái
- **Git workflow** - commit, PR, branch kezelés a SpaceOS-ben
- **Telegram integráció** - üzenet küldés, bot kezelés

### P3 - Hasznos (ha van idő)
- **Debug workflow** - 2 próbálkozás szabály, web search fallback
- **Planning pipeline** - idea → consensus → queue
- **Knowledge indexing** - dokumentum feldolgozás, ChromaDB

## Skill Formátum

Minden skill-hez:
```
~/.claude/skills/<skill-name>/SKILL.md
```

Tartalmazzon:
1. **Mikor használd?** - trigger feltételek
2. **Lépések** - konkrét parancsok/MCP hívások
3. **Példák** - valós használati esetek
4. **Hibakezelés** - gyakori hibák és megoldások

## Meglévő Skill-ek (NE duplikáld!)

```
~/.claude/skills/
├── parallel-workers/      ← ADR-049 Phase 3 (ÚJ)
├── telegram-bot-registration/
├── handoff/
├── retrospective/
├── skill-factory/
├── skill-management/
└── ...
```

## Együttműködés Explorer-rel

Az Explorer segít a codebase-ből kinyerni a mintákat. Koordináljatok:
- Te (Librarian): Knowledge docs, MEMORY fájlok, outbox elemzés
- Explorer: Kód minták, implementációs részletek

## Acceptance Criteria

- [ ] Minimum 3 új skill létrehozva (P1 prioritás)
- [ ] Minden skill tesztelhető (van példa workflow)
- [ ] Nincs duplikáció meglévő skill-ekkel
- [ ] DONE outbox a létrehozott skill-ek listájával
