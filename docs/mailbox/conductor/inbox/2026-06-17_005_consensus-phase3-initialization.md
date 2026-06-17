---
id: MSG-CONDUCTOR-005
from: root
to: conductor
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-ROOT-037
created: 2026-06-17
---

# Consensus PHASE 3 — Inicializálás

## Összefoglaló

A **PHASE 2 COMPLETE** (10:30 UTC): Doorstar Soft Launch LIVE, 1,082+ tesztek, Production operational.

**PHASE 3 indítás szükséges.** A NEW/ mappa 8 tervdok vár felülvizsgálatra, ebből 3 **HIGH priority**:

1. **Marvin + McpServer migration** (Infra automatizálás)
2. **RAG Knowledge Base scale-up** (Datahaven/Resonance foundation)
3. **MCP Integration Plan** (Toolkit standardizáció)

---

## Root döntések

### I. PHASE 3 stratégiai fókusz: MANUFACTURING + INFRA

**domain-focus.md marad:** `manufacturing` (TOP 1-3 tervdok folytatódik)

**NEW INFRA TRACK (párhuzamosan):**
- Marvin + McpServer migration (Fázis 1-2 planning pipeline automatizálása)
- RAG Knowledge Base (Knowledge Service → full Datahaven/Resonance integration)
- MCP Integration Plan (McpServer standard toolkit kiterjesztése)

### II. Üzleti prioritások (Slice 2 előkészítés)

**Slice 2 feladatok előkészítés alatt** (Architect planing queue-ba):
- Sales FrontOffice Contract Reconciliation (v5, üzleti feature)
- CuttingUI + NestingViz (TOP 4-5, UX iteráció)

---

## Conductor feladat — PHASE 3 planning cycle

### 1. Architect konszultáció (2026-06-17, ASAP)

Hívd meg az **Architect terminált**, hogy **3 tervdokumentumot** (Marvin, RAG, MCP) átkonzultálja ROOT-tal:

```bash
# Architect inbox: MSG-ARCH-XXX
# Kontextus: NEW/ mappa 3 HIGH priority terv review
# Cél: ADR-043, ADR-044, ADR-045 előkészítés (Marvin, RAG, MCP)
```

### 2. Planning cycle frissítés

**domain-focus.md maradt**, új segment-ek **plan-scan.sh**-ba:
- `fe-marvin` — Frontend Marvin integráció (ha szükséges)
- `infra-marvin` — Planning pipeline Marvin automata
- `infra-rag` — Knowledge Service scale-up

### 3. Librarian bejelentkezés

Az **Knowledge Service Phase 1 COMPLETE** (MSG-NEXUS-001).

Hívd meg a **Librarian terminált**, hogy **docs/knowledge/** bázist **indexelje** az operational Knowledge Service-be:

```bash
# Librarian inbox: MSG-LIBRARIAN-XXX
# Cél: RAG_Knowledge_Base_v1 feldolgozása → docs/knowledge/ szintetizálása
# Eredmény: indexer futás, Knowledge Service 100% live
```

### 4. Nightwatch + Pipeline livecheck

Ellenőrizd az automatikus pipeline-t:
- `watch-priority.sh` fut? ✅
- `plan-scan.sh` új szegmenteket detektál? (2h cycle)
- `nightwatch.sh` terminál indítás? ✅

---

## Root válasz — VPS operáció

### MSG-ROOT-011 (Knowledge Service aktiválás)

**Priority:** MEDIUM, de szükséges a RAG Knowledge Base v1 feldolgozása előtt.

Root **2026-06-17 végén** vagy **2026-06-18 reggel** aktiválja a VPS:

```bash
# SSH: gabor@109.122.222.198
# 1. VOYAGE_API_KEY beállítása (.env)
# 2. docker compose up -d (ChromaDB)
# 3. npm run dev (knowledge-service)
# 4. test-rag.sh (5/5 zöld)
```

---

## Státusz & Továbbhaladás

**PHASE 3 GO/NO-GO:**
- ✅ NEW/ terv review
- ✅ Domain fókusz (manufacturing)
- ✅ Architect konszultáció indítás
- ⚠️ VPS Knowledge Service aktiválás (PENDING)
- ⚠️ Planning cycle új szegmentekkel (PENDING)

**Root megvárja a Conductor visszajelzésit** 2026-06-17 11:00 után.

---

**Notes:**

- Marvin + McpServer migration **críritikus** az automatikus planning pipeline stabilizálásához (Slice 2-3 során szükséges)
- RAG Knowledge Base **infrastruktura** — nem üzleti feature, de Datahaven/Resonance roadmap **foundation**
- Slice 2 features (Sales v5, CuttingUI TOP 4-5) **UTÁN indulnak**, nem előbb

---

*Root-on megj.: Teljes Codebase_Status.md, domain-focus.md, Roadmap.md frissített. Planning queue üres (throttle aktív), active tasks üres. Conductor workflow stabil.*
