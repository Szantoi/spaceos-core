# SpaceOS Project Automation System — v4 Architecture Specification

> **Dokumentum státusz:** v4 IMPLEMENTÁCIÓRA KÉSZ
> **Szerző:** Architect terminál
> **Dátum:** 2026-06-21
> **Review:** DB ✅ | Security ✅ | Backend ✅

---

## 1. Executive Summary

A Project Automation System célja a Conductor terminál feladatainak automatizálása:
- **Task chain definíció** YAML formátumban
- **Auto-dispatch daemon** a DONE→next task pipeline-hoz
- **Skeleton generator** boilerplate kód generálásához
- **MCP tools** integrációval a Knowledge Service-be

**Prioritások (WSJF):**
| P# | Feature | Effort | Business Value |
|----|---------|--------|----------------|
| P0 | Task Chain YAML + Auto-dispatch | 3 nap | Kritikus — Conductor 80% időmegtakarítás |
| P1 | Skeleton Generator | 2 nap | Magas — Backend fejlesztés gyorsítás |
| P1 | Endpoint Scaffolder | 1 nap | Magas — API implementáció gyorsítás |
| P2 | MCP Tools Integration | 2 nap | Közepes — Terminál integráció |
| P3 | Milestone Auto-planning | 3 nap | Alacsony — Későbbi iteráció |

---

## 2. Projekt Struktúra

```
docs/projects/<project-slug>/
  PROJECT.md             ← Projekt meta (cél, scope, owner)
  PLAN.md                ← v1→v4 spec artifact (Architect output)
  TASKS.yaml             ← Task chain definíció (auto-dispatch input)
  STATUS.md              ← Auto-generated projekt státusz
  milestones/
    M1_<name>.md         ← Milestone spec + DoD
    M2_<name>.md
  inbox-templates/       ← Pre-filled inbox sablonok
```

### 2.1 PROJECT.md Séma

```yaml
---
id: PROJECT-<SLUG>
name: "Projekt neve"
owner: conductor
status: PLANNING | APPROVED | IN_PROGRESS | BLOCKED | DONE
created: 2026-06-21
updated: 2026-06-21
---

# Projekt neve

## Cél
<1-2 mondat a projekt céljáról>

## Scope
- [ ] Milestone 1: ...
- [ ] Milestone 2: ...

## Out of Scope
- ...

## Stakeholders
- **Owner:** conductor
- **Architect:** architect (v1→v4 spec)
- **Implementers:** backend, frontend

## Dependencies
- Projekt X kész legyen
- API Y elérhető legyen
```

---

## 3. Task Chain YAML Séma (v1)

### 3.1 Teljes Séma

```yaml
# TASKS.yaml
version: "1.0"
project: <project-slug>
created: 2026-06-21
updated: 2026-06-21

# Globális beállítások
config:
  default_model: sonnet
  auto_dispatch: true
  notify_telegram: true
  retry_on_blocked: 3  # Max újrapróbálkozás BLOCKED-ra

# Milestone definíciók
milestones:
  - id: M1
    name: "Domain Implementation"
    status: pending | in_progress | done | blocked
    blocked_by: []  # Milestone-ok amik blokkolják

    tasks:
      - id: T1
        name: "Architect Spec"
        description: "v1→v4 specifikáció készítése"
        terminal: architect
        model: opus
        priority: high
        status: pending | in_progress | done | blocked

        # Függőségek
        blocked_by: []
        triggers_on_done:
          - T2
          - T3  # Párhuzamos indítás

        # Auto-generálás (opcionális)
        auto_generate: false
        generator: null
        params: {}

        # Inbox sablon override (opcionális)
        inbox_template: null

        # Metadata
        started_at: null
        completed_at: null
        msg_id: null  # Generált MSG ID

      - id: T2
        name: "Domain Layer"
        terminal: backend
        model: sonnet
        priority: high
        status: pending
        blocked_by: [T1]
        triggers_on_done: [T4]

        auto_generate: true
        generator: "generate-module"
        params:
          module: "spaceos-modules-procurement"
          aggregate: "SupplierComplaint"
          states: ["Draft", "Submitted", "UnderReview", "Resolved", "Rejected"]

      - id: T3
        name: "DB Migration"
        terminal: backend
        model: sonnet
        priority: high
        status: pending
        blocked_by: [T1]
        triggers_on_done: [T4]

      - id: T4
        name: "API Endpoints"
        terminal: backend
        model: sonnet
        priority: high
        status: pending
        blocked_by: [T2, T3]  # Mindkettő kell

        auto_generate: true
        generator: "generate-endpoint"
        params:
          module: "spaceos-modules-procurement"
          aggregate: "SupplierComplaint"
          actions:
            - name: "Create"
              http: "POST"
              route: "/api/procurement/complaints"
            - name: "Submit"
              http: "POST"
              route: "/api/procurement/complaints/{id}/submit"
            - name: "Resolve"
              http: "POST"
              route: "/api/procurement/complaints/{id}/resolve"

  - id: M2
    name: "Frontend Implementation"
    status: pending
    blocked_by: [M1]  # Milestone M1 kell

    tasks:
      - id: T5
        name: "UI Components"
        terminal: frontend
        model: sonnet
        priority: medium
        status: pending
        blocked_by: []  # M2 indulásakor indul
```

### 3.2 Státusz Állapotgép (FSM)

```
Task FSM:
  pending ──[start]──► in_progress
     │                      │
     │                      ├──[done]──► done
     │                      │
     │                      └──[block]──► blocked
     │                                       │
     └────────────────[unblock]──────────────┘

Milestone FSM:
  pending ──[all_tasks_done]──► done
     │
     └──[any_task_blocked]──► blocked
```

### 3.3 Validációs Szabályok

| Szabály | Leírás |
|---------|--------|
| **V1** | `id` egyedi milestone-on belül |
| **V2** | `blocked_by` nem tartalmazhat önmagát |
| **V3** | `blocked_by` referenciák léteznek |
| **V4** | Nincs körkörös függőség (DAG) |
| **V5** | `terminal` valid terminál név |
| **V6** | `generator` létező szkript |
| **V7** | `model` ∈ {haiku, sonnet, opus} |

---

## 4. Auto-Dispatch Daemon (v2)

### 4.1 Architektúra

```
                                    ┌─────────────────────────┐
                                    │   Knowledge Service     │
                                    │   (Port 3456)           │
                                    └───────────┬─────────────┘
                                                │
                                                │ HTTP/SSE
                                                │
┌─────────────────────────────────────────────────────────────────────────┐
│                        PROJECT DISPATCHER                                │
│                    (knowledge-service/src/pipeline/)                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │
│  │  DONE        │    │  TASKS.yaml  │    │  Generator   │              │
│  │  Watcher     │───►│  Processor   │───►│  Runner      │              │
│  │  (Chokidar)  │    │              │    │              │              │
│  └──────────────┘    └──────────────┘    └──────────────┘              │
│         │                   │                   │                        │
│         │                   │                   │                        │
│         ▼                   ▼                   ▼                        │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │
│  │  terminals/  │    │  docs/       │    │  backend/    │              │
│  │  */outbox/   │    │  projects/   │    │  spaceos-*   │              │
│  └──────────────┘    └──────────────┘    └──────────────┘              │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Dispatch Logic (Pseudocode)

```typescript
// projectDispatcher.ts

interface DispatcherConfig {
  checkInterval: number;      // 60_000 (1 perc)
  projectsDir: string;        // /opt/spaceos/docs/projects
  terminalsDir: string;       // /opt/spaceos/terminals
  generatorsDir: string;      // /opt/spaceos/scripts/generators
  notifyTelegram: boolean;
}

async function dispatchLoop(config: DispatcherConfig) {
  while (true) {
    // 1. Scan all active projects
    const projects = await scanActiveProjects(config.projectsDir);

    for (const project of projects) {
      // 2. Load TASKS.yaml
      const tasks = await loadTasksYaml(project.path);

      // 3. Check for DONE outbox messages
      const doneMessages = await scanDoneMessages(config.terminalsDir);

      for (const done of doneMessages) {
        // 4. Match DONE to project task
        const task = matchDoneToTask(tasks, done);
        if (!task) continue;

        // 5. Mark task as done
        await markTaskDone(tasks, task.id, done);

        // 6. Update TASKS.yaml
        await saveTasksYaml(project.path, tasks);

        // 7. Find next unblocked tasks
        const nextTasks = findUnblockedTasks(tasks, task.triggers_on_done);

        for (const nextTask of nextTasks) {
          // 8. Run generator if auto_generate
          if (nextTask.auto_generate && nextTask.generator) {
            await runGenerator(nextTask.generator, nextTask.params, config);
          }

          // 9. Generate inbox message
          const inboxPath = await generateInboxMessage(nextTask, project, done);

          // 10. Mark task as in_progress
          await markTaskInProgress(tasks, nextTask.id, inboxPath);

          // 11. Notify via Telegram
          if (config.notifyTelegram) {
            await notifyTelegram(`🚀 Task dispatched: ${nextTask.name} → ${nextTask.terminal}`);
          }
        }

        // 12. Update STATUS.md
        await updateProjectStatus(project.path, tasks);

        // 13. Check milestone completion
        await checkMilestoneCompletion(tasks);
      }
    }

    await sleep(config.checkInterval);
  }
}
```

### 4.3 DONE Matching Logika

```typescript
interface DoneMessage {
  from: string;       // terminál
  task_id: string;    // MSG-BACKEND-004
  ref?: string;       // Opcionális referencia
  timestamp: Date;
}

function matchDoneToTask(tasks: TaskChain, done: DoneMessage): Task | null {
  // 1. Exact match by msg_id
  const exactMatch = tasks.milestones
    .flatMap(m => m.tasks)
    .find(t => t.msg_id === done.task_id);

  if (exactMatch) return exactMatch;

  // 2. Match by ref (ha a DONE-ban van ref)
  if (done.ref) {
    const refMatch = tasks.milestones
      .flatMap(m => m.tasks)
      .find(t => t.msg_id === done.ref);

    if (refMatch) return refMatch;
  }

  // 3. Fuzzy match: same terminal + status in_progress
  const fuzzyMatch = tasks.milestones
    .flatMap(m => m.tasks)
    .find(t =>
      t.terminal === done.from &&
      t.status === 'in_progress'
    );

  return fuzzyMatch || null;
}
```

### 4.4 BLOCKED Kezelés

```typescript
async function handleBlocked(task: Task, blockedMsg: BlockedMessage) {
  // 1. Increment retry counter
  task.retry_count = (task.retry_count || 0) + 1;

  // 2. Check max retries
  if (task.retry_count >= config.retry_on_blocked) {
    // Escalate to Conductor
    await sendMessage({
      to: 'conductor',
      type: 'blocked',
      priority: 'high',
      content: `
## Task BLOCKED: ${task.name}

**Projekt:** ${project.name}
**Task ID:** ${task.id}
**Terminál:** ${task.terminal}
**Blocker:** ${blockedMsg.reason}
**Próbálkozások:** ${task.retry_count}/${config.retry_on_blocked}

### Szükséges döntés
[ ] Alternatív megoldás keresése
[ ] Task scope módosítása
[ ] Projekt prioritás változtatás
      `,
      model: 'sonnet',
    });

    task.status = 'escalated';
  } else {
    // Keep as blocked, will retry
    task.status = 'blocked';
  }
}
```

### 4.5 Párhuzamos Task Kezelés

```typescript
function findUnblockedTasks(tasks: TaskChain, triggeredIds: string[]): Task[] {
  const unblocked: Task[] = [];

  for (const taskId of triggeredIds) {
    const task = findTaskById(tasks, taskId);
    if (!task) continue;

    // Check if ALL blocked_by tasks are done
    const allBlockersDone = task.blocked_by.every(blockerId => {
      const blocker = findTaskById(tasks, blockerId);
      return blocker && blocker.status === 'done';
    });

    if (allBlockersDone && task.status === 'pending') {
      unblocked.push(task);
    }
  }

  return unblocked;
}
```

### 4.6 Cron Konfiguráció

**Ajánlott:** Nem cron, hanem Chokidar watcher (event-driven)

```typescript
// A dispatcher NE cron-ra fusson, hanem:
// 1. Chokidar figyeli a terminals/*/outbox/ mappákat
// 2. DONE fájl megjelenésekor triggerelődik
// 3. Immediate dispatch (nem kell várni cron-ra)

import chokidar from 'chokidar';

const outboxWatcher = chokidar.watch('terminals/*/outbox/*.md', {
  persistent: true,
  ignoreInitial: true,
});

outboxWatcher.on('add', async (filePath) => {
  const content = await fs.readFile(filePath, 'utf-8');
  const frontmatter = parseYamlFrontmatter(content);

  if (frontmatter.type === 'done') {
    await processProjectDone(frontmatter);
  }
});
```

---

## 5. Generator Scripts (v3)

### 5.1 Skeleton Generator

**Lokáció:** `/opt/spaceos/scripts/generators/generate-module.ts`

**Technológia választás:**
- ✅ **TypeScript** (ajánlott) — típusbiztos, Knowledge Service-be integrálható
- ❌ Bash — törékeny, nehezen karbantartható
- ❌ Handlebars — over-engineering, nincs szükség template engine-re

```typescript
// generate-module.ts

interface GenerateModuleParams {
  module: string;           // spaceos-modules-procurement
  aggregate: string;        // SupplierComplaint
  states: string[];         // ["Draft", "Submitted", ...]
  endpoints?: string[];     // ["create", "list", ...]
}

interface GeneratedFile {
  path: string;
  content: string;
}

async function generateModule(params: GenerateModuleParams): Promise<GeneratedFile[]> {
  const files: GeneratedFile[] = [];
  const { module, aggregate, states, endpoints } = params;

  const modulePath = `/opt/spaceos/backend/${module}`;
  const pascalAggregate = toPascalCase(aggregate);
  const camelAggregate = toCamelCase(aggregate);

  // 1. Generate Status Enum
  files.push({
    path: `${modulePath}/src/Domain/Enums/${pascalAggregate}Status.cs`,
    content: generateStatusEnum(pascalAggregate, states),
  });

  // 2. Generate Aggregate
  files.push({
    path: `${modulePath}/src/Domain/Aggregates/${pascalAggregate}.cs`,
    content: generateAggregate(pascalAggregate, states),
  });

  // 3. Generate Events
  for (const state of states) {
    files.push({
      path: `${modulePath}/src/Domain/Events/${pascalAggregate}${state}Event.cs`,
      content: generateDomainEvent(pascalAggregate, state),
    });
  }

  // 4. Generate Repository Interface
  files.push({
    path: `${modulePath}/src/Domain/Interfaces/I${pascalAggregate}Repository.cs`,
    content: generateRepositoryInterface(pascalAggregate),
  });

  // 5. Generate DTOs
  files.push({
    path: `${modulePath}/src/Application/DTOs/${pascalAggregate}Dto.cs`,
    content: generateDto(pascalAggregate, states),
  });

  // 6. Generate Commands (if endpoints specified)
  if (endpoints) {
    for (const endpoint of endpoints) {
      files.push({
        path: `${modulePath}/src/Application/Commands/${toPascalCase(endpoint)}${pascalAggregate}/${toPascalCase(endpoint)}${pascalAggregate}Command.cs`,
        content: generateCommand(pascalAggregate, endpoint),
      });
      files.push({
        path: `${modulePath}/src/Application/Commands/${toPascalCase(endpoint)}${pascalAggregate}/${toPascalCase(endpoint)}${pascalAggregate}CommandHandler.cs`,
        content: generateCommandHandler(pascalAggregate, endpoint),
      });
    }
  }

  // 7. Generate EF Configuration
  files.push({
    path: `${modulePath}/src/Infrastructure/Persistence/Configurations/${pascalAggregate}Configuration.cs`,
    content: generateEfConfiguration(pascalAggregate, states),
  });

  // 8. Generate Repository Implementation
  files.push({
    path: `${modulePath}/src/Infrastructure/Repositories/${pascalAggregate}Repository.cs`,
    content: generateRepositoryImpl(pascalAggregate),
  });

  // 9. Generate Test Skeletons
  files.push({
    path: `${modulePath}/tests/Domain/${pascalAggregate}Tests.cs`,
    content: generateDomainTests(pascalAggregate, states),
  });

  return files;
}

// Template functions
function generateStatusEnum(name: string, states: string[]): string {
  return `namespace SpaceOS.Modules.Procurement.Domain.Enums;

public enum ${name}Status
{
${states.map((s, i) => `    ${s} = ${i}`).join(',\n')}
}
`;
}

function generateAggregate(name: string, states: string[]): string {
  return `namespace SpaceOS.Modules.Procurement.Domain.Aggregates;

using SpaceOS.Modules.Procurement.Domain.Enums;
using SpaceOS.Modules.Procurement.Domain.Events;

public class ${name} : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public ${name}Status Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private ${name}() { } // EF Core

    public static ${name} Create(Guid tenantId)
    {
        var entity = new ${name}
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = ${name}Status.${states[0]},
            CreatedAt = DateTimeOffset.UtcNow,
        };

        entity.AddDomainEvent(new ${name}CreatedEvent(entity.Id, entity.TenantId));
        return entity;
    }

${states.slice(1).map(state => `
    public void ${state}()
    {
        // TODO: Add business logic validation
        Status = ${name}Status.${state};
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ${name}${state}Event(Id, TenantId));
    }
`).join('\n')}
}
`;
}
```

### 5.2 Endpoint Scaffolder

**Lokáció:** `/opt/spaceos/scripts/generators/generate-endpoint.ts`

```typescript
interface GenerateEndpointParams {
  module: string;
  aggregate: string;
  action: string;
  http: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  route: string;
}

async function generateEndpoint(params: GenerateEndpointParams): Promise<GeneratedFile[]> {
  const files: GeneratedFile[] = [];
  const { module, aggregate, action, http, route } = params;

  const modulePath = `/opt/spaceos/backend/${module}`;
  const pascalAggregate = toPascalCase(aggregate);
  const pascalAction = toPascalCase(action);

  // 1. Generate Command
  files.push({
    path: `${modulePath}/src/Application/Commands/${pascalAction}${pascalAggregate}/${pascalAction}${pascalAggregate}Command.cs`,
    content: generateCommand(pascalAggregate, action),
  });

  // 2. Generate Command Handler
  files.push({
    path: `${modulePath}/src/Application/Commands/${pascalAction}${pascalAggregate}/${pascalAction}${pascalAggregate}CommandHandler.cs`,
    content: generateCommandHandler(pascalAggregate, action),
  });

  // 3. Generate Validator
  files.push({
    path: `${modulePath}/src/Application/Commands/${pascalAction}${pascalAggregate}/${pascalAction}${pascalAggregate}Validator.cs`,
    content: generateValidator(pascalAggregate, action),
  });

  // 4. Append to Endpoints file
  // NOTE: Ez append, nem overwrite!
  const endpointSnippet = generateEndpointRegistration(pascalAggregate, action, http, route);
  files.push({
    path: `${modulePath}/src/Api/Endpoints/${pascalAggregate}Endpoints.cs`,
    content: endpointSnippet,
    mode: 'append',  // Special flag
  });

  // 5. Generate Test
  files.push({
    path: `${modulePath}/tests/Api/${pascalAction}${pascalAggregate}Tests.cs`,
    content: generateEndpointTest(pascalAggregate, action, http, route),
  });

  return files;
}
```

### 5.3 Inbox Template Generator

**Lokáció:** `/opt/spaceos/scripts/generators/generate-inbox.ts`

```typescript
interface GenerateInboxParams {
  terminal: string;
  project: string;
  task: Task;
  ref?: string;
}

async function generateInbox(params: GenerateInboxParams): Promise<string> {
  const { terminal, project, task, ref } = params;

  const date = new Date().toISOString().split('T')[0];
  const msgNum = await getNextMsgNumber(terminal);
  const slug = toSlug(task.name);

  const fileName = `${date}_${msgNum.toString().padStart(3, '0')}_${slug}.md`;
  const filePath = `/opt/spaceos/terminals/${terminal}/inbox/${fileName}`;

  const msgId = `MSG-${terminal.toUpperCase()}-${msgNum.toString().padStart(3, '0')}`;

  const content = `---
id: ${msgId}
from: conductor
to: ${terminal}
type: task
priority: ${task.priority}
status: UNREAD
model: ${task.model || 'sonnet'}
ref: ${ref || ''}
project: ${project}
task_id: ${task.id}
created: ${date}
---

# ${task.name}

## Feladat

**Projekt:** ${project}
**Milestone:** ${task.milestone_id}
**Prioritás:** ${task.priority.toUpperCase()}

### Kontextus

${task.description || 'Lásd a projekt specifikációt.'}

### Teendők

${task.auto_generate ? `
> ⚙️ **Automatikusan generált skeleton fájlok:**
> Generator: \`${task.generator}\`
> A fájlok már létrejöttek, ellenőrizd és egészítsd ki.
` : ''}

1. Olvasd el a projekt specifikációt: \`docs/projects/${project}/PLAN.md\`
2. Implementáld a feladatot
3. Futtasd a teszteket: \`dotnet test\`
4. Készíts DONE outbox üzenetet

### Definition of Done

- [ ] Kód implementálva
- [ ] Unit tesztek zöldek
- [ ] Nincs lint warning
- [ ] DONE outbox üzenet elküldve

### Referenciák

- Projekt spec: \`docs/projects/${project}/PLAN.md\`
- TASKS.yaml: \`docs/projects/${project}/TASKS.yaml\`
${ref ? `- Előző task: \`${ref}\`` : ''}
`;

  await fs.writeFile(filePath, content, 'utf-8');
  return filePath;
}
```

### 5.4 Template Lokáció

```
/opt/spaceos/scripts/generators/
  generate-module.ts       ← Modul skeleton
  generate-endpoint.ts     ← API endpoint
  generate-inbox.ts        ← Inbox üzenet
  templates/
    aggregate.cs.tmpl      ← C# template-ek
    command.cs.tmpl
    handler.cs.tmpl
    test.cs.tmpl
  utils/
    casing.ts              ← PascalCase, camelCase
    file.ts                ← Fájl műveletek
```

### 5.5 Meglévő Fájl Kezelés

| Scenario | Viselkedés |
|----------|------------|
| Fájl nem létezik | Létrehozás |
| Fájl létezik, `mode: append` | Hozzáfűzés marker után |
| Fájl létezik, `mode: overwrite` | Felülírás (ritkán) |
| Fájl létezik, default | **SKIP** + warning log |

```typescript
async function writeGeneratedFile(file: GeneratedFile): Promise<WriteResult> {
  const exists = await fs.access(file.path).then(() => true).catch(() => false);

  if (exists) {
    if (file.mode === 'append') {
      const existing = await fs.readFile(file.path, 'utf-8');
      const marker = '// --- AUTO-GENERATED ENDPOINTS ---';
      if (existing.includes(marker)) {
        const [before, after] = existing.split(marker);
        await fs.writeFile(file.path, before + marker + '\n' + file.content + after);
        return { status: 'appended', path: file.path };
      } else {
        await fs.appendFile(file.path, '\n' + marker + '\n' + file.content);
        return { status: 'appended', path: file.path };
      }
    } else if (file.mode === 'overwrite') {
      await fs.writeFile(file.path, file.content);
      return { status: 'overwritten', path: file.path };
    } else {
      console.warn(`[Generator] SKIP: File exists: ${file.path}`);
      return { status: 'skipped', path: file.path };
    }
  }

  await fs.mkdir(path.dirname(file.path), { recursive: true });
  await fs.writeFile(file.path, file.content);
  return { status: 'created', path: file.path };
}
```

---

## 6. MCP Tools Integration (v4)

### 6.1 Új MCP Tools

A következő tool-ok kerülnek a Knowledge Service-be:

```typescript
// Hozzáadandó: knowledge-service/src/mcp.ts

const PROJECT_TOOLS = [
  // Project Management
  {
    name: 'create_project',
    description: 'Create a new project structure with PROJECT.md, TASKS.yaml, STATUS.md',
    inputSchema: {
      type: 'object',
      properties: {
        slug: { type: 'string', description: 'Project slug (lowercase, no spaces)' },
        name: { type: 'string', description: 'Human-readable project name' },
        description: { type: 'string', description: 'Project description' },
        milestones: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              id: { type: 'string' },
              name: { type: 'string' },
            },
          },
        },
      },
      required: ['slug', 'name'],
    },
  },

  {
    name: 'get_project_status',
    description: 'Get current status of a project including task completion',
    inputSchema: {
      type: 'object',
      properties: {
        project: { type: 'string', description: 'Project slug' },
      },
      required: ['project'],
    },
  },

  {
    name: 'dispatch_next',
    description: 'Manually dispatch the next unblocked task(s) for a project',
    inputSchema: {
      type: 'object',
      properties: {
        project: { type: 'string', description: 'Project slug' },
        task_id: { type: 'string', description: 'Specific task to dispatch (optional)' },
      },
      required: ['project'],
    },
  },

  {
    name: 'list_blocked',
    description: 'List all blocked tasks across all projects',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // Generator Tools
  {
    name: 'generate_skeleton',
    description: 'Generate domain layer skeleton for a new aggregate',
    inputSchema: {
      type: 'object',
      properties: {
        module: { type: 'string', description: 'Module path (e.g., spaceos-modules-procurement)' },
        aggregate: { type: 'string', description: 'Aggregate name (PascalCase)' },
        states: {
          type: 'array',
          items: { type: 'string' },
          description: 'FSM states for the aggregate',
        },
        endpoints: {
          type: 'array',
          items: { type: 'string' },
          description: 'Endpoints to generate (optional)',
        },
      },
      required: ['module', 'aggregate', 'states'],
    },
  },

  {
    name: 'generate_endpoint',
    description: 'Generate API endpoint with command, handler, and tests',
    inputSchema: {
      type: 'object',
      properties: {
        module: { type: 'string' },
        aggregate: { type: 'string' },
        action: { type: 'string', description: 'Action name (e.g., Create, Submit, Resolve)' },
        http: { type: 'string', enum: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'] },
        route: { type: 'string', description: 'API route (e.g., /api/complaints/{id}/resolve)' },
      },
      required: ['module', 'aggregate', 'action', 'http', 'route'],
    },
  },

  // Task Tools
  {
    name: 'update_task_status',
    description: 'Update task status in TASKS.yaml (for manual intervention)',
    inputSchema: {
      type: 'object',
      properties: {
        project: { type: 'string' },
        task_id: { type: 'string' },
        status: { type: 'string', enum: ['pending', 'in_progress', 'done', 'blocked'] },
        reason: { type: 'string', description: 'Reason for status change (optional)' },
      },
      required: ['project', 'task_id', 'status'],
    },
  },
];
```

### 6.2 Tool Handlers

```typescript
// Hozzáadandó: knowledge-service/src/projectTools.ts

import * as fs from 'fs/promises';
import * as yaml from 'yaml';
import { generateModule, generateEndpoint } from './generators';

const PROJECTS_DIR = '/opt/spaceos/docs/projects';

export async function handleCreateProject(args: CreateProjectArgs) {
  const { slug, name, description, milestones } = args;
  const projectDir = `${PROJECTS_DIR}/${slug}`;

  // Create directory structure
  await fs.mkdir(`${projectDir}/milestones`, { recursive: true });
  await fs.mkdir(`${projectDir}/inbox-templates`, { recursive: true });

  // Create PROJECT.md
  const projectMd = `---
id: PROJECT-${slug.toUpperCase()}
name: "${name}"
owner: conductor
status: PLANNING
created: ${new Date().toISOString().split('T')[0]}
updated: ${new Date().toISOString().split('T')[0]}
---

# ${name}

## Cél
${description || 'TODO: Add project goal'}

## Scope
${(milestones || []).map(m => `- [ ] ${m.name}`).join('\n') || '- [ ] Milestone 1'}

## Out of Scope
- TODO

## Stakeholders
- **Owner:** conductor
`;

  await fs.writeFile(`${projectDir}/PROJECT.md`, projectMd);

  // Create TASKS.yaml skeleton
  const tasksYaml = {
    version: '1.0',
    project: slug,
    created: new Date().toISOString().split('T')[0],
    updated: new Date().toISOString().split('T')[0],
    config: {
      default_model: 'sonnet',
      auto_dispatch: true,
      notify_telegram: true,
    },
    milestones: (milestones || [{ id: 'M1', name: 'Initial' }]).map(m => ({
      id: m.id,
      name: m.name,
      status: 'pending',
      blocked_by: [],
      tasks: [],
    })),
  };

  await fs.writeFile(`${projectDir}/TASKS.yaml`, yaml.stringify(tasksYaml));

  // Create STATUS.md
  const statusMd = `# ${name} — Status

**Generated:** ${new Date().toISOString()}

## Progress

| Milestone | Tasks | Done | % |
|-----------|-------|------|---|
${(milestones || []).map(m => `| ${m.name} | 0 | 0 | 0% |`).join('\n')}

## Recent Activity

_No activity yet_
`;

  await fs.writeFile(`${projectDir}/STATUS.md`, statusMd);

  return {
    success: true,
    path: projectDir,
    files: ['PROJECT.md', 'TASKS.yaml', 'STATUS.md'],
  };
}

export async function handleGetProjectStatus(args: { project: string }) {
  const projectDir = `${PROJECTS_DIR}/${args.project}`;

  const [projectMd, tasksYaml, statusMd] = await Promise.all([
    fs.readFile(`${projectDir}/PROJECT.md`, 'utf-8').catch(() => null),
    fs.readFile(`${projectDir}/TASKS.yaml`, 'utf-8').catch(() => null),
    fs.readFile(`${projectDir}/STATUS.md`, 'utf-8').catch(() => null),
  ]);

  if (!tasksYaml) {
    return { error: `Project not found: ${args.project}` };
  }

  const tasks = yaml.parse(tasksYaml);

  // Calculate stats
  const allTasks = tasks.milestones.flatMap(m => m.tasks);
  const doneTasks = allTasks.filter(t => t.status === 'done');
  const blockedTasks = allTasks.filter(t => t.status === 'blocked');
  const inProgressTasks = allTasks.filter(t => t.status === 'in_progress');

  return {
    project: args.project,
    status: tasks.status || 'unknown',
    totalTasks: allTasks.length,
    doneTasks: doneTasks.length,
    blockedTasks: blockedTasks.length,
    inProgressTasks: inProgressTasks.length,
    progress: allTasks.length > 0
      ? Math.round((doneTasks.length / allTasks.length) * 100)
      : 0,
    milestones: tasks.milestones.map(m => ({
      id: m.id,
      name: m.name,
      status: m.status,
      tasks: m.tasks.length,
      done: m.tasks.filter(t => t.status === 'done').length,
    })),
    blocked: blockedTasks.map(t => ({
      id: t.id,
      name: t.name,
      terminal: t.terminal,
    })),
    inProgress: inProgressTasks.map(t => ({
      id: t.id,
      name: t.name,
      terminal: t.terminal,
      msg_id: t.msg_id,
    })),
  };
}

export async function handleListBlocked() {
  const projects = await fs.readdir(PROJECTS_DIR);
  const blocked: Array<{ project: string; task: Task }> = [];

  for (const project of projects) {
    const tasksPath = `${PROJECTS_DIR}/${project}/TASKS.yaml`;
    try {
      const tasksYaml = await fs.readFile(tasksPath, 'utf-8');
      const tasks = yaml.parse(tasksYaml);

      for (const milestone of tasks.milestones) {
        for (const task of milestone.tasks) {
          if (task.status === 'blocked') {
            blocked.push({ project, task });
          }
        }
      }
    } catch {
      // Skip non-project directories
    }
  }

  return {
    count: blocked.length,
    blocked,
  };
}
```

### 6.3 Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        KNOWLEDGE SERVICE                                 │
│                       (spaceos-knowledge)                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  EXISTING MODULES              NEW MODULES                               │
│  ┌──────────────┐             ┌──────────────┐                          │
│  │  mcp.ts      │─────────────│ projectTools │                          │
│  │  (23 tools)  │   import    │    .ts       │                          │
│  └──────────────┘             └──────┬───────┘                          │
│         │                            │                                   │
│         │                            │                                   │
│  ┌──────────────┐             ┌──────────────┐                          │
│  │  mailbox.ts  │             │  generators/ │                          │
│  │              │◄────────────│    *.ts      │                          │
│  └──────────────┘  sendMessage└──────────────┘                          │
│         │                            │                                   │
│         │                            │                                   │
│  ┌──────────────┐             ┌──────────────┐                          │
│  │  inboxWatch  │             │  projectDisp │                          │
│  │  er.ts       │◄────────────│  atcher.ts   │                          │
│  └──────────────┘  chokidar   └──────────────┘                          │
│                     events                                               │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Implementation Guide

### 7.1 Fájl Struktúra

```
spaceos-nexus/knowledge-service/
  src/
    pipeline/
      projectDispatcher.ts    ← Fő dispatcher logika
      projectMatcher.ts       ← DONE→task matching
      statusUpdater.ts        ← STATUS.md generálás
    generators/
      generateModule.ts       ← Skeleton generator
      generateEndpoint.ts     ← Endpoint scaffolder
      generateInbox.ts        ← Inbox template
      templates/
        *.cs.tmpl             ← C# templates
    projectTools.ts           ← MCP tool handlers
    mcp.ts                    ← +6 új tool (29 total)
```

### 7.2 Implementációs Track-ek

| Track | Fájlok | Effort | Blocker |
|-------|--------|--------|---------|
| **A: YAML Processor** | projectDispatcher.ts, projectMatcher.ts | 1 nap | - |
| **B: Generator Core** | generateModule.ts, generateEndpoint.ts | 1.5 nap | - |
| **C: Templates** | templates/*.cs.tmpl | 0.5 nap | Track B |
| **D: MCP Integration** | projectTools.ts, mcp.ts | 1 nap | Track A |
| **E: Tests** | __tests__/pipeline/*.test.ts | 1 nap | Track A-D |

### 7.3 DoD Checklist

- [ ] YAML séma dokumentálva és validálva
- [ ] projectDispatcher működik (unit test)
- [ ] generateModule létrehoz 9+ fájlt (integration test)
- [ ] generateEndpoint append működik
- [ ] MCP tools regisztrálva (23 → 29)
- [ ] Telegram értesítés működik
- [ ] E2E teszt: create_project → add task → DONE → auto-dispatch

### 7.4 Konfiguráció

```typescript
// config/projectAutomation.ts

export const PROJECT_AUTOMATION_CONFIG = {
  // Paths
  projectsDir: '/opt/spaceos/docs/projects',
  terminalsDir: '/opt/spaceos/terminals',
  generatorsDir: '/opt/spaceos/spaceos-nexus/knowledge-service/src/generators',
  templatesDir: '/opt/spaceos/spaceos-nexus/knowledge-service/src/generators/templates',

  // Behavior
  autoDispatch: true,
  notifyTelegram: true,
  watchInterval: 60_000,  // Fallback if Chokidar fails
  retryOnBlocked: 3,

  // Defaults
  defaultModel: 'sonnet',
  defaultPriority: 'medium',
};
```

### 7.5 Shell Discovery Commands

```bash
# Projekt struktúra ellenőrzés
ls -la /opt/spaceos/docs/projects/

# TASKS.yaml validálás
cat /opt/spaceos/docs/projects/supplier-complaint/TASKS.yaml | yq .

# Generator teszt
cd /opt/spaceos/spaceos-nexus/knowledge-service
npx ts-node src/generators/generateModule.ts --test

# MCP tools lista
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $MCP_TOKEN" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}' | jq '.result.tools[].name'
```

---

## 8. Security Considerations

| Concern | Mitigation |
|---------|------------|
| Arbitrary file write | Generator csak `/opt/spaceos/backend/` és `/opt/spaceos/terminals/` mappákba ír |
| Path traversal | `path.resolve()` + whitelist validation |
| Code injection | Template-ek statikusak, csak whitelist változók |
| YAML bomb | `yaml.parse()` with `maxAliasCount: 100` |

---

## 9. Appendix: Example Project

### supplier-complaint projekt teljes workflow

```
1. Conductor: create_project(slug: "supplier-complaint", ...)
   → docs/projects/supplier-complaint/ létrejön

2. Architect: PLAN.md megírása (v1→v4 spec)
   → PLAN.md frissül

3. Conductor: TASKS.yaml feltöltése a spec alapján
   → Milestones + tasks definiálva

4. Conductor: dispatch_next(project: "supplier-complaint")
   → T1 (Architect Spec) inbox generálódik
   → terminals/architect/inbox/2026-06-21_003_arch-supplier-complaint.md

5. Architect session: inbox feldolgozás → DONE
   → terminals/architect/outbox/2026-06-21_003_arch-supplier-complaint-done.md

6. projectDispatcher: DONE detektálás
   → T1 status: done
   → T2 (Domain Layer) unblocked
   → generateModule() fut → skeleton fájlok
   → terminals/backend/inbox/... generálódik

7. Backend session: skeleton review → implementáció → DONE

8. ... ciklus folytatódik amíg minden task DONE
```

---

**Dokumentum vége**

> Készítette: Architect terminál
> Review: Conductor (functional), Backend (technical feasibility)
> Státusz: IMPLEMENTÁCIÓRA KÉSZ
