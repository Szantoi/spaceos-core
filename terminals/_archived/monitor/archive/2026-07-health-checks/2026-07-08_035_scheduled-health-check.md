---
id: MSG-MONITOR-035
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-07-08
---

# Scheduled Health Check — Mode-Aware

**Operációs mód:** `planning_pipeline`

---

## 🧠 Mode #2/#3 Planning Pipeline Health Checks

### 1. **Planning Queue**
```
- [ ] docs/planning/queue/ — hány item?
- [ ] Selected → Debate → Consensus szövegek feldolgozva?
- [ ] Consensus docs írva?
```

### 2. **Idea Generation**
```
- [ ] docs/planning/ideas/ — új ideák létrejöttek?
- [ ] Scan script futott <30 min?
- [ ] Idea count növekedett az utolsó check óta?
```

### 3. **Pipeline Activity**
```
- [ ] logs/dispatcher/pipeline.log frissül
- [ ] plan-debate.sh, plan-consensus.sh fut?
- [ ] Konszenzus generálódott az utolsó óra alatt?
```

### 4. **Queue Processing**
```
- [ ] Conductor queue-t feldolgozza?
- [ ] Conductor idle-e túl sokáig? (>30 min)
- [ ] Termináloknak megvan-e a munka?
```

---

**Output:** Írj outbox összefoglalót. Ha probléma (queue stuck, idea scan fail, pipeline error), küldj Root inbox-ot.

---

**Session mode:** Hot — folyamatosan futsz, várj a következő inbox-ra.
**Mode Note:** ADR-053 mode-aware health checks aktívak.


---
**Timestamp:** 05:55:23
**Scheduled by:** nightwatch.sh (5-cycle interval)
