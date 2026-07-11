---
id: MSG-ARCHITECT-002
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-21
content_hash: 39e3db66039b979ff5e3b4452f9f37f3f1177297e67f0ae1bb32d855ac85e0db
---

# ARCH-002: Projekt Koordináció Automatizálás Rendszer Tervezése

## Háttér

A Conductor hosszabb, több milestone-os feladatokat koordinál. Jelenleg minden manuális:
- Task kiosztás kézzel (inbox írás)
- DONE feldolgozás kézzel
- Következő task meghatározás kézzel
- Skeleton/API kód írás a terminálok által (ismétlődő munka)

**Cél:** Automatizálni a projekt koordinációt és a boilerplate kód generálást.

---

## Tervezési feladat

Készíts **v1→v4 architektúra specifikációt** az alábbi rendszerre:

### 1. Projekt struktúra

```
docs/projects/<project-slug>/
  PROJECT.md           ← Projekt definíció
  PLAN.md              ← v1→v4 spec output
  TASKS.yaml           ← Task chain definíció
  STATUS.md            ← Auto-updated státusz
  milestones/
    M1_<name>.md
    M2_<name>.md
```

### 2. Task Chain YAML séma

Tervezd meg a YAML sémát ami leírja:
- Milestone-ok és task-ok
- Függőségek (`blocked_by`, `triggers_on_done`)
- Terminál hozzárendelés
- Model választás (haiku/sonnet/opus)
- Auto-generate flag és generator params

**Példa:**
```yaml
project: supplier-complaint
milestones:
  - id: M1
    name: "Domain"
    tasks:
      - id: T1
        name: "Architect Spec"
        terminal: architect
        model: opus
        triggers_on_done: [T2]
      - id: T2
        name: "Domain Layer"
        terminal: backend
        blocked_by: [T1]
        auto_generate: true
        generator: "generate-module"
        params:
          aggregate: "SupplierComplaint"
          states: ["Draft", "Submitted", "Resolved"]
```

### 3. Auto-dispatch daemon

**Tervezd meg a logikát:**
1. Scan `terminals/*/outbox/` for DONE
2. Match DONE → projekt task
3. Update `TASKS.yaml` státusz
4. Find next unblocked task(s)
5. If `auto_generate: true` → run generator
6. Generate inbox message
7. Send to terminal
8. Update `STATUS.md`

**Kérdések:**
- Milyen gyakran fusson? (cron interval)
- Hogyan kezelje a BLOCKED-ot?
- Hogyan kezelje a párhuzamos task-okat?
- Hol legyen a daemon? (knowledge-service vs standalone script)

### 4. Skeleton Generator

**Input → Output tervezés:**

```bash
./scripts/generate-module.sh \
  --module "spaceos-modules-procurement" \
  --aggregate "SubcontractOrder" \
  --states "Pending,Accepted,Rejected,Completed" \
  --endpoints "create,list,accept,reject"
```

**Generált fájlok:**
- Domain: Enum, Aggregate, Events, Repository interface
- Application: Commands, Handlers, DTOs
- Infrastructure: EF Config, Repository impl
- API: Endpoints
- Tests: Unit + Integration skeleton

**Kérdések:**
- Bash script vs Node.js/TypeScript?
- Template engine (Handlebars, EJS, string replace)?
- Hol legyenek a template-ek?
- Hogyan kezelje a meglévő fájlokat (append vs overwrite)?

### 5. API Endpoint Scaffolder

**Input → Output tervezés:**

```bash
./scripts/generate-endpoint.sh \
  --module "spaceos-modules-procurement" \
  --aggregate "SupplierComplaint" \
  --action "Resolve" \
  --http "POST" \
  --route "/api/procurement/complaints/{id}/resolve"
```

**Generált fájlok:**
- Command + Handler + Validator
- Endpoint registration (append)
- Test skeleton

### 6. Inbox Template Generator

```bash
./scripts/generate-inbox.sh \
  --terminal "backend" \
  --project "supplier-complaint" \
  --task-id "T3" \
  --ref "MSG-BACKEND-002-DONE"
```

**Output:** Pre-filled inbox markdown a TASKS.yaml alapján.

### 7. MCP Tools (spaceos-knowledge integration)

**Új MCP tool-ok:**
| Tool | Input | Output |
|------|-------|--------|
| `create_project` | name, description, milestones | Projekt struktúra létrehozva |
| `get_project_status` | project_slug | STATUS.md tartalom |
| `dispatch_next` | project_slug | Következő task inbox generálva |
| `generate_skeleton` | module, aggregate, states | Skeleton fájlok létrehozva |
| `generate_endpoint` | module, aggregate, action | Endpoint fájlok létrehozva |
| `list_blocked` | - | BLOCKED task-ok listája |

---

## Definition of Done

- [ ] v1: YAML séma specifikáció (task chain, dependencies)
- [ ] v2: Auto-dispatch daemon terv (logic, error handling, cron)
- [ ] v3: Generator scripts terv (skeleton, endpoint, inbox)
- [ ] v4: Implementation guide (fájlok, technológia választás, MCP integráció)

---

## Referenciák

- Conductor MEMORY.md: `/opt/spaceos/terminals/conductor/MEMORY.md` (elvárás lista)
- Knowledge service: `/opt/spaceos/spaceos-nexus/knowledge-service/`
- Meglévő pipeline: `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/`

---

## Output

A specifikációt ide írd:
`docs/tasks/new/SpaceOS_Project_Automation_Architecture_v4.md`
