---
id: IDEA-NEXUS-001
title: "Librarian és Explorer terminálok működése: Claude Code chat history feldolgozás"
priority: medium
domain: nexus
created: 2026-06-22
author: root
tags: [librarian, explorer, chat-history, knowledge-mining]
---

# Librarian és Explorer terminálok működése

## Probléma

A Claude Code lokálisan tárolja az összes beszélgetést (`~/.claude/projects/<projekt>/*.jsonl`).
Ez **hatalmas tudásanyag** (329 MB a SpaceOS projektnél, 272 conversation fájl), amit jelenleg
senki nem dolgoz fel szisztematikusan.

### Jelenlegi állapot

| Forrás | Méret | Formátum |
|--------|-------|----------|
| Claude Code chat history | 329 MB | JSON Lines (.jsonl) |
| Tiered Memory DB | 44 KB | SQLite |
| MEMORY.md fájlok | 2 db (conductor, root) | Markdown |
| Outbox/Inbox üzenetek | ~100+ | Markdown |

## Ötlet

### 1. Explorer terminál

**Feladat:** Codebase és chat history kutatás, onboarding segítség

**Működés:**
- `.jsonl` fájlok olvasása és indexelése
- Keresés korábbi megoldásokra: "Hogyan csináltuk korábban a...?"
- Onboarding új termináloknak: kontextus építés korábbi session-ökből
- Minta: Marveen `conversation-continuity.md` — session-ök közötti folytonosság

### 2. Librarian terminál

**Feladat:** Tudásbázis kurátori gondozása

**Működés:**
- Tiered memory → `docs/knowledge/` szintetizálás
- Hot → Warm → Cold promóció (Marveen: salience decay, 0.5%/nap)
- Chat history → Knowledge doc extrakció
- Minta: Marveen `memory-system.md` — 3-tier memory + hibrid keresés (FTS5 + vektor)

## Marveen inspiráció összefoglaló

### Memory-system.md

| Feature | Leírás |
|---------|--------|
| **3-tier rendszer** | hot (most), warm (stabil), cold (archívum), shared (cross-agent) |
| **Salience decay** | 7 nap után 0.5%/nap csökkenés, minimum 0.01 |
| **Hibrid keresés** | FTS5 (kulcsszó) + Vektor (768-dim embedding) + RRF fúzió |
| **Napi napló** | Append-only, ágensenként, este összefoglaló |
| **PreCompact hook** | Kontextus tömörítés előtt automatikus mentés |
| **Gráf nézet** | Force-directed Canvas, kulcsszó-kapcsolatok ágensek közt |

### Skill-factory.md

| Feature | Leírás |
|---------|--------|
| **Auto-generálás** | 5+ tool hívás után skill létrehozás |
| **Buktatók szekció** | Hibákból tanulás, nem csak sikerekből |
| **Patch vs újraírás** | Célzott módosítás, nem teljes felülírás |
| **Progresszív betöltés** | L0: név+leírás, L1: teljes SKILL.md, L2: references/ |

## Javaslat implementációra

### Explorer működés

```
1. Session indítás
2. Fogadj keresési kérdést (pl. "Hogyan kezeljük a tenant isolation-t?")
3. Keresés:
   - tiered memory DB (SQLite)
   - chat history .jsonl fájlok (grep-szerű keresés)
   - docs/knowledge/ markdown fájlok
4. Válasz összeállítás: releváns kontextus + fájl hivatkozások
```

### Librarian működés

```
1. Session indítás (napi/heti ütemezéssel vagy manuálisan)
2. Források áttekintése:
   - terminals/*/outbox/ — DONE üzenetek
   - tiered memory DB — hot/warm memóriák
   - logs/sessions/*.jsonl — MCP audit
3. Szintetizálás:
   - Ismétlődő minták → docs/knowledge/patterns/
   - Döntések → docs/knowledge/architecture/ADR_CATALOGUE.md
   - Hibák/gotchák → docs/knowledge/deployment/KNOWN_GOTCHAS.md
4. Memory promóció:
   - hot → warm (48h után, ha még releváns)
   - warm → cold (14 nap után)
5. PROCESSED_LOG.md frissítése
```

## Kapcsolódó fájlok

- `/opt/marveen/docs/memory-system.md` — Marveen memória rendszer
- `/opt/marveen/docs/skill-factory.md` — Marveen skill factory
- `/opt/marveen/docs/conversation-continuity.md` — Session folytonosság
- `~/.claude/projects/-opt-spaceos/*.jsonl` — SpaceOS chat history
- `/opt/spaceos/spaceos-nexus/knowledge-service/data/memory.db` — Tiered memory DB

## SpaceOS vs Marveen összehasonlítás

| Feature | Marveen | SpaceOS | Státusz |
|---------|---------|---------|---------|
| **3-tier memória** (hot/warm/cold/shared) | ✅ | ✅ | **MEGVAN** |
| **FTS5 full-text keresés** | ✅ | ✅ | **MEGVAN** |
| **Salience mező** | ✅ | ✅ | **MEGVAN** (de nincs decay) |
| **Access count tracking** | ✅ | ✅ | **MEGVAN** |
| **Retrospective proposals** | ❌ | ✅ | **SpaceOS előny!** |
| **Session history** | ❌ | ✅ | **SpaceOS előny!** |
| **Salience decay** (0.5%/nap) | ✅ | ❌ | Hiányzik |
| **Embedding + vektor keresés** | ✅ | ❌ | Hiányzik |
| **RRF fúzió** | ✅ | ❌ | Hiányzik |
| **Napi napló** (append-only) | ✅ | ❌ | Hiányzik |
| **PreCompact hook** | ✅ | ❌ | Hiányzik |
| **Gráf vizualizáció** | ✅ | ❌ | Hiányzik |
| **Chat history keresés** | ✅ | ❌ | Hiányzik |

## Prioritási lista — Mit érdemes átvenni

### 🔴 Magas prioritás (gyors ROI)

| # | Feature | Miért | Nehézség | Becslés |
|---|---------|-------|----------|---------|
| 1 | **Salience decay** | Automatikus memória "felejtés", nem kell kézi cleanup | Könnyű | 2-4 óra |
| 2 | **Chat history keresés** | 329 MB tudás feltárása, Explorer alapja | Közepes | 1-2 nap |
| 3 | **Napi napló** | Librarian input, session összefoglaló | Könnyű | 4-6 óra |

### 🟡 Közepes prioritás

| # | Feature | Miért | Nehézség | Becslés |
|---|---------|-------|----------|---------|
| 4 | **Embedding + vektor keresés** | Jelentés-alapú keresés, nem csak kulcsszó | Nehéz | 2-3 nap |
| 5 | **PreCompact hook** | Kontextus tömörítés előtt mentés | Közepes | 1 nap |

### 🟢 Alacsony prioritás (szép, de nem sürgős)

| # | Feature | Miért | Nehézség |
|---|---------|-------|----------|
| 6 | **Gráf vizualizáció** | Datahaven dashboard bővítés | Közepes |
| 7 | **RRF fúzió** | Csak ha embedding is van | A vektor keresés része |

## Implementációs sorrend (javasolt)

```
Phase 1 — Alapok (1-2 nap)
├── 1. Salience decay cron job (knowledge-service)
└── 2. Napi napló API endpoint

Phase 2 — Explorer működés (2-3 nap)
├── 3. Chat history .jsonl parser
├── 4. search_chat_history MCP tool
└── 5. Explorer CLAUDE.md frissítés

Phase 3 — Librarian működés (1-2 nap)
├── 6. Librarian kurátori workflow CLAUDE.md
└── 7. Memory promóció automatizálás

Phase 4 — Advanced (opcionális)
├── 8. Ollama embedding integráció
├── 9. Vektor keresés + RRF
└── 10. Gráf vizualizáció Datahaven-ben
```

## Technikai részletek

### Salience decay implementáció

```typescript
// Éjszakai cron job (03:00)
// 7 nap után 0.5%/nap csökkenés, minimum 0.01
UPDATE memories
SET salience = MAX(0.01, salience * 0.995)
WHERE accessed_at < datetime('now', '-7 days')
  AND tier IN ('warm', 'cold');
```

### Chat history parser

```typescript
// ~/.claude/projects/-opt-spaceos/*.jsonl feldolgozás
interface ChatMessage {
  type: 'message' | 'file-history-snapshot' | 'summary';
  messageId: string;
  message?: {
    role: 'user' | 'assistant';
    content: string;
  };
  timestamp: string;
}

// Grep-szerű keresés a .jsonl fájlokban
function searchChatHistory(query: string, options: {
  project?: string;
  dateFrom?: string;
  dateTo?: string;
  limit?: number;
}): SearchResult[];
```

## Következő lépések

1. **Phase 1** indítás — Salience decay + napi napló
2. **Explorer CLAUDE.md** kiegészítése chat history keresési képességgel
3. **Librarian CLAUDE.md** kiegészítése kurátori workflow-val
4. `.jsonl` parser implementálása (Node.js script vagy MCP tool)
5. Knowledge Service bővítése: `search_chat_history` MCP tool
