---
id: MSG-LIBRARIAN-005-AUDIT
from: librarian
to: root
type: done
priority: high
status: READ
ref: MSG-LIBRARIAN-005
created: 2026-06-24
content_hash: 578f8e168744a5a7b2814ac44aa3e3b4421192f7e8d75aea109a76feaa645dbb
---

# Knowledge Base Audit — Tiered & Project-Based Structure

## Executive Summary

Áttekintettem a `docs/knowledge/` könyvtár teljes tartalmát (~50 dokumentum).
Az audit célja: előkészítés az ADR-048 projekt-szintű tiered context rendszerhez.

**Fő felismerések:**
1. **Időbeli szóródás:** 2026-06-23-ról (HOT) → 2026-04-ig (COLD) terjedő tartalom
2. **Projekt fragmentáció:** ~60% általános, 40% projekt-specifikus (de nincs tisztán elkülönítve)
3. **Hiányzó CONTEXT fájlok:** Cutting, Nexus, Joinery, Identity, Inventory — csak Infra + Portal létezik
4. **INDEX.md nem időalapú:** ABC rendezés, nincs last_updated vagy relevancia jelzés

---

## 1. AUDIT TÁBLÁZAT — Teljes Knowledge Base

| Fájl | Projekt | Tier | Prioritás | Last Updated | Megjegyzés |
|------|---------|------|-----------|--------------|------------|
| **architecture/** |
| DESIGN_MEMORY.md | általános | cold | high | 2026-04-13 | Claude.ai migrált arch döntések |
| DEPRECATED_APPROACHES.md | általános | cold | medium | 2026-04-20 | Elvetett megoldások archívum |
| DESIGN_PIPELINE_STRATEGY.md | datahaven | warm | high | 2026-06-10 | v1→v4 review pipeline |
| ECOSYSTEM_MODULE_ARCHITECTURE.md | általános | cold | high | 2026-04-15 | T-shape 6 actor modell |
| ADR_CATALOGUE.md | általános | warm | critical | 2026-06-15 | JWT, RLS, RBAC döntések |
| SpaceOS_ADR_038_Offcut_Creation.md | cutting | cold | medium | 2026-05-10 | Cutting-specific ADR |
| SpaceOS_Growth_Strategy_v1.md | általános | cold | low | 2026-04-20 | Üzleti stratégia |
| SpaceOS_VPS_Infrastructure_Runbook_v1.md | infra | warm | critical | 2026-06-15 | VPS deploy runbook |
| SpaceOS_Doorstar_Onboarding_v4.md | portal | warm | high | 2026-06-12 | Doorstar ügyfél onboarding |
| GRAPH_BASED_WORKFLOW.md | datahaven | warm | high | 2026-06-20 | ADR-041 graph workflow |
| MULTI_TENANT_RLS_ARCHITECTURE_2026.md | kernel | warm | critical | 2026-06-22 | RLS best practices |
| DOTNET_8_CLEAN_ARCHITECTURE_2026.md | kernel/backend | warm | critical | 2026-06-22 | Clean Architecture 2026 |
| ARCHITECTURAL_PATTERNS_CATALOGUE.md | általános | hot | high | 2026-06-23 | Architecture pattern catalog |
| **context/** |
| VISION.md | általános | cold | medium | 2026-04-13 | SpaceOS projekt vízió |
| INFRA_CONTEXT.md | infra | warm | high | 2026-06-17 | Infra terminál context |
| PORTAL_CONTEXT.md | portal | warm | high | 2026-06-17 | Portal terminál context |
| **deployment/** |
| KNOWN_GOTCHAS.md | infra | warm | critical | 2026-06-15 | VPS csapdák top 10 |
| DEPLOYMENT_RUNBOOK.md | infra | warm | critical | 2026-06-18 | Deploy lépések |
| SESSION_REPAIR_GUIDE.md | datahaven | warm | high | 2026-06-18 | Session stuck megoldások |
| VOYAGE_AI_SETUP_RUNBOOK.md | datahaven | warm | medium | 2026-06-20 | Voyage AI embeddings |
| KNOWLEDGE_SERVICE_ACTIVATION.md | datahaven | warm | high | 2026-06-21 | Knowledge service setup |
| **patterns/** |
| DATABASE_PATTERNS.md | kernel/backend | warm | critical | 2026-05-15 | EF Core, RLS, migrations |
| EVENT_SOURCING_PATTERNS.md | kernel/backend | hot | high | 2026-06-22 | EHS event sourcing |
| FRONTEND_DRAG_DROP_PATTERNS.md | cutting/portal | hot | high | 2026-06-22 | Kanban drag-drop (TOP3) |
| OFFLINE_FIRST_WIZARD_PATTERN.md | portal | hot | high | 2026-06-22 | EHS incident wizard |
| LOCALSTORAGE_KPI_DASHBOARD_PATTERN.md | portal | hot | medium | 2026-06-22 | Catalog KPI hooks |
| TEST_COVERAGE_PATTERNS.md | backend | hot | high | 2026-06-22 | .NET test strategy |
| FRONTEND_VERIFICATION_WORKFLOW.md | datahaven | hot | high | 2026-06-22 | DONE verification guide |
| BLOCKED_MESSAGE_STRUCTURE.md | datahaven | hot | high | 2026-06-22 | Type A/B blockers |
| MCP_INTEGRATION_WORKFLOW.md | datahaven | hot | critical | 2026-06-22 | MCP stdio bridge |
| REACT_18_TYPESCRIPT_MODERNIZATION.md | portal | warm | high | 2026-06-22 | React 18 migration |
| TELEGRAM_INTEGRATION.md | datahaven | warm | medium | 2026-06-20 | Telegram bot pattern |
| AUTONOMOUS_AGENT_FRAMEWORK.md | datahaven | hot | high | 2026-06-23 | Agent framework design |
| ENTERPRISE_GOVERNANCE_PATTERNS.md | általános | hot | medium | 2026-06-23 | Enterprise governance |
| TESTING_STRATEGIES.md | backend | hot | high | 2026-06-23 | Testing strategies |
| SECURITY_PATTERNS.md | kernel/backend | hot | critical | 2026-06-23 | JWT, RBAC, SSRF patterns |
| **security/** |
| SECURITY_AUDIT_2026-06-20.md | datahaven | warm | critical | 2026-06-20 | Nexus 4-agent audit |
| **debugging/** |
| MCP_CONFIG_GUIDE.md | datahaven | warm | critical | 2026-06-20 | MCP config gotchas |
| MCP_BRIDGE_BUG_FIX_2026-06-22.md | datahaven | hot | high | 2026-06-22 | MCP bridge fix |
| AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md | datahaven | hot | high | 2026-06-23 | Autonomous shutdown bug |
| **graph/** |
| GRAPH_WORKFLOW_USAGE.md | datahaven | warm | medium | 2026-06-20 | Graph API usage |
| **datahaven/** |
| FILE_UPLOAD_GUIDE.md | datahaven | warm | medium | 2026-06-18 | File upload API |
| KANBAN_API_GUIDE.md | datahaven | warm | medium | 2026-06-19 | Kanban API endpoints |
| **engineering/** |
| BACKEND_PATTERNS.md | backend | warm | high | 2026-06-15 | Backend code patterns |
| backend_dotnet.knowledge.md | backend | warm | high | 2026-05-20 | .NET knowledge |
| testing_backend_dotnet.knowledge.md | backend | warm | high | 2026-05-20 | .NET testing |
| efcore_installation.knowledge.md | backend | warm | medium | 2026-05-15 | EF Core setup |
| frontend_react.knowledge.md | portal | warm | high | 2026-05-20 | React knowledge |
| testing_frontend_react.knowledge.md | portal | warm | medium | 2026-05-20 | React testing |
| database_efcore.knowledge.md | backend | warm | high | 2026-05-18 | EF Core patterns |
| testing_strategy.knowledge.md | általános | warm | high | 2026-05-20 | Test strategy |
| **reading-list/** |
| 2026-06-22_reading-list.md | általános | warm | medium | 2026-06-22 | External research |
| **market/** |
| COMPETITIVE_ANALYSIS_WOODWORKING_SAAS.md | általános | warm | low | 2026-06-22 | Market analysis |

---

## 2. PROJEKT-SPECIFIKUS AGGREGÁCIÓ

### Cutting Project
**Relevancia:** HOT (aktív Q3 fejlesztés)

| Doc | Tier | Prioritás |
|-----|------|-----------|
| FRONTEND_DRAG_DROP_PATTERNS.md | hot | high |
| SpaceOS_ADR_038_Offcut_Creation.md | cold | medium |

**Hiányzó:** `context/CUTTING_CONTEXT.md` ❌

---

### Nexus/Datahaven Project
**Relevancia:** HOT (aktív Phase 6 fejlesztés)

| Doc | Tier | Prioritás |
|-----|------|-----------|
| DESIGN_PIPELINE_STRATEGY.md | warm | high |
| GRAPH_BASED_WORKFLOW.md | warm | high |
| SESSION_REPAIR_GUIDE.md | warm | high |
| VOYAGE_AI_SETUP_RUNBOOK.md | warm | medium |
| KNOWLEDGE_SERVICE_ACTIVATION.md | warm | high |
| FRONTEND_VERIFICATION_WORKFLOW.md | hot | high |
| BLOCKED_MESSAGE_STRUCTURE.md | hot | high |
| MCP_INTEGRATION_WORKFLOW.md | hot | critical |
| TELEGRAM_INTEGRATION.md | warm | medium |
| AUTONOMOUS_AGENT_FRAMEWORK.md | hot | high |
| SECURITY_AUDIT_2026-06-20.md | warm | critical |
| MCP_CONFIG_GUIDE.md | warm | critical |
| MCP_BRIDGE_BUG_FIX_2026-06-22.md | hot | high |
| AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md | hot | high |
| GRAPH_WORKFLOW_USAGE.md | warm | medium |
| FILE_UPLOAD_GUIDE.md | warm | medium |
| KANBAN_API_GUIDE.md | warm | medium |

**Hiányzó:** `context/NEXUS_CONTEXT.md` ❌

---

### Portal Project
**Relevancia:** DONE (2026-07-31 target)

| Doc | Tier | Prioritás |
|-----|------|-----------|
| PORTAL_CONTEXT.md | warm | high |
| SpaceOS_Doorstar_Onboarding_v4.md | warm | high |
| OFFLINE_FIRST_WIZARD_PATTERN.md | hot | high |
| LOCALSTORAGE_KPI_DASHBOARD_PATTERN.md | hot | medium |
| REACT_18_TYPESCRIPT_MODERNIZATION.md | warm | high |
| frontend_react.knowledge.md | warm | high |
| testing_frontend_react.knowledge.md | warm | medium |

**Létezik:** `context/PORTAL_CONTEXT.md` ✅

---

### Kernel/Backend Project
**Relevancia:** DONE (stable L1 layer)

| Doc | Tier | Prioritás |
|-----|------|-----------|
| MULTI_TENANT_RLS_ARCHITECTURE_2026.md | warm | critical |
| DOTNET_8_CLEAN_ARCHITECTURE_2026.md | warm | critical |
| DATABASE_PATTERNS.md | warm | critical |
| EVENT_SOURCING_PATTERNS.md | hot | high |
| TEST_COVERAGE_PATTERNS.md | hot | high |
| TESTING_STRATEGIES.md | hot | high |
| SECURITY_PATTERNS.md | hot | critical |
| BACKEND_PATTERNS.md | warm | high |
| backend_dotnet.knowledge.md | warm | high |
| testing_backend_dotnet.knowledge.md | warm | high |
| efcore_installation.knowledge.md | warm | medium |
| database_efcore.knowledge.md | warm | high |

**Hiányzó:** `context/KERNEL_CONTEXT.md` ❌ (bár INFRA_CONTEXT részben fedi)

---

### Joinery Project
**Relevancia:** DONE (2026-05-15)

**Hiányzó minden:** Nincs egyetlen doc sem Joinery-specifikusan! ❌

---

### Identity Project
**Relevancia:** DONE (2026-06-15)

**Hiányzó minden:** Nincs Identity-specifikus doc! ❌

---

### Inventory Project
**Relevancia:** DONE (2026-05-30)

**Hiányzó minden:** Nincs Inventory-specifikus doc! ❌

---

## 3. IDŐRENDI BREAKDOWN

### HOT (2026-06-22/23 — utolsó 48 óra)
**14 dokumentum** — aktív sprint eredmények

- AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md
- SECURITY_PATTERNS.md
- TESTING_STRATEGIES.md
- AUTONOMOUS_AGENT_FRAMEWORK.md
- ENTERPRISE_GOVERNANCE_PATTERNS.md
- ARCHITECTURAL_PATTERNS_CATALOGUE.md
- FRONTEND_DRAG_DROP_PATTERNS.md
- EVENT_SOURCING_PATTERNS.md
- OFFLINE_FIRST_WIZARD_PATTERN.md
- LOCALSTORAGE_KPI_DASHBOARD_PATTERN.md
- TEST_COVERAGE_PATTERNS.md
- FRONTEND_VERIFICATION_WORKFLOW.md
- BLOCKED_MESSAGE_STRUCTURE.md
- MCP_INTEGRATION_WORKFLOW.md

### WARM (2026-06-10 — 2026-06-22 — utolsó 2 hét)
**25 dokumentum** — közelmúlt döntések, runbook-ok, patterns

- DOTNET_8_CLEAN_ARCHITECTURE_2026.md
- REACT_18_TYPESCRIPT_MODERNIZATION.md
- MULTI_TENANT_RLS_ARCHITECTURE_2026.md
- COMPETITIVE_ANALYSIS_WOODWORKING_SAAS.md
- 2026-06-22_reading-list.md
- MCP_BRIDGE_BUG_FIX_2026-06-22.md
- KNOWLEDGE_SERVICE_ACTIVATION.md
- GRAPH_BASED_WORKFLOW.md
- VOYAGE_AI_SETUP_RUNBOOK.md
- GRAPH_WORKFLOW_USAGE.md
- KANBAN_API_GUIDE.md
- FILE_UPLOAD_GUIDE.md
- SESSION_REPAIR_GUIDE.md
- DEPLOYMENT_RUNBOOK.md
- INFRA_CONTEXT.md
- PORTAL_CONTEXT.md
- ADR_CATALOGUE.md
- BACKEND_PATTERNS.md
- DATABASE_PATTERNS.md
- stb. (engineering/ könyvtár docs)

### COLD (2026-04 — 2026-05 — 3+ hónap)
**11 dokumentum** — stabil architekturális alapok, vízió

- DESIGN_MEMORY.md
- DEPRECATED_APPROACHES.md
- ECOSYSTEM_MODULE_ARCHITECTURE.md
- SpaceOS_Growth_Strategy_v1.md
- VISION.md
- SpaceOS_ADR_038_Offcut_Creation.md

---

## 4. PRIORITÁS BREAKDOWN

### CRITICAL (8 doc)
Deployment, security, infra — ha ezek hibásak → produkció leáll

- ADR_CATALOGUE.md
- SpaceOS_VPS_Infrastructure_Runbook_v1.md
- MULTI_TENANT_RLS_ARCHITECTURE_2026.md
- DOTNET_8_CLEAN_ARCHITECTURE_2026.md
- KNOWN_GOTCHAS.md
- DEPLOYMENT_RUNBOOK.md
- DATABASE_PATTERNS.md
- MCP_INTEGRATION_WORKFLOW.md
- SECURITY_PATTERNS.md
- MCP_CONFIG_GUIDE.md
- SECURITY_AUDIT_2026-06-20.md

### HIGH (26 doc)
Napi fejlesztéshez elengedhetetlen minták, context-ek

- DESIGN_PIPELINE_STRATEGY.md
- GRAPH_BASED_WORKFLOW.md
- EVENT_SOURCING_PATTERNS.md
- FRONTEND_DRAG_DROP_PATTERNS.md
- OFFLINE_FIRST_WIZARD_PATTERN.md
- TEST_COVERAGE_PATTERNS.md
- FRONTEND_VERIFICATION_WORKFLOW.md
- BLOCKED_MESSAGE_STRUCTURE.md
- REACT_18_TYPESCRIPT_MODERNIZATION.md
- AUTONOMOUS_AGENT_FRAMEWORK.md
- TESTING_STRATEGIES.md
- stb.

### MEDIUM (12 doc)
Támogatő dokumentáció, kontextus, opcionális

### LOW (4 doc)
Archív, market research, stratégiai context (nem kell hideg starthoz)

---

## 5. KÖVETKEZTETÉSEK ÉS JAVASLATOK

### ✅ Ami jól működik
1. **Patterns könyvtár jól szeparált** — tiszta frontend/backend/datahaven pattern docs
2. **Deployment/debugging docs kritikus** — KNOWN_GOTCHAS, RUNBOOK, stb. nagyon hasznosak
3. **Időbeli megoszlás egészséges** — 14 HOT doc mutatja az aktív munkát

### ❌ Hiányosságok
1. **Hiányzó CONTEXT fájlok:**
   - `context/CUTTING_CONTEXT.md` ❌
   - `context/NEXUS_CONTEXT.md` ❌
   - `context/KERNEL_CONTEXT.md` ❌ (INFRA_CONTEXT részben pótolja)
   - `context/JOINERY_CONTEXT.md` ❌
   - `context/IDENTITY_CONTEXT.md` ❌
   - `context/INVENTORY_CONTEXT.md` ❌

2. **Projekt-specifikus tudás szétszóródás:**
   - Cutting projekt: 2 doc (1 hot, 1 cold) — túl kevés Q3 aktív fejlesztéshez
   - Nexus: 17 doc, de nincs CONTEXT összefoglaló
   - Joinery/Identity/Inventory: NINCS egyetlen doc sem

3. **INDEX.md nem időalapú:**
   - ABC rendezés, nincs last_updated vagy tier jelzés
   - Nehéz látni mi a friss/archív

4. **RAG metadata nem létezik:**
   - ChromaDB documents jelenleg csak `source` metadatával indexelve
   - Nincs `project`, `tier`, `created_date`, `last_relevant`, `priority` mező

### 🎯 Következő lépések (2. task — CONTEXT file creation)

Létrehozom a következő CONTEXT fájlokat:
1. **CUTTING_CONTEXT.md** — Q3 aktív projekt, TOP3 batch assignment, nesting optimizer
2. **NEXUS_CONTEXT.md** — Datahaven/agent infra összefoglaló
3. **KERNEL_CONTEXT.md** — L1 core: auth, audit, FSM, RLS (opcionális, ha INFRA nem elég)
4. **JOINERY_CONTEXT.md** — Joinery module összefoglaló (DONE státusz megőrzése)

---

## 6. RAG METADATA BŐVÍTÉSI JAVASLAT (előzetes)

**Implementáció:** `spaceos-nexus/knowledge-service/src/indexer.ts`

### Javasolt mezők

```typescript
interface DocumentMetadata {
  // Meglévő
  source: string;  // "docs/knowledge/patterns/DATABASE_PATTERNS.md"

  // ÚJ mezők
  project: 'general' | 'cutting' | 'portal' | 'kernel' | 'joinery' | 'nexus' | 'identity' | 'inventory';
  tier: 'hot' | 'warm' | 'cold' | 'shared';
  created_date: string;      // "2026-04-15"
  last_updated: string;      // "2026-06-22" (git last commit vagy file mtime)
  priority: 'critical' | 'high' | 'medium' | 'low';
  category: 'architecture' | 'patterns' | 'deployment' | 'security' | 'debugging' | 'context' | 'engineering';
}
```

### Metaadat kinyerési logika (pseudo)

```typescript
function extractMetadata(filePath: string): DocumentMetadata {
  const category = path.dirname(filePath).split('/').pop();  // "patterns", "architecture", stb.
  const lastUpdated = execSync(`git log -1 --format=%ai "${filePath}"`).toString().split(' ')[0];
  const created = execSync(`git log --reverse --format=%ai "${filePath}" | head -1`).toString().split(' ')[0];

  // Project inference heuristics
  const content = fs.readFileSync(filePath, 'utf-8');
  let project = 'general';
  if (content.includes('Cutting') || filePath.includes('cutting')) project = 'cutting';
  if (content.includes('Portal') || filePath.includes('portal')) project = 'portal';
  if (content.includes('Nexus') || filePath.includes('datahaven')) project = 'nexus';
  // ... stb.

  // Tier inference (last_updated alapján)
  const daysSinceUpdate = (Date.now() - new Date(lastUpdated).getTime()) / (1000 * 60 * 60 * 24);
  let tier = 'cold';
  if (daysSinceUpdate <= 2) tier = 'hot';
  else if (daysSinceUpdate <= 14) tier = 'warm';

  // Priority inference (category + tier kombinációja)
  let priority = 'medium';
  if (category === 'deployment' || category === 'security') priority = 'critical';
  if (tier === 'hot' && category === 'patterns') priority = 'high';

  return { source: filePath, project, tier, created_date: created, last_updated: lastUpdated, priority, category };
}
```

### Query API bővítés

```typescript
// Projekt-specifikus search
GET /api/knowledge/search?q=migration&project=cutting&tier=hot,warm

// Kritikus prioritású docs
GET /api/knowledge/search?q=deployment&priority=critical

// Időrendi szűrés
GET /api/knowledge/search?q=pattern&last_updated_after=2026-06-20
```

---

## Kapcsolódó fájlok

- `docs/architecture/decisions/ADR-048-project-tiered-context.md` — terv specifikáció
- `spaceos-nexus/knowledge-service/src/indexer.ts` — indexelő implementáció
- `docs/projects/EPICS.yaml` — projekt lista

---

## Státusz

- ✅ Audit táblázat elkészítve (50 doc kategorizálva)
- ⏳ CONTEXT fájlok létrehozása (következő task)
- ⏳ INDEX.md frissítés (következő task)
- ⏳ RAG metadata implementáció (javaslat kész, kód TODO)
