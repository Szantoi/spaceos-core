# SpaceOS — Root Orchestration Workflow

> Ez a dokumentum a **teljes SpaceOS multi-terminál workflow**-t definiálja.
> Minden terminál ezt követi: root · kernel · orchestrator · portal · fe · joinery · abstractions · cutting · inventory · procurement · infra · e2e · tester · librarian · architect

---

## ⚠️ KÖTELEZŐ PIPELINE — SOHA NEM HAGYHATÓ KI

Ez a 9 lépés minden feladatra kötelező. Ha bármelyik lépés kimarad, a feladat **nem tekinthető befejezettnek.**

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → E2E → TESTER → OUTBOX
```

| Lépés | Mit jelent | Mikor kész | Kihagyható? |
|---|---|---|---|
| **1. INBOX READ** | Feladat elolvasása, státusz READ-re állítása | Frontmatter `status: READ` | ❌ NEM |
| **2. CODE** | Implementáció | — | ❌ NEM |
| **3. BUILD** | Build futtatás | 0 error, 0 warning | ❌ NEM |
| **4. TEST** | Unit + integration tesztek írása + futtatás | Minden teszt zöld | ❌ NEM |
| **5. REVIEW** | Önellenőrzés a CLAUDE.md szabályok alapján | Layer rules, naming, no TODO/FIXME | ❌ NEM |
| **6. SECURITY** | Biztonsági ellenőrzés | OWASP, RLS, auth, input validation | ❌ NEM |
| **7. E2E** | Backend E2E validáció (API szinten) | Minden érintett endpoint 2xx, adat perzisztál | ❌ NEM — backend module lezáráshoz kötelező |
| **8. TESTER** | TESTER terminál frontend validációja | Playwright MCP vagy Claude in Chrome: UI elemek működnek | ❌ NEM — ha a module-nak van frontend érintése |
| **9. OUTBOX** | Válaszüzenet kiírása | Fájl létezik az outbox-ban, E2E + TESTER eredmény dokumentálva | ❌ NEM |

### Elakadás esetén

Ha bármelyik lépésnél probléma van → **BLOCKED** státusszal outbox üzenetet kell írni, nem folytatni.

```
INBOX READ → CODE → BUILD ← HIBA → OUTBOX (BLOCKED) → STOP, várj válaszra
```

---

## ⚠️ TESZTELÉSI KAPU — Lezárás előtt kötelező

### Szabály 1: Backend E2E gate

> **Minden implementált backend endpoint E2E-tesztelve legyen a DONE outbox előtt.**

| Mit jelent | Hogyan |
|---|---|
| Minden új API endpoint valódi HTTP hívással tesztelt | `curl` vagy TESTER-terminál Playwright session |
| CRUD teljes körű: CREATE → READ visszaolvas → státusz megjelenik | Lifecycle teszt (nem csak egységteszt) |
| JWT auth + RLS érvényesül a valódi stack-en | Keycloak token-nel hívva (nem mock) |
| Adatok perzisztálnak DB-ben | GET utána GET: ugyanaz az adat |

**E2E gate alól NEM mentesít:**
- ❌ "Unit teszt megvan" (unit teszt nem a stack-et teszteli)
- ❌ "Integration teszt InMemory EF-fel" (valódi DB-t kell tesztelni)
- ❌ "Más terminál majd teszteli" (az implementáló terminál felelős az E2E gate-ért)

**Elfogadott E2E módszerek:**
- ✅ TESTER terminál Playwright session (preferált)
- ✅ Root terminál `curl` hívásokkal (egyszerű esetekre)
- ✅ E2E test suite (`/opt/spaceos/e2e`) futtatása az érintett endpointokon

---

### Szabály 2: TESTER frontend gate

> **Ha a module-hoz frontend elem is kerül (új oldal, új form, új komponens), a TESTER terminál leteszteli mielőtt a module lezárul.**

| Frontend érintettség | TESTER feladat |
|---|---|
| Új UI oldal / route | Playwright MCP: megnyílik, adatok betöltenek, nincs console error |
| Új form (POST) | Sikeres beküldés + visszajelzés UI-n |
| Új lista / táblázat | Adatok megjelennek, pagination működik |
| Új státusz badge / workflow state | Vizuális állapot helyes |
| Mobil viewport | 375px-en is működik (BUG-013 tanulság) |

**TESTER gate alól NEM mentesít:**
- ❌ "A backend tesztelve van" (frontend render külön verifikiálandó)
- ❌ "Portal unit teszt megvan" (böngészős interakció más mint unit teszt)

**TESTER gate NEM szükséges:**
- ✅ Pure backend modul, nincs Portal/FE érintés (pl. CUTTING Phase 1+2 — csak API)
- ✅ Refactor amely a UI-t nem érinti

---

### Szabály 3: Root felelőssége

Root az DONE elfogadáskor ellenőrzi:

```
checklist:
  [ ] build: 0 error, 0 warning
  [ ] unit tests: összes zöld
  [ ] E2E: backend endpoint-ok tesztelve valódi stack-en
  [ ] TESTER: frontend érintés esetén validálva
  [ ] security: RLS, auth, OWASP
```

Ha az E2E vagy TESTER gate hiányzik → **visszadobás**, nem elfogadás.

---

---

---

## Terminál architektúra

```
┌──────────────────────────────────────────────────────────────────┐
│  ROOT terminál  (/opt/spaceos)                  [persistent]     │
│  Feladat: tervezés, koordináció, vision→epic                     │
│  Olvas:  docs/mailbox/*/outbox/                                  │
│  Ír:     docs/mailbox/*/inbox/, tasks/, Codebase_Status.md       │
└──┬──────────┬──────────┬──────────┬──────────┬───────────────────┘
   │          │          │          │           │
   ▼          ▼          ▼          ▼           ▼
KERNEL     ORCH       JOINERY    CUTTING    INVENTORY   ... (on-demand)
:5000      :3000      :5002      :5005      :5004
ABSTRACTIONS  PROCUREMENT  PORTAL  FE  INFRA  E2E  TESTER

┌──────────────────────────────────────────────────────────────────┐
│  ARCHITECT terminál  (/opt/spaceos/spaceos-architect/)           │
│  [persistent — mindig fut, Gábor bármikor kérdezhet]            │
│  Feladat: konzultatív arch partner, spec előkészítés             │
│  Olvas:  docs/knowledge/, codebase                               │
│  Ír:     mailbox/architect/outbox/ (Root-nak vagy Gábornak)      │
│  Nem ír kódot. Nem küld termináloknak inbox üzenetet.            │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│  LIBRARIAN terminál  (/opt/spaceos/spaceos-librarian/)           │
│  [on-demand — Root kiadja, feldolgoz, DONE, leáll]              │
│  Feladat: tudásbázis gondozás, archívum szintézis               │
│  Olvas:  docs/mailbox/*/archive/, */outbox/                      │
│  Ír:     docs/knowledge/ (INDEX, ADR, patterns, context stb.)    │
│  Nem ír kódot. Csak Root-tól kap feladatot.                      │
└──────────────────────────────────────────────────────────────────┘
```

### Teljes terminál lista

| Terminál | Könyvtár | Port | Típus | Szerepkör |
|---|---|---|---|---|
| ROOT | `/opt/spaceos/` | — | persistent | Koordináció, tervezés |
| KERNEL | `spaceos-kernel/` | 5000 | on-demand | .NET 8 backend |
| ORCH | `spaceos-orchestrator/` | 3000 | on-demand | Node.js BFF |
| PORTAL | `design-portal/` | — | on-demand | React (Turborepo) |
| FE | `spaceos-doorstar-portal/` | — | on-demand | Doorstar brand portal |
| JOINERY | `spaceos-modules-joinery/` | 5002 | on-demand | Joinery modul |
| ABSTRACTIONS | `spaceos-modules-abstractions/` | 5003 | on-demand | Abstractions modul |
| CUTTING | `spaceos-modules-cutting/` | 5005 | on-demand | Cutting modul |
| INVENTORY | `spaceos-modules-inventory/` | 5004 | on-demand | Inventory modul |
| PROCUREMENT | `spaceos-modules-procurement/` | 5006 | on-demand | Procurement modul |
| INFRA | `infra/` | — | on-demand | VPS deploy, nginx, systemd |
| E2E | `e2e/` | — | on-demand | Vitest E2E teszt suite |
| TESTER | `tester/` | — | on-demand | Manuális + Playwright tesztelés |
| LIBRARIAN | `spaceos-librarian/` | — | on-demand | Tudásbázis gondozás |
| **ARCHITECT** | `spaceos-architect/` | — | **persistent** | Konzultatív arch partner |

---

## Fő folyamat

```
1. VISION         Root olvassa a docs/vision/ tartalmát
                  ↓
2. EPIC TERVEZÉS  Root lebontja epic-ekre
                  ↓
3. KIOSZTÁS       Root → inbox üzenetet ír a megfelelő projektnek
                  ↓
4. VÉGREHAJTÁS    Projekt terminál olvassa inbox → végrehajtja (CODE→TEST→REVIEW→SECURITY)
                  ↓
5. VISSZAJELZÉS   Projekt terminál → outbox üzenetet ír
                  ↓
6. ELLENŐRZÉS     Root olvassa outbox → dönt (elfogadja / visszadobja)
                  ↓
7. LEZÁRÁS        Root frissíti BACKLOG.md → következő epic
```

---

## Mailbox rendszer

### Könyvtár struktúra

```
docs/mailbox/
├── kernel/           inbox/ outbox/ archive/
├── orchestrator/     inbox/ outbox/ archive/
├── portal/           inbox/ outbox/ archive/
├── fe/               inbox/ outbox/ archive/
├── joinery/          inbox/ outbox/ archive/
├── abstractions/     inbox/ outbox/ archive/
├── cutting/          inbox/ outbox/ archive/
├── inventory/        inbox/ outbox/ archive/
├── procurement/      inbox/ outbox/ archive/
├── infra/            inbox/ outbox/ archive/
├── e2e/              inbox/ outbox/ archive/
├── tester/           inbox/ outbox/ archive/
├── librarian/        inbox/ outbox/ archive/ PROCESSED_LOG.md
├── architect/        inbox/ outbox/            ← Root kér spec-et, Gábor kérdez
└── root/             outbox/                   ← Root saját feljegyzései
```

**Symlink konvenció:** Nem-kód terminálok (tester, architect, librarian) a saját könyvtárukból érik el a mailboxot symlinken keresztül:
```
/opt/spaceos/tester/mailbox       → docs/mailbox/tester/
/opt/spaceos/spaceos-architect/mailbox  → docs/mailbox/architect/
/opt/spaceos/spaceos-librarian/   (nincs symlink — közvetlenül az abs. utat használja)
```

### Üzenet formátum

Minden üzenet egy `.md` fájl, neve: `YYYY-MM-DD_NNN_[SLUG].md`

```markdown
---
id: MSG-001
from: root | kernel | orchestrator | portal
to: root | kernel | orchestrator | portal
type: epic-assign | task-assign | status-update | question | answer | review-request | bug-report
priority: P1 | P2 | P3
status: UNREAD | READ | DONE | BLOCKED
created: 2026-03-31T14:00:00
epic: E28 (ha releváns)
---

## Tárgy

[Rövid, egyértelmű leírás]

## Tartalom

[Részletes leírás, AC-k, kontextus]

## Várt válasz

[Mit vár a küldő — implementáció, döntés, státusz frissítés]
```

### Üzenet típusok

| Típus | Irány | Mikor |
|-------|-------|-------|
| `epic-assign` | Root → Projekt | Új epic kiosztásakor |
| `task-assign` | Root → Projekt | Specifikus task delegálásakor |
| `status-update` | Projekt → Root | Phase befejezésekor (CODE/TEST/REVIEW/SECURITY) |
| `question` | Bármely irány | Döntés szükséges |
| `answer` | Bármely irány | Válasz kérdésre |
| `review-request` | Root → Projekt | Cross-project review kérés |
| `bug-report` | Bármely irány | Hiba bejelentés |

---

## Root terminál feladatai

### 1. Vision → Epic lebontás

```
Olvasd el: docs/vision/
Olvasd el: docs/Codebase_Status.md (jelenlegi állapot)

Hozd létre az epic leírást:
  docs/epics/E28_[SLUG]/EPIC.md

Frissítsd: docs/BACKLOG.md (új sor az epic táblában)
```

**Epic fájl sablon:**

```markdown
# Epic: E28 — [Cím]

**Prioritás:** P1 | P2 | P3
**Projekt(ek):** kernel | orchestrator | portal | [több is lehet]
**Státusz:** BACKLOG_READY
**Előfeltétel:** E27 CLOSED_DONE (ha van)

---

## Kontextus

[Miért kell ez az epic? Melyik vision célhoz kapcsolódik?]

## Scope

[Mi tartozik bele, mi NEM tartozik bele]

## Acceptance Criteria

- [ ] ...

## Érintett projektek és feladataik

| Projekt | Feladat | Prioritás |
|---------|---------|-----------|
| Kernel | [mit kell csinálni] | P1 |
| Orchestrator | [mit kell csinálni] | P2 |
| Portal | [mit kell csinálni] | P1 |

## Cross-project függőségek

[Pl.: Portal E28 nem indulhat amíg Kernel E28 API végpont nem kész]
```

### 2. Epic kiosztás (inbox üzenet)

Miután az epic fájl kész, a Root üzenetet ír az érintett projektek inbox-ába:

```bash
# Példa: Kernel-nek szóló üzenet
cat > docs/mailbox/kernel/inbox/2026-03-31_001_E28-assign.md << 'EOF'
---
id: MSG-001
from: root
to: kernel
type: epic-assign
priority: P1
status: UNREAD
created: 2026-03-31T14:00:00
epic: E28
---

## Tárgy

E28 — Dashboard Stats Endpoint implementálása

## Tartalom

Olvasd el: /opt/spaceos/docs/epics/E28_DASHBOARD_STATS/EPIC.md

Feladat:
1. Olvasd el az EPIC.md-t
2. Hozd létre a task fájlokat a projekt docs/epics/ mappájában
3. Hajtsd végre a pipeline-t: CODE → TEST → REVIEW → SECURITY
4. Minden phase után küldj status-update-et az outbox-ba

## Várt válasz

status-update minden phase után az outbox-ba.
EOF
```

### 3. Outbox figyelés

A Root rendszeresen ellenőrzi a projekt outbox-okat:

```bash
# Új üzenetek?
ls docs/mailbox/kernel/outbox/
ls docs/mailbox/orchestrator/outbox/
ls docs/mailbox/portal/outbox/

# UNREAD üzenetek keresése
grep -l "status: UNREAD" docs/mailbox/*/outbox/*.md
```

### 4. Válasz / döntés

Ha egy projekt kérdést tesz fel (outbox-ban `type: question`):
1. Root olvassa a kérdést
2. Root ír egy `type: answer` üzenetet a projekt inbox-ába
3. Root frissíti az eredeti kérdés státuszát `DONE`-ra

---

## Projekt terminál feladatai

Minden projekt CLAUDE.md-jébe belekerül a mailbox protokoll.

### Induláskor

```
1. Ellenőrizd az inbox-ot: docs/mailbox/[projekt]/inbox/
2. Keresd az UNREAD üzeneteket: grep "status: UNREAD" inbox/*.md
3. Olvasd el → jelöld READ-nek
4. Hajtsd végre a feladatot a saját WORKFLOW.md szerint
```

### Phase befejezésekor

Írj status-update-et az outbox-ba:

```markdown
---
id: MSG-002
from: kernel
to: root
type: status-update
priority: P1
status: UNREAD
created: 2026-03-31T15:30:00
epic: E28
---

## Tárgy

E28 — CODE phase kész

## Tartalom

| Fájl | Változás |
|------|---------|
| Endpoints/DashboardEndpoints.cs | Új GET /api/dashboard/stats |
| Application/Dashboard/GetDashboardStatsQuery.cs | Új query handler |

Build: 0 error, 0 warning
Tesztek: még nem futottak (TEST phase következik)

## Következő lépés

TEST phase indítása — kernel-test-writer agent
```

### Ha elakad

```markdown
---
id: MSG-003
from: kernel
to: root
type: question
priority: P1
status: UNREAD
created: 2026-03-31T16:00:00
epic: E28
---

## Tárgy

E28 — Kérdés: DashboardStats DTO struktúra

## Tartalom

A vision nem specifikálja pontosan milyen statisztikákat kell visszaadni.
Két lehetőség:

A) Egyszerű count-ok: { tenants: 5, facilities: 12, workstations: 30 }
B) Részletes: { tenants: 5, facilitiesByTenant: [...], workstationsByStatus: {...} }

## Várt válasz

Melyik opció? Vagy más struktúra?
```

---

## BACKLOG.md (root szintű)

```markdown
# SpaceOS — Master Backlog

| Epic | Cím | Projekt(ek) | Prioritás | Státusz |
|------|-----|-------------|-----------|---------|
| E1–E10 | Kernel Core | kernel | P1 | CLOSED_DONE |
| E11–E17 | Orchestrator | orchestrator | P1 | CLOSED_DONE |
| E18–E27 | Design Portal | portal | P1 | CLOSED_DONE |
| E28 | Dashboard Stats | kernel + portal | P1 | BACKLOG_READY |
```

---

## Cross-project epic sorrend

Ha egy epic több projektet érint:

```
1. Root létrehozza az EPIC.md-t (cross-project scope)
2. Root kiosztja a Kernelnek ELSŐ (API végpont kell a frontendnek)
3. Kernel befejezi → outbox status-update
4. Root olvassa → kiosztja az Orchestratornak (ha kell proxy)
5. Orchestrator befejezi → outbox status-update
6. Root olvassa → kiosztja a Portalnak (frontend rá tud csatlakozni)
7. Portal befejezi → outbox status-update
8. Root olvassa → BACKLOG.md → CLOSED_DONE
```

---

## Fontos szabályok

1. **Root soha nem ír kódot** — csak tervez, koordinál, ellenőriz
2. **Projektek soha nem olvasnak más projekt inbox/outbox-át** — csak a sajátjukat
3. **Minden üzenet .md fájl** — versionálható, kereshető, auditálható
4. **UNREAD → READ → DONE** — minden üzenet átmegy ezen a cikluson
5. **Kérdés mindig választ kap** — ne hagyd függőben
6. **Cross-project sorrend betartása** — backend → middleware → frontend

---

## Munka módszerek

Ez a fejezet azt rögzíti, hogyan dolgozunk a gyakorlatban — session felépítéstől az agent koordinációig.

### 1. Session-alapú munka + kontextus folytonosság

Minden munkamenet egy Claude Code session. A hosszú sessionök automatikusan tömörülnek — a folytatáshoz szükséges kontextust az alábbiak biztosítják:

| Mechanizmus | Hol | Mire való |
|---|---|---|
| `memory/` mappa | `/home/gabor/.claude/projects/-opt-spaceos/memory/` | Felhasználó preferenciái, projekt állapot, feedback — sessionök között megmarad |
| `MEMORY.md` index | ugyanott | Gyors áttekintés, minden session elején betöltődik |
| `Codebase_Status.md` | `/opt/spaceos/docs/Codebase_Status.md` | Minden modul aktuális státusza, teszt számok, sprint roadmap |
| JSONL history | `~/.claude/projects/.../<session-id>.jsonl` | Teljes session log, visszaolvasható ha kell |

**Szabály:** Session elején a root terminál elolvassa a `Codebase_Status.md`-t és a mailbox outbox-okat, hogy a jelenlegi állapotot rekonstruálja.

---

### 2. Párhuzamos multi-terminál munka

Egy időben több Claude Code terminál fut, mindegyik más repón dolgozik:

```
Root       /opt/spaceos/                      — koordináció, tervezés    [persistent]
Architect  /opt/spaceos/spaceos-architect/    — konzultatív partner       [persistent]
Kernel     /opt/spaceos/spaceos-kernel/       — .NET 8 backend            [on-demand]
Orch       /opt/spaceos/spaceos-orchestrator/ — Node.js BFF               [on-demand]
Portal     /opt/spaceos/design-portal/        — React frontend            [on-demand]
...és így tovább (lásd: Terminál lista fentebb)
```

A terminálok **aszinkron** kommunikálnak a mailbox rendszeren keresztül — soha nem direktben egymással. A root terminál látja az összképet, a projekt terminálok csak a saját scope-jukat.

**Kivétel — Architect:** Gábor közvetlenül kommunikálhat az Architect terminállal, nem csak mailbox-on keresztül. Az Architect persistent, bármikor megszólítható.

---

### 3. Epic → Sprint ciklus

```
Vision doc → Epic tervezés → Cross-project kiosztás → Párhuzamos fejlesztés → Review → Deploy
```

Egy epic tipikusan:
- **1-3 nap** ha egy projektet érint
- **3-7 nap** ha cross-project (Kernel + Orchestrator + Portal sorrendben)
- **Infra track** párhuzamosan fut a kód trackel (pl. Keycloak: KC01+KC02+KC03 kód + INFRA-KC01 VPS)

---

### 4. Phase pipeline (projekt terminál)

Minden task egységen belül:

```
CODE → TEST → REVIEW → SECURITY → DONE
```

| Phase | Mit jelent | Mikor kész |
|---|---|---|
| `CODE` | Implementáció | Build: 0 error, 0 warning |
| `TEST` | Tesztek írása + futtatás | Minden teszt zöld |
| `REVIEW` | Kód minőség, architectural fit | CLAUDE.md szabályok teljesülnek |
| `SECURITY` | OWASP, RLS, input validation | Nincs nyilvánvaló sebezhetőség |
| `CODE_REVIEW` | Root jóváhagyásra vár | — |
| `CLOSED_DONE` | Elfogadva, archivált | — |

---

### 5. Archív rendszer

Lezárt feladatok nem törlődnek, hanem archiválódnak:

```
docs/mailbox/<projekt>/inbox/<feladat>.md   →  archive/<feladat>.md
docs/mailbox/<projekt>/outbox/<válasz>.md   →  archive/<válasz>.md
```

Az archív megőrzi az audit trailtet — bármikor visszanézhető mi történt és mikor.

---

### 6. Codebase_Status.md mint SSoT

A `docs/Codebase_Status.md` az egyetlen forrás az aktuális állapothoz:

- Minden phase completion után frissül
- Tartalmazza: modul státusz, teszt számok, sprint roadmap
- Szimlinkelve: `infra/docs/Codebase_Status.md` → ugyanaz a fájl

**Szabály:** Ha a kód és a státuszfájl ellentmond egymásnak → a kód az igazság, a státuszfájl frissítendő.

---

### 7. Döntési hierarchia

```
Gabor (user) > Arch doc (vision/docs) > CLAUDE.md szabályok > Agent döntés
```

- Ha valami nincs dokumentálva → agent dönt, majd rögzíti
- Ha arch doc és CLAUDE.md ellentmond → arch doc nyer, CLAUDE.md frissítendő
- Ha Gabor és arch doc ellentmond → Gabor nyer, mindig

---

### 9. Claude Skills (2026-04-14+)

A projekt dedikált Claude Code skilleket tartalmaz, amelyek a session rituált és a kommunikációs protokollt egységesítik:

| Skill | Kinek | Mire való |
|---|---|---|
| `/spaceos-root` | Root terminál | Session-start outbox olvasás, döntési mátrix, inbox írás sablon, task FSN lifecycle |
| `/spaceos-terminal` | Minden projekt terminál | Inbox olvasás ritual, build/test gate, DONE/BLOCKED outbox sablonok |
| `/spaceos-architect` | Architect terminál | Konzultatív session ritual, codebase elemzés, sub-agent hívás, outbox spec írás |

**Elérési út:** `/opt/spaceos/.claude/skills/`

**Architect sub-agentek** (Architect terminálban elérhetők):
- `SE: Architect` — Well-Architected review, ADR generálás (Claude Opus)
- `Principal SE` — kód-szintű döntések, tech debt
- `SE: Security` — OWASP, RLS/RBAC audit
- `Devil's Advocate` — döntések stressz-tesztelése

**Használat:** Skill neve alapján automatikusan triggerel, vagy paranccsal hívható. A CLAUDE.md-t nem váltja ki — kiegészíti.

---

### 8. Infra mint különálló concern (2026-04-09+)

A VPS infrastruktúra (nginx, PostgreSQL, Keycloak, systemd) külön "projekt terminálként" kezelendő:

- Saját mailbox: `docs/mailbox/infra/`
- Saját CLAUDE.md: `infra/CLAUDE.md`
- Dokumentáció: `infra/` mappa
- Nem ír kódot — konfigurál, deployol, üzemeltet

Az infra track a kód tracktől **független**, párhuzamosan futhat.

---

### 10. E2E terminál — kanonikus parancs cheat sheet (2026-04-15+)

Az E2E verifikáció minden Kernel/Orchestrator deploy után kötelező lépés. Forrás: `/opt/spaceos/e2e/CLAUDE.md`.

```bash
# Inbox olvasás — új feladat van-e?
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/e2e/inbox/

# Alap teszt futtatás
cd /opt/spaceos/e2e && npm test

# Teljes rerun verbose log-gal (cross-project verifikáció, sprint zárásnál)
cd /opt/spaceos/e2e && npx vitest run --reporter=verbose 2>&1 | tee /tmp/e2e-rerun-NNN.log

# Cooldown két teljes rerun között — Keycloak rate limit sliding window
sleep 85

# Stack health ellenőrzés (E2E előtt kötelező)
curl -s http://127.0.0.1:3000/bff/health
curl -s http://127.0.0.1:5001/healthz
```

**Fontos:** E2E csak stabil stack-en futtatható — Kernel + Orchestrator + Keycloak zöld health után.
Cross-project sorrend: `Kernel deploy → Orchestrator deploy → E2E rerun`.

---

## 11. VPS RAM korlát — terminál párhuzamosság

**8GB RAM áll rendelkezésre.**

**Persistent sessionök (mindig futnak):**
- `spaceos-root` — Root koordinátor
- `spaceos-architect` — Architect konzultatív partner

**On-demand sessionök:** minden más terminál (Kernel, Orch, Joinery, stb.) — csak ha aktív feladat van.

**Szabályok:**
- Root max 3 on-demand terminálnak ad egyszerre feladatot (a 2 persistent mellé)
- DA audit / security review: páronként (2-es csoportok)
- Fix sprint kiírásnál: 3 on-demand terminál egyszerre — a 4. csak ha egy végzett
- On-demand terminálokat le kell állítani ha DONE/BLOCKED outbox érkezett
- Root koordinálja a sorrendet a RAM limit figyelembevételével
- Architect persistent volta miatt **nem számít bele** az on-demand 3-as limitbe

---

## 12. TESTER terminál — manuális + Playwright tesztelés (2026-04-18+)

A TESTER egy dedikált Claude Code terminál a **manuális és automatizált felhasználói teszteléshez**. Külön session, így a Root koordinálhat miközben Gábor tesztel.

### Elhelyezkedés

```
/opt/spaceos/tester/
├── CLAUDE.md               ← TESTER session szabályok
├── .env.test               ← chmod 600, teszt credentials (NEM commitolódik)
├── mailbox → docs/mailbox/tester/   ← symlink a központi mailboxhoz
├── tests/                  ← generált Playwright .spec.ts fájlok
└── auth-state.json         ← Keycloak PKCE session állapot (újrafelhasználható)
```

### TESTER felelősségei

| Feladat | TESTER | Root |
|---|---|---|
| Böngészős tesztelés (manuális / Playwright) | ✅ | ❌ |
| TEST_LOG.md kitöltése | ✅ | ❌ |
| Bug azonosítása és leírása | ✅ | ❌ |
| Bug priorizálása és kiosztása | ❌ | ✅ |
| Alkalmazás kód írása | ❌ | ❌ |

### Bug report pipeline

```
TESTER felfedez bugot
  ↓
TEST_LOG.md-be beírja (❌ FAIL sor)
  ↓
outbox/YYYY-MM-DD_NNN_bug-slug.md — type: report
  ↓
Root olvassa → priorizálja → forward a megfelelő terminálnak
  ↓
Terminál javítja → DONE outbox
  ↓
Root elfogadja → INFRA deploy → TESTER reteszt
```

### Credentials kezelés

- `/opt/spaceos/tester/.env.test` — `chmod 600`
- Tartalmaz: `TESZT_USERNAME`, `TESZT_PASSWORD`, `TESZT_DESIGNER_USERNAME/PASSWORD`, `KC_TEST_CLIENT_SECRET`, URL-ek
- Forrás: `e2e/.env` + `/etc/spaceos/keycloak.env`
- INFRA kezeli és frissíti ha jelszó változik

### Párhuzamos munka mintája (2026-04-18 tanulság)

```
Root koordinál  ←→  Terminálok: DONE/BLOCKED üzenetek feldolgozása
      ↕
TESTER tesztel  →  Bug reportok outbox-ba → Root forward-olja
```

A Root és a TESTER **egyszerre aktív** lehet — a TESTER független session, nem zavarja a Root koordinációt.

### Dispatcher integráció

A `spaceos-dispatcher.sh` figyeli a TESTER inbox-ot:
```
[tester]="spaceos-tester:/opt/spaceos/tester"
```
UNREAD inbox üzenet esetén a dispatcher értesíti a TESTER session-t.

**Teljes dispatcher dokumentáció:** `docs/ops/SpaceOS-Munkamodszer-Tmux-Dispatcher.md`

---

## 14. ARCHITECT terminál — konzultatív partner (2026-04-20+)

Az Architect egy **persistent** Claude Code session — Gábor bármikor megszólíthatja, nincs feladatfeldolgozási nyomás.

### Két működési mód

| Mód | Hogyan | Mikor |
|---|---|---|
| **Közvetlen párbeszéd** | Gábor megnyitja az Architect sessiont és kérdez | Architektúrai kérdések, folyamatok megbeszélése, döntési opciók |
| **Spec-kérés Root-tól** | Root inbox üzenetet küld → Architect codebase-t olvas → outbox spec | Root komplex inbox írása előtt, ha technikai mélység kell |

### Mikor vonja be Root az Architectet

- Új cross-module interfész definiálásakor (pl. event bus, provider contract)
- >5 napos feladat spec-je előtt
- Ha Root nem biztos a meglévő kódbázis mintájában
- Komplex domain döntésnél (aggregate root vs. value object, FSM tervezés)

### Kommunikáció az üzenetláncban

```
Root → docs/mailbox/architect/inbox/YYYY-MM-DD_NNN_slug.md
Architect → docs/mailbox/architect/outbox/YYYY-MM-DD_NNN_slug-response.md → Root olvassa
```

**Mailbox:** `docs/mailbox/architect/`  
**Symlink:** `spaceos-architect/mailbox → docs/mailbox/architect/`  
**Dispatcher:** `[architect]="spaceos-architect:/opt/spaceos/spaceos-architect"` — persistent

### Tudásbázis kapcsolat

Az Architect a Librarian által karbantartott `docs/knowledge/` könyvtárat olvassa session elején — ez a gyors kontextus-építés forrása, nem a nyers kódbázis.

---

## 15. LIBRARIAN terminál — tudásbázis gondozás (2026-04-17+)

A Librarian archivált üzeneteket és mailbox outbox-okat dolgoz fel, majd strukturált tudásdokumentumokat ír.

### Felelősségek

| Feladat | Librarian | Root |
|---|---|---|
| `docs/mailbox/*/archive/` feldolgozása | ✅ | ❌ |
| `docs/knowledge/` karbantartása | ✅ | ❌ |
| Terminál kontextus fájlok frissítése | ✅ | ❌ |
| ADR-ek szintetizálása | ✅ | ❌ |
| Feladat kiosztás más termináloknak | ❌ | ✅ |

### Feldolgozási napló

`docs/mailbox/librarian/PROCESSED_LOG.md` — minden feldolgozott fájl listája.  
A Librarian **csak az ÚJ fájlokat** dolgozza fel (amelyek nincsenek a naplóban).

### Tudásbázis struktúra

```
docs/knowledge/
  INDEX.md                    ← ELSŐ olvasnivaló minden terminálnak
  security/                   ← JWT, RLS, RBAC, OWASP minták
  deployment/                 ← VPS deploy runbook, gotchas
  patterns/                   ← DB, testing, dev difficulties
  architecture/               ← ADR katalógus, API contracts, module boundaries
  context/                    ← Terminálonkénti kontextus összefoglaló
```

**Az INDEX.md** minden terminál hidegindítási referenciája — a Librarian tartja naprakészen.

### Dispatcher integráció

```
[librarian]="spaceos-librarian:/opt/spaceos/spaceos-librarian"  — on-demand
```

---

## 13. Konstruktív kritika elve (2026-04-18+)

> "Csak konstruktív kritikával van értelme csinálni." — Gábor

Ha valami nem stimmel a workflow-ban, tooling-ban vagy megközelítésben — **ki kell mondani, nem csendben folytatni**. Ez vonatkozik:

- Architektúrai döntésekre (pl. SQLite vs fájl alapú mailbox)
- Workflow inefficienciákra (pl. race condition fájlírásnál)
- Technikai adósságra (pl. shadow property warning, enum mismatch)
- Prioritásbeli kérdésekre (mi blokkolja a Soft Launch-ot valójában)

A Root terminál feladata nemcsak koordinálni, hanem **aktívan jelezni ha valami jobb lehetne** — még akkor is ha ez extra munkát jelent rövid távon.
