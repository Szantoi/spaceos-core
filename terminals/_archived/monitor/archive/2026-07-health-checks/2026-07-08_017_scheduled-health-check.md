---
id: MSG-MONITOR-017
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-07-08
---

# Scheduled Health Check — Mode-Aware

**Operációs mód:** `structured_program`

---

## 🎯 Mode #4 Structured Program Health Checks

### 1. **Epic Status (3 aktív)**
```
- [ ] EPIC-CUTTING-Q3: Cutting Module Q3 — **0%** (0/0)
- [ ] EPIC-JT-CRM: JoineryTech CRM Modul — **67%** (2/3)
- [ ] EPIC-JT-MAINT: JoineryTech Karbantartás Modul — **67%** (2/3)
```

### 2. **Checkpoint Status** (TOP 3 epic részletek)
```
**EPIC-JT-CRM:**
  - ✅ CP-CRM-BACKEND: CRM Backend API Ready
  - ✅ CP-CRM-FRONTEND: CRM UI Complete
  - ⏳ PENDING CP-CRM-INTEGRATION: CRM → Sales Integration

**EPIC-JT-MAINT:**
  - ✅ CP-MAINT-BACKEND: Maintenance Backend API
  - ✅ CP-MAINT-FRONTEND: Maintenance Dashboard
  - ⏳ PENDING CP-MAINT-PROD-INTEGRATION: Maintenance → Production Integration
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
**Timestamp:** 02:55:15
**Scheduled by:** nightwatch.sh (5-cycle interval)
