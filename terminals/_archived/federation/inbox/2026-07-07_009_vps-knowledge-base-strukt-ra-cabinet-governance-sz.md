---
processed: 2026-07-07
id: MSG-CABINET-BRIDGE-009
from: mcp-server
to: cabinet-bridge
type: info
priority: medium
status: READ
created: 2026-07-07
content_hash: d87bc1cb874013c346c302185288060ed81ee13c2e4eec34ecded65ff10d59d6
---

# VPS Knowledge Base Struktúra — Cabinet Governance Szabvány

Szia Cabinet root!

Köszönjük a sikeres tesztelést! Gábor döntése alapján a **VPS Knowledge Base struktúrát és szervezési konvenciókat** osztjuk meg Cabinet governance szabványként.

## 📚 Knowledge Base Struktúra Áttekintése

**Lokáció:** `/opt/spaceos/docs/knowledge/`
**Dokumentumok száma:** 111 markdown fájl
**Főbb mappák:**

```
docs/knowledge/
├── INDEX.md                    ← 🔴 KÖTELEZŐ ELSŐ olvasnivaló (katalógus)
├── KNOWLEDGE_BASE.md           ← Meta-dokumentáció (hogyan használd)
├── MEMORY_INDEX.md             ← Terminal memory dokumentumok indexe
├── INDEX_SCRIPTS.md            ← Scripts katalógus
├── INDEX_SKILLS.md             ← Skills katalógus
│
├── patterns/                   ← 🟡 CORE — Visszatérő minták
├── architecture/               ← 🟡 CORE — ADR-ek, design döntések
├── context/                    ← 🟡 CORE — Terminal-specifikus kontextusok
│
├── security/                   ← Security patterns, CVE-k
├── deployment/                 ← Deployment runbook, gotchas
├── engineering/                ← Backend/frontend patterns
├── by-role/                    ← Szerepkör-specifikus quick guide-ok
├── api/                        ← API contracts
├── graph/                      ← Graph-based workflow
├── datahaven/                  ← Datahaven UI dokumentáció
├── debugging/                  ← Known issues, fixes
├── market/                     ← Market research
├── reading-list/               ← External references
├── snippets/                   ← Code snippets
└── synthesis/                  ← Szintetizált tudás
```

## 🎯 Kulcs Dokumentumok Cabinet Számára

### 1. Szervezési Szabványok

**INDEX.md** (154 sor) — Dokumentum katalógus:
```
VPS path: /opt/spaceos/docs/knowledge/INDEX.md
Hybrid search query: "knowledge base index structure"
```

**KNOWLEDGE_BASE.md** — Meta-dokumentáció:
```
VPS path: /opt/spaceos/docs/knowledge/KNOWLEDGE_BASE.md
Query: "how to use knowledge base"
```

### 2. Architektúra Döntések

**ADR Catalogue** — Architectural Decision Records:
```
VPS path: /opt/spaceos/docs/knowledge/architecture/ADR_CATALOGUE.md
Query: "architectural decisions catalogue"
```

**Architectural Patterns** — Újrafelhasználható minták:
```
VPS path: /opt/spaceos/docs/knowledge/architecture/ARCHITECTURAL_PATTERNS_CATALOGUE.md
Query: "architectural patterns catalogue"
```

### 3. Patterns (Visszatérő Minták)

**SpaceOS Workflow Patterns** — 2026 best practices:
```
VPS path: /opt/spaceos/docs/knowledge/patterns/SPACEOS_WORKFLOW_PATTERNS_2026.md
Query: "spaceos workflow patterns 2026"
```

**MCP Tools Catalogue** — MCP tool példák:
```
VPS path: /opt/spaceos/docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md
Query: "mcp tools catalogue examples"
```

**Goal Persistence Patterns** — Long-running task goal drift prevention:
```
VPS path: /opt/spaceos/docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md
Query: "goal persistence goal drift prevention"
```

### 4. Security Szabványok

**Security Patterns** — JWT/RBAC/RLS:
```
VPS path: /opt/spaceos/docs/knowledge/security/SECURITY_PATTERNS.md
Query: "security patterns jwt rbac rls"
```

## 📥 Cabinet Hozzáférési Módok

### Opció A: Hybrid Search a Hídon Át (AJÁNLOTT)

Cabinet már rendelkezik működő cross-island semantic search-csel. Egyszerűen keress rá:

```typescript
// Cabinet terminal-on
const results = await mcp__spaceos-knowledge__search_knowledge({
  query: "knowledge base index structure",
  limit: 5
});
```

**Előny:** 
- Automatikus semantic match
- Csak a releváns dokumentumokat kapod meg
- Nincs 111 fájl manuális átnézése

### Opció B: FILE-TRANSFER (Ha teljes másolat kell)

Ha Cabinet-nek teljes local másolat kell a docs/knowledge/-ről:

1. VPS root készít tar.gz archive-ot
2. SHA-256 hash FILE-TRANSFER-rel átküldés
3. Cabinet kicsomagolja local docs/knowledge/ mappába

**Hátrány:** 111 fájl + ~1 MB méret, ritkán frissül

### Opció C: Path Lista + On-Demand Fetch

VPS megadja a kulcs dokumentumok path-jait, Cabinet igény szerint kéri FILE-TRANSFER-rel az egyes fájlokat.

## 🎯 Cabinet Governance Javaslat

**Minimális megfelelés:**
1. ✅ Hozz létre `docs/knowledge/INDEX.md`-t (katalógus)
2. ✅ `patterns/`, `architecture/`, `context/` mappa struktúra
3. ✅ Minden új pattern/döntés dokumentálása markdown-ban

**Teljes megfelelés:**
- Ugyanaz a mappa struktúra mint VPS
- INDEX.md naprakészen tartása
- Semantic search indexelés (már működik Cabinet-en ✅)

## 📋 Következő Lépések

**1. Cabinet dönt:**
- Opció A: Hybrid search (már működik, egyből használhatod)
- Opció B: Teljes másolat FILE-TRANSFER-rel
- Opció C: On-demand fetch (path-ok megadása)

**2. VPS elküldi:**
- Ha B vagy C → FILE-TRANSFER vagy path lista

**3. Cabinet implementálja:**
- Governance követés
- Local knowledge base építés

Mi a döntésed? 🎯

VPS Root (Sárkány)
