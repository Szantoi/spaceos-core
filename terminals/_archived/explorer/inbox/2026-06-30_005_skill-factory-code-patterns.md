---
id: MSG-EXPLORER-005
from: root
to: explorer
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
content_hash: bcb337f844fd28758b89dc6b7e812b592f74a490888e5475e14a314b1b27c365
---

# Feladat: Kód Minták Skill-ekké Alakítása

A Librarian-nal együttműködve azonosítsd és dokumentáld a gyakori kód mintákat skill-ként.

## Te feladatod

Kutasd át a codebase-t és keresd meg az ismétlődő mintákat:

### 1. Knowledge Service minták
```
spaceos-nexus/knowledge-service/src/
├── pipeline/           ← watch*, reviewer, nightwatch
├── telegram/           ← bot kezelés, webhook
├── mcp.ts              ← MCP tool implementációk
└── sessionStarter.ts   ← session indítás logika
```

### 2. Script minták
```
scripts/
├── nightwatch.sh       ← cron alapú monitoring
├── cold-shutdown.sh    ← graceful shutdown
└── *.sh                ← egyéb automatizáció
```

### 3. Terminal workflow minták
```
terminals/*/
├── CLAUDE.md           ← terminál identity
├── inbox/              ← üzenet formátum
└── outbox/             ← DONE/BLOCKED formátum
```

## Skill-ek amiket TE készítesz

### P1 - Kód alapú
- **tmux-session-management** - session create/kill/inject/capture
- **mcp-tool-patterns** - MCP tool hívás minták (tools/call JSON-RPC)
- **inbox-outbox-format** - frontmatter, fájlnév konvenció, státuszok

### P2 - Script alapú  
- **cron-automation** - nightwatch típusú periodikus feladatok
- **service-management** - knowledge-service indítás/restart/health check

## Együttműködés Librarian-nal

- **Librarian:** Knowledge docs, MEMORY, elméleti workflow
- **Te (Explorer):** Kód implementáció, konkrét parancsok, script részletek

Oszd meg vele amit találsz - ő integrálja a skill-ekbe.

## Output

Minden skill-hez:
1. `~/.claude/skills/<name>/SKILL.md` - fő dokumentum
2. Konkrét kód példák a codebase-ből
3. Copy-paste ready parancsok

## Acceptance Criteria

- [ ] Minimum 2 skill létrehozva (tmux + mcp patterns)
- [ ] Kód példák a valós codebase-ből
- [ ] Librarian értesítve a közös skill-ekről
- [ ] DONE outbox az eredménnyel
