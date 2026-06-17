# SpaceOS Discovery-Validation-Iteration (DWI) Workflow

> **Forrás:** JoineryTech.McpServer `DWI.workflow.md` — adaptálva SpaceOS planning pipeline-ra
> **Dátum:** 2026-06-16
> **Státusz:** ÉRVÉNYES — minden planning session ezt a flow-t követi

---

## Összefoglaló

A DWI workflow 4 fázisból áll, és a SpaceOS planning pipeline szkriptjeire mappel:

```
IDEATION        → plan-scan.sh (Haiku)
VALIDATION      → plan-select.sh (Haiku szűrés → Sonnet kiválasztás)
ITERATION       → plan-debate.sh (2× Sonnet A/B vita)
DELIVERY_HANDOFF → Architect inbox (Opus spec)
```

---

## Fázis 1 — IDEATION (plan-scan.sh)

### Entrance criteria
- [ ] `Codebase_Status.md` friss (≤24h régi)
- [ ] `docs/tasks/README.md` naprakész
- [ ] Nincs CRITICAL blocker az aktív feladatokon
- [ ] `domain-focus.md` beállítva (ha van fókuszterület)

### Eszközök
- `plan-scan.sh` — Haiku headless session
- Input: `Codebase_Status.md` + `docs/tasks/new/` + `docs/knowledge/INDEX.md`
- **RAG placeholder:** `knowledge_search(query)` — ha RAG kész, korábbi ötletek és döntések lekérdezése

### Exit criteria
- [ ] Minimum **5 ötlet** dokumentálva `docs/planning/ideas/` mappában
- [ ] Minden ötlet tartalmaz: egymondat leírás, érintett modulok, becsült komplexitás (S/M/L)
- [ ] Duplikáció ellenőrzés: nem ismétli korábbi elvetett ötletet (RAG query / grep)

### Artifact
```
docs/planning/ideas/YYYY-MM-DD_idea-{N}.md
```

---

## Fázis 2 — VALIDATION (plan-select.sh)

### Entrance criteria
- [ ] IDEATION fázis lezárva: 5+ ötlet van `docs/planning/ideas/`-ben
- [ ] `domain-focus.md` ellenőrizve (ha van, szűrés arra a domainre)

### Eszközök
- `plan-select.sh` — Haiku előszűrés → Sonnet kiválasztás
- Input: ötletek + `Codebase_Status.md` + blokkerek
- Validációs szempontok:
  - Backend API létezik-e? (nem feltételezés, kód-ellenőrzés)
  - Van-e függőség (cross-modul, INFRA)?
  - WSJF prioritás (Business Value / Time-to-Market)

### Exit criteria
- [ ] Maximum **2 ötlet** kiválasztva `docs/planning/selected/`-ba
- [ ] Minden kiválasztottnál: indoklás miért ez, miért nem a többi
- [ ] Constraint check: nem ütközik DESIGN_MEMORY zárolt döntéssel

### Artifact
```
docs/planning/selected/YYYY-MM-DD_selected-{slug}.md
```

---

## Fázis 3 — ITERATION (plan-debate.sh)

### Entrance criteria
- [ ] VALIDATION fázis lezárva: 1-2 kiválasztott ötlet `docs/planning/selected/`-ben
- [ ] Mindkét ötletnél a backend API és frontend állapot verifikálva

### Eszközök
- `plan-debate.sh` — 2× Sonnet (A és B tervező) párhuzamos vitája
- Sonnet-A: üzleti érték fókusz (TOP feature = legmagasabb impact)
- Sonnet-B: indíthatóság fókusz (TOP feature = legkevesebb blokkoló)
- Cross-evaluation: A értékeli B tervét és fordítva

### Exit criteria
- [ ] **Konsenzus terv** dokumentálva `docs/planning/consensus/`-ban
- [ ] API feltételezések explicit listázva (létezik/nem létezik)
- [ ] Időbecslés (szekvenciális + párhuzamos)
- [ ] Nyitott kérdések listája az Architect-nek

### Artifact
```
docs/planning/consensus/YYYY-MM-DD_consensus-{slug}.md
docs/planning/plans/YYYY-MM-DD_plan-sonnet-a.md
docs/planning/plans/YYYY-MM-DD_plan-sonnet-b.md
```

---

## Fázis 4 — DELIVERY_HANDOFF (Architect inbox)

### Entrance criteria
- [ ] ITERATION fázis lezárva: konsenzus terv kész
- [ ] API feltételezések listázva
- [ ] Nyitott kérdések explicit felsorolva

### Eszközök
- Architect terminál (Opus) — kódbázis verifikáció + spec írás
- `spaceos-arch-planner` skill — v1→v4 pipeline (ha scope indokolja)
- Input: konsenzus terv + `Codebase_Status.md` + meglévő kód grep/read

### Exit criteria
- [ ] **Implementációs spec** `docs/tasks/new/`-ben (minimum v1, optimális v2+)
- [ ] API feltételezések **verifikálva** a tényleges kódbázis ellen
- [ ] Terminál hozzárendelés (ki implementálja)
- [ ] Függőségi lánc (mi blokkolja mit)
- [ ] ADR bejegyzés ha új architekturális döntés született

### Artifact
```
docs/tasks/new/SpaceOS_{Feature}_v{N}.md
docs/mailbox/architect/outbox/YYYY-MM-DD_NNN_*-done.md
```

---

## Cross-fázis szabályok

1. **Visszalépés engedélyezett:** Ha a VALIDATION-ban kiderül hogy nincs elegendő minőségi ötlet → vissza IDEATION-be
2. **Skip policy:** Egyszerű feature-nél (1 endpoint, 1 page) a IDEATION+VALIDATION kihagyható — közvetlen ITERATION-nal indul
3. **RAG integráció:** Minden fázisban `knowledge_search()` hívás a korábbi döntések figyelembevételéhez (jelenleg placeholder → `grep -r` fallback)
4. **Artifact megőrzés:** Elvetett ötletek és tervek is megmaradnak `docs/planning/archive/`-ban — jövőbeli RAG forrás
