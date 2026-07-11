# SpaceOS Agent Pipeline - Talált hibák és javítások

> Folyamatosan frissített log az agent pipeline problémáiról és megoldásairól.
> Utolsó frissítés: 2026-07-02

---

## Javított hibák

### 1. yaml-parser.sh inline comment bug (2026-06-21 00:32)

**Tünet:** plan-scan.sh hibával leállt:
```
[: 5          # Ennyi ötlet felett select indul: integer expression expected
```

**Ok:** A yaml_get funkció nem szűrte ki az inline kommenteket (pl. `value: 5  # comment`)

**Javítás:** `scripts/yaml-parser.sh` - awk-ban hozzáadva:
```bash
gsub(/#.*/, "");  # Remove inline comments
```

**Fájl:** `/opt/spaceos/scripts/yaml-parser.sh`

---

### 2. plan-select.sh pending.md üres (2026-06-21 00:47)

**Tünet:** Pipeline log:
```
Plan-select HIBA: pending.md üres
```

**Ok:** A Claude API válasz nem mindig `---`-vel kezdődik (frontmatter). A `sed -n '/^---$/,$ p'` nem talált semmit.

**Javítás:** `scripts/plan-select.sh` - robusztusabb output kezelés:
1. Először próbálja meg a `---`-vel kezdődő blokkot
2. Ha nincs, kinyeri a YAML és markdown kódblokkokat
3. Fallback: frontmatter generálás + teljes output mentés

**Fájl:** `/opt/spaceos/scripts/plan-select.sh` (88-111. sor)

---

## Ismert problémák (még nem javított)

### 5. reviewer.sh Haiku 4 modell 404 hiba (2026-06-21 01:02)

**Tünet:** Review-reject üzenet:
```
Review hiba: 404 {"type":"error","error":{"type":"not_found_error","message":"model: claude-haiku-4-20250514"}}
```

**Ok:** A `claude -p --model haiku` parancs a `claude-haiku-4-20250514` modell ID-t próbálja használni, de ez nem érhető el.

**Workaround:** Root manuálisan feldolgozza a DONE-t (APPROVED_BY_ROOT státusz), stale review-reject törölve.

**Javasolt megoldás:**
- plan-config.yaml-ban explicit `reviewer.model_a: sonnet` és `reviewer.model_b: sonnet` beállítás
- Vagy: `haiku` → `claude-3-5-haiku-latest` explicit modell ID

**Státusz:** ✅ JAVÍTVA (2026-06-21 01:30)
- plan-config.yaml: `reviewer.model_a: sonnet` és `reviewer.model_b: sonnet` hozzáadva
- 13 stale review-reject fájl archiválva: `docs/mailbox/_stale_review_rejects_archive/`

---

### 3. FE terminál teszt setup hiányosságok

**Tünet:** 19/846 teszt fail (97.7% pass rate):
- ProductionPage tesztek: hiányzó MemoryRouter wrapper
- NestingViewer tesztek: CSS selector eltérés
- ToastProvider context hiány néhány tesztben

**Státusz:** FE terminál dolgozik rajta (2026-06-21 00:55)

**Javasolt megoldás:**
- Test utility wrapper létrehozása: `renderWithProviders(component)`
- Globális test setup frissítése: `src/test/setup.ts`

---

### 4. Conductor session context alacsony (12%)

**Tünet:** tmux capture-pane mutatja: "Context left until auto-compact: 12%"

**Ok:** Hosszú session, sok koordináció

**Javasolt megoldás:**
- Conductor session újraindítása ha < 10%
- `/compact` parancs használata

---

## Pipeline működési összefoglaló (2026-06-21 01:35)

| Komponens | Státusz | Megjegyzés |
|-----------|---------|------------|
| nightwatch.sh | ✅ OK | 2 percenként fut |
| plan-scan.sh | ✅ OK | Ötletek detektálása működik |
| plan-select.sh | ✅ JAVÍTVA | YAML/MD blokk kezelés robusztusabb |
| plan-debate.sh | ✅ OK | Konsenzus létrejött |
| reviewer.sh | ✅ JAVÍTVA | Sonnet reviewer (Haiku 404 fix) |
| pipeline.sh | ✅ OK | Docs frissítés működik |
| continuous-improvement.sh | ✅ ÚJ | 20 percenként változatos feladatok |
| yaml-parser.sh | ✅ JAVÍTVA | Inline comment stripping |

## Automatikus takarítás végrehajtva

- 13 stale review-reject fájl archiválva: `docs/mailbox/_stale_review_rejects_archive/`
- Ezek a Haiku 404 bug miatt keletkeztek, mielőtt a reviewer.sh config fix megtörtént

---

## Hot spot tracking (2026-06-21)

```
kernel-memory:2
joinery-memory:2
knowledge-adr:2
```

Ez azt jelenti, hogy ezek a témák gyakran előkerülnek az ötletekben.

---

### 6. Root Outbox → Conductor Inbox Routing Hiányzik (2026-07-01)

**Prioritás:** MEDIUM
**Státusz:** ✅ JAVÍTVA (2026-07-01 19:45)
**Assignee:** Root terminál

**Tünet:** Root terminál outbox válasza (`type: response`, `to: conductor`) nem triggerel automatikus értesítést a Conductor-nak. A terminálok várakoznak, de nem kapnak notifikációt.

**Reprodukálás:**
1. Root ír outbox-ba: `/opt/spaceos/terminals/root/outbox/2026-07-01_001_*.md`
2. Frontmatter: `to: conductor`, `type: response`
3. Conductor NEM kap SSE wake event-et
4. Conductor session idle marad, várakozik

**Workaround:** Manuális `tmux send-keys -t spaceos-conductor "..." Enter` nudge.

**Érintett fájlok:**
- `spaceos-nexus/knowledge-service/src/inboxWatcher.ts` — Csak inbox-ot figyel
- `spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts` — DONE routing, de response nincs
- `spaceos-nexus/knowledge-service/src/pipeline/messageRouter.ts` — MESSAGE_ROUTER disabled

**Megoldás:** `watchResponse.ts` modul hozzáadva a Nightwatch pipeline-hoz:
- Figyeli a `type: response` outbox üzeneteket
- Ha van `to:` mező, SSE event emit (`response:routed`)
- Ha a target terminál session fut, tmux nudge küldés
- 5 perc cooldown per response (duplikált nudge elkerülése)
- Fájl: `spaceos-nexus/knowledge-service/src/pipeline/watchResponse.ts`

**EventBus bővítés:**
- Új event típus: `response:routed` hozzáadva az eventBus.ts-hez

**Acceptance Criteria:**
- [x] Root response outbox automatikusan triggerel Conductor értesítést
- [x] SSE wake event tüzel a target terminálnak
- [x] Nincs szükség manuális nudge-ra

---

### 7. Conductor Nem Kap Triggert Queued Task Dispatch-ra (2026-07-01)

**Prioritás:** MEDIUM
**Státusz:** ✅ JAVÍTVA (2026-07-01 19:40)
**Assignee:** Root terminál
**Kapcsolódik:** ISSUE-006

**Tünet:** Ha van queued task a focus queue-ban, a Conductor nem kap automatikus triggert hogy dispatch-olja őket. A terminálok idle-ben maradnak amíg manuális nudge nem érkezik.

**Reprodukálás:**
1. Focus queue-ban van 3 queued task (Frontend, Architect)
2. Conductor session idle
3. Nincs automatikus dispatch — terminálok várakoznak

**Workaround:** Manuális `tmux send-keys -t spaceos-conductor "..." Enter` nudge.

**Gyökérok:** A pipeline "pull" alapú (Nightwatch poll-ol), de nincs "push" mechanizmus ami értesítené a Conductort új queued task-ról.

**Megoldási javaslatok:**

**A) Nightwatch bővítés** — `watch-queue.sh` ami triggereli a Conductort ha van queued task és a target terminál idle.

**B) SSE Event** — Focus queue változáskor SSE event a Conductornak.

**C) AutonomousDev bővítés** — 30 percenként ne csak a focus file-t nézze, hanem a queued task-okat is.

**Acceptance Criteria:**
- [x] Conductor automatikusan dispatch-ol ha van queued task és a target terminál idle
- [x] Nem kell manuális nudge a párhuzamos task-ok indításához

**Megoldás:** `watchQueue.ts` modul hozzáadva a Nightwatch pipeline-hoz:
- Ellenőrzi a focus queue-t minden Nightwatch ciklusban
- Ha van queued task és a target terminál idle → nudge Conductor
- 5 perc cooldown per task (duplikált nudge elkerülése)
- Fájl: `spaceos-nexus/knowledge-service/src/pipeline/watchQueue.ts`

---

## Új hibák és javítások (2026-07-02)

### 8. TypeScript Import Extension Hibák (2026-07-02 04:42)

**Prioritás:** 🔴 CRITICAL
**Státusz:** ✅ JAVÍTVA (2026-07-02 04:42)
**Assignee:** Root terminál

**Tünet:** Knowledge-service crash-elt induláskor:
```
Error: Cannot find module './codegen/index.js'
Require stack:
- /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp.ts
```

**Érintett fájlok:**
- `spaceos-nexus/knowledge-service/src/mcp.ts:109`
- `spaceos-nexus/knowledge-service/src/codegen/index.ts:22`

**Probléma:** TypeScript import-okban `.js` extension használata:
```typescript
// HIBÁS
} from './codegen/index.js';

// HELYES
} from './codegen/index';
```

**Következmény:**
- Knowledge-service nem tudott elindulni
- 3456-os port nem hallgatott
- MCP API endpoints nem érhetők el
- Memory save endpoint 404/502 hibát adott

**Javítás:** Mindkét fájlban eltávolítva a `.js` extension.

**Megelőzés:**
- ESLint szabály: `import/extensions: never` TypeScript fájlokra
- Pre-commit hook: grep ellenőrzés `.js` import-okra
- CI build step: TypeScript compile hibák blokkolják a merge-t

**Részletes issue:** `.github/issues/2026-07-02_001_typescript-import-extensions.md`

**Acceptance Criteria:**
- [x] Knowledge-service elindult hibátlanul
- [x] 3456-os port hallgat
- [x] Health endpoint válaszol
- [x] Memory save API működik (200 OK)

---

### 9. AutonomousDev Manual Control Ütközés (2026-07-02 04:50)

**Prioritás:** 🟡 HIGH
**Státusz:** ⚠️ WORKAROUND (2026-07-02 05:15)
**Assignee:** Root terminál

**Tünet:** Conductor terminál folyamatosan újraindult 30 percenként, felülírva a manuális irányítást.

**Reprodukálás:**
1. Root ír inbox üzenetet Conductor-nak (pl. JoineryTech feladat)
2. AutonomousDev 30 perc múlva cold-start-olja a Conductor-t más feladattal
3. Manuális inbox üzenet feldolgozatlan marad
4. Conductor ismét és ismét ugyanazt az autonomous feladatot kapja

**Nightwatch log:**
```
04:14:43 [AutonomousDev] Cycle 22: Conductor started with optimized prompt
04:40:43 [AutonomousDev] Cycle 1: Conductor started with optimized prompt
05:10:13 [AutonomousDev] Cycle 2: Conductor started with optimized prompt
```

**Gyökérok:** AutonomousDev nem érzékeli:
- Van-e UNREAD inbox üzenet
- Fut-e manuális session
- Aktív-e manual control mode

**Workaround (alkalmazva):**
```bash
curl -X POST http://localhost:3456/api/autonomous/stop
# → running: false
```

**Javasolt fix:**
1. Control mode awareness: `manual | autonomous | hybrid`
2. UNREAD inbox check → skip autonomous cycle
3. Session tagging: `startedBy: 'manual' | 'autonomous'`
4. `/api/control/mode` endpoint létrehozása

**Részletes issue:** `.github/issues/2026-07-02_002_autonomous-dev-manual-control-conflict.md`

**Acceptance Criteria:**
- [ ] Manual mode: autonomous NEM indít Conductor-t
- [ ] Hybrid mode: autonomous skip-el ha UNREAD inbox van
- [ ] Session metadata tartalmazza a `startedBy` mezőt
- [ ] `/api/control/mode` API működik

---

### 10. Datahaven API 502 Bad Gateway (2026-07-02 04:32)

**Prioritás:** 🟡 MEDIUM
**Státusz:** ⚠️ INTERMITTENT
**Assignee:** Infra terminál

**Tünet:** Datahaven Dashboard API endpoint-ok időnként 502-t adnak vissza.

**Példa:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status
# → 502 Bad Gateway (nginx)
```

**Lehetséges okok:**
1. Nginx upstream timeout nincs beállítva
2. Backend process crash/restart (unhandled exception)
3. Port confusion (3456 vs 3457)

**Megfigyelések:**
- Backend process fut (PID 1784359, port 3457)
- Knowledge-service külön porton (3456)
- Nginx upstream config ismeretlen

**Diagnosztika teendők:**
```bash
# 1. Nginx config ellenőrzés
cat /etc/nginx/sites-available/datahaven.joinerytech.hu | grep upstream

# 2. Backend logs
tail -100 /opt/spaceos/datahaven-web/logs/server.log

# 3. Nginx error log
tail -100 /var/log/nginx/error.log | grep datahaven
```

**Javasolt fix:**
1. Nginx timeout növelés (proxy_read_timeout, proxy_send_timeout)
2. Backend health check + alerting
3. PM2 process manager (auto-restart)
4. Unhandled exception logging + Telegram alert

**Workaround:** Lokális knowledge-service API használata (localhost:3456)

**Részletes issue:** `.github/issues/2026-07-02_003_datahaven-api-502-bad-gateway.md`

**Acceptance Criteria:**
- [ ] Nginx config has proper timeouts
- [ ] Backend runs on correct port
- [ ] Health check consistently returns 200 OK
- [ ] Load test: 100 req/s → no 502
- [ ] PM2 auto-restart works

---

---

### 11. tmux Enter Command Buffering Issue (2026-07-02 07:42)

**Prioritás:** 🟡 MEDIUM (Documentation)
**Státusz:** ✅ DOCUMENTED
**Assignee:** Root terminál

**Tünet:** `tmux send-keys -t <session> Enter` parancs **elnyelődik** és csak sortörés lesz a session-ben, nem triggerel Claude Code prompt submit-et.

**Reprodukálás:**
```bash
# HIBÁS (nem működik)
tmux send-keys -t spaceos-conductor "Folytasd" Enter
# → csak sortörés, Claude nem válaszol

# HIBÁS (nem működik)
tmux send-keys -t spaceos-conductor Enter Enter
# → csak sortörés, Claude nem submit-ol
```

**Gyökérok:** Tmux buffering mechanizmus miatt az Enter key event azonnal elveszik ha:
- Nincs késleltetés (sleep) a parancs előtt
- Vagy nincs hexa kód `-H 0x0D` formátum

**Helyes megoldások:**

```bash
# 1. HEXA kód használat (AJÁNLOTT)
tmux send-keys -t spaceos-conductor -H 0x0D 0x0D

# 2. Sleep késleltetés
sleep 2 && tmux send-keys -t spaceos-conductor Enter Enter
```

**Lesson (2026-07-02):** 6+ sikertelen próbálkozás után user tanítása alapján megoldva.

**Dokumentáció:**
- Root MEMORY.md frissítve tmux Enter szabállyal
- ISSUES.md #11 bejegyzés

**Acceptance Criteria:**
- [x] Root MEMORY.md tartalmazza a helyes tmux Enter módszert
- [x] ISSUES.md dokumentálja a problémát
- [ ] Nightwatch watch-stuck.sh script frissítése hexa kóddal

---

## Pipeline működési összefoglaló (2026-07-02 07:30)

| Komponens | Státusz | Megjegyzés |
|-----------|---------|------------|
| nightwatch.sh | ✅ OK | 2 percenként fut |
| knowledge-service | ✅ JAVÍTVA | TypeScript import fix, port 3456 hallgat |
| memory save API | ✅ OK | POST /api/memories/save → 200 OK |
| autonomousDev | ⚠️ STOPPED | Manual control mode (workaround) |
| datahaven API | ⚠️ INTERMITTENT | 502 errors időnként |
| conductor | ✅ RUNNING | Manual session, JoineryTech dispatched |

## JoineryTech Projekt Státusz (2026-07-02 06:54)

**Conductor sikeres dispatch:**
- MSG-FRONTEND-089: UI/UX, Performance & A11y Audit (MEDIUM, queued)
- MSG-BACKEND-105: Backend API Architecture Design (HIGH, queued)

**Focus queue:**
- 2 queued task
- 0 blocked
- 0 active (idle system)

**Várható deliverable-ek:**
- Frontend audit riport: ~2-3 nap
- Backend architektúra terv: ~3-5 nap

---

