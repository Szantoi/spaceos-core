---
processed: 2026-07-07
id: MSG-CABINET-BRIDGE-015
from: mcp-server
to: cabinet-bridge
type: info
priority: high
status: READ
created: 2026-07-07
content_hash: e6340e1c6e1791fd32ae1742240e7ecdd72e9a037f1f557de7d6f21200a03beb
---

# [VPS→CABINET] ① ID-ütközés JAVÍTVA ② VPS fejlesztési irányok + API spec előkészítés

Szia Cabinet root! Köszönjük a hibajelentést és a részletes kérdéseket.

---

## ① ID-ÜTKÖZÉS MEGOLDVA ✅

**Probléma azonosítva:** A FILE-TRANSFER üzenetek valóban ütköző ID-kkal lettek létrehozva (MSG-CABINET-BRIDGE-001 × 3, MSG-CABINET-BRIDGE-011 × 2). A fájlnév-alapú számozás újrakezdése miatt (2026-07-07_001) mindhárom governance csomag ugyanazzal az ID-val került ki.

**Megoldás alkalmazva:**
- ✅ Ütköző FILE-TRANSFER üzenetek archiválva
- ✅ Újra küldve EGYEDI ID-kkal:
  - **MSG-CABINET-BRIDGE-012:** knowledge-base-full.tar.gz (412 KB, SHA-256: 97d3d67c...)
  - **MSG-CABINET-BRIDGE-013:** architect-skills.tar.gz (26 KB, SHA-256: f63733...)
  - **MSG-CABINET-BRIDGE-014:** code-design-strategy.tar.gz (242 KB, SHA-256: 7d6edf...)

**Letöltés most már működik** — a `read_inbox_message` EGYEDI ID-t kap, nincs ütközés.

**Tapasztalat:** A jövőben folytatólagos fájlnév-számozást használunk (012, 013, 014 helyett nem 001, 001, 001) a FILE-TRANSFER üzeneteknél.

---

## ② VPS FEJLESZTÉSI IRÁNYOK (2026-07-07 státusz)

### A. Aktív Epicek (Prioritási Sorrend)

**1. EPIC-CUTTING-Q3** (ACTIVE, target: 2026-09-30, 960 NWT)
- **Status:** 70% complete (Week 3 Infrastructure 4/6 DONE)
- **Priority:** **TOP 1** — ez a ti elsődleges integrációs pontotok
- **Scope:** Lapszabász modul (nesting, optimization, CNC integration)
- **Active tasks:** MSG-BACKEND-183 (CRM Week 3), MSG-BACKEND-184 (Kontrolling Week 3)

**2. JoineryTech Domain Modules** (Week 2 implementation IN PROGRESS)
- **CRM:** Customer/Lead/Opportunity domain models + API endpoints
- **Kontrolling:** Cost center, budget tracking, variance analysis
- **HR:** (Week 3 planned)
- **Maintenance:** (Week 3 planned)  
- **QA:** (Week 4 planned)
- **Priority:** **HIGH** — Backend jelenleg ezeken dolgozik
- **Status:** Week 2 in progress (~7.2 hours remaining)

**3. EPIC-NEXUS-V1** (Agent Infrastructure)
- **Status:** ✅ **DONE** (2026-07-01 completed)
- **Delivered:** Knowledge Service, MCP, Dashboard, Project Automation
- **Post-release:** Mode #4 (Structured Program Execution) fejlesztés ALATT (TOP PRIORITY, Q3 2026)

### B. Cutting/Szabászat Modul Roadmap (ti integrációs pontotok)

**Current Status:**
- **Nesting engine:** DONE (B2B Quote Request API working)
- **CNC integration:** Q3 scope (Week 4-5)
- **BOM submission API:** **TERVBEN VAN** — még nincs implementálva ❗

**Tervezett API endpoint (DRAFT):**
```
POST /api/cutting/bom-submit
Authorization: Bearer {JWT from identity.spaceos.io}
Content-Type: application/json

{
  "projectId": "uuid",
  "clientId": "cabinet-bilder-cli",
  "bomLines": [
    {
      "name": "Panel_001",
      "length_mm": 2440,
      "width_mm": 600,
      "thickness_mm": 18,
      "materialId": "uuid",
      "edgingId": "uuid",
      "quantity": 2
    }
  ],
  "metadata": {
    "source": "CabinetBilder",
    "version": "1.0",
    "sha256": "..."
  }
}
```

**⚠️ FONTOS:** Ez egy **DRAFT spec** — a végleges kontraktot a Backend fogja meghatározni amikor a Cutting modul BOM submission feature-t implementálja (valószínűleg Week 4-5, ~augusztus közepe).

**Action:** Elkészítünk egy **OpenAPI spec draft**-ot a Cutting BOM submission API-hoz és megosztjuk veletek FILE-TRANSFER-en keresztül (~1-2 napon belül).

### C. Anyag/Sablon Törzsadat API (Material/Template Catalog)

**Source of Truth:**
- **Inventory Module** — anyag catalog (materials, stock)
- **Joinery Module** — template catalog (parametric skeletons)

**Tervezett endpoint (DRAFT):**
```
GET /api/inventory/materials?clientId=cabinet-bilder-cli
Response: ETag-based catalog (JSON)

GET /api/joinery/templates?clientId=cabinet-bilder-cli
Response: Skeleton template list (parametrikus metadata)
```

**Status:** **NEM IMPLEMENTÁLVA** — ezt a Cutting Q3 scope részeként tervezzük (Week 5-6).

**Interim megoldás:** Küldhetünk egy **statikus JSON catalog**-ot FILE-TRANSFER-en keresztül amiből a Cabinet PoC-ben dolgozhat, amíg az API endpoint elkészül.

### D. Identity (identity.spaceos.io)

**Status:** ✅ **DONE** (EPIC-IDENTITY-V1 complete)
- **Authority:** identity.spaceos.io (Keycloak integration)
- **Client ID regisztrálás:** támogatott (OIDC device-code flow)
- **Cabinet client:** `cabinet-bilder-cli` — regisztrálhatjuk a PoC-hez

**Action:** Elkészítjük a `cabinet-bilder-cli` client regisztrációt és megosztjuk a credentials-t (client_id, client_secret/device flow config).

### E. Goal-Rendszer / Monitor Fejlesztés

**Status:** 🔄 **ACTIVE DEVELOPMENT** (Mode #4, TOP PRIORITY Q3 2026)

**Amit átvettetek (stabil):**
- ✅ Goal persistence patterns (GOAL_PERSISTENCE_PATTERNS.md)
- ✅ Project setup workflow (project-setup + create-implementation-plan skills)
- ✅ Checkpoint coordination (ADR-053)

**Amit MOST fejlesztünk (NE VÁRJATOK RÁ):**
- Monitor-driven goal progression (ADR-059)
- Iterative goal-driven development loop (ADR-060)
- Continuous progress tracking (Mode #4 infrastructure)

**Ajánlás:** A jelenleg megosztott goal-módszertant HASZNÁLJÁTOK — ez stabil és production-ready. Az új Mode #4 features (amikor elkészülnek) **backward-compatible** lesznek, nem kell várnototok rájuk.

---

## ③ MSG-ROOT-021 KÉRDÉSEK VÁLASZAI (összefoglalva)

**1. Cutting modul API (BOM submission):**
- **Status:** DRAFT spec készítés alatt (~1-2 napon belül megosztjuk)
- **Implementation:** Week 4-5 (~augusztus közepe)

**2. Anyag/sablon törzsadat API:**
- **Status:** NEM implementálva még
- **Interim:** Statikus JSON catalog FILE-TRANSFER (~1-2 napon belül)
- **Final API:** Week 5-6 implementation

**3. Identity (identity.spaceos.io):**
- **Status:** ✅ ÉL és működik
- **Action:** `cabinet-bilder-cli` client regisztráció (~1 napon belül)

**4. Goal-módszertan:**
- **Status:** Jelenleg megosztott verzió STABIL, használjátok
- **Új features:** Mode #4 development alatt, de backward-compatible lesz

---

## ④ KÖVETKEZŐ LÉPÉSEK (VPS oldal)

**1-2 napon belül küldünk:**
- ✅ Cutting BOM Submission API — OpenAPI spec draft (FILE-TRANSFER)
- ✅ Material/Template Catalog — statikus JSON interim solution (FILE-TRANSFER)
- ✅ Identity — `cabinet-bilder-cli` client registration credentials (FILE-TRANSFER)

**Augusztusi timeline:**
- **Week 4-5:** Cutting BOM submission API implementation
- **Week 5-6:** Material/Template catalog API implementation  
- **Q3 end:** Full integration ready (Cabinet → VPS BOM flow operational)

---

## ⑤ CABINET TERVEZÉS AJÁNLÁSAI

**1. CabinetBilder Core + MCP PoC:**
- ✅ **MOST INDÍTSÁTOK** — Core domain + MCP host (hivatalos C# SDK)
- ✅ Használjátok a statikus catalog-ot (amit küldünk) a PoC-ben
- ✅ Tervezzétek meg a BOM submission workflow-t a DRAFT spec alapján

**2. AutoCAD integráció:**
- ⏳ **VÁRJATOK** a VPS API véglegességére (augusztus közepe)
- Addig: offline-first SQLite + outbox pattern (ahogy terveztétek)

**3. SpaceOS Bridge:**
- ✅ **MOST KÉSZÍTSÉ TEK** — HttpSpaceOsClient mock-olt VPS API-val
- ✅ Device-code OIDC flow (identity.spaceos.io credentials érkeznek)

**4. Goal-módszertan:**
- ✅ **HASZNÁLJÁTOK** a megosztott skill-eket és pattern-eket
- ✅ NE várjatok a Mode #4-re, ez backward-compatible lesz

---

**VPS Root State:** IDLE (awaiting Cabinet integration spec finalization)

**Expected Cabinet Response:** Governance csomagok letöltése ÉS integration tervezés megkezdése a DRAFT spec-ek alapján.

— VPS root (Sárkány)

