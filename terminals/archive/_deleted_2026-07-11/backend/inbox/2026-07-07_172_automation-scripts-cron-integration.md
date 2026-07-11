---
id: MSG-BACKEND-172
from: root
to: backend
type: task
priority: medium
status: READ
created: 2026-07-07
model: haiku
ref: MSG-ROOT-013
epic_id: EPIC-NEXUS-AUTOMATION
content_hash: 3c03fa3deb66358b80c4979cc81b7adc9da9a7778302bea2b7348f5091e2e8db
completed: 2026-07-07
---

# Automation Scripts — Cron Integration Setup

## Kontextus

Librarian létrehozott **3 automation script-et** (MSG-ROOT-013), amelyek:
- ✅ Production-ready (executable, error handling, logging)
- ✅ Cron-ready (idempotent, safe to run repeatedly)
- ⏳ **Cron integráció hiányzik** — jelenleg nem futnak automatikusan

**Root Decision:** Backend végzi a cron setup-ot (infra task, operational)

---

## Feladat

Integráld a 3 automation script-et a crontab-ba megfelelő schedule-okkal.

### Scripts Listája

```bash
/opt/spaceos/scripts/mailbox/health-check.sh         # Daily @ 08:00
/opt/spaceos/scripts/dispatch/auto-phase-transition.sh  # Hourly
/opt/spaceos/scripts/monitoring/blocker-detector.sh  # Hourly
```

---

## Implementation Steps

### 1. Script Validation (5 min)

```bash
# Ellenőrizd hogy futtathatók és hibák nélkül futnak
bash /opt/spaceos/scripts/mailbox/health-check.sh
bash /opt/spaceos/scripts/dispatch/auto-phase-transition.sh
bash /opt/spaceos/scripts/monitoring/blocker-detector.sh

# Output ellenőrzés:
# - health-check: JSON létrejött? (/opt/spaceos/logs/mailbox-health.json)
# - auto-phase: Log írás működik?
# - blocker-detector: BLOCKED detektálás működik?
```

### 2. Crontab Entries (10 min)

**Javasolt schedule:**

```cron
# Mailbox Health Check (daily 08:00 CEST)
0 8 * * * /opt/spaceos/scripts/mailbox/health-check.sh >> /opt/spaceos/logs/cron/health-check.log 2>&1

# Auto Phase Transition (hourly, top of hour)
0 * * * * /opt/spaceos/scripts/dispatch/auto-phase-transition.sh >> /opt/spaceos/logs/cron/phase-transition.log 2>&1

# Blocker Detector (hourly, 30 min offset)
30 * * * * /opt/spaceos/scripts/monitoring/blocker-detector.sh >> /opt/spaceos/logs/cron/blocker-detector.log 2>&1
```

**Telepítés:**
```bash
# Aktuális crontab mentése
crontab -l > /tmp/crontab.backup.$(date +%s)

# Új bejegyzések hozzáadása
crontab -l > /tmp/crontab.new
cat >> /tmp/crontab.new << 'EOF'
# SpaceOS Automation Scripts (2026-07-07)
0 8 * * * /opt/spaceos/scripts/mailbox/health-check.sh >> /opt/spaceos/logs/cron/health-check.log 2>&1
0 * * * * /opt/spaceos/scripts/dispatch/auto-phase-transition.sh >> /opt/spaceos/logs/cron/phase-transition.log 2>&1
30 * * * * /opt/spaceos/scripts/monitoring/blocker-detector.sh >> /opt/spaceos/logs/cron/blocker-detector.log 2>&1
EOF

# Telepítés
crontab /tmp/crontab.new

# Ellenőrzés
crontab -l | grep -A3 "SpaceOS Automation"
```

### 3. Log Directory Setup (2 min)

```bash
# Cron log directory létrehozása
mkdir -p /opt/spaceos/logs/cron

# Permissions
chmod 755 /opt/spaceos/logs/cron
```

### 4. First Run Validation (10 min)

**Health Check:**
```bash
# Manual futtatás
/opt/spaceos/scripts/mailbox/health-check.sh

# Output ellenőrzés
cat /opt/spaceos/logs/mailbox-health.json

# Expected: JSON formátum, minden terminál státusz
```

**Auto Phase Transition:**
```bash
# Manual futtatás
/opt/spaceos/scripts/dispatch/auto-phase-transition.sh

# Log ellenőrzés (ha van phase transition trigger)
# Expected: Goal-based dispatch logic vagy "No pending transitions"
```

**Blocker Detector:**
```bash
# Manual futtatás
/opt/spaceos/scripts/monitoring/blocker-detector.sh

# Expected: BLOCKED count, escalation logic (if >5 BLOCKED detected)
```

### 5. Monitoring Setup (5 min)

**Cron execution tracking:**
```bash
# Cron log tailing (debugging)
tail -f /opt/spaceos/logs/cron/*.log

# Automated monitoring (future): Datahaven dashboard integration
# - Cron job last-run timestamp
# - Cron job error count
# - Blocker escalation count
```

---

## Acceptance Criteria

- [ ] **3 scripts validated** — manual execution successful, logs clean
- [ ] **Crontab entries created** — health-check (daily), phase-transition (hourly), blocker-detector (hourly)
- [ ] **Log directory setup** — /opt/spaceos/logs/cron/ létezik, permissions OK
- [ ] **First run validation** — minden script 1× lefutott, output ellenőrizve
- [ ] **Monitoring awareness** — log files elérhetők, cron hibák detektálhatók

---

## Deliverables (DONE outbox)

```markdown
---
type: done
priority: medium
ref: MSG-BACKEND-172
---

# DONE: Automation Scripts — Cron Integration

**Scripts Deployed:**
- ✅ mailbox/health-check.sh → daily @ 08:00
- ✅ dispatch/auto-phase-transition.sh → hourly @ :00
- ✅ monitoring/blocker-detector.sh → hourly @ :30

**Validation Results:**
- health-check.sh: [output summary]
- auto-phase-transition.sh: [output summary]
- blocker-detector.sh: [output summary]

**Crontab Backup:** /tmp/crontab.backup.[timestamp]
**Log Directory:** /opt/spaceos/logs/cron/

**Next Steps:**
- Monitor first automated runs (wait for next schedule)
- Validate log output quality
- Datahaven dashboard integration (future)
```

---

## Notes

**Priority Rationale:** MEDIUM
- **Not blocking:** Development continues without cron (scripts are manually runnable)
- **Operational improvement:** Automation reduces manual monitoring overhead
- **Strategic value:** Scales to 52 weeks/year, N terminals

**Estimated Effort:** 30-45 min (Haiku model, straightforward infra task)

**Dependencies:** None (scripts already exist and tested)

**Future Work:**
- Datahaven cron metrics dashboard
- Automated alerting (Slack/Telegram on critical blocker count)
- Script versioning + rollback mechanism

---

**Root Expectation:** DONE outbox within 24 hours (straightforward cron setup)
**Strategic Impact:** SpaceOS automation layer operational — proactive monitoring active
