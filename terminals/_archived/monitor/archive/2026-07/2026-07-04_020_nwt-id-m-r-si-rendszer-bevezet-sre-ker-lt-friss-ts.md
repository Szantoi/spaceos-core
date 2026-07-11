---
id: MSG-MONITOR-020
from: root
to: monitor
type: task
priority: medium
status: READ
model: haiku
created: 2026-07-04
completed: 2026-07-04 13:27:15
content_hash: cd95aa4587c0d37718c742a76ab97fe250f1a1630fd16464215fdf0344fa3798
---

# NWT időmérési rendszer bevezetésre került — frissítsd a health check logikát

## Összefoglaló

A SpaceOS-ban bevezetésre került az **NWT (Nightwatch Tick)** időmérési rendszer. Ez a rendszer egységes időmérést biztosít az összes agent művelethez.

**1 NWT = 2 perc = 1 Nightwatch ciklus** — a rendszer "szívverése"

## Változások

### 1. Új konstansok (`spaceos-nexus/knowledge-service/src/constants/nwt.ts`)

- `NWT_MS = 120_000` (2 perc ms-ben)
- `NWT_TIMEOUTS` — minden timeout NWT-ben definiálva
- `NWT_ESTIMATES` — task becslési guide

### 2. Pipeline timeout-ok migrálva

| Fájl | Változás |
|------|----------|
| `watchInbox.ts` | NUDGE_COOLDOWN, AUTOSTART timeout-ok NWT-re |
| `watchIdle.ts` | IDLE_TIMEOUT, INJECTED_ESCALATION NWT-re |
| `watchStuck.ts` | STUCK_COOLDOWN NWT-re |
| `watchMonitor.ts` | MONITOR_MIN_GAP NWT-re |
| `nightwatch.ts` | Scheduler intervallum NWT_MS-re |
| `taskEscalation.ts` | retryIntervalNWT |

### 3. EPICS.yaml frissítve

Minden aktív/pending epic-nek van `estimated_nwt` mezője.

## Monitor Teendők

1. **Health check prompt frissítése** — Ha timeout-okat ellenőrzöl, használd az NWT skálát
2. **Stuck detection** — 2 NWT (~4 perc) = stuck session
3. **Idle warning** — 5 NWT (~10 perc) = idle warning
4. **Task timeout** — 15 NWT (~30 perc) = task warning

## NWT Skálák (referencia)

| NWT | Idő | Jelentés |
|-----|-----|----------|
| 1 | 2 min | Egy Nightwatch ciklus |
| 3 | 6 min | Inbox nudge threshold |
| 5 | 10 min | Monitor health check intervallum |
| 8 | 16 min | Idle session shutdown |
| 15 | 30 min | Task warning |
| 30 | 1 óra | Standard task |
| 240 | 8 óra | Agent munkanap |

## Dokumentáció

- `docs/WORKFLOW.md` — 16. szekció: NWT dokumentáció
- `terminals/conductor/CLAUDE.md` — NWT becslési irányelvek
- `docs/projects/EPICS.yaml` — NWT becslések epic-enként

## Acceptance Criteria

- [ ] Health check logika NWT-aware
- [ ] Timeout ellenőrzések NWT skálát használnak
- [ ] MEMORY.md frissítve NWT referenciával
