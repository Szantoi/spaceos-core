---
name: spaceos-root
description: >
  SpaceOS root terminál koordinációs skill. Használd amikor üzeneteket kell olvasni
  a terminál outboxokból ("olvasd el az üzeneteket", "olvasd el a neked szóló üzeneteket"),
  következő feladatot kell meghatározni ("mi a következő lépés", "van kiadható feladat?"),
  DONE/BLOCKED/QUESTION üzenetre kell reagálni, terminálnak inbox üzenetet kell írni,
  vagy a projekt állapotát kell felmérni. A skill a SpaceOS root terminál teljes
  koordinációs munkafolyamatát tartalmazza — mailbox olvasás, döntés, inbox írás,
  dokumentáció frissítés.
---

# SpaceOS Root — Koordinációs Workflow

Root = tervez, koordinál, ellenőriz. **Soha nem ír kódot.**

## Session-start ritual

```bash
# 1. UNREAD üzenetek (mindig ezzel kezd)
grep -rl "status: UNREAD" docs/mailbox/*/outbox/ 2>/dev/null

# 2. Ha üres: legfrissebb outbox fájlok
ls -lt docs/mailbox/*/outbox/ | grep "^-" | head -10

# 3. Aktív és new task-ok
cat docs/tasks/README.md
```

## Döntési mátrix

| Üzenet | Teendő |
|---|---|
| `type: done` — rendben | Elfogad → task `active/` → `archive/` → következő inbox → docs frissít |
| `type: done` — hiányos | Visszadob → új inbox konkrét hiánylistával → task marad `active/` |
| `type: blocked` | Dönt → válasz inbox → ha másik terminál kell, azt is kiadja |
| `type: question` | Válaszol → `type: answer` inbox üzenettel |
| `type: report` | Feldolgoz → prioritást dönt → task-okat létrehoz `new/`-ban |
| `type: done` (TESTER-től) | Teszt session lezárult → elfogad → nyitott bugokat PORTAL/INFRA task-ként adja ki |

## Kötelező pipeline — DONE elfogadáskor

```
1. outbox fájl: status: UNREAD → READ
2. task: active/ → archive/
3. Következő inbox üzenet (ha van következő lépés)
4. docs/tasks/README.md frissítés
5. docs/Codebase_Status.md frissítés
```

A 4–5 soha nem maradhat ki — ezek az egyetlen igazságforrások.

## Inbox üzenet írás

**Fájlnév:** `YYYY-MM-DD_NNN_slug.md` → `docs/mailbox/<terminál>/inbox/`

NNN = adott terminál következő sorszáma:
```bash
ls docs/mailbox/<terminál>/inbox/ | sort | tail -1
```

**Frontmatter sablon** — részletes példák: `references/message-templates.md`

```yaml
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: <terminál>
type: task
priority: critical|high|medium|low
status: UNREAD
ref: <kapcsolódó MSG ID>
created: YYYY-MM-DD
---
```

**Terminál ID-k:** `KERNEL` · `ORCH` · `INFRA` · `E2E` · `PORTAL` · `JOINERY` · `ABSTRACTIONS` · `TESTER` · `ARCHITECT` · `LIBRARIAN`

## Task FSN lifecycle

```
new/      ← tervdok kész, nem kiadva még
active/   ← inbox elment, terminál dolgozik
archive/  ← DONE elfogadva
```

**Fájlnév:** `<EPIC-ID>_<slug>.md`  
Részletes konvenciók: `references/task-conventions.md`

## Cross-project sorrend

```
Kernel → Orchestrator → Portal     (backend-first, kötelező)
Kernel → Abstractions
Infra                               párhuzamos a kód tracktől
E2E                                 csak stabil stack-en
```

Különböző service-ek párhuzamosan futhatnak (pl. Kernel + Orchestrator egyszerre).

## Proaktív blokker-azonosítás

DONE elfogadása után mindig ellenőrizd:

1. **Blokkolja-e a következő E2E tesztet?** → ha igen, a fix most kiadható?
2. **Szükséges-e downstream deploy?** → INFRA task azonnal kiadható
3. **BFF route → Kernel endpoint névegyezés?** Ha a DONE BFF route-ot érint, ellenőrizd, hogy a proxy URL egyezik-e a Kernel endpoint-tal (precedens: ORCH-060)
4. **Tech debt azonosítva?** → `docs/tasks/new/` CLEANUP task

## Codebase_Status.md frissítési szabályok

- **Projekt táblázat:** státusz: `DEPLOYED` · `ACTIVE 🔴` · `DONE ✅` · `SUPERSEDED`
- **Összesített tesztszám:** minden deploy után
- **Sprint roadmap:** új sor minden kiadott és lezárt sprintnél
- **Utolsó frissítés fejléc:** dátum + egysoros összefoglaló

## Aktív terminálok és portok

| Terminál | Port | Repo branch | Aktuális commit |
|---|---|---|---|
| Kernel | 5000 | develop | b270ccf |
| Orchestrator | 3000 | develop | b7b4581 |
| Joinery | 5002 | develop | — |
| Abstractions | 5003 | develop | — |
| Cutting | 5005 | develop | b0a11ba |
| Inventory | 5004 | develop | 2fe889e |
| E2E baseline | — | — | 266/266 pass |
| **Architect** | — | `/opt/spaceos/spaceos-architect/` | konzultatív — nem deployol |
| **Librarian** | — | `/opt/spaceos/spaceos-librarian/` | tudásbázis gondozó — nem deployol |

> Stack részletek mindig a `docs/Codebase_Status.md`-ben élnek — ez a táblázat csak gyors referencia.
