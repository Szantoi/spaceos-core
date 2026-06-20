# Terminal CLAUDE.md Template — Datahaven Integration

> **Használat:** Minden terminál CLAUDE.md fájljába be kell illeszteni az alábbi szakaszokat.
> Cseréld ki a `<TERMINAL_NAME>` és `<TERMINAL_PATH>` placeholdereket a konkrét értékekkel.

---

## SESSION INDÍTÁSI RUTIN

**Minden session elején automatikus lépések:**

```bash
# 1. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<TERMINAL_NAME>",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 2. Inbox ellenőrzés — van-e új feladat?
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/<TERMINAL_NAME>/inbox/

# 3. Ha van UNREAD üzenet → olvasd el, állítsd READ státuszra
# 4. Folytasd a feladatot (CODE→BUILD→TEST→REVIEW→SECURITY→E2E/TESTER→OUTBOX)
```

---

## SESSION LEZÁRÁSI RUTIN

**Minden session végén (DONE vagy BLOCKED outbox írása után):**

```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<TERMINAL_NAME>",
    "status": "idle"
  }'
```

**FONTOS:** Soha ne hagyd WORKING státuszban a terminált session vége után — ez félrevezető a monitoring-ban.

---

## DATAHAVEN DASHBOARD — MONITORING

> **URL:** https://datahaven.joinerytech.hu
> **Auth Token:** `dev-token-spaceos-dashboard-2026`

A Datahaven Dashboard a SpaceOS agent infrastruktúra központi monitoring felülete. 4 fő oldala van:

| Oldal | URL | Mit látsz |
|---|---|---|
| **Dashboard** | `/` | Minden terminál állapota (WORKING/IDLE), inbox/outbox metrikák, aktív sessionök |
| **Kanban** | `/kanban` | Dual-track board: Discovery (Planning pipeline) + Delivery (Terminal swimlanes) |
| **Planning** | `/planning` | 5-stage planning pipeline: Idea → Selected → Debate → Consensus → Queue |
| **Projects** | `/projects` | Gantt timeline + projekt lista (8 hónapos ablak: -2 hónap / +6 hónap) |

### Dashboard használat session közben

**Mit látsz rólad a Dashboard-on:**
- Terminál neve (<TERMINAL_NAME>)
- Státusz (WORKING/IDLE)
- Inbox üzenetek száma (UNREAD/összes)
- Outbox üzenetek száma
- Utolsó aktivitás időpontja

**Session közben státusz frissítés:**
Ha hosszabb feladat közben konkrét lépésnél vagy, frissítheted a `currentTask` mezőt:

```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<TERMINAL_NAME>",
    "status": "working",
    "currentTask": "Building project (0 errors so far)"
  }'
```

Ez opcionális — csak ha van értelme jelezni konkrét lépést (pl. hosszú teszt futtatás, build, deployment).

### Dashboard API-k elérhetők számodra

| Endpoint | Method | Mire használható |
|---|---|---|
| `/api/dashboard` | GET | Összes terminál metrikái (ha kíváncsi vagy ki dolgozik most) |
| `/api/kanban/snapshot` | GET | Discovery + Delivery board teljes snapshot |
| `/api/planning/snapshot` | GET | Planning pipeline 5 stage teljes snapshot |
| `/api/terminal/status` | POST | Saját státusz regisztráció (WORKING/IDLE) |
| `/health` | GET | Backend health check |

**Auth:** Minden API híváshoz `Authorization: Bearer dev-token-spaceos-dashboard-2026` header szükséges.

---

## WORKFLOW SZABÁLYOK — EMLÉKEZTETŐ

**Kötelező pipeline minden feladatra:**
```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → E2E → TESTER → OUTBOX
```

1. **INBOX READ** — frontmatter `status: UNREAD` → `READ`
2. **CODE** — implementáció
3. **BUILD** — 0 error, 0 warning
4. **TEST** — unit + integration, minden teszt zöld
5. **REVIEW** — CLAUDE.md szabályok teljesülnek
6. **SECURITY** — OWASP, RLS, auth, input validation
7. **E2E** — backend endpoint-ok tesztelve valódi stack-en
8. **TESTER** — frontend érintés esetén Playwright validáció
9. **OUTBOX** — DONE vagy BLOCKED üzenet kiírása

**Ha bármelyik lépésnél probléma van** → **BLOCKED** outbox, ne próbáld tovább erőltetni.

**Teljes workflow leírás:** `/opt/spaceos/docs/WORKFLOW.md`

---

## MIGRATION NOTE (2026-06-20)

Ez a terminál a **Datahaven Dashboard integrációs rollout** részeként frissült.

**Migration status:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`

**Változások:**
1. Session startup ritual bővítve Datahaven státusz regisztrációval
2. Session shutdown ritual bővítve IDLE státusz regisztrációval
3. Dashboard API reference hozzáadva
4. WORKFLOW.md frissítve központi Datahaven szekcióval

**Ha kérdésed van** a Datahaven használatáról, olvasd el:
- `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md` — teljes migration guide
- `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz

---

## PÉLDA SESSION — TELJES FOLYAMAT

**1. Session indítás:**
```bash
# Datahaven: WORKING státusz
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<TERMINAL_NAME>","status":"working","currentTask":"Checking inbox"}'

# Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/<TERMINAL_NAME>/inbox/
```

**2. Feladat végrehajtás:**
```bash
# Olvasd az inbox üzenetet
cat docs/mailbox/<TERMINAL_NAME>/inbox/2026-06-20_001_task.md

# Állítsd READ státuszra (frontmatter: status: READ)
# Implementáld a feladatot (CODE→BUILD→TEST→REVIEW→SECURITY→E2E/TESTER)
```

**3. Session lezárás:**
```bash
# Írj DONE vagy BLOCKED outbox üzenetet
cat > docs/mailbox/<TERMINAL_NAME>/outbox/2026-06-20_001_task-done.md << 'EOF'
---
id: MSG-<TERMINAL_NAME>-001
from: <TERMINAL_NAME>
to: root
type: done
priority: high
status: UNREAD
created: 2026-06-20
---

## Tárgy
Task befejezve

## Eredmények
- Build: 0 error, 0 warning
- Tests: 15/15 passed
- E2E: validated
...
EOF

# Datahaven: IDLE státusz
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<TERMINAL_NAME>","status":"idle"}'
```

---

## TEMPLATE HASZNÁLAT

**Terminálonként teendő:**
1. Nyisd meg a terminál CLAUDE.md fájlját: `<TERMINAL_PATH>/CLAUDE.md`
2. Keresd meg a "SESSION INDÍTÁSI RUTIN" szakaszt
3. Cseréld le vagy egészítsd ki az alábbi tartalommal
4. Cseréld ki minden `<TERMINAL_NAME>` előfordulást a konkrét terminál nevével (pl. `kernel`, `orch`, `fe`)
5. Cseréld ki minden `<TERMINAL_PATH>` előfordulást a konkrét terminál útvonalával (pl. `/opt/spaceos/spaceos-kernel`)
6. Ellenőrizd hogy a curl parancsok működnek (tesztelj egyet)
7. Commit a változtatást

**Terminál nevek listája:**
- `root` — /opt/spaceos
- `conductor` — /opt/spaceos/spaceos-conductor
- `kernel` — /opt/spaceos/spaceos-kernel
- `orch` — /opt/spaceos/spaceos-orchestrator
- `fe` — /opt/spaceos/spaceos-doorstar-portal
- `joinery` — /opt/spaceos/spaceos-modules-joinery
- `abstractions` — /opt/spaceos/spaceos-modules-abstractions
- `cutting` — /opt/spaceos/spaceos-modules-cutting
- `inventory` — /opt/spaceos/spaceos-modules-inventory
- `procurement` — /opt/spaceos/spaceos-modules-procurement
- `sales` — /opt/spaceos/spaceos-modules-sales
- `identity` — /opt/spaceos/spaceos-modules-identity
- `infra` — /opt/spaceos/infra
- `e2e` — /opt/spaceos/e2e
- `tester` — /opt/spaceos/tester
- `architect` — /opt/spaceos/spaceos-architect
- `librarian` — /opt/spaceos/spaceos-librarian
- `nexus` — /opt/spaceos/spaceos-nexus

---

**Template verzió:** 2026-06-20 v1
**Kapcsolódó dokumentumok:**
- `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- `docs/WORKFLOW.md` (Datahaven Dashboard szakasz)
