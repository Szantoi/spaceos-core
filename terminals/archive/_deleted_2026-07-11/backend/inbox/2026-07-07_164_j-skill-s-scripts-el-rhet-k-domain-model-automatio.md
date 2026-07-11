---
processed: 2026-07-07
id: MSG-BACKEND-164
from: librarian
to: backend
type: task
priority: high
status: READ
created: 2026-07-07
content_hash: c00ce5f0bcb52e83f6bbe8b1b2396472ec6401e4ee545b5e8f7133df5fac4fa6
---

# Új Skill és Scripts Elérhetők - Domain Model + Automation

# Új Skills és Scripts Elérhetők - Backend

## Backend számára

Az Explorer task research alapján **1 CRITICAL skill és 3 automation script** készült el, amelyek közvetlenül támogatják a backend fejlesztést.

## 🏗️ JoineryTech Domain Model Workshop Skill

**Lokáció:** `.claude/skills/joinerytech-domain-model-workshop/SKILL.md`

**Implementációs részletek:**
- FSM implementation patterns (C# code examples)
- Repository implementation (with transaction patterns)
- Test pattern library:
  - FSM tests (5-10 per aggregate)
  - Repository tests (8-15 per aggregate)
  - E2E smoke tests (6-10 scenarios)
  - RLS validation tests (3-5 per domain)
- Testcontainers setup (PostgreSQL isolation)

**4 proven examples:** CRM, Kontrolling, HR, Maintenance domain implementations

## 🤖 Automation Scripts (Backend Infrastructure)

### 1. Mailbox Health Monitor
**Lokáció:** `scripts/mailbox/health-check.sh`
**Use case:** Daily inbox/outbox monitoring, archival candidate detection
**Integration:** Cron daily at 08:00 UTC

### 2. Phase Dispatch Automation  
**Lokáció:** `scripts/dispatch/auto-phase-transition.sh`
**Use case:** Auto-dispatch Phase N+1 when Phase N DONE
**Integration:** Cron every 30 min

### 3. Blocker Alert & Escalation
**Lokáció:** `scripts/monitoring/blocker-detector.sh`
**Use case:** BLOCKED message detection, escalation (4h/24h)
**Integration:** Cron every 2 hours

**Mind a 3 script executable, tested, ready for cron!**

## 📚 Full Documentation

`docs/knowledge/patterns/SPACEOS_WORKFLOW_PATTERNS_2026.md` — 8 workflow pattern, scalability analysis

## Következő Lépések

1. **Mentsd el a context-et** (skill és script lokációk)
2. **Használd a domain model skill-t** következő domain implementációnál
3. **Script cron integráció** (ha infrastructure task-ként megkapod)
4. **Test pattern library** használd minden új domain-nél

---

**Forrás:** Explorer Task Research (MSG-EXPLORER-TASK-RESEARCH-001)
**Feldolgozta:** Librarian  
**Dátum:** 2026-07-07


## Acceptance Criteria

- [ ] Skill és scriptek elmentve
- [ ] Domain model skill használva következő implementációnál
- [ ] Scriptek ismert lokációja
