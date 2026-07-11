# ADR-060: Iterative Goal-Driven Development Loop

**Státusz:** Proposed
**Dátum:** 2026-07-04
**Kontextus:** Session iteratív fejlesztési paradigma-váltás
**Döntéshozó:** Root + User

---

## Kontextus

A jelenlegi SpaceOS agent infrastruktúra **reaktív monitoring** alapú:
- Nightwatch figyeli a terminálokat
- BLOCKED/DONE üzenetek triggerelnek feldolgozást
- Pipeline-ok automatikusan futnak

Ez működik, de sok energiát fordítunk arra, hogy **spontán** alakuljanak ki a folyamatok.

---

## Paradigma-váltás: Iteratív Ciklusvezérlés

Az új megközelítés **célorientált iteratív ciklusokat** használ:

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│   ┌─────────────┐                                    │
│   │ MEGFIGYELÉS │ ◄────────────────────────────┐     │
│   │ Observation │                              │     │
│   └──────┬──────┘                              │     │
│          │                                     │     │
│          ▼                                     │     │
│   ┌─────────────┐                              │     │
│   │  TERVEZÉS   │                              │     │
│   │  Planning   │                              │     │
│   └──────┬──────┘                              │     │
│          │                                     │     │
│          ▼                                     │     │
│   ┌─────────────┐                              │     │
│   │ MEGVALÓSÍTÁS│                              │     │
│   │Implementation│                             │     │
│   └──────┬──────┘                              │     │
│          │                                     │     │
│          ▼                                     │     │
│   ┌─────────────┐     CÉL ELÉRVE?              │     │
│   │ ELLENŐRZÉS  │────────────────────► EXIT    │     │
│   │   Review    │       ✅                     │     │
│   └──────┬──────┘                              │     │
│          │ ❌                                   │     │
│          └─────────────────────────────────────┘     │
│                                                      │
│              Addig iterál, amíg CÉL ✅               │
└──────────────────────────────────────────────────────┘
```

---

## A Ciklus 4 Fázisa

### 1. MEGFIGYELÉS (Observation)
**Ki végzi:** Monitor terminál
**Mit csinál:**
- Rendszer állapot felmérése
- EPICS.yaml progress ellenőrzése
- Terminálok státusza
- BLOCKED/stuck detektálás

**Output:** Health check report + javaslatok

### 2. TERVEZÉS (Planning)
**Ki végzi:** Conductor (+ Architect ha kell)
**Mit csinál:**
- Cél definiálása (Goal YAML)
- Terminálok feladat-allokációja
- Dependency mapping
- Checkpoint-ok meghatározása

**Output:** Goal definíció + task breakdown

### 3. MEGVALÓSÍTÁS (Implementation)
**Ki végzi:** Backend, Frontend, Designer (fejlesztő terminálok)
**Mit csinál:**
- Kód írás/módosítás
- Tesztek futtatása
- DONE outbox küldés

**Output:** Kész kód + DONE üzenet

### 4. ELLENŐRZÉS (Review)
**Ki végzi:** Reviewer pipeline (Haiku) + Conductor
**Mit csinál:**
- Dual Haiku review
- Acceptance criteria ellenőrzés
- Goal completion check

**Output:** APPROVE/REJECT + új ciklus vagy EXIT

---

## Cél (Goal) Struktúra

```yaml
id: GOAL-<EPIC>-<NNN>
status: watching | triggered | completed
goal:
  description: "Mit akarunk elérni"
  success_criteria: "Mikor tekintjük késznek"
completion_criteria:
  - type: done_outbox
    terminal: backend
    message_id: MSG-BACKEND-XXX
  - type: checkpoint_status
    epic_id: EPIC-CUTTING-Q3
    checkpoint_id: CP-NESTING-V1
on_complete:
  trigger_terminal: conductor
  prompt: "Goal completed, next iteration..."
```

---

## Terminálok Szerepe a Ciklusban

| Terminál | Fázis | Felelősség |
|----------|-------|------------|
| **Monitor** | Megfigyelés | Health check, stuck detection, javaslatok |
| **Conductor** | Tervezés | Goal definiálás, task dispatch, coordination |
| **Backend/Frontend/Designer** | Megvalósítás | Kód írás, DONE production |
| **Architect** | Tervezés (opcionális) | Complex architectural decisions |
| **Librarian** | Review (opcionális) | Knowledge synthesis |
| **Root** | Meta-koordináció | Goal approval, strategic decisions |

---

## Előnyök a Reaktív Rendszerrel Szemben

| Reaktív (jelenlegi) | Iteratív (új) |
|---------------------|---------------|
| Vár a triggerekre | Aktívan keresi a célt |
| Egyedi üzenet feldolgozás | Cél-orientált munka |
| Stuck detection utólag | Progress tracking előre |
| Spontán koordináció | Explicit iteráció |
| "Mi történt?" | "Hol tartunk?" |

---

## Implementációs Fázisok

### Phase 1: watchGoals (DONE - ADR-059)
- Goal YAML fájlok
- Completion criteria evaluation
- Conductor trigger on completion

### Phase 2: Iterative Loop Controller (PROPOSED)
- Monitor → Conductor feedback loop
- Automatic iteration cycling
- Goal progress dashboard

### Phase 3: Self-Organizing Cycles (FUTURE)
- AI-driven goal decomposition
- Adaptive iteration timing
- Cross-epic coordination

---

## Kapcsolódó ADR-ek

- **ADR-053:** Checkpoint-based coordination
- **ADR-059:** Monitor-Driven Goal Progression
- **ADR-049:** Parallel workers

---

## Döntés

**A ciklusvezérlés implementálásra kerül Phase 2-ben.**

Első lépés: Monitor health check output → Conductor tervezési input pipeline megvalósítása.

---

## Következmények

1. Monitor terminál felelőssége bővül (megfigyelés → javaslat)
2. Conductor aktívabb koordinátor lesz (várakozás → iteráció)
3. Root kevesebb reaktív beavatkozás, több stratégiai döntés
4. Goal tracking dashboard szükséges (Datahaven)

---

## Iparági Kutatás és Elemzés (2026-07-04)

### Források

- [Requesty: Loop Engineering Guide](https://www.requesty.ai/blog/loop-engineering-how-to-build-ai-agent-loops-that-run-themselves)
- [Anthropic: Multi-Agent Coordination Patterns](https://claude.com/blog/multi-agent-coordination-patterns)
- [From Reactive to Proactive: DevOps and SRE in 2026](https://medium.com/@meena.nukala1992/from-reactive-to-proactive-how-ai-agents-are-redefining-devops-and-sre-in-2026-626cea469855)
- [Viston: Monitoring Strategy for AI Agent Systems](https://viston.tech/building-a-robust-monitoring-strategy-for-ai-agent-systems-in-2026/)
- [Gleecus: Agent Loop Complete Guide 2026](https://gleecus.com/blogs/agent-loop-adaptive-ai-agents-complete-guide-2026/)

---

## 🔴 ELLENÉRVEK (Iteratív Loop Kockázatai)

### 1. **Token Runaway / Költség-robbanás**
> "A goal loop with no max_iterations can burn through $500 in an hour. Always set a ceiling."

**Kockázat:** Iteratív ciklusok kontrollálatlanul futhatnak, hatalmas API költséget generálva.

**SpaceOS kontextus:** Nightwatch 2 percenként fut. Ha minden ciklus triggerel egy Goal loop-ot, 30 loop/óra = potenciálisan $15-50/óra extra költség.

**Mitigáció:** `max_iterations` limit minden Goal-ra, cost limiter (ADR-049 már implementált).

### 2. **Reactive Loop / Ping-pong Csapda**
> "Agent A writes a finding, Agent B reads it and writes a follow-up, Agent A sees the follow-up and responds. The system keeps burning tokens on work that isn't converging."

**Kockázat:** Monitor → Conductor → Backend → DONE → Monitor → ... végtelen kör.

**SpaceOS kontextus:** Már láttunk ilyet! Frontend + Designer REJECT loop 2026-07-04-én.

**Mitigáció:**
- Cooldown időszakok (már van: `NUDGE_INTERVAL_MINUTES`)
- Explicit termination condition (ne soft judgement!)
- State tracking: checkpoint-ok, ne újra-feldolgozás

### 3. **Context Degradation**
> "Use fresh subagent contexts rather than accumulating state"

**Kockázat:** Hosszú iterációs ciklusokban a kontextus "elszennyeződik", az agent elfelejti az eredeti célt.

**SpaceOS kontextus:** Terminálok cold-start-tal indulnak, de hot session-ben akkumulálódhat a kontextus.

**Mitigáció:** Goal completion után session restart, vagy explicit context reset.

### 4. **Overconfident Termination**
> "Replace soft judgments with hard conditions — zero test failures, zero lint errors"

**Kockázat:** Az agent azt *gondolja* hogy kész, de valójában nem teljesítette a célt.

**SpaceOS kontextus:** DONE outbox nem garantálja a minőséget.

**Mitigáció:** Dual Haiku review (már van), explicit acceptance criteria a Goal-ban.

### 5. **Komplexitás Növekedés**
A reaktív rendszer **egyszerű**: event → handler → done.
Az iteratív rendszer **komplex**: goal → plan → execute → verify → iterate.

**Kockázat:** Több mozgó alkatrész = több hibalehetőség, nehezebb debugging.

---

## 🟢 ÉRVEK (Iteratív Loop Előnyei)

### 1. **Proaktív vs Reaktív**
> "By 2026, AI agents are projected to gain significant autonomy, evolving from reactive tools to proactive decision-makers."

**Előny:** Nem várunk arra, hogy valami elromoljon — aktívan keressük a célt.

**SpaceOS kontextus:** A jelenlegi rendszer "vár a DONE-ra". Az iteratív megközelítés "hajtja a DONE-t".

### 2. **MTTR Csökkenés**
> "SRE teams are seeing MTTR (Mean Time to Resolution) drop by 40-60%"

**Előny:** Gyorsabb hibaelhárítás, mert a rendszer előre látja a problémákat.

**SpaceOS kontextus:** BLOCKED üzenetek 24+ óráig maradhatnak. Iteratív loop: 1 cikluson belül eszkalálódik.

### 3. **Goal-Oriented vs Task-Oriented**
> "Pursuing high-level objectives rather than responding to one-shot prompts"

**Előny:** A terminál tudja MIÉRT dolgozik, nem csak MIT csinál.

**SpaceOS kontextus:** Jelenleg: "Olvasd az inbox-ot". Iteratív: "Érjük el az EPIC-CUTTING-Q3 CP-NESTING-V1 checkpoint-ot".

### 4. **Reflection és Self-Critique**
> "Reflection prompts allow the agent to critique its own performance"

**Előny:** Minden iteráció végén: "Közelebb kerültünk a célhoz?"

**SpaceOS kontextus:** Monitor health check = reflection pont.

### 5. **Hybrid Architecture Lehetőség**
> "Hybrid architectures combine strengths: reactive layers handle immediate responses, deliberative layers manage strategic planning"

**Előny:** NEM kell választani — lehet mindkettő!

---

## 🟡 JAVASLAT: HYBRID MEGKÖZELÍTÉS

### A Jelenlegi Reaktív Réteg Marad

```
Nightwatch (reaktív)
  ├── watch-inbox → UNREAD trigger (gyors)
  ├── watch-done → DONE feldolgozás (gyors)
  ├── watch-stuck → Enter nudge (gyors)
  └── watch-priority → Session alive (gyors)
```

**Miért marad?**
- Egyszerű, bevált, alacsony overhead
- Azonnali reakció kritikus eseményekre
- Nem igényel komplex state management-et

### Új Deliberatív Réteg Hozzáadása

```
Goal Loop (deliberatív)
  ├── watch-goals → Cél progress tracking (lassabb, 5-10 NWT)
  ├── goal-planner → Iteráció tervezés (Conductor)
  ├── goal-verifier → Acceptance criteria check
  └── goal-escalator → Stuck goal → Root
```

**Miért új réteg?**
- Stratégiai szintű koordináció
- Epic/checkpoint orientált
- Emberi beavatkozás nélküli célkövetés

### Rétegek Kapcsolata

```
┌─────────────────────────────────────────────────────────┐
│ DELIBERATÍV RÉTEG (Goal Loop)                          │
│   - Stratégiai célok                                   │
│   - Epic progress                                      │
│   - Iteráció vezérlés                                  │
│   - 5-10 NWT ciklus                                    │
└──────────────────────────┬──────────────────────────────┘
                           │ "Ez a cél, hajtsátok végre"
                           ▼
┌─────────────────────────────────────────────────────────┐
│ REAKTÍV RÉTEG (Nightwatch)                             │
│   - Inbox/Outbox kezelés                               │
│   - Stuck detection                                    │
│   - Session management                                 │
│   - 1 NWT ciklus                                       │
└─────────────────────────────────────────────────────────┘
```

### Konfliktuskezelés

Ha a két réteg ütközik:
1. **Reaktív elsőbbség** kritikus eseményeknél (BLOCKED, session crash)
2. **Deliberatív elsőbbség** stratégiai döntéseknél (melyik task jön következő)
3. **Root döntés** ha nincs egyértelmű

---

## Frissített Döntés

**A HYBRID megközelítést választjuk:**

1. **Reaktív réteg (Nightwatch)** — változatlan, gyors reakció
2. **Deliberatív réteg (Goal Loop)** — új, stratégiai ciklusvezérlés
3. **Koordinációs protokoll** — mikor melyik réteg dönt

**NEM paradigma-váltás, hanem KIEGÉSZÍTÉS.**

---

## Implementációs Terv (Frissített)

### Phase 1: watchGoals ✅ DONE (ADR-059)
- Goal YAML fájlok
- Completion criteria evaluation
- Conductor trigger on completion

### Phase 2: Hybrid Integration 🔄 IN PROGRESS
- Goal Loop futtatás 5 NWT ciklusonként
- Reaktív réteg változatlan
- Konfliktuskezelési szabályok

### Phase 3: Intelligent Escalation (FUTURE)
- Goal stuck detection (>20 NWT progress nélkül)
- Automatic Root escalation
- Self-healing iteration restart

### Phase 4: Self-Organizing Goals (FUTURE)
- AI-driven goal decomposition
- Cross-epic dependency resolution
- Adaptive iteration timing
