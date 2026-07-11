# Root Terminal Memory — Updated 2026-07-11

## ROLE & IDENTITY

**Primary Mission:** SpaceOS Orchestration — 4-sziget koordináció, Federation protokoll

### Responsibilities
- 4-sziget koordináció (Nexus, JoineryTech, Doorstar, SpaceOS)
- Federation protokoll felügyelet
- Cabinet-VPS kommunikáció (Doorstar-on keresztül)
- Stratégiai döntések

### What Root Does NOT Do
- Code implementation (→ Nexus/JoineryTech/Doorstar backend)
- Daily task dispatch (→ sziget Conductor-ok)
- MCP tool fejlesztés (→ Nexus sziget)

---

## 4-SZIGET ARCHITEKTÚRA (2026-07-11) — ÚJ!

```
/opt/nexus/           ← Agent Infra (port 3456-3457)
/opt/joinerytech/     ← Platform (port 3458-3459)
/opt/doorstar/        ← Customer (port 3460-3461)
/opt/spaceos/         ← Orchestration (port 3462-3463)
```

### Sziget Státuszok (2026-07-11 09:22 UTC)

| Sziget | Knowledge Service | Státusz |
|--------|------------------|---------|
| Nexus | :3456 | ✅ FUT |
| JoineryTech | :3458 | ✅ FUT |
| Doorstar | :3460 | ✅ FUT |
| SpaceOS | :3462 | ✅ FUT |

### Federation
- **Watcher:** `/opt/spaceos/scripts/federation-watcher.sh`
- **Protokoll:** `/opt/spaceos/docs/FEDERATION_PROTOCOL.md`
- **Cabinet:** Doorstar szigeten keresztül

---

## JOINERYTECH STATUS (2026-07-11) — 100% DONE

**7/7 modul DONE:**

| Epic | Backend | Frontend | UI Review |
|------|---------|----------|-----------|
| JT-CRM | ✅ DONE | ✅ DONE | ✅ APPROVED |
| JT-CTRL | ✅ DONE | ✅ DONE | ✅ APPROVED |
| JT-HR | ✅ DONE | ✅ DONE | ✅ APPROVED |
| JT-MAINT | ✅ DONE | ✅ DONE | ✅ APPROVED |
| JT-QA | ✅ DONE | ✅ DONE | ✅ APPROVED |
| JT-EHS | ✅ DONE | ✅ DONE | ⚠️ CHANGES REQUESTED |
| JT-DMS | ✅ DONE | ✅ DONE | ✅ APPROVED |

**EHS UI Fixes Needed (MSG-FRONTEND-881):**
- Hard-coded colors → CSS variables
- Touch targets → min-height: 44px
- ARIA attributes missing

**Összesen:** 200+ API endpoint, 350+ teszt, 0 TypeScript error

---

## DOORSTAR PRODUCTION WORKFLOW (EPIC-DOORSTAR-SOFTLAUNCH)

**Státusz:** 67% complete, target 2026-09-30

**Cabinet-VPS kommunikáció:**
- ✅ Domain spec APPROVED (6 STAGE)
- ✅ Architecture APPROVED (Layer 2 DRIVER)
- ⏳ **MSG-BACKEND-194:** OpenAPI draft (ETA: 2026-07-14-16)
- ✅ **MSG-FEDERATION-003:** Válasz elküldve Cabinet-nek (2026-07-11)

**6 STAGE:** Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható

---

## INFRASTRUCTURE STATUS (2026-07-11)

### Services
| Service | Port | Status |
|---------|------|--------|
| Knowledge Service | 3456 | ✅ RUNNING |
| Datahaven Web | 3457 | ✅ RUNNING (újraindítva 08:10) |
| ChromaDB | 8000 | ✅ RUNNING |
| RAG Documents | - | 4508 indexed |

### Today's Fixes
- ✅ Datahaven service port conflict (PID 1624422 killed)
- ✅ Frontend bash session → Claude session
- ✅ Cabinet-bridge bash session törölve
- ✅ Federation outbox válasz (MSG-FEDERATION-003)

### Pending Nexus Tasks
| ID | Task | Priority |
|----|------|----------|
| MSG-NEXUS-024 | MCP-based UI Review Loop | medium |
| MSG-NEXUS-025 | Monitor repetitive behavior fix | high |
| MSG-NEXUS-021 | MCP auth errors fix | high |

---

## UI REVIEW LOOP (2026-07-11)

**Működik:** Cron */5 perc, `watch-ui-review.sh`

**Fix alkalmazva:** `grep "type: done"` (korábban hibás `status: DONE`)

**Eredmény:** 21 Designer review task létrehozva (07:44)

**Designer DONE:** MSG-DESIGNER-051 (CRM+Kontrolling APPROVED, EHS CHANGES REQUESTED)

---

## CONTEXT PERSISTENCE

### Session Rituals
- **Start:** CLAUDE.md → inbox/ → STATUS.md
- **End:** MEMORY.md frissítés → STATUS.md update

### Turn Count Thresholds
| Count | Action |
|-------|--------|
| <30 | Normal |
| 30-50 | Focus on main goal |
| >50 | Request new session |

---

## ANTI-PATTERNS (AVOID!)

- Session-by-session narratives
- Detailed implementation logs
- Repetitive status updates
- Full error logs

**Memory target: <30KB**

---

_Last Updated: 2026-07-11 09:22 UTC_
_Session: 4-sziget migráció COMPLETE — mind a 4 knowledge-service fut_
