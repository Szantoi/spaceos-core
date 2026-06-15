---
id: 2026-06-16_051_tasks-docs-ai-execbi-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-16_051_tasks-docs-ai-execbi.md
date: 2026-06-16
---

# DONE — FE-051 Feladatkezelő + Dokumentumtár + AI munkaterület + ExecBI világ implementálása

## Commit

`2b562d1` — feat: FE-051 Feladatkezelő + Dokumentumtár + AI munkaterület + ExecBI világ implementálása

## Elvégzett munka

### TasksPage.tsx (új fájl)
- `TaskPriorityPill` — prioritás badge (Sürgős/Magas/Közepes/Alacsony, rose/amber/sky/stone)
- `TaskStatusPill` — státusz badge (Todo/In Progress/Review/Done)
- `TaskDetailSlideOver` — feladat részletek: prioritás, státusz, határidő, feladott, forrás-világ, subtask progress, komment szám
- `MyTasksList` — saját feladatok listája prioritás + státusz badge-ekkel + SlideOver
- `KanbanBoard` — 4 oszlopos Kanban (Todo / In Progress / Review / Done)
- `TasksDashboard` — KPI: Lejárt / Ma esedékes / Folyamatban / Kész ezen a héten + feladat-lista preview
- `TasksWorldPage` — router: dash / mytasks / kanban, route: `/w/tasks`
- Violet akcent, ikon: `clipboard`

### DocsPage.tsx (új fájl)
- `DocTypeBadge` — dokumentum típus badge (Műszaki rajz/Szerződés/Tanúsítvány/Munkautasítás/Egyéb)
- `DocStatusPill` — státusz badge dot-tal (Piszkozat/Ellenőrzés/Kiadott/Archivált)
- `DocDetailSlideOver` — dokumentum részletek: típus, státusz, verzió, tulajdonos, megjegyzés, teljes verzió-history tábla
- `DocsList` — dokumentum lista típus badge + státusz pill + verzió + tulajdonos
- `DocsDashboard` — KPI: Összes dokumentum / Ezen a héten feltöltve / Kiadott / Ellenőrzés alatt + recent docs preview
- `DocsWorldPage` — router: dash / files, route: `/w/docs`
- Amber akcent, ikon: `file`

### AiPage.tsx (új fájl)
- `AgentStagePill` — ágense státusz badge (Definiált/Aktív/Várakozik/Archivált)
- `AgentDetailSlideOver` — ágens részletek: szerep, stage pill, systemPrompt kivonat, skill-ek listája, memória-bejegyzések száma
- `AgentsList` — ágensek listája stage pill + név + szerep + utolsó futás
- `SkillsList` — receptek listája: név + leírás + input változók mint tag-ek
- `AiChatPanel` — mock chat interfész (2 üzenet + input mező)
- `AiDashboard` — KPI: Ma indított sessionök / Tool hívások / Mentett receptek / Aktív ágensek + ágens preview
- `AiWorldPage` — router: dash / chat / agents / skills, route: `/w/ai`
- Purple akcent, ikon: `sparkle`

### ExecBiPage.tsx (új fájl)
- `TrendDetailSlideOver` — trend részletek: utolsó 6 hó adatai táblában
- `FinanceTab` — Árbevétel KPI (17.4M Ft) / Fedezet KPI (25%) / Rendelésállomány + Top 5 ügyfél tábla
- `ProductionTab` — Aktív gépek / Átl. kihasználtság / Nyitott jegyek / Incident-free napok
- `SalesTab` — Pipeline KPI / Aktív projektek + Top 5 projekt tábla (revenue + margin%)
- `HrTab` — Headcount / Ma bent / Késő / Hiányzó
- `ExecBiDashboard` — 4 tab-os cockpit (Pénzügy/Gyártás/Értékesítés/HR), default: Pénzügy
- `ExecBiWorldPage` — router: dash, route: `/w/execbi`
- Indigo akcent, ikon: `chart`

### Mock adatok
- `src/mocks/tasks.ts`: 8 feladat Kanban elosztásban (2-2-2-2), prioritás + státusz meta
- `src/mocks/docs.ts`: 9 dokumentum a prototípus seed alapján, FSM-szerű státusz meta
- `src/mocks/ai.ts`: 3 ágens, 6 recept (skill), 5 memória-bejegyzés a prototípus alapján
- `src/mocks/execbi.ts`: 12 havi trend-adat, Top 5 projektek + ügyfelek, 4 tab meta

### Navigáció + routing
- `worlds.ts`: tasks/docs/ai/execbi hozzáadva, WORLD_ORDER 19→23
- `Icon.tsx`: `chart`, `clipboard`, `file`, `sparkle` ikonok hozzáadva
- `types/index.ts`: WorldKey `| 'tasks' | 'docs' | 'ai' | 'execbi'`
- `HomeScreen.tsx`: Admin mind a 23 világ; Designer + tasks/docs/ai (execbi csak Admin)
- `App.tsx`: /w/tasks, /w/docs, /w/ai, /w/execbi (8 új route)
- `data.test.ts`: 19→23 world count

## Tesztek

| Fájl | Tesztek |
|---|---|
| `TasksPage.test.tsx` | 15 teszt — dashboard, mytasks, kanban, SlideOver |
| `DocsPage.test.tsx` | 15 teszt — dashboard, files, SlideOver, version history |
| `AiPage.test.tsx` | 15 teszt — dashboard, chat, agents, skills, SlideOver |
| `ExecBiPage.test.tsx` | 15 teszt — dashboard, 4 tab, trend data |
| **Összesen** | **656 teszt — mind zöld** |

```
Test Files  67 passed (67)
Tests       656 passed (656)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 656/656 zöld
- [x] Feladatkezelő világ megjelenik a Home-on (Admin + Designer)
- [x] Dokumentumtár világ megjelenik a Home-on (Admin + Designer)
- [x] AI munkaterület világ megjelenik a Home-on (Admin + Designer)
- [x] Vezetői BI világ megjelenik a Home-on (Admin only)
- [x] Feladat lista renderel mock adattal (8 feladat, prioritás + státusz badge-ekkel)
- [x] Kanban 4 oszlop (Todo/In Progress/Review/Done) renderel
- [x] TaskDetail SlideOver (leírás, subtask progress, kommentek)
- [x] Dokumentum lista renderel mock adattal (9 doc, típus + státusz badge-ekkel)
- [x] DocDetail SlideOver (verzió-history, tulajdonos, megjegyzés)
- [x] AI chat mock üzenetekkel + input mező
- [x] Ágensek listája stage badge-ekkel + AgentDetail SlideOver
- [x] Receptek listája névvel + leírással + input változókkal
- [x] ExecBI 4 tab mindegyike renderel (Pénzügy/Gyártás/Értékesítés/HR)
- [x] Top 5 ügyfelek + projektek táblák

## Státusz
FE-A track: **23/27 világ kész**. Maradék 4: shop (koordináció kell), hr/kontrolling/service (FE-B track).
