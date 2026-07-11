---
id: MSG-NEXUS-015
from: conductor
to: nexus
type: task
priority: medium
status: READ
model: sonnet
ref: docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md
created: 2026-06-18
---

# Marvin McpServer Migration — Python Orchestrator for Planning Pipeline

## Összefoglaló

Implementáld a **Marvin**-alapú Python orchestrator-t, amely a planning pipeline bash script-jeit (plan-scan.sh, plan-select.sh, plan-debate.sh) felváltja egy resumable, MCP-integráns folyamattal.

**Scope:** Marvin Scheduler + PlanningThread + ScanTask + SelectTask + DebateTask + McpServer
**Language:** Python 3.11+
**Framework:** Marvin (Prefect-alternative)
**MCP:** stdio protocol (search_knowledge, submitArtifact)

---

## Architektúra

```
Marvin Scheduler (Python daemon)
  ↓
PlanningThread (resumable workflow)
  ├── ScanTask (Haiku, 9 segment, MCP knowledge_search)
  ├── SelectTask (Sonnet, MCP search_knowledge)
  └── DebateTask (2× Sonnet parallel + synthesis)
        ↓
McpServer (stdio)
  ├── search_knowledge (FTS/vector query)
  └── submitArtifact (idea/consensus regisztráció)
```

**Jelenleg (bash):**
```
plan-scan.sh → plan-select.sh → plan-debate.sh
  (30 perc cron, hideg indítás minden alkalommal)
```

**Cél (Marvin):**
```
Marvin resumable PlanningThread
  (kontextus megmarad, warm state, MCP tool use)
```

---

## Komponensek

### 1. Marvin Scheduler

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/scheduler.py`

**Feladat:** Periodikus PlanningThread indítás (30 perc interval).

```python
import marvin
from marvin.workflows import Workflow
from planning_thread import PlanningThread

scheduler = marvin.Scheduler()

@scheduler.task(interval="30m")
async def run_planning_cycle():
    thread = PlanningThread()
    await thread.run()

if __name__ == "__main__":
    scheduler.start()
```

---

### 2. PlanningThread (Workflow)

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/planning_thread.py`

**Feladat:** Resumable workflow, amely végigvezeti a planning pipeline-t.

```python
from marvin.workflows import Workflow
from tasks.scan_task import ScanTask
from tasks.select_task import SelectTask
from tasks.debate_task import DebateTask

class PlanningThread(Workflow):
    async def run(self):
        # 1. Scan phase (9 segments)
        scan_task = ScanTask()
        ideas = await scan_task.run()

        # 2. Select phase (ideas → specs)
        select_task = SelectTask(ideas=ideas)
        specs = await select_task.run()

        # 3. Debate phase (specs → consensus)
        debate_task = DebateTask(specs=specs)
        consensus = await debate_task.run()

        # 4. Submit consensus to queue
        await self.submit_to_queue(consensus)

    async def submit_to_queue(self, consensus):
        # Write to docs/planning/queue/
        pass
```

---

### 3. ScanTask (Haiku, 9 segment)

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/tasks/scan_task.py`

**Feladat:** 9 memory szegmens iterálása, MCP `knowledge_search` tool use, idea generálás.

**Segments:**
```python
SEGMENTS = [
    "kernel-memory",
    "orch-memory",
    "fe-memory",
    "joinery-memory",
    "cutting-memory",
    "infra-memory",
    "sales-memory",
    "identity-memory",
    "abstractions-memory"
]
```

**MCP Tool Use:**

```python
from marvin.tools import mcp_tool

@mcp_tool
async def knowledge_search(query: str, limit: int = 5):
    """Search knowledge base for relevant context."""
    # MCP stdio call to Librarian McpServer
    pass

class ScanTask:
    async def run(self):
        ideas = []
        for segment in SEGMENTS:
            # 1. Load memory segment (docs/memory/{segment}.md)
            context = await self.load_memory(segment)

            # 2. MCP knowledge search
            knowledge = await knowledge_search(query=segment, limit=5)

            # 3. Claude Haiku: generate idea
            idea = await self.generate_idea(context, knowledge)

            # 4. MCP submitArtifact
            await self.submit_artifact(idea, type="idea")

            ideas.append(idea)

        return ideas
```

---

### 4. SelectTask (Sonnet)

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/tasks/select_task.py`

**Feladat:** Ideas → specs (pending.md).

```python
class SelectTask:
    def __init__(self, ideas: list):
        self.ideas = ideas

    async def run(self):
        # 1. MCP knowledge search (context)
        context = await knowledge_search(query="planning specs", limit=10)

        # 2. Claude Sonnet: select top ideas
        specs = await self.select_specs(self.ideas, context)

        # 3. Write to pending.md
        await self.write_pending(specs)

        return specs
```

---

### 5. DebateTask (2× Sonnet parallel + synthesis)

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/tasks/debate_task.py`

**Feladat:** 2× parallel Sonnet review + synthesis → consensus.

```python
import asyncio

class DebateTask:
    def __init__(self, specs: list):
        self.specs = specs

    async def run(self):
        consensus_list = []

        for spec in self.specs:
            # 1. Parallel reviews
            review_a, review_b = await asyncio.gather(
                self.review_sonnet_a(spec),
                self.review_sonnet_b(spec)
            )

            # 2. Synthesis
            consensus = await self.synthesize(spec, review_a, review_b)

            # 3. MCP submitArtifact
            await self.submit_artifact(consensus, type="consensus")

            consensus_list.append(consensus)

        return consensus_list
```

---

### 6. McpServer (stdio)

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/mcp_server.py`

**Feladat:** MCP stdio server, amely a Marvin tasks-ból hívható.

**Tools:**

```python
from mcp.server import MCPServer

server = MCPServer()

@server.tool("search_knowledge")
async def search_knowledge(query: str, limit: int = 5):
    # PostgreSQL FTS query (via Orchestrator /knowledge/search)
    response = await http_post("http://localhost:3000/knowledge/search", {
        "query": query,
        "limit": limit
    })
    return response.json()

@server.tool("submitArtifact")
async def submit_artifact(content: str, type: str):
    # Write to docs/planning/ideas/ or docs/planning/queue/
    if type == "idea":
        await write_file(f"docs/planning/ideas/{timestamp}_{slug}.md", content)
    elif type == "consensus":
        await write_file(f"docs/planning/queue/{timestamp}_{slug}.md", content)

if __name__ == "__main__":
    server.run_stdio()
```

---

## Konfigurációs Fájl

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/config.yaml`

```yaml
planning:
  interval: 30m
  segments:
    - kernel-memory
    - orch-memory
    - fe-memory
    - joinery-memory
    - cutting-memory
    - infra-memory
    - sales-memory
    - identity-memory
    - abstractions-memory

  models:
    scan: haiku
    select: sonnet
    debate: sonnet

  limits:
    ideas_max: 10
    pending_max: 3
    queue_max: 3

mcp:
  protocol: stdio
  server_path: /opt/spaceos/spaceos-nexus/marvin-planner/mcp_server.py

knowledge:
  api_url: http://localhost:3000/knowledge/search
  search_limit: 5
```

---

## Environment Setup

### Python Dependencies

**Path:** `/opt/spaceos/spaceos-nexus/marvin-planner/requirements.txt`

```txt
marvin==0.8.0
anthropic==0.21.0
pydantic==2.6.0
aiohttp==3.9.0
pyyaml==6.0.1
```

**Install:**

```bash
cd /opt/spaceos/spaceos-nexus/marvin-planner
python3.11 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

---

## Systemd Service

**Path:** `/etc/systemd/system/marvin-planner.service`

```ini
[Unit]
Description=Marvin Planning Pipeline Scheduler
After=network.target postgresql.service

[Service]
Type=simple
User=gabor
WorkingDirectory=/opt/spaceos/spaceos-nexus/marvin-planner
ExecStart=/opt/spaceos/spaceos-nexus/marvin-planner/venv/bin/python scheduler.py
Restart=on-failure
RestartSec=10s

Environment="ANTHROPIC_API_KEY=<api-key>"
Environment="PYTHONUNBUFFERED=1"

StandardOutput=journal
StandardError=journal
SyslogIdentifier=marvin-planner

[Install]
WantedBy=multi-user.target
```

**Enable:**

```bash
sudo systemctl daemon-reload
sudo systemctl enable marvin-planner.service
sudo systemctl start marvin-planner.service
```

**Logs:**

```bash
sudo journalctl -u marvin-planner.service -f
```

---

## Migration Plan

### Phase 1: McpServer Standalone

1. Implementáld a `mcp_server.py`-t (`search_knowledge`, `submitArtifact`)
2. Test standalone: `python mcp_server.py`
3. Verify: MCP stdio protocol működik

### Phase 2: ScanTask Prototype

1. Implementáld a `ScanTask`-ot (1 segment only)
2. Test: `python -m tasks.scan_task`
3. Verify: idea generálás + MCP tool use

### Phase 3: SelectTask + DebateTask

1. Implementáld a `SelectTask` + `DebateTask`-ot
2. Test: teljes pipeline 1 ciklus
3. Verify: consensus generálás + queue-ba írás

### Phase 4: Scheduler + Systemd

1. Implementáld a `scheduler.py`-t
2. Systemd service setup
3. Test: 30 perces intervalum működik
4. Cron disable: `plan-scan.sh` + `plan-select.sh` + `plan-debate.sh`

---

## Cutover Strategy

**Bash → Marvin átmenet:**

1. **Parallel Run (1 hét):** Marvin + bash párhuzamosan fut, output compare
2. **Confidence Check:** Marvin ugyanazt vagy jobb outputot ad mint a bash
3. **Cutover:** Bash cron disable, Marvin systemd enable
4. **Monitoring:** 1 hét monitoring (journalctl, queue output)

**Rollback Plan:** Ha Marvin fail, bash cron re-enable.

---

## Definition of Done

- [ ] McpServer implemented (`search_knowledge`, `submitArtifact`)
- [ ] ScanTask implemented (9 segments, MCP tool use)
- [ ] SelectTask implemented (ideas → specs)
- [ ] DebateTask implemented (2× parallel Sonnet + synthesis)
- [ ] PlanningThread workflow (resumable)
- [ ] Marvin Scheduler (30 perc interval)
- [ ] config.yaml létrehozva
- [ ] requirements.txt + venv setup
- [ ] Systemd service configured + enabled
- [ ] Test run: 1 teljes planning cycle sikeres
- [ ] Parallel run (Marvin + bash) 1 hét
- [ ] Cutover: bash cron disabled
- [ ] DONE outbox message sent to Conductor

---

## Referenciák

- Spec: `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md`
- Marvin docs: https://www.askmarvin.ai/
- MCP protocol: https://modelcontextprotocol.io/
- Orchestrator knowledge API: MSG-ORCH-003

---

**NEXUS Terminal: Implementáld a Marvin-alapú planning orchestrator-t MCP integration-nel!**

Timestamp: 2026-06-18 05:02 UTC
