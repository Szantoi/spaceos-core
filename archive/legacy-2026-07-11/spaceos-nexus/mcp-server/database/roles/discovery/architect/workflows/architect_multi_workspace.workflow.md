---
id: workflow-architect-multi-workspace
title: "Architect Multi-Workspace Communication Protocol"
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-02-16
---

# ?? Architect Multi-Workspace Workflow

**Szerepkör**: Senior Software Architect
**Scope**: Csak Multi-Workspace deployment esetén töltsd be ezt a fájlt
**Célja**: Communication Hub üzenetek olvasása, feldolgozása és válaszok küldése

---

## ? Mikor használd ezt a workflow-t?

**Runbook szabályozza:** A `architect.runbook.md` "Multi-Workspace Detection" szekciója határozza meg, hogy mikor kell betölteni ezt a fájlt.

## ?? 1. Fázis: Inbox Check Protocol

Használd a `scripts/communication-hub-helper.ps1` modult az inbox ellenõrzéséhez.

```powershell
# Turbo: check architect inbox
./scripts/communication-hub-helper.ps1 -CheckInbox -Role "Architect"
```

1. **Pending Messages**: Keresd a `docs/{project}/communication_hub/architect_inbox.md` fájlban a "PENDING" állapotú üzeneteket.
2. **Context Extraction**: Olvasd be a hivatkozott üzenet fájlt a `docs/{project}/communication_hub/messages/{date}/` mappából.

## ?? 2. Fázis: Message Processing

1. **Context Loading**: Töltsd be az üzenetben megjelölt kontextus fájlokat (pl. `goal.md`, `standards/`, `epic_plan.md`).
2. **Cognitive Patterns Alkalmazása**:
   - **Alternative Approach Pattern**: Vizsgálj meg technikai alternatívákat a kért döntéshez.
   - **Chain of Thought**: Vezesd le a választott építészeti irányt.
3. **Task Execution**: Hajtsd végre a kért építészeti feladatot (ADR írás, Epic Review, Design Audit).

## ?? 3. Fázis: Response Creation

Készíts választ a feladónak.

1. **Response Message**: Hozz létre egy új üzenet fájlt a `docs/{project}/communication_hub/messages/{date}/` mappában.
   - Használj egyedi ID-t: `msg-{timestamp}-architect-response.md`.
2. **Inbox Update**: Frissítsd az inbox bejegyzést "PROCESSED" állapotra és rögzítsd a válasz üzenet elérhetõségét.
3. **Partner Inbox Notification**: Értesítsd a cél-szerepkört (pl. Tech Lead vagy Orchestrator) az õ inbox fájljában.

## ?? Common Message Templates

### Arch-001: Epic Review Feedback
Használd, ha a Tech Lead-tõl érkezett Epic tervezési csomagot bírálod el.

**Template**: `src/agent-system/database/roles/discovery/architect/messages/tech_lead_epic_feedback.message.md`

### Arch-002: Strategic Architecture Sign-off
Használd az Epic lezárásakor, ha minden technikai DoD teljesült.

### Arch-003: Technical Blocker Escalation
Használd, ha olyan akadályba ütközöl, ami projekt szintû döntést igényel (vissza az Orchestrator-hoz).

---

## ? Befejezés

1. Frissítsd a `state.md` fájlt a fenti lépések eredményével.
2. Jelentsd a munka befejezését a User-nek (Product Owner).
3. **STOP**
