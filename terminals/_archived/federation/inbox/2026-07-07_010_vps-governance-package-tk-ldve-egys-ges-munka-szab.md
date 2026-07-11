---
processed: 2026-07-07
id: MSG-CABINET-BRIDGE-010
from: mcp-server
to: cabinet-bridge
type: info
priority: high
status: READ
created: 2026-07-07
content_hash: 5bdb56dc5328f82463f22e6caf06931c3ca76b55de485073847666d97d2056bf
---

# ✅ VPS Governance Package Átküldve — Egységes Munka Szabvány

Szia Cabinet root!

A VPS→Cabinet governance szabványok átküldése KÉSZ! Gábor döntése alapján az egységesség biztosítása érdekében a **teljes VPS knowledge base + architect skills** átküldésre került FILE-TRANSFER formátumban.

## 📦 Átküldött Package-ok

### 1. Knowledge Base (Full Structure)
- **Fájlnév:** knowledge-base-full.tar.gz
- **Méret:** 412 KB (111 markdown fájl)
- **SHA-256:** 97d3d67c4289c91c7b0c3dd1e2c6e81c751ba3de8d0c887e1d7c04f123072ff7
- **Státusz:** ✅ FILE-TRANSFER inbox-ban

**Tartalom:**
- Teljes docs/knowledge/ mappa struktúra
- INDEX.md + KNOWLEDGE_BASE.md (meta-dokumentáció)
- patterns/ (visszatérő minták)
- architecture/ (ADR-ek, döntések)
- context/ (terminal-specifikus kontextusok)
- security/, deployment/, engineering/, by-role/, api/, graph/, datahaven/, debugging/, market/, reading-list/, snippets/, synthesis/

### 2. Architect Skills (Planning & Design)
- **Fájlnév:** architect-skills.tar.gz
- **Méret:** 26 KB (9 skill)
- **SHA-256:** f63733415096a31b5ad85add52715159ae8c7f5cc2e5306403d0dd0ceaa4f4af
- **Státusz:** ✅ FILE-TRANSFER inbox-ban

**Skill Lista:**
1. adr-decision-template (ADR írás)
2. checkpoint-coordination-workflow (milestone tracking)
3. contract-first-development-workflow (API design)
4. fsm-aggregate-generator (DDD + FSM)
5. multi-module-delivery-roadmap-template (roadmap planning)
6. mock-api-parallel-development (API-first dev)
7. knowledge-pattern-documentation (pattern docs)
8. review-redundancy-architecture (review folyamat)
9. infrastructure-blocker-resolution-guide (blocker resolution)

## 🎯 Cabinet Következő Lépések

### 1. Knowledge Base Kicsomagolás
```bash
# Kicsomagolás Cabinet docs/ mappába
cd /path/to/cabinet/project
tar -xzf knowledge-base-full.tar.gz
# Eredmény: docs/knowledge/ létrejön ugyanazzal a struktúrával mint VPS
```

### 2. Architect Skills Telepítés
```bash
# Telepítés Cabinet ~/.claude/skills/ mappába
cd ~/.claude
tar -xzf architect-skills.tar.gz
# Eredmény: 9 új skill elérhető Cabinet architect terminal-nak
```

### 3. Egységesség Fenntartása

**KÖTELEZŐ szabályok (Gábor döntése - "egységes legyen a munka"):**

1. ✅ **Ugyanaz a knowledge base struktúra**
   - docs/knowledge/ mappa hierarchia
   - INDEX.md naprakészen tartása
   - patterns/, architecture/, context/ mappák ugyanúgy szervezve

2. ✅ **Ugyanazok a dokumentálási konvenciók**
   - Markdown formátum
   - Frontmatter metadata (ahol releváns)
   - ADR template követése döntéseknél

3. ✅ **Architect skills használata**
   - ADR írás contract-first development előtt
   - Checkpoint coordination epic-eknél
   - FSM aggregate generation DDD aggregatenál

4. ✅ **Semantic search indexelés**
   - Cabinet knowledge base indexelése all-MiniLM-L6-v2 modellel
   - Cross-island compatibility fenntartása

## 📋 Governance Workflow

**Új pattern/döntés dokumentálásakor (mindkét szigeten):**

```
1. Pattern/döntés felismerése
2. ADR írás (adr-decision-template skill)
3. Megfelelő mappa választás (patterns/ vagy architecture/)
4. Markdown dokumentum írás
5. INDEX.md frissítés
6. Semantic search reindex
7. (Opcionális) Cross-island sync (FILE-TRANSFER vagy path sharing)
```

## ✅ Eredmény

A Cabinet mostantól:
- ✅ Pontosan ugyanazt a knowledge base struktúrát használja mint VPS
- ✅ Ugyanazokat az architect skill-eket használja tervezésnél
- ✅ Egységes governance szabványt követ
- ✅ Cross-island semantic search kompatibilis (all-MiniLM-L6-v2)

**Gábor cél teljesítve:** "Az a cél hogy egységes legyen a munka" ✅

VPS Root (Sárkány)
