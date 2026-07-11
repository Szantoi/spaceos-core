---
description: how to execute the QA Tester Testing Loop for a single Task (Phase 4)
---

# QA Tester – Testing Loop (Fázis 4)

**Szerepkör:** QA Tester
**Trigger:** Orchestrator per-Task dispatch (P4: `qa_tester_testing.message.md`)
**FSM Output:** Task: `QA_WAITING` → `QA_IN_PROGRESS` → `Done` (APPROVED) / `Blocked` (REJECTED)
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 4

> ⚠️ **3-Strike Rule:** Ha egy Task 3-szor is REJECTED, automatikusan `ESCALATED` státuszba kerül → Tech Lead beavatkozás szükséges.

---

## Lépések

### 1. Input dokumentumok beolvasása

```
<EPIC_ROOT>/tasks/TASK-<N>-<M>.md                     ← Task "szerződés"
<EPIC_ROOT>/implementation-summary/TASK-<N>-<M>-*.md  ← Dev összefoglaló
state.md                                               ← Jelenlegi állapot (retry count)
```

Ellenőrizd:
- [ ] Task státusza `QA_WAITING`
- [ ] Implementation Summary létezik

### 2. FSM frissítés: QA_IN_PROGRESS

- `state.md` frissítés: Task → `QA_IN_PROGRESS`

### 3. Acceptance Criteria ellenőrzés

Menj végig a Task fájlban lévő **minden egyes Acceptance Criteriamon**. Minden tételt valós teszteléssel ellenőrizz:

- [ ] AC-01: [teszt leírás + eredmény]
- [ ] AC-02: ...
- [ ] AC-N: ...

### 4. Funkcionális és integrációs tesztek futtatása

// turbo
```powershell
# E2E / integrációs tesztek futtatása
dotnet test --filter Category=Integration

# Frontend E2E (ha releváns)
npm run test:e2e
```

### 5. Döntés: APPROVED vagy REJECTED?

#### Ha APPROVED ✅

- [ ] Minden AC teljesítve
- [ ] Minden teszt zöld
- Hozd létre a QA Sign-off dokumentumot:
  ```
  <EPIC_ROOT>/qa/TASK-<N>-<M>-qa-approved.md
  ```
- Sablon: `src/agent-system/database/roles/engineering/templates/qa_signoff.template.md`
- `state.md` frissítés: Task → `Done`
- Üzenet Tech Leadnek: `messages/tech-lead/<timestamp>_from-qa_task-approved.md` (P15)

#### Ha REJECTED ❌

- [ ] Dokumentáld pontosan melyik AC bukott és miért
- Frissítsd a Blocker Register-t:
  ```
  <EPIC_ROOT>/qa/TASK-<N>-<M>-qa-rejected-<retry_count>.md
  ```
- `state.md` frissítés: `fsm_retry_count` += 1
- `state.md` frissítés: Task → `Blocked`

**3-Strike Rule ellenőrzés:**
- Ha `fsm_retry_count < 3`: Bug Fix dispatch → Developernek (P13/P14)
- Ha `fsm_retry_count >= 3`: `state.md` → `ESCALATED`, Tech Lead értesítése

### 6. Bug Fix Dispatch (ha REJECTED és < 3 retry)

Bug fix üzenet küldése:
- Backend: `messages/backend-developer/<timestamp>_from-qa_bug-fix.md` (P13)
- Frontend: `messages/frontend-developer/<timestamp>_from-qa_bug-fix.md` (P14)

Az implementáció után → vissza Fázis 4 elejére (ez a loop).

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| QA Sign-off (APPROVED) | `<EPIC_ROOT>/qa/TASK-<N>-<M>-qa-approved.md` |
| QA Rejection (REJECTED) | `<EPIC_ROOT>/qa/TASK-<N>-<M>-qa-rejected-<N>.md` |

## Következő fázis (ha minden Task Done)

→ **Fázis 5:** `tech-lead-epic-review.md`
