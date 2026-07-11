# Marvin Planning Scheduler

> **Status:** Implemented (MSG-NEXUS-013)
> **Requires:** OPENAI_API_KEY for full operation

## Overview

Replaces bash cron (`plan-scan.sh`, `plan-select.sh`, `plan-debate.sh`) with Marvin-native async scheduler.

## Architecture

```
planning_scheduler.py
  ├─ scan_task() — Scans one segment every 10 min
  │  ├─ Rotating segment selection
  │  ├─ Knowledge Service integration
  │  └─ Idea extraction with Marvin
  │
  ├─ select_task() — Selects top ideas when threshold reached
  │  ├─ Triggered when IDEAS_DIR >= 5 files
  │  └─ WebSearch validation (TODO)
  │
  └─ debate_task() — Parallel Pro/Con debate
     ├─ Planner-A vs Planner-B
     └─ Consensus synthesis
```

## Usage

### Manual Tasks

```bash
cd /opt/spaceos/spaceos-nexus/marvin
source venv/bin/activate

# Run single scan
python planning_scheduler.py scan

# Run selection
python planning_scheduler.py select

# Run debate
python planning_scheduler.py debate
```

### Scheduler Daemon

```bash
# Run scheduler (default: 10 min interval)
python planning_scheduler.py run

# Custom interval (e.g., 5 minutes = 300 seconds)
python planning_scheduler.py run 300
```

### Systemd Service

```bash
# Install
sudo cp spaceos-marvin-scheduler.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable spaceos-marvin-scheduler

# Start
sudo systemctl start spaceos-marvin-scheduler

# Status
sudo systemctl status spaceos-marvin-scheduler
sudo journalctl -u spaceos-marvin-scheduler -f
```

## Configuration

### Environment Variables

Create `/opt/spaceos/spaceos-nexus/marvin/.env`:

```bash
OPENAI_API_KEY=sk-...
SPACEOS_ROOT=/opt/spaceos
```

### Thresholds

Edit `planning_scheduler.py`:

```python
IDEAS_THRESHOLD = 5   # Trigger select when this many ideas
QUEUE_THRESHOLD = 2   # Notify Conductor when queue has this many
```

### Segments

Segments are defined in `SEGMENTS` list:

```python
SEGMENTS = [
    ("fe-memory", "frontend/joinerytech-portal/MEMORY.md"),
    ("kernel-memory", "backend/spaceos-kernel/MEMORY.md"),
    ...
]
```

## Integration with Bash Pipeline

During transition period, both can run:

1. **Bash cron** — Existing pipeline (stable)
2. **Marvin scheduler** — New async pipeline (testing)

Once validated, disable bash cron:

```bash
# Disable plan-scan.sh cron
crontab -e
# Comment out: */10 * * * * /opt/spaceos/scripts/plan-scan.sh
```

## Limitations

**Current (OPENAI_API_KEY not configured):**
- `scan_task()` cannot call Marvin `extract()` (requires API)
- `select_task()` and `debate_task()` are stubs (full implementation pending)

**Full Operation Requires:**
1. Configure OPENAI_API_KEY in `.env`
2. Test manual scan: `python planning_scheduler.py scan`
3. Validate ideas saved to `docs/planning/ideas/`
4. Enable systemd service

## Logging

```bash
# View scheduler logs
sudo journalctl -u spaceos-marvin-scheduler -f

# View last 100 lines
sudo journalctl -u spaceos-marvin-scheduler -n 100

# View logs since 1 hour ago
sudo journalctl -u spaceos-marvin-scheduler --since "1 hour ago"
```

## Troubleshooting

### Scheduler Won't Start

```bash
# Check service status
sudo systemctl status spaceos-marvin-scheduler

# Check environment file
cat /opt/spaceos/spaceos-nexus/marvin/.env

# Test manually
cd /opt/spaceos/spaceos-nexus/marvin
source venv/bin/activate
python planning_scheduler.py scan
```

### No Ideas Generated

1. **Check OPENAI_API_KEY:** Must be valid OpenAI API key
2. **Check Knowledge Service:** `curl http://localhost:3456/health`
3. **Check segment files:** Verify paths in `SEGMENTS` exist

### High API Costs

Reduce scan frequency:

```bash
# Edit service file
sudo nano /etc/systemd/system/spaceos-marvin-scheduler.service

# Change interval to 30 minutes (1800 seconds)
ExecStart=... planning_scheduler.py run 1800

# Reload and restart
sudo systemctl daemon-reload
sudo systemctl restart spaceos-marvin-scheduler
```

## Future Enhancements

- [ ] Actual idea loading from `IDEAS_DIR`
- [ ] WebSearch MCP tool integration
- [ ] Consensus saving to `QUEUE_DIR`
- [ ] Conductor inbox notification (Telegram)
- [ ] Cost tracking and budget limits
- [ ] Retry logic for API failures
