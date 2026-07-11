---
id: MSG-MONITOR-004
from: nightwatch
to: monitor
type: task
priority: low
status: DONE
injected: 2026-07-03
processed: 2026-07-03
model: haiku
created: 2026-07-02
---

# Scheduled Health Check — Mode-Aware

**Operációs mód:** `structured_program`

---

## 🎯 Mode #4 Structured Program Health Checks

### 1. **Epic Status**
```
- [ ] EPICS.yaml létezik és olvasható
- [ ] Active epic jelenlévő: ✅ EPIC-CUTTING-Q3
- [ ] Checkpoint COUNT: 0
- [ ] Progress: 0%
```

### 2. **Checkpoint Status** (KRITIKUS!)
```
- [ ] Nincs checkpoint
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

**Session mode:** Cold — fejezd be a session-t DONE után.
**Mode Note:** ADR-053 mode-aware health checks aktívak.


---
**Timestamp:** 22:09:41
**Scheduled by:** nightwatch.sh (5-cycle interval)
