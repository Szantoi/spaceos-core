# CONDUCTOR Terminal TODO

> Utolsó frissítés: 2026-07-02
> Kontextus: JoineryTech CRM projekt folyamatban, Datahaven UI később

## Prioritás: CRITICAL (Aktív)

### 0. JoineryTech Wave 2 Implementation (Aktív)
**Státusz:** 🟡 75% COMPLETE (2026-07-02 21:15 UTC)
**Leírás:** 3/4 task befejezve, Frontend performance optimization in progress

**Wave 1 (Audit/Planning):**
- ✅ MSG-FRONTEND-089 DONE - UI/UX/Performance/A11y audit
- ✅ MSG-BACKEND-105 DONE - Backend architecture plan (5,200+ sor)

**Wave 2 (Implementation) - 75% DONE:**
- ✅ MSG-BACKEND-120 DONE - Mode #4 Program-Awareness (524 LOC TypeScript, 3 modulok)
- ✅ MSG-BACKEND-121 DONE - JoineryTech Week 1 Foundation (1,109 LOC, PostgreSQL + entities)
- ✅ MSG-DESIGNER-023 DONE - UI/UX Coordination (WCAG AAA guidance, design system alignment)
- 🟡 MSG-FRONTEND-092 IN PROGRESS - Performance P0 quick wins (store splitting + lazy loading)

**Coordination:**
- outbox MSG-CONDUCTOR-1007 (Wave 2 dispatch)
- outbox MSG-CONDUCTOR-1008 (Terminal activation monitoring)
- outbox MSG-CONDUCTOR-1009 (Mode #4 Monitor implementation done)
- outbox MSG-CONDUCTOR-1010 (Wave 2 progress: 3/4 tasks complete)

### 0a. JoineryTech CRM Backend Infrastructure Unblock
**Státusz:** ✅ DONE (2026-07-02)
**Leírás:** Backend Week 2 Application Layer manuálisan jóváhagyva, Week 3 Infrastructure task dispatch-elve.
- Root decision végrehajtva (MSG-CONDUCTOR-064)
- MSG-BACKEND-116: Week 3 Infrastructure Layer (INJECTED)
- NuGet fix Root-nál koordinálva

### 0b. JoineryTech Monitoring Active
**Státusz:** ✅ DONE (2026-07-02)
**Leírás:** Task subscription monitoring beállítva Frontend és Backend task-okra.
- MSG-FRONTEND-089: 3h timeout, Telegram alert (EXPIRED - DONE received)
- MSG-BACKEND-105: 5h timeout, Telegram alert (EXPIRED - DONE received)
- Subscription ID-k: outbox MSG-CONDUCTOR-1005

### 0c. Intelligent Conductor Briefing System
**Státusz:** ✅ DONE (2026-07-02 20:30 UTC)
**Inbox:** `terminals/conductor/inbox/2026-07-02_066_intelligent-briefing-system-implementation.md`
**Outbox:** `terminals/conductor/outbox/2026-07-02_1011_msg-066-intelligent-briefing-done.md`
**Leírás:** Mode #4 utolsó komponense - Intelligent Briefing System implementálva.

**Deliverables:**
- ✅ conductorBriefing.ts module (416 LOC TypeScript)
- ✅ Recent activity aggregation (2h window, minden terminál)
- ✅ Blocker identification (BLOCKED üzenetek automatikus detektálás)
- ✅ Priority determination (checkpoint-based prioritás algoritmus)
- ✅ Markdown briefing generation (Program Status + Recent Activity + Priorities + Blockers)
- ✅ Session start trigger integration (sessionStarter.ts)
- ✅ Manual testing successful (MSG-CONDUCTOR-BRIEFING-002 generated)

**Impact:**
- Conductor cold-start probléma megoldva
- Session start után azonnali kontextus (~5-10 perc megtakarítás)
- Mode #4 infrastructure COMPLETE ✅

**Ref:** MSG-MONITOR-005, ADR-053

---

## Prioritás: HIGH (Datahaven projekt - KÉSŐBB)

### 1. MSG-CONDUCTOR-060: JoineryTech UI Fejlesztés Koordináció
**Státusz:** PENDING - Datahaven UI projektre vár
**Inbox:** `terminals/conductor/inbox/2026-07-01_060_joinerytech-ui-fejleszt-s-koordin-ci-haiku-first-m.md`
**Kontextus:**
- 8 UI ötlet a Datahaven Dashboard-hoz (`docs/planning/ideas/2026-06-30_*.md`)
- Explorer kutatás szükséges: következő modul kiválasztás
- Designer + Frontend koordináció
- Haiku-first stratégia minden UI task-ra

**Acceptance Criteria:**
- [ ] Explorer task kiadva (következő modul kutatás a 8 ötletből)
- [ ] Designer koordinációs szerepkör megerősítve
- [ ] Frontend következő Datahaven UI task kiadva
- [ ] MCP használat monitoring

**FIGYELEM:** Ez Datahaven UI-hoz kapcsolódik, NEM a JoineryTech CRM projekthez. Feldolgozandó amikor a Datahaven UI projekt kerül fókuszba.

### 2. TaskMessageBox integráció kipróbálása
**Státusz:** PENDING
**Leírás:** A root terminál implementált egy új TaskMessageBox rendszert, amely SQLite DB-ben tárolja az üzeneteket és automatikusan renderel .md fájlokat.

**Új MCP toolok (amikor elérhetőek lesznek):**
- `tmb_create_task` - Új task létrehozása terminálnak
- `tmb_get_inbox` - Terminál inbox lekérdezése
- `tmb_get_outbox` - Terminál outbox lekérdezése
- `tmb_complete_message` - Task DONE-ra állítása
- `tmb_append_note` - Progress note hozzáadása élő taskhoz

**FONTOS:** Jelenleg az MCP auth token nem működik. A root terminál dolgozik a javításon.

### 3. Planning Pipeline koordináció
**Státusz:** ONGOING
**Leírás:** Folytatni a napi koordinációt (Datahaven UI projektre):
- `docs/planning/queue/` feldolgozása
- Termináloknak inbox kiadás
- DONE/BLOCKED kezelés

**Megjegyzés:** Jelenleg JoineryTech CRM fókusz van, Datahaven UI planning később folytatódik.

---

## Prioritás: MEDIUM

### 3. Terminal státusz figyelés
**Leírás:** Dashboard API-n keresztül ellenőrizd a terminálok állapotát:
```bash
curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  https://datahaven.joinerytech.hu/api/dashboard
```

---

## Referencia: Régi vs Új mailbox kezelés

**Régi módszer (fájl alapú):**
```bash
# Inbox fájl manuális írása
echo "---\nid: MSG-BACKEND-001\n..." > terminals/backend/inbox/file.md
```

**Új módszer (TaskMessageBox - ha működik az MCP):**
```
# MCP tool híváson keresztül
tmb_create_task(to: 'backend', title: '...', description: '...', priority: 'high')
```
