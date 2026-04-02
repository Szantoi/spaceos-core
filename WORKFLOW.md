# SpaceOS — Root Orchestration Workflow

> Ez a dokumentum a **root terminál** működését definiálja.
> A root terminál feladatokat oszt ki a projekteknek, követi az állapotukat,
> és a `docs/mailbox/` rendszeren keresztül kommunikál velük.
>
> **A root terminál soha nem ír kódot.** Tervez, koordinál, ellenőriz.

---

## Terminál architektúra

```
┌──────────────────────────────────────────────────┐
│  ROOT terminál  (/opt/spaceos)                   │
│  Feladat: tervezés, koordináció, vision→epic     │
│  Olvas:  docs/vision/, docs/mailbox/*/outbox/    │
│  Ír:     docs/mailbox/*/inbox/, docs/BACKLOG.md  │
└───────┬──────────┬──────────┬────────────────────┘
        │          │          │
   inbox/     inbox/     inbox/
        │          │          │
┌───────▼──┐ ┌────▼─────┐ ┌──▼──────────┐
│ KERNEL   │ │ ORCH     │ │ PORTAL      │
│ terminál │ │ terminál │ │ terminál    │
│ .NET 8   │ │ Node.js  │ │ React       │
│ port5000 │ │ port3000 │ │ port5173    │
└───────┬──┘ └────┬─────┘ └──┬──────────┘
   outbox/    outbox/    outbox/
        │          │          │
        └──────────┴──────────┘
              ROOT olvassa
```

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
├── kernel/
│   ├── inbox/          ← Root ír IDE, Kernel terminál olvassa
│   └── outbox/         ← Kernel terminál ír IDE, Root olvassa
├── orchestrator/
│   ├── inbox/          ← Root ír IDE, Orchestrator terminál olvassa
│   └── outbox/         ← Orchestrator terminál ír IDE, Root olvassa
└── portal/
    ├── inbox/          ← Root ír IDE, Portal terminál olvassa
    └── outbox/         ← Portal terminál ír IDE, Root olvassa
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
