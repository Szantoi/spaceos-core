---
id: MSG-MONITOR-030
from: nightwatch
to: monitor
type: task
priority: low
status: READ
processed: 2026-07-05 07:00
model: haiku
created: 2026-07-05
---

# Scheduled Health Check — Mode-Aware

**Operációs mód:** `structured_program`

---

## 🎯 Mode #4 Structured Program Health Checks

### 1. **Epic Status (8 aktív)**
```
- [ ] EPIC-CUTTING-Q3: Cutting Module Q3 — **0%** (0/0)
- [ ] EPIC-GRAPH-WORKFLOW: Graph-Based Workflow (ADR-041) — **67%** (2/3)
- [ ] EPIC-JT-CRM: JoineryTech CRM Modul — **33%** (1/3)
- [ ] EPIC-JT-CTRL: JoineryTech Kontrolling Modul — **50%** (1/2)
- [ ] EPIC-JT-HR: JoineryTech HR & Kapacitás Modul — **0%** (0/2)
- [ ] EPIC-JT-MAINT: JoineryTech Karbantartás Modul — **0%** (0/3)
- [ ] EPIC-JT-QA: JoineryTech Minőségbiztosítás Modul — **50%** (1/2)
- [ ] EPIC-JT-DMS: JoineryTech Dokumentumtár Modul — **50%** (1/2)
```

### 2. **Checkpoint Status** (TOP 3 epic részletek)
```
**EPIC-GRAPH-WORKFLOW:**
  - ✅ CP-FLOW-EDITOR: Interactive Flow Editor Complete
  - ✅ CP-MERMAID-RENDER: Mermaid Diagram Rendering
  - ⏳ PENDING CP-JOINERYTECH-MIGRATION: JoineryTech Migration Complete

**EPIC-JT-CRM:**
  - ✅ CP-CRM-BACKEND: CRM Backend API Ready
  - ⏳ PENDING CP-CRM-FRONTEND: CRM UI Complete
  - ⏳ PENDING CP-CRM-INTEGRATION: CRM → Sales Integration

**EPIC-JT-CTRL:**
  - ✅ CP-CTRL-BACKEND: Kontrolling Backend API
  - ⏳ PENDING CP-CTRL-FRONTEND: Kontrolling Dashboard
```

### 3. **Conductor On-Program Check** (FONTOS!)
```
- [ ] Conductor terminál fut-e? (tmux: spaceos-conductor)
- [ ] Recent tasks match epic? (CHECK outbox DONE)
- [ ] Conductor <30 min idle-e MUNKA NÉLKÜL?
- [ ] Ha idle + munka: Conductor inbox message ("Folytatható munka észlelve")
```

### 4. **BLOCKED Messages Check** (FIGYELJ!)
```
- [ ] BLOCKED count <20
- [ ] BLOCKED messages <24h old
- [ ] Kritikus BLOCKED-ok felderítve? (pl. MSG-BACKEND-119)
```

### 5. **Nightwatch Activity** (ALAPVETÕ)
```
- [ ] Nightwatch script lefutott <2h
- [ ] logs/dispatcher/pipeline.log frissül
- [ ] logs/dispatcher/nightwatch.log frissül
```

### ❌ NE ELLENÕRIZZ (Mode #4-ben irreleváns):
```
- ❌ Planning queue (disabled)
- ❌ Idea scan progress (disabled)
- ❌ Consensus documents (disabled)
```

---

**Output:** Írj outbox összefoglalót. Ha probléma: BLOCKED hosszú óta vagy Conductor idle + munka, küldj Root inbox-ot.

---

**Session mode:** Hot — folyamatosan futsz, várj a következő inbox-ra.
**Mode Note:** ADR-053 mode-aware health checks aktívak.


---
**Timestamp:** 04:59:31
**Scheduled by:** nightwatch.sh (5-cycle interval)
