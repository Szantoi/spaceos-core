---
id: MSG-FE-051
from: root
to: fe
type: task
priority: high
status: UNREAD
ref: MSG-FE-050
created: 2026-06-16
---

# FE-051 — Feladatkezelő + Dokumentumtár + AI munkaterület + ExecBI világ

## Kontextus

FE-050 (Karbantartás+Minőség+EHS+Jelenléti) elfogadva ✅ — 596 teszt · 19/27 világ · commit `51a55c8`.

Ez a sprint visz **19→23 világra** — ezzel az FE-A track lényegében kész, csak a Shop világ marad (FE-B-vel koordinálva).

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett

## Prototípus fájlok

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-tasks.jsx      — Feladatkezelő: saját feladatok, csapat taskok, Kanban
  data-tasks.js       — Tasks store (mock)
  page-docs.jsx       — Dokumentumtár: mappák, fájlok, verziók
  data-docs.js        — Docs store (mock)
  page-ai.jsx         — AI munkaterület: chat, tool-hívások, előzmények
  page-ai-2.jsx       — AI detail: receptek, javasolt workflow-k
  data-ai.js          — AI store (mock sessions, tools)
  page-execbi.jsx     — Vezetői BI: KPI cockpit, trendek, összehasonlítás
  data-execbi.js      — ExecBI trend seed + engine
```

## Amit implementálni kell

### Feladatkezelő világ (`TasksPage.tsx`)
- Saját feladatok lista (prioritás: urgent/high/normal/low, határidő, státusz)
- Csapat Kanban (To Do / In Progress / Review / Done oszlopok)
- Dashboard KPI: Lejárt / Ma esedékes / Folyamatban / Kész ezen a héten
- Stone + violet akcent, ikon: `clipboard`
- Router: `/w/tasks`
- SlideOver: TaskDetail (leírás, subtask-ok, kommentek, státusz FSM)

### Dokumentumtár világ (`DocsPage.tsx`)
- Mappa-fa nézet (bal oldal) + fájl lista (jobb oldal)
- Fájl kártyák: neve, típus badge (PDF/DWG/XLSX/DOC), méret, feltöltő, dátum
- Keresőmező (fájlnév + tartalom alapján)
- Dashboard KPI: Összes dokumentum / Ezen a héten feltöltve / Megosztott / Verziókövetett
- Stone + amber akcent, ikon: `file`
- Router: `/w/docs`
- SlideOver: DocDetail (verziólista, megosztott személyek, letöltés gomb)

### AI munkaterület világ (`AiPage.tsx`)
- Chat interfész (üzenet lista + input mező — mock válaszokkal)
- Tool-hívás előzmények panel (melyik tool-t hívta, mikor, eredmény)
- Gyors-receptek kártyák (pl. "Ajánlat generálása", "Készlet-elemzés", "Menetrend tervezés")
- Dashboard KPI: Ma indított session-ök / Tool hívások / Mentett receptek / Token használat
- Stone + purple akcent, ikon: `sparkle`
- Router: `/w/ai`

### Vezetői BI világ (`ExecBiPage.tsx`)
- 4 tab-os cockpit (Pénzügy / Gyártás / Értékesítés / HR)
- Trend kártyák: havi bontású sparkline-ok (mock adattal)
- Top 5 lista komponens (pl. legjövedelmezőbb projektek, legtöbbet vásárló ügyfelek)
- Dashboard KPI: Havi árbevétel / Fedezet % / Aktív projektek / Headcount
- Stone + indigo akcent, ikon: `chart`
- Router: `/w/execbi` (+ homescreen-en "BI" névvel)
- SlideOver: TrendDetail (részletes idősor, összehasonlítás előző időszakkal)

## Mock adatok
- `src/mocks/tasks.ts` — 8 feladat, Kanban-os elosztásban (`data-tasks.js` alapján)
- `src/mocks/docs.ts` — 3 mappa, 10 dokumentum különböző típusokkal (`data-docs.js` alapján)
- `src/mocks/ai.ts` — 3 session, 6 tool-hívás előzmény, 4 recept (`data-ai.js` alapján)
- `src/mocks/execbi.ts` — havi trend adatok 4 kategóriához, top-5 listák (`data-execbi.js` alapján)

## Tesztek
- `TasksPage.test.tsx`, `DocsPage.test.tsx`, `AiPage.test.tsx`, `ExecBiPage.test.tsx`
- `pnpm test` 596 → ~650

## DONE feltételek
- [ ] 4 új világ a Home-on
- [ ] Minden lista/view renderel mock adattal
- [ ] AI chat input + mock válasz renderel
- [ ] ExecBI 4 tab mindegyike renderel
- [ ] `pnpm build` + `pnpm test` zöld

## Megjegyzés

Ez után **FE-A track lényegében kész** (23/27 világ). A maradék 4 világ:
- `shop` — FE-B koordinációt igényel (ha ők csinálják meg), VAGY FE-052-ben
- `hr`, `kontrolling` — FE-B csinálja (FE2-001)
- `service` — FE-B csinálja (FE2-002)

## Koordináció
Ne módosítsd: HrPage, ControllingPage, WarehousePage (bővített), ServicePage
