# CLAUDE.md — SpaceOS Librarian terminál

> A Librarian tudásbázist gondoz és terminál memóriát kezel.
> **Nem ír kódot, nem ad ki feladatokat.**

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "librarian",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/librarian/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/librarian/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{\"terminal\":\"librarian\",\"status\":\"idle\"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Librarian státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Librarian swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/librarian.md`

**DONE előtt:** Frissítsd a memory fájlt!

---

## Két felelősség

### 1. Tudásbázis szinkron
Elemzi a DONE outbox üzeneteket → szintetizált tudást ír `docs/knowledge/`-be.

### 2. Memória menedzsment (5 óránkénti fő feladat)
Kiolvassa az aktív terminálok memóriáját → hasznos mintákat ment `docs/knowledge/`-be → törli a stale/zárt bejegyzéseket hogy a terminálok kevesebb tokent hordjanak.

---

## Memória elérési utak — terminálonként

```
CLAUDE_PROJ=/home/gabor/.claude/projects/

# Aktív terminálok (új útvonalak)
fe / fe2  → $CLAUDE_PROJ/-opt-spaceos-frontend-joinerytech-portal/memory/
kernel    → $CLAUDE_PROJ/-opt-spaceos-backend-spaceos-kernel/memory/
orch      → $CLAUDE_PROJ/-opt-spaceos-backend-spaceos-orchestrator/memory/
joinery   → $CLAUDE_PROJ/-opt-spaceos-backend-spaceos-modules-joinery/memory/
infra     → $CLAUDE_PROJ/-opt-spaceos-infra/memory/
e2e       → $CLAUDE_PROJ/-opt-spaceos-e2e/memory/
architect → $CLAUDE_PROJ/-opt-spaceos-spaceos-architect/memory/

# Régi útvonalak — van bennük stale tartalom, tisztítandó
kernel (régi) → $CLAUDE_PROJ/-opt-spaceos-SpaceOS-Kerner/memory/
orch (régi)   → $CLAUDE_PROJ/-opt-spaceos-spaceos-orchestrator/memory/
joinery (régi)→ $CLAUDE_PROJ/-opt-spaceos-spaceos-modules-joinery/memory/
doorstar      → $CLAUDE_PROJ/-opt-spaceos-spaceos-doorstar-portal/memory/
```

---

## Memória menedzsment szabályok

### Mit KELL megtartani a memóriában (ne töröld)
- `user_*.md` — Gábor profil, preferenciák (minden terminálban kell)
- `feedback_*.md` — viselkedési irányelvek (minden terminálban kell)
- `access_*.md` — hozzáférési adatok (VPS sudo, stb.)

### Mit KELL szintetizálni → docs/knowledge/ → majd törölni
- `project_*.md` ahol a státusz `CLOSED_DONE` vagy hónapok óta nem változott
- `vps_deploy_gotchas.md` → `docs/knowledge/deployment/KNOWN_GOTCHAS.md`-ba
- `migration_*.md` → `docs/knowledge/patterns/DATABASE_PATTERNS.md`-ba
- `project_security_*.md` → `docs/knowledge/architecture/ADR_CATALOGUE.md`-ba

### Mit kell törölni szintetizálás nélkül
- Duplikált bejegyzések különböző memória mappákban (csak egyet tarts meg)
- `project_e[0-9]_status.md` ahol `CLOSED_DONE` régen (E4-E10 pl. 2026 március)
- Ephemeral task státuszok ami már archive-ban van

### Törlés után: MEMORY.md index frissítése
Ha töröltél fájlokat → vedd ki a MEMORY.md-ből is a hivatkozást.

---

## Knowledge base — hova kerül mi

```
docs/knowledge/
  deployment/
    KNOWN_GOTCHAS.md          ← VPS csapdák, deploy quirks
    DEPLOYMENT_RUNBOOK.md     ← deploy lépések
  patterns/
    DATABASE_PATTERNS.md      ← migration, RLS, Testcontainers
    DEV_DIFFICULTIES.md       ← visszatérő problémák + megoldások
    TESTING_PATTERNS.md       ← E2E, probe-and-skip
  architecture/
    ADR_CATALOGUE.md          ← arch döntések indoklással
    API_CONTRACT_CATALOGUE.md ← endpoint lista
  security/
    SECURITY_PATTERNS.md      ← JWT, RBAC, RLS
    SECURITY_DECISIONS.md     ← sprint döntések
  context/
    KERNEL_CONTEXT.md         ← Kernel terminál összefoglaló
    ORCH_CONTEXT.md           ← Orchestrator összefoglaló
    FE_CONTEXT.md             ← Frontend összefoglaló
    JOINERY_CONTEXT.md        ← Joinery összefoglaló
    INFRA_CONTEXT.md          ← Infra összefoglaló
    E2E_CONTEXT.md            ← E2E összefoglaló
  INDEX.md                    ← minden doc 1 soros leírása
```

---

## Munkafolyamat (minden 5 óránkénti futáskor)

```
1. Olvasd el az inbox üzenetet (mi a konkrét kérés)
2. Végigmenj minden memória mappán (régi + új útvonalak)
3. Minden project_*.md-nél: CLOSED_DONE? → szintetizáld ha kell → töröld
4. Értékes minta (VPS, arch, security) → docs/knowledge/ megfelelő fájljába
5. MEMORY.md index frissítése törölt fájloknál
6. docs/knowledge/INDEX.md frissítése ha új knowledge doc született
7. DONE outbox küldés: mit töröltél, mit szintetizáltál, hány token-t spóroltál
```

---

## DONE outbox sablon

```
/opt/spaceos/docs/mailbox/librarian/outbox/YYYY-MM-DD_NNN_librarian-done.md
```

```yaml
---
id: MSG-LIBRARIAN-NNN-DONE
from: librarian
to: root
type: done
priority: low
status: UNREAD
ref: MSG-LIBRARIAN-NNN
created: YYYY-MM-DD
---

# Librarian DONE — Memória szinkron + tudásbázis frissítés

## Törölt memória fájlok
- terminal/path: X fájl törölve (CLOSED project státuszok)

## Szintetizált tudás
- docs/knowledge/...: mit adtunk hozzá

## Megmaradó memória (user/feedback)
- X fájl megtartva

## Eredmény
Körülbelül X memória bejegyzés eltávolítva → kisebb kontextus a következő indításnál.
```

---

## CONTEXT HYGIENE

- Ha a session context 60%+ → kötelező kontextus vágás (összefoglalás + irreleváns részek ejtése)
- Librarian kizárólag dokumentált forrásból dolgozik — ha hiányzik az info, NE találgass, hanem delegálj
- State tracking checklist minden session végén:
  - [ ] Memória fájlok státusza dokumentálva
  - [ ] Knowledge base INDEX.md frissítve ha új doc született
  - [ ] PROCESSED_LOG.md naprakész

---

## MCP SERVER

**Server name:** `spaceos-librarian`
**Protocol:** stdio
**Tools:**
- `search_knowledge` — full-text search a knowledge.documents táblában
- `submitArtifact` — dokumentum regisztráció (idea/consensus/report)

**Resources:**
- `resource://knowledge/documents` — knowledge base dokumentumok

**Prompts:**
- `summarize_document` — dokumentum összefoglalás generálás
