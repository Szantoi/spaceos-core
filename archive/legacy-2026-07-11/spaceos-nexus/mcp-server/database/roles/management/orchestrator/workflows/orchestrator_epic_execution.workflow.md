---
id: workflow-orchestrator-epic-execution
title: "Orchestrator Epic Execution Management Workflow"
type: workflow
scope: global
category: agile-workflow
last_updated: 2026-02-18
---

# ?? Orchestrator: Epic Megvalósítási Levezénylési Workflow

**Szerepkör**: Orchestrator
**Küldetés**: Egy Epic teljes életciklusának koordinálása a Planning-tõl a Closure-ig, az összes ágens orchestrálásával.

---

## ?? Cognitive Setup

1.  **Orchestration Pattern**: Te vagy a karmester - minden ágensnek precíz utasításokat adsz, és nyomon követed az elõrehaladást.
2.  **State Tracking Mindset**: **MINDEN fázis után frissítsd a state.md**-t! Context loss = projekt halál.
3.  **Decision Logging**: Minden jelentõs döntésed (Epic prioritás, blocker handling, resource allocation) kerüljön a decision_log.md-ba.

---

## ?? Required Skills & Context

**Töltsd be KÖTELEZÕEN:**

| Skill/Document | Fizikai útvonal | Miért Kell |
|:---------------|:---------------|:-----------|
| **VS Code Copilot Agent Operating Model** | [../../../knowledge/management/vscode_copilot_agent_operating_model.knowledge.md](../../../../knowledge/management/vscode_copilot_agent_operating_model.knowledge.md) | **[KRITIKUS]** Mûködési modell megértése: virtual multi-agent, role switching, context management, state persistence |
| **Project Documentation Structure** | [../../../knowledge/management/project_documentation_structure.knowledge.md](../../../../knowledge/management/project_documentation_structure.knowledge.md) | Dokumentációs struktúra, file ownership, validációs checklistek |
| **Task Dispatching Logic** | [../../../knowledge/management/orchestrator_dispatching.knowledge.md](../../../../knowledge/management/orchestrator_dispatching.knowledge.md) | Task kiosztási logika, dependency check, quality gate |
| **Prompt Engineering** | [../../../../knowledge/core/prompt_engineering.knowledge.md](../../../../knowledge/core/prompt_engineering.knowledge.md) | Ágens kommunikáció, Mega-Prompt konstruálás |
| **Project Goal** | `docs/{project}/goal.md` | Üzleti cél, success kritériumok, scope |
| **Project State** | `docs/{project}/state.md` | Aktuális Epic/Task állapotok, blocker-ek, next steps |
| **Dependency Map** | `docs/{project}/dependency_map.md` | Epic/Task függõségek, critical path, circular dependencies |

---

## ?? Epic Execution Lifecycle (7 Fázis)

### **Fázis 0: Pre-flight Check (Projektindítási Validáció)**

**Cél**: Biztosítani, hogy a projekt dokumentációs infrastruktúra rendben van.

#### 0.1 Dokumentációs Validáció

**Checklist**:

- [ ] `docs/{project}/goal.md` létezik (használd a [project_documentation_structure.knowledge.md](../../../../knowledge/management/project_documentation_structure.knowledge.md) "Validációs Checklist" szekcióját)
- [ ] `docs/{project}/state.md` létezik és naprakész (ha nem: használd a [project_state_snapshot.message.md](../messages/project_state_snapshot.message.md)-t)
- [ ] `docs/{project}/dependency_map.md` létezik (ha van legalább 1 Epic)
- [ ] `docs/{project}/orchestrator_decision_log.md` létezik
- [ ] Nincs Critical Blocker a state.md Blocker Register-ben

**Ha bármi hiányzik:**

```powershell
# Ha state.md hiányzik vagy elavult
# Használd: src/agent-system/database/roles/management/orchestrator/messages/project_state_snapshot.message.md
# 4-phase discovery – 3 dokumentum automatikus generálás

# Ha goal.md hiányzik
# STOP! Projekt nem indítható goal.md nélkül! Kérj Product Owner-t vagy Stakeholder-t, hogy definiálja a projektet.
```

#### 0.2 Context Loading

**Tölts be (reasoning fázis)**:

1.  **Project Goal**: `docs/{project}/goal.md` - Üzleti cél, persona, success kritériumok, constraints
2.  **Current State**: `docs/{project}/state.md` - Epic State Map, Task State Map, Blocker Register, Next Steps Queue
3.  **Dependencies**: `docs/{project}/dependency_map.md` - Dependency Matrix, Critical Path, Circular Dependency Check
4.  **Decision History**: `docs/{project}/orchestrator_decision_log.md` - Korábbi döntések, rationale-ek

**Gondold végig:**

- Melyik Epic-et kell most indítani? (Priority, Dependencies alapján)
- Van-e blocker, ami megakadályozná az indítást?
- Hány Epic fut párhuzamosan? (Ne terhelj túl!)

#### 0.3 Deployment Mode Selection

**Döntés: Single-Workspace vagy Multi-Workspace deployment?**

Lásd: [vscode_copilot_agent_operating_model.knowledge.md](../../../../knowledge/management/vscode_copilot_agent_operating_model.knowledge.md) - "Advanced Pattern: Multi-Workspace Parallel Execution"

**Single-Workspace (Default)**:

- ? Kis-közepes projektek (< 10 Epic, < 50 Task)
- ? 1-2 developer párhuzamos munka
- ? Gyors setup
- ? Context token limite (200k) gyorsan elérve nagy projekteknél
- ? Szekvenciális végrehajtás (nincs valós párhuzamosítás)

**Multi-Workspace (Advanced)**:

- ? Nagy projektek (10+ Epic, 50+ Task)
- ? 3+ developer/ágens párhuzamos munka
- ? Token efficiency (context isolation)
- ? Valós párhuzamosítás
- ? Komplexebb setup (N VS Code ablak + communication_hub)
- ? Message queue management overhead

**Ha Multi-Workspace deployment-et választasz:**

1.  **Communication Hub Initialize** (Elsõ indításkor):

    ```markdown
    - [ ] Hozd létre: `docs/{project}/communication_hub/` mappát
    - [ ] Hozd létre az alábbi struktúrát:
      - `messages/` - Date-sharded message storage (`messages/{YYYY-MM-DD}/msg-{id}-{from}-to-{to}.md`)
      - `daily_summary/` - Daily summary files (`daily_summary/{YYYY-MM-DD}.md`)
      - `inbox/` - Link-based inbox tables:
        - `orchestrator_inbox.md` - Orchestrator-nek címzett messages (link table)
        - `architect_inbox.md` - Architect-nek címzett messages (link table)
        - `tech_lead_inbox.md` - Tech Lead-nek címzett messages (link table)
        - `backend_developer_inbox.md` - Backend Developer-nek címzett messages (link table)
        - `frontend_developer_inbox.md` - Frontend Developer-nek címzett messages (link table)
        - `qa_tester_inbox.md` - QA Tester-nek címzett messages (link table)
    ```

    **PowerShell Helper Module**:

    ```powershell
    # Load helper functions
    Import-Module .\scripts\communication-hub-helper.ps1

    # Verify setup
    Get-MessageStatistics
    ```

2.  **VS Code Workspace Setup**:

    ```powershell
    # Orchestrator (Main)
    code c:\Path\To\JoineryTech.Flow

    # Architect (Új ablak)
    code c:\Path\To\JoineryTech.Flow --new-window

    # Tech Lead (Új ablak)
    code c:\Path\To\JoineryTech.Flow --new-window

    # Backend Developer (Új ablak)
    code c:\Path\To\JoineryTech.Flow --new-window

    # Frontend Developer (Új ablak)
    code c:\Path\To\JoineryTech.Flow --new-window

    # QA Tester (Új ablak - opcionális)
    code c:\Path\To\JoineryTech.Flow --new-window
    ```

3.  **Workflow Adaptation**:
    -   **Fázis 1-6**: Orchestrator message-eket küld (`New-Message` PowerShell helper), ágensek inbox-ot poll-oznak (`Get-PendingMessages`)
    -   **State Tracking**: state.md frissítés MINDEN fázis után (Orchestrator felelõssége!)
    -   **Message Monitoring**: PowerShell helper használata új responses számára:

        ```powershell
        # Check for completed messages
        Get-PendingMessages -Role "orchestrator" |
          Where-Object { $_.Status -eq "completed" }

        # Read specific message
        Read-Message -MessageId "msg-XXX"

        # Get daily summary
        Get-DailySummary
        ```

---

#### 0.4 Git Branch Létrehozása

**Cél**: Minden Epic saját izolált Git branch-en fut, hogy a kód- és dokumentációmódosítások nyomon követhetõk legyenek, és a `develop` ág tiszta maradjon.

**Branch Convention:**

```
epic/{EPIC_ID}-{epic-short-name}
```

**Példák:**

-   `epic/EPIC-001-invoicing`
-   `epic/EPIC-003-user-auth`
-   `epic/EPIC-007-dxf-importer`

**Lépések (az Epic indítása ELÕTT kötelezõ):**

```powershell
# 1. Checkout develop és frissítés
git checkout develop
git pull origin develop

# 2. Új Epic branch létrehozása
git checkout -b epic/{EPIC_ID}-{epic-short-name}

# 3. Branch remote push
git push -u origin epic/{EPIC_ID}-{epic-short-name}

# 4. Ellenõrzés
git branch -vv
```

**State Tracking és jegyezd fel az Epic branch nevét:**

```markdown
<!-- docs/{project}/state.md Epic State Map-be add hozzá: -->
| {EPIC_ID} | {EPIC_TITLE} | Planning | epic/{EPIC_ID}-{epic-short-name} | ... |
```

> **?? Fontos**: Az ágensek NEM váltanak branch-t! Minden dokumentáció és kódmódosítás az Orchestrator által menedzselt `epic/...` branch-re kerül commit-olva. A `develop` és `main` ágak csak PR-on keresztül frissülnek.

---

### **Fázis 1: Epic Planning (Architect Bevonás)**

**Cél**: Architektúrális terv elkészítése az Epic-hez.

#### 1.1 Architect Dispatch

**?? Ágensváltás: Architect Role-ra**

**?? Használandó Prompt**:

-   **P5** (inverted): User már megadta az Epic-et, most Architect-et kell instruálni
-   **Manuális Mega-Prompt** (nincs elõre elkészített sablon erre):

```markdown
**Role**: Te most a projekt Senior Architect-je vagy.

**Mission**: Tervezd meg a következõ Epic architektúráját.

**Epic Details**:
- **Epic ID**: {EPIC_ID}
- **Epic Title**: {EPIC_TITLE}
- **Epic Goal**: {EPIC_GOAL} (a user által megadott)
- **Constraints**: {Technológiai/idõbeli/erõforrás korlátok}

**Context for You**:
- **Project Goal**: docs/{project}/goal.md
- **Existing Architecture**: docs/{project}/standards/architecture_standards.md (ha létezik)
- **Dependencies**: docs/{project}/dependency_map.md (ellenõrizd, mely Epic-ektõl függ ez az Epic)

**Your Workflow**:
- Kövesd pontosan: src/agent-system/database/roles/discovery/architect/architect.workflow.md
- Töltsd be: src/agent-system/database/knowledge/discovery/design_thinking_architecture.knowledge.md (5-phase Design Thinking: Empathize – Define – Ideate – Prototype – Test)
- **Output**: docs/{project}/epics/{EPIC_ID}/plan.md (használd: src/agent-system/database/roles/discovery/architect/templates/epic_plan.template.md)

**Success Criteria**:
- [ ] Epic Plan létrehozva (plan.md)
- [ ] Legalább 2 alternatíva értékelve (Design Thinking Ideate fázis)
- [ ] ADR készítve kritikus döntésekhez (docs/{project}/decisions/ADR-XXX.md)
- [ ] Epic dependencies dokumentálva
- [ ] Tech Lead handoff notes megadva

**Start Analysis**: Kezdd a project goal és az Epic goal összehangolásának elemzésével.
```

#### 1.2 Epic Plan Validation

**Architect outputjának ellenõrzése:**

```powershell
# 1. Epic plan.md létezik?
if (-not (Test-Path "docs/{project}/epics/{EPIC_ID}/plan.md")) {
    ERROR: "Epic plan.md hiányzik! Architect workflow nem fejezõdött be!"
    # Visszadobás Architect-nek
}

# 2. Frontmatter validáció (type: epic-plan, id, title, scope, created, last_updated)
# 3. Szekciók teljesek? (Epic Goal, Architecture, Quality Attributes, Dependencies, ADR refs, Tech Stack, Risk Analysis, Success Criteria)
```

#### 1.3 State Tracking (Fázis 1 után)

**?? Frissítsd KÖTELEZÕEN:**

1.  **FSM State Frissítés (MCP)**:
    -   Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{EPIC_ID}", "action": "success" }` paraméterekkel.
    -   Ezután kérd le a jelenlegi állapotot a `get_workflow_state` MCP eszközzel, és jegyezd fel a kapott állapotot a `state.md`-ben.
    -   Next Steps: "Tech Lead - Task Breakdown - {EPIC_ID}"

2.  **dependency_map.md** (ha új Epic):
    -   Dependency Matrix: új sor hozzáadása `{EPIC_ID}`-hez
    -   Dependency Graph (Mermaid): új Epic node hozzáadása

3.  **decision_log.md**:
    -   Decision Entry: "DEC-XXX: {EPIC_ID} Architecture Design"
    -   Context: Miért választottuk ezt az Epic-et most?
    -   Alternatives: Alternatív megközelítések (Architect által dokumentált)
    -   Decision: Milyen architektúra lett elfogadva
    -   Impact: Mely Epic-eket érinti ez

---

### **Fázis 2: Task Planning (Tech Lead Bevonás)**

**Cél**: Epic lebontása konkrét, implementálható Task-okra.

#### 2.1 Tech Lead Dispatch

**?? Ágensváltás: Tech Lead Role-ra**

**?? Használandó Prompt**:

-   **P1**: [tech_lead_epic_planning.message.md](../messages/tech_lead_epic_planning.message.md)
-   **Paraméterek**: `{EPIC_ID}`, `{project}`
-   **Mikor**: Epic plan.md elkészült, Task-okra bontás indítása

**Példa prompt használat**:

```markdown
[BETÖLTÖD A P1 PROMPT SABLONT]

Paraméterek behelyettesítése:
- {EPIC_ID} = EPIC-001
- {project} = joinerytech-flow

Tech Lead-nek átadod a vezérlést.
```

#### 2.2 Task Plan Validation

**Tech Lead outputjának ellenõrzése:**

```powershell
# 1. Minden Task-hoz van TASK-XXX.md?
$tasks = Get-ChildItem "docs/{project}/epics/{EPIC_ID}/tasks/*.md" -Exclude "implementation_reports", "qa_reports"
if ($tasks.Count -eq 0) {
    ERROR: "Nincs Task fájl! Tech Lead workflow nem fejezõdött be!"
    # Visszadobás Tech Lead-nek
}

# 2. Minden TASK-XXX.md tartalmaz:
#    - Frontmatter (type: task, id, epic, title, assigned_role, state, created)
#    - Task Goal
#    - Implementation Details
#    - Acceptance Criteria (DoD)
#    - Dependencies (Task-Task függõség)

# 3. Circular Dependency Check (használd a dependency_map.md-t)
```

#### 2.3 Devil's Advocate Review (Opcionális, De AJÁNLOTT)

**Ha magas kockázatú Epic** (új technológia, komplex domain):

**?? Használandó Prompt**:

-   **P17**: [devils_advocate/messages/tech_lead_task_critique.message.md](../../devils_advocate/messages/tech_lead_task_critique.message.md)
-   **Mikor**: Task Plans elkészültek, kritikai validáció kérése
-   **Paraméterek**: `{EPIC_ID}`, `{TASK_ID_list}`

**Devil's Advocate képes azonosítani:**

-   Hiányzó edge case-eket
-   Függõségi problémákat
-   DoD hiányosságokat
-   Over-/Under-engineering jeleit

#### 2.4 State Tracking (Fázis 2 után)

**?? Frissítsd KÖTELEZÕEN:**

1.  **FSM State Frissítés (MCP)**:
    -   Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{EPIC_ID}", "action": "success" }` paraméterekkel.
    -   Ezután kérd le a jelenlegi állapotot a `get_workflow_state` MCP eszközzel, és jegyezd fel a kapott állapotot a `state.md`-ben.
    -   Task State Map: Új sorok minden TASK-XXX-hez a kezdõ FSM állapottal (pl. `BACKLOG_READY`).
    -   Next Steps: "Backend Developer - TASK-001", "Frontend Developer - TASK-002", ...

2.  **dependency_map.md**:
    -   Task-szintû függõségek hozzáadása (Epic-en belül)
    -   Critical Path Analysis frissítése (ha változik)

3.  **decision_log.md**:
    -   Decision Entry: "DEC-XXX: {EPIC_ID} Task Decomposition Strategy"
    -   Context: Miért ezt a task bontási stratégiát választottuk? (Vertical Slice / Horizontal / Event Storming?)
    -   Alternatives: Tech Lead által dokumentált alternatívák
    -   Decision: Végleges task struktúra
    -   Impact: Task számok, estimated effort, dependencies

---

### **Fázis 3: Implementation (Developer Dispatch Ciklus)**

**Cél**: Task-ok implementálása Backend/Frontend Developer-ek által.

#### 3.1 Task Dispatching Loop

**Algoritmus (használd az [orchestrator_dispatching.knowledge.md](../../../../knowledge/management/orchestrator_dispatching.knowledge.md)-t):**

```python
while (van "Todo" Task):
    # 1. Dependency Check
    task = select_next_task_with_dependencies_met()

    # 2. Role Selection
    role = task.assigned_role  # Backend Developer / Frontend Developer

    # 3. Quality Gate
    if (task.dependencies NOT met):
        SKIP this task
        continue

    # 4. Dispatch
    if (role == "Backend Developer"):
        use_prompt(P2, backend_developer_task_implementation.message.md)
    elif (role == "Frontend Developer"):
        use_prompt(P3, frontend_developer_task_implementation.message.md)

    # 5. FSM State Tracking (azonnal!)
    request_workflow_transition(task.id, "success")
    # 6. Wait for Implementation Report
    wait_for(implementation_report)

    # 7. FSM State Tracking (implementation után!)
    request_workflow_transition(task.id, "success")
```

#### 3.2 Backend Developer Dispatch (Task-onként)

**?? Ágensváltás: Backend Developer Role-ra**

**?? Használandó Prompt**:

-   **P2**: [backend_developer_task_implementation.message.md](../messages/backend_developer_task_implementation.message.md)
-   **Paraméterek**: `{TASK_ID}`, `{EPIC_ID}`
-   **Mikor**: Backend Task State = "Todo" ÉS Dependencies teljesültek

**Példa**:

```markdown
[BETÖLTÖD A P2 PROMPT SABLONT]

Paraméterek:
- {TASK_ID} = TASK-001
- {EPIC_ID} = EPIC-001
- {project} = joinerytech-flow

Backend Developer-nek átadod a vezérlést.
```

**Elvárt Output**: `docs/{project}/epics/{EPIC_ID}/tasks/implementation_reports/TASK-XXX_backend_implementation_report.md`

#### 3.3 Frontend Developer Dispatch (Task-onként)

**?? Ágensváltás: Frontend Developer Role-ra**

**?? Használandó Prompt**:

-   **P3**: [frontend_developer_task_implementation.message.md](../messages/frontend_developer_task_implementation.message.md)
-   **Paraméterek**: `{TASK_ID}`, `{EPIC_ID}`
-   **Mikor**: Frontend Task State = "Todo" ÉS Dependencies teljesültek

**Elvárt Output**: `docs/{project}/epics/{EPIC_ID}/tasks/implementation_reports/TASK-XXX_frontend_implementation_report.md`

#### 3.4 Implementation Report Validation

**Developer outputjának ellenõrzése (minden Task után!):**

```powershell
# 1. Implementation Report létezik?
if (-not (Test-Path "docs/{project}/epics/{EPIC_ID}/tasks/implementation_reports/TASK-XXX_*_implementation_report.md")) {
    ERROR: "Implementation Report hiányzik! Task NEM mehet QA-ba!"
    # Task State marad "In Progress"
    # Kérd el újra a Developer-tõl
}

# 2. Report tartalmaz:
#    - Implementation Summary
#    - Changed Files
#    - Key Design Decisions
#    - Testing (unit/integration tests írva)
#    - Edge Cases Handled
#    - Known Limitations
#    - Next Steps (QA Handoff)
```

### **Fázis 4: QA Testing (QA Tester Dispatch Ciklus)**

**Cél**: Task-ok tesztelése és QA Sign-off megszerzése.

#### 4.1 QA Dispatching Loop

**Algoritmus:**

```python
while (van "Ready for QA" Task):
    # 1. Task Selection
    task = select_next_qa_ready_task()

    # 2. QA Dispatch
    use_prompt(P4, qa_tester_testing.message.md)

    # 3. FSM State Tracking (azonnal!)
    # Nincs szükség manuális state.md átírásra, a transition tool végzi
    request_workflow_transition(task.id, "success")

    # 4. Wait for QA Signoff
    signoff = wait_for(qa_signoff)

    # 5. Decision Point
    if (signoff.status == "APPROVED"):
        request_workflow_transition(task.id, "success")
    elif (signoff.status == "REJECTED"):
        # Bug fix cycle
        request_workflow_transition(task.id, "fail")
        create_blocker(task.id, signoff.bug_list)
        re_dispatch_to_developer(task.id, signoff.bug_list)
```

#### 4.2 QA Tester Dispatch (Task-onként)

**?? Ágensváltás: QA Tester Role-ra**

**?? Használandó Prompt**:

- **P4**: [qa_tester_testing.message.md](../messages/qa_tester_testing.message.md)
- **Paraméterek**: `{TASK_ID}`, `{EPIC_ID}`
- **Mikor**: Task State = "Ready for QA" (implementation report létezik)

**Példa**:

```markdown
[BETÖLTÖD A P4 PROMPT SABLONT]

Paraméterek:
- {TASK_ID} = TASK-001
- {EPIC_ID} = EPIC-001
- {project} = joinerytech-flow

QA Tester-nek átadod a vezérlést.
```

**Elvárt Output**: `docs/{project}/epics/{EPIC_ID}/tasks/qa_reports/TASK-XXX_qa_signoff.md`

#### 4.3 QA Signoff Validation

**QA Tester outputjának ellenõrzése:**

```powershell
# 1. QA Signoff létezik?
if (-not (Test-Path "docs/{project}/epics/{EPIC_ID}/tasks/qa_reports/TASK-XXX_qa_signoff.md")) {
    ERROR: "QA Signoff hiányzik! Task NEM zárható le!"
    # Task State marad "In QA"
}

# 2. Signoff tartalmaz:
#    - Testing Summary
#    - Test Cases Executed
#    - Test Results (Pass/Fail)
#    - Bug List (ha van)
#    - QA Sign-off Decision (APPROVED/REJECTED/CONDITIONAL)

# 3. Decision Point
$signoff_status = extract_signoff_status()
if ($signoff_status -eq "REJECTED") {
    # Bug fix cycle indítása
    # Lásd: 4.4 Bug Fix Cycle
}
```

#### 4.4 Bug Fix Cycle (Ha QA REJECTED)

**Blocker kezelés:**

1. **Blocker Register frissítés** (state.md):

   ```markdown
   | Blocker ID | Affected Entity | Description | Severity | Owner | Date Identified | Status |
   |:-----------|:----------------|:------------|:---------|:------|:----------------|:-------|
   | BLK-001 | TASK-001 | QA Rejected: [Bug List] | High | Backend Developer | 2026-02-16 | Active |
   ```

2. **Developer Re-dispatch**:
   - **P14**: [qa_tester/messages/backend_developer_bug_fix.message.md](../../qa_tester/messages/backend_developer_bug_fix.message.md) vagy
   - **P15**: [qa_tester/messages/frontend_developer_bug_fix.message.md](../../qa_tester/messages/frontend_developer_bug_fix.message.md)
   - Paraméterek: `{TASK_ID}`, `{BUG_LIST}`, `{QA_SIGNOFF_PATH}`

3. **Re-test**:
   - Bug fix után: Task State = "Ready for QA" (újra)
   - QA újra tesztel (P4 prompt ismét)

#### 4.5 State Tracking (Minden Task QA után!)

**?? Frissítsd KÖTELEZÕEN:**

1.  **FSM State Frissítés (MCP)**:
    -   Ha APPROVED: Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{TASK_ID}", "action": "success" }` paraméterekkel.
    -   Ha REJECTED: Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{TASK_ID}", "action": "fail" }` paraméterekkel.
    -   Next Steps: Ha minden Task Done › "Tech Lead - Epic Review"

2.  **decision_log.md** (ha bug fix strategy döntés volt):
    -   Decision Entry: "DEC-XXX: {TASK_ID} Bug Fix Priority"
    -   Context: Miért blocker ez a bug? (Severity, Impact)
    -   Alternatives: Immediate fix / Defer to next Epic / Workaround?
    -   Decision: Végleges stratégia
    -   Impact: Timeline, scope, quality

---

### **Fázis 5: Epic Review (Tech Lead Closure)**

**Cél**: Epic összegzés, lessons learned dokumentálása, calibration instructions generálása.

#### 5.1 Epic Completion Check

**Validáció (használd a [project_documentation_structure.knowledge.md](../../../../knowledge/management/project_documentation_structure.knowledge.md) "Validációs Checklist - Epic Closure elõtt" szekcióját):**

```powershell
# 1. Minden Task Done? (Ellenõrizd az MCP-vel!)
# get_workflow_state project_id={TASK_ID}

# 2. Minden Task-hoz van Implementation Report?
# 3. Minden Task-hoz van QA Signoff (APPROVED)?
```

#### 5.2 Tech Lead Epic Review Dispatch

**?? Ágensváltás: Tech Lead Role-ra**

**?? Használandó Prompt**:

-   **Manuális Mega-Prompt** (használd a P1 prompt struktúráját, de "Epic Review" kontextussal):

```markdown
**Role**: Te most a projekt Tech Lead-je vagy.

**Mission**: Készítsd el a következõ Epic Review-ját.

**Epic Details**:
- **Epic ID**: {EPIC_ID}
- **Epic Title**: {EPIC_TITLE}
- **Epic State**: Minden Task Done

**Context for You**:
- **Epic Plan**: docs/{project}/epics/{EPIC_ID}/plan.md
- **Task Plans**: docs/{project}/epics/{EPIC_ID}/tasks/*.md
- **Implementation Reports**: docs/{project}/epics/{EPIC_ID}/tasks/implementation_reports/*.md
- **QA Signoffs**: docs/{project}/epics/{EPIC_ID}/tasks/qa_reports/*.md
- **Project State**: docs/{project}/state.md

**Your Workflow**:
- Kövesd pontosan: src/agent-system/database/roles/discovery/tech_lead/tech_lead_closure.workflow.md
- **Output**: docs/{project}/epics/{EPIC_ID}/epic_review.md (használd: src/agent-system/database/roles/discovery/tech_lead/templates/epic_review.template.md)

**Success Criteria**:
- [ ] Epic Review létrehozva (epic_review.md)
- [ ] Task-ok státusza összefoglalva
- [ ] Technikai kihívások dokumentálva
- [ ] Lessons Learned dokumentálva
- [ ] Calibration Instructions generálva (global skill/template frissítési javaslatok)
- [ ] Metrics összegyûjtve (idõ, scope, quality)

**Start Review**: Kezdd az Epic célkitûzésének és a tényleges eredményeknek az összehasonlításával.
```

**Elvárt Output**: `docs/{project}/epics/{EPIC_ID}/epic_review.md`

#### 5.3 Epic Review Validation

```powershell
# 1. epic_review.md létezik?
if (-not (Test-Path "docs/{project}/epics/{EPIC_ID}/epic_review.md")) {
    ERROR: "Epic Review hiányzik! Epic NEM zárható le!"
    # Visszadobás Tech Lead-nek
}

# 2. Epic Review tartalmaz:
#    - Epic Summary
#    - Task-ok Status
#    - Technical Challenges
#    - Lessons Learned
#    - Calibration Instructions (skill/template/standards frissítési javaslatok)
#    - Metrics (time, scope, quality)
```

### **Fázis 6: Architect Sign-off (Architect Closure)**

**Cél**: Architektúrális validáció, Clean Architecture & DDD compliance check, Sign-off döntés.

#### 6.1 Architect Sign-off Dispatch

**?? Ágensváltás: Architect Role-ra**

**?? Használandó Prompt**:

- **P5**: [architect_epic_review.message.md](../messages/architect_epic_review.message.md) (de "Sign-off" kontextussal)
- **Paraméterek**: `{EPIC_ID}`, `{project}`
- **Mikor**: `epic_review.md` elkészült, architekt validáció kérése

**Példa**:

```markdown
**Role**: Te most a projekt Senior Architect-je vagy.

**Mission**: Validáld és sign-off-old a következõ Epic-et.

**Epic Details**:
- **Epic ID**: {EPIC_ID}
- **Epic Title**: {EPIC_TITLE}
- **Epic State**: Review (Tech Lead epic_review.md elkészült)

**Context for You**:
- **Epic Plan**: docs/{project}/epics/{EPIC_ID}/plan.md
- **Epic Review**: docs/{project}/epics/{EPIC_ID}/epic_review.md
- **Implementation Reports**: docs/{project}/epics/{EPIC_ID}/tasks/implementation_reports/*.md
- **Source Code**: src/ (ellenõrizd a Clean Architecture betartását)
- **Architecture Standards**: docs/{project}/standards/architecture_standards.md

**Your Workflow**:
- Kövesd pontosan: src/agent-system/database/roles/discovery/architect/architect_closure.workflow.md
- **Output**: docs/{project}/epics/{EPIC_ID}/architect_signoff.md (használd: src/agent-system/database/roles/discovery/architect/templates/architect_signoff.template.md)

**Success Criteria**:
- [ ] Epic Review Validation (epic_review.md alapján)
- [ ] Architectural Compliance Check (Clean Architecture, DDD)
- [ ] Quality Gate Pass/Fail
- [ ] Sign-off Decision (APPROVED / CONDITIONAL / REJECTED)
- [ ] Calibration Instructions generálva (global standards/skills frissítési javaslatok)

**Decision Points**:
- **If APPROVED**: Epic lezárható
- **If CONDITIONAL**: Minor fix szükséges (Epic marad Review-ban, fix után újra sign-off)
- **If REJECTED**: Epic visszamegy Planning fázisba (architektúrális probléma)

**Start Validation**: Kezdd az Epic Plan és a tényleges implementáció összehasonlításával.
```

**Elvárt Output**: `docs/{project}/epics/{EPIC_ID}/architect_signoff.md`

#### 6.2 Architect Sign-off Validation

```powershell
# 1. architect_signoff.md létezik?
if (-not (Test-Path "docs/{project}/epics/{EPIC_ID}/architect_signoff.md")) {
    ERROR: "Architect Signoff hiányzik! Epic NEM zárható le!"
    # Visszadobás Architect-nek
}

# 2. Sign-off tartalmaz:
#    - Epic Review Validation
#    - Architectural Compliance (Clean Architecture, DDD)
#    - Quality Gate Result (Pass/Fail with reasons)
#    - Sign-off Decision (APPROVED/CONDITIONAL/REJECTED)
#    - Calibration Instructions

# 3. Decision Point
$signoff_decision = extract_signoff_decision()
```

#### 6.3 Sign-off Decision Handling

**Decision Matrix:**

| Sign-off Decision | Epic State Változás | Orchestrator Akció | state.md Frissítés |
|:------------------|:--------------------|:-------------------|:-------------------|
| **APPROVED** | Review › **Done** | › Fázis 7 (Closure) | Epic State Map: Done |
| **CONDITIONAL** | Review › Review (marad) | Minor fix koordinálás › újra Sign-off | Next Steps: Fix koordinálás |
| **REJECTED** | Review › **Planning** | Epic rollback › Architect újratervezés | Epic State Map: Planning, Blocker Register: Architectural Issue |

**Ha REJECTED:**

1.  **Blocker Register frissítés** (state.md):

    ```markdown
    | Blocker ID | Affected Entity | Description | Severity | Owner | Date Identified | Status |
    |:-----------|:----------------|:------------|:---------|:------|:----------------|:-------|
    | BLK-002 | EPIC-001 | Architect Rejected: [Architectural Issues] | Critical | Architect | 2026-02-16 | Active |
    ```

2.  **Epic Rollback**:
    -   Epic State: Review › Planning
    -   Architect újratervezés (architect.workflow.md)
    -   Task-ok: Done › Blocked (architektúrális módosítás miatt)

#### 6.4 State Tracking (Fázis 6 után)

**?? Frissítsd KÖTELEZÕEN:**

1.  **FSM State Frissítés (MCP)**:
    -   Ha APPROVED: Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{EPIC_ID}", "action": "success" }` paraméterekkel.
    -   Ha REJECTED: Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{EPIC_ID}", "action": "fail" }` paraméterekkel.
    -   Next Steps: Ha APPROVED › "Knowledge Steward - Epic Archiválás - {EPIC_ID}"

2.  **decision_log.md**:
    -   Decision Entry: "DEC-XXX: {EPIC_ID} Architect Sign-off Decision"
    -   Context: Epic Review alapján milyen döntés született
    -   Alternatives: APPROVED / CONDITIONAL / REJECTED (indoklással)
    -   Decision: Végleges sign-off döntés
    -   Impact: Epic lezárható / Fix szükséges / Rollback Planning-be
    -   Follow-up Actions: Ha CONDITIONAL vagy REJECTED › konkrét akciók

---

### **Fázis 7: Epic Closure (Knowledge Steward Archiválás)**

**Cél**: Epic dokumentációs archiválás, calibration instructions feldolgozása, struktúra cleanup.

#### 7.1 Knowledge Steward Dispatch

**?? Ágensváltás: Knowledge Steward Role-ra**

**?? Használandó Prompt**:

-   **P6** (módosított verzió, "Epic Closure" kontextussal):

```markdown
**Role**: Te most a projekt Knowledge Steward-ja vagy.

**Mission**: Archiváld a következõ lezárt Epic dokumentációját és dolgozd fel a calibration instructions-öket.

**Epic Details**:
- **Epic ID**: {EPIC_ID}
- **Epic Title**: {EPIC_TITLE}
- **Epic State**: Done (Architect Sign-off APPROVED)

**Context for You**:
- **Epic Folder**: docs/{project}/epics/{EPIC_ID}/
- **Epic Review**: docs/{project}/epics/{EPIC_ID}/epic_review.md (calibration instructions itt!)
- **Architect Signoff**: docs/{project}/epics/{EPIC_ID}/architect_signoff.md (calibration instructions itt!)

**Your Workflow**:
- Kövesd pontosan: src/agent-system/database/roles/management/knowledge_steward/knowledge_steward.workflow.md
- **Archiválás**: docs/{project}/archived/{EPIC_ID}/ (teljes Epic mappa másolása)
- **Calibration**: src/agent-system/database/roles/management/knowledge_steward/knowledge_steward_calibration.workflow.md

**Success Criteria**:
- [ ] Epic archiválva (docs/{project}/archived/{EPIC_ID}/)
- [ ] Calibration Instructions feldolgozva:
  - [ ] Global skill-ek frissítve (ha szükséges)
  - [ ] Template-ek finomítva (ha szükséges)
  - [ ] Standards frissítve (ha szükséges)
- [ ] Original Epic folder marad (történelmi referencia)
- [ ] knowledge_map.md frissítve (ha új skill/template/standard került be)

**Start Archival**: Kezdd az Epic dokumentáció teljesség ellenõrzésével (plan.md, tasks/*.md, implementation_reports/*.md, qa_reports/*.md, epic_review.md, architect_signoff.md).
```

**Elvárt Output**:

- `docs/{project}/archived/{EPIC_ID}/` (teljes Epic archiválva)
- Global skill/template/standards frissítések (ha calibration instructions javasoltak)

#### 7.2 Product Owner Review (Stratégiai Értékelés)

**?? Ágensváltás: Product Owner Role-ra**

**Cél**: Üzleti célok szempontjából értékeli a lezárt Epic-et, feldolgozza a minõségi riportokat, és javaslatot tesz a következõ Epic(ek)re.

**?? Használandó Prompt**:

```markdown
**Role**: Te most a projekt Product Owner-e vagy.

**Mission**: Értékeld az éppen lezárt Epic-et üzleti szempontból, dolgozd fel a riportokat, és adj stratégiai irányt a következõ lépésekhez.

**Epic Details**:
- **Epic ID**: {EPIC_ID}
- **Epic Title**: {EPIC_TITLE}
- **Epic State**: Closed (Knowledge Steward archiválta)

**Context for You**:
- **Project Goal**: docs/{project}/goal.md
- **Epic Review**: docs/{project}/epics/{EPIC_ID}/epic_review.md
- **Architect Signoff**: docs/{project}/epics/{EPIC_ID}/architect_signoff.md
- **QA Reports**: docs/{project}/epics/{EPIC_ID}/tasks/qa_reports/
- **Project State**: docs/{project}/state.md
- **Product Backlog**: docs/{project}/product_backlog.md (ha létezik)

**Your Workflow**:
- Kövesd pontosan: src/agent-system/database/roles/discovery/product_owner/workflows/product_owner.workflow.md – Section A
- **Output**:
  1. Product Backlog frissítve (docs/{project}/product_backlog.md)
  2. PO Strategic Directive (az Orchestratornak)
  3. Epic Proposal (az Architectnek, ha van)

**Success Criteria**:
- [ ] Riportok feldolgozva (quality metrics összesítve)
- [ ] Goal alignment értékelve (MVP progress %)
- [ ] Quality assessment elvégezve (??/??/?)
- [ ] Product Backlog frissítve (prioritizált Epic lista)
- [ ] Orchestrator Strategic Directive elkészítve
- [ ] Architect input elkészítve (ha van új Epic javaslat)
```

**Elvárt Output**:

- `docs/{project}/product_backlog.md` (frissített, prioritizált Epic lista)
- PO Strategic Directive (Orchestrator számára: mit csináljunk és miért)
- Epic Proposal (Architect számára, ha van új javaslat)

#### 7.3 Git Pull Request & Merge (Epic Lezárás)

**Cél**: Az Epic branch összes munkáját egy rendezett PR-on keresztül kell összeolvasztani a `develop` ágba, megõrizve a nyomon követhetõséget és az auditálhatóságot.

**Elõfeltételek (ellenõrizd ELÕTTE):**

- [ ] Architect Sign-off: ? APPROVED (`architect_signoff.md`)
- [ ] Product Owner Review: ? Elvégezve (Product Backlog frissítve)
- [ ] Knowledge Steward Archival: ? Epic archiválva
- [ ] Minden Task státusza: "Done"

**PR Létrehozása – Lépések:**

```powershell
# 1. Ellenõrizd, hogy az Epic branch naprakész-e
git checkout epic/{EPIC_ID}-{epic-short-name}
git status  # Nincs uncommitted változás?

# 2. Frissítsd develop-ból (ha szükséges)
git fetch origin develop
git merge origin/develop  # Ha develop elõrébb van

# 3. PR létrehozása GitHub CLI-vel (ha elerhetõ)
gh pr create \
  --base develop \
  --head epic/{EPIC_ID}-{epic-short-name} \
  --title "Epic/{EPIC_ID}: {EPIC_TITLE}" \
  --body-file docs/{project}/epics/{EPIC_ID}/epic_review.md
```

**PR Body Sablon (töltsd ki manuálisan, ha a GitHub CLI nem érhetõ el):**

```markdown
## Epic/{EPIC_ID}: {EPIC_TITLE}

### Összefoglaló
{Epic cél és elért eredmény 2-3 mondatban}

### Deliverables
- [ ] Backend Tasks: {TASK_ID lista}
- [ ] Frontend Tasks: {TASK_ID lista}
- [ ] QA Signoffs: All APPROVED

### Quality Metrics
- Task Count: {N}
- Bug Count (QA Cycles): {N}
- Rework Iterations: {N}
- Architect Sign-off: APPROVED ?
- PO Review: DONE ?

### Kapcsolódó Dokumentumok
- [Epic Review](docs/{project}/epics/{EPIC_ID}/epic_review.md)
- [Architect Signoff](docs/{project}/epics/{EPIC_ID}/architect_signoff.md)
- [Archived Epic](docs/{project}/archived/{EPIC_ID}/)

### Ágens Jóváhagyások
- Architect: ? (architect_signoff.md)
- Product Owner: ? (PO Review elvégezve)
- Knowledge Steward: ? (Epic archiválva)
```

**Merge & Cleanup – Merge jóváhagyása után:**

```powershell
# 1. PR merge (GitHub UI-n vagy CLI-vel)
gh pr merge --squash --delete-branch

# Vagy manuálisan:
git checkout develop
git merge --no-ff epic/{EPIC_ID}-{epic-short-name} -m "Epic/{EPIC_ID}: {EPIC_TITLE} [closed]"
git push origin develop

# 2. Epic branch törlése (merge után)
git branch -d epic/{EPIC_ID}-{epic-short-name}
git push origin --delete epic/{EPIC_ID}-{epic-short-name}

# 3. Epic lezárás tagelése
git tag -a epic/{EPIC_ID}-closed -m "Epic {EPIC_ID} ({EPIC_TITLE}) closed and merged to develop"
git push origin epic/{EPIC_ID}-closed
```

> [!TIP]
> **Merge stratégia**: Squash merge ajánlott (tiszta develop history), kivéve ha a commit history megõrzése üzleti követelmény. A döntést rögzítsd a `decision_log.md`-ban.

---

#### 7.4 Final State Tracking (Epic Lezárása)

**?? Frissítsd KÖTELEZÕEN (UTOLSÓ LÉPÉS!):**

1.  **FSM State Frissítés (MCP)**:
    -   Hívd meg a `request_workflow_transition` tool-t `{ "project_id": "{EPIC_ID}", "action": "success" }` paraméterekkel.
    -   Project State: `{EPIC_ID}` State = **"Done"** › **"Closed"**

2.  **decision_log.md**:
    -   Decision Entry: "DEC-XXX: {EPIC_ID} Epic Closure"
    -   Context: Epic teljes életciklusa lezárult
    -   Decision: Epic archiválva, calibration instructions feldolgozva
    -   Impact: Knowledge base frissült (../skills/templates/standards), Epic docs archiválva
    -   Follow-up Actions: Következõ Epic indítása (ha van backlog-ban)

---

## ?? Epic Execution Metrics (Automatikus Tracking)

**Minden Epic lezárásakor számold ki:**

| Metric | Mibõl Számítható | Hová Kerül |
|:-------|:-----------------|:-----------|
| **Epic Duration** | Epic Planning start date – Epic Closure date | state.md Project Timeline |
| **Task Count** | Összes TASK-XXX.md szám | epic_review.md Metrics |
| **Bug Count** | QA Signoff-ok Bug List összege | epic_review.md Metrics |
| **Rework Count** | Task State: "Blocked (Bug Fix)" elõfordulások | decision_log.md (bug fix decisions) |
| **Architect Iterations** | Epic Sign-off REJECTED › Planning loop count | decision_log.md (rollback decisions) |

---

## ??? Hibaelhárítás (Troubleshooting)

| Probléma | Ok | Megoldás |
|:---------|:---|:---------|
| **Context Loss** | `state.md` nincs frissítve | **AZONNAL** használd a `get_workflow_state` MCP eszközt! |
| **Task Dependencies hiányoznak** | `dependency_map.md` nincs frissítve | Frissítsd a Task-level Dependencies szekciót! |
| **Circular Dependency** | Rossz task bontás | `dependency_map.md` Circular Dependency Check – CRITICAL WARNING! |
| **Epic nem halad** | Blocker nincs kezelve | `state.md` Blocker Register › priorizálj blocker handling-et! |
| **QA Rejected folyamatosan** | DoD nem egyértelmû | Task Plan DoD frissítése + Tech Lead re-planning |
| **Architect Rejected** | Epic Plan nem megfelelõ | Epic rollback › Architect Design Thinking újra (3 alternatíva!) |
| **Token Overflow** | Kontextus túlterhelt | Context Hygiene (orchestrator.workflow.md Fázis C) |

---

## ?? Kommunikációs Prompt Összefoglaló

**Ebben a workflow-ban használt prompts:**

| Fázis | Cél ágens | Prompt ID | Prompt Sablon | Paraméterek |
|:------|:----------|:---------:|:--------------|:------------|
| **1 (Planning)** | Architect | Manual | Architect Epic Planning (Mega-Prompt) | `{EPIC_ID}`, `{EPIC_TITLE}`, `{EPIC_GOAL}` |
| **2 (Task Planning)** | Tech Lead | **P1** | [tech_lead_epic_planning.message.md](../messages/tech_lead_epic_planning.message.md) | `{EPIC_ID}`, `{project}` |
| **2 (Critique)** | Devil's Advocate | **P17** | [devils_advocate/messages/tech_lead_task_critique.message.md](../../devils_advocate/messages/tech_lead_task_critique.message.md) | `{EPIC_ID}`, `{TASK_ID_list}` |
| **3 (Backend)** | Backend Developer | **P2** | [backend_developer_task_implementation.message.md](../messages/backend_developer_task_implementation.message.md) | `{TASK_ID}`, `{EPIC_ID}` |
| **3 (Frontend)** | Frontend Developer | **P3** | [frontend_developer_task_implementation.message.md](../messages/frontend_developer_task_implementation.message.md) | `{TASK_ID}`, `{EPIC_ID}` |
| **4 (QA)** | QA Tester | **P4** | [qa_tester_testing.message.md](../messages/qa_tester_testing.message.md) | `{TASK_ID}`, `{EPIC_ID}` |
| **4 (Bug Fix Backend)** | Backend Developer | **P14** | [qa_tester/messages/backend_developer_bug_fix.message.md](../../qa_tester/messages/backend_developer_bug_fix.message.md) | `{TASK_ID}`, `{BUG_LIST}` |
| **4 (Bug Fix Frontend)** | Frontend Developer | **P15** | [qa_tester/messages/frontend_developer_bug_fix.message.md](../../qa_tester/messages/frontend_developer_bug_fix.message.md) | `{TASK_ID}`, `{BUG_LIST}` |
| **5 (Epic Review)** | Tech Lead | Manual | Tech Lead Epic Review (Mega-Prompt) | `{EPIC_ID}` |
| **6 (Sign-off)** | Architect | **P5** (mod) | Architect Sign-off (Mega-Prompt) | `{EPIC_ID}` |
| **7 (Closure)** | Knowledge Steward | **P6** (mod) | Knowledge Steward Epic Archiválás (Mega-Prompt) | `{EPIC_ID}` |
| **7 (PO Review)** | Product Owner | Manual | Product Owner Epic Closure Review (Mega-Prompt) | `{EPIC_ID}` |
| **7 (Git PR)** | Orchestrator | – | Git PR & Merge (Fázis 7.3 inline utasítás) | `{EPIC_ID}`, `{epic-short-name}` |

---

## ? Epic Execution Checklist (Teljes Életciklus)

**Használd ezt a checklistet minden Epic-nél!**

### Pre-flight

- [ ] `goal.md` létezik
- [ ] `state.md` létezik és naprakész
- [ ] `dependency_map.md` létezik (ha van Epic)
- [ ] `decision_log.md` létezik
- [ ] Nincs Critical Blocker
- [ ] `develop` branch naprakész (`git pull origin develop`)
- [ ] Epic branch létrehozva: `epic/{EPIC_ID}-{epic-short-name}` (Fázis 0.4)

### Fázis 1 (Epic Planning)

- [ ] Architect dispatched (Manual Mega-Prompt)
- [ ] `plan.md` létrehozva
- [ ] ADR-ek készítve (ha kritikus döntés)
- [ ] FSM Transition (MCP) meghívva
- [ ] `dependency_map.md` frissítve (új Epic node)
- [ ] `decision_log.md` frissítve (Epic Planning döntés)

### Fázis 2 (Task Planning)

- [ ] Tech Lead dispatched (P1 prompt)
- [ ] `TASK-XXX.md` fájlok létrehozva (minden Task-ra)
- [ ] Devil's Advocate review (opcionális)
- [ ] FSM Transition (MCP) meghívva
- [ ] `dependency_map.md` frissítve (Task-level dependencies)
- [ ] `decision_log.md` frissítve (Task Decomposition döntés)

### Fázis 3 (Implementation)

- [ ] Minden Backend Task dispatched (P2 prompt)
- [ ] Minden Frontend Task dispatched (P3 prompt)
- [ ] Implementation Reports elkészültek (minden Task-ra)
- [ ] FSM Transition (MCP) meghívva (minden Task után!)
- [ ] `decision_log.md` frissítve (ha implementation döntés volt)

### Fázis 4 (QA Testing)

- [ ] Minden Task QA dispatched (P4 prompt)
- [ ] QA Signoffs elkészültek (minden Task-ra, APPROVED status)
- [ ] Bug fix cycles lezárva (ha voltak REJECTED Task-ok)
- [ ] FSM Transition (MCP) meghívva (minden Task után!)
- [ ] `decision_log.md` frissítve (ha bug fix strategy döntés volt)

### Fázis 5 (Epic Review)

- [ ] Minden Task Done
- [ ] Tech Lead dispatched (Manual Mega-Prompt)
- [ ] `epic_review.md` létrehozva
- [ ] Calibration Instructions generálva
- [ ] FSM Transition (MCP) meghívva

### Fázis 6 (Architect Sign-off)

- [ ] Architect dispatched (P5 modified prompt)
- [ ] `architect_signoff.md` létrehozva
- [ ] Sign-off Decision: APPROVED
- [ ] FSM Transition (MCP) meghívva
- [ ] `decision_log.md` frissítve (Sign-off döntés)

### Fázis 7 (Epic Closure)

- [ ] Knowledge Steward dispatched (P6 modified prompt)
- [ ] Epic archiválva (`docs/{project}/archived/{EPIC_ID}/`)
- [ ] Calibration Instructions feldolgozva (`../skills/templates/standards` frissítve)
- [ ] Product Owner dispatched (Manual Mega-Prompt)
- [ ] Riportok feldolgozva, quality assessment elvégezve (??/??/?)
- [ ] Product Backlog frissítve (prioritizált Epic lista)
- [ ] Orchestrator Strategic Directive elkészítve
- [ ] Architect input elkészítve (ha van új Epic javaslat)
- [ ] Git PR létrehozva (`epic/{EPIC_ID}-{name}` › `develop`)
- [ ] PR jóváhagyva (Architect ? + PO ? szükséges)
- [ ] Epic branch merge-elve (`develop`-ba, squash merge)
- [ ] Epic branch törölve (remote + local)
- [ ] Git tag elkészítve (`epic/{EPIC_ID}-closed`)
- [ ] FSM Transition (MCP) meghívva (Epic Closure)
- [ ] `decision_log.md` frissítve (Epic Closure döntés)

---

*Ez a workflow az Orchestrator "master orchestrator playbook"-ja. Kövesd pontosan minden Epic-nél, és dokumentálj minden döntést!*
