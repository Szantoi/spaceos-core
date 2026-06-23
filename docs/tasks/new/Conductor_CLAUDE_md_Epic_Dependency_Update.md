# Conductor CLAUDE.md — Epic Dependency Update Proposal

> **Státusz:** JAVASLAT
> **ADR:** ADR-041-graph-based-workflow-architecture.md
> **Dátum:** 2026-06-22

---

## Hozzáadandó szekció

A következő szekciót kell hozzáadni a `terminals/conductor/CLAUDE.md` fájlhoz, a "PROJEKT KEZELÉS — MCP TOOLOK" szekció után:

---

```markdown
## EPIC DEPENDENCY KEZELÉS

> **Új funkcionalitás (ADR-041):** Epic-szintű dependency tracking.
>
> A Conductor figyelembe veszi az epic-szintű függőségeket is a task dispatch-nál.

### Mi az Epic?

Az **Epic** több projekt/modul felett átívelő nagy feature vagy milestone.
Példák:
- `EPIC-KERNEL-STABLE` — Kernel L1 alapok
- `EPIC-CUTTING-Q3` — Cutting modul Q3 release
- `EPIC-PORTAL-V2` — Customer Portal v2

### EPICS.yaml

**Lokáció:** `/opt/spaceos/docs/projects/EPICS.yaml`

Ez a fájl tartalmazza az összes SpaceOS epic függőségét.
A Conductor és a Graph API ezt használja.

### Epic dispatch szabályok

1. **Blokkolt epic = blokkolt task dispatch**
   Ha egy epic függ egy másik epic-től (`depends_on`), és az még nem `done`,
   akkor az epic taskjait **NE indítsd el**.

   ```yaml
   # Példa: EPIC-PORTAL-V2 függ EPIC-IDENTITY-V1-től
   - id: EPIC-PORTAL-V2
     depends_on: ["EPIC-IDENTITY-V1"]
     status: pending
   ```

   Ha `EPIC-IDENTITY-V1` status ≠ `done` → Portal taskok blokkoltak.

2. **Parallel epics**
   Ha két epic `parallel_with` kapcsolatban van, mindkettő taskjai indíthatók párhuzamosan
   (feltéve, hogy nincs más blokkoló depends_on).

   ```yaml
   - id: EPIC-CUTTING-Q3
     parallel_with: ["EPIC-PORTAL-V2"]
   ```

3. **Epic státusz frissítés**
   Ha egy epic **összes** taskja `done`, frissítsd az epic státuszát is `done`-ra.
   Ez automatikusan unblock-olja a downstream epic-eket.

### Ellenőrzés dispatch előtt

**Bash (yq):**
```bash
# Epic dependency check
EPIC_ID="EPIC-CUTTING-Q3"
DEPS=$(yq ".epics[] | select(.id == \"$EPIC_ID\") | .depends_on[]" /opt/spaceos/docs/projects/EPICS.yaml)

for DEP in $DEPS; do
  STATUS=$(yq ".epics[] | select(.id == \"$DEP\") | .status" /opt/spaceos/docs/projects/EPICS.yaml)
  if [ "$STATUS" != "done" ]; then
    echo "BLOCKED: $EPIC_ID depends on $DEP (status: $STATUS)"
    exit 1
  fi
done

echo "CLEAR: $EPIC_ID can proceed"
```

**API (Graph endpoint):**
```bash
# Epic gráf lekérdezés
curl -s http://localhost:3456/api/graph/epics | jq '.graph.nodes[] | select(.status != "done") | .id'

# Critical path (leghosszabb dependency lánc)
curl -s http://localhost:3456/api/graph/critical-path/epic/EPICS

# Mermaid diagram (vizualizáció)
curl -s http://localhost:3456/api/graph/mermaid/epic/EPICS
```

### Epic dispatch workflow

```
1. Task dispatch kérés érkezik (project: spaceos/cutting)
       ↓
2. Keresse meg a project epic-jét (EPIC-CUTTING-Q3)
       ↓
3. Ellenőrizze a depends_on listát
       ↓
4. Ha bármely dependency nem done → BLOCKED, skip dispatch
       ↓
5. Ha minden dependency done → PROCEED with dispatch
       ↓
6. Task completion → check all tasks in epic
       ↓
7. Ha mind done → epic.status = "done" (EPICS.yaml update)
       ↓
8. Downstream epics unblocked
```

### MCP Tool (hamarosan)

**Phase 2-ben érkezik:**
```
mcp__spaceos-knowledge__get_epic_graph
  → Returns: WorkflowGraph of all epics

mcp__spaceos-knowledge__check_epic_ready
  epic_id: "EPIC-CUTTING-Q3"
  → Returns: { ready: boolean, blockers: string[] }
```

### Példa: Epic blokkolás kezelése

**Scenario:** Frontend terminal UNREAD inbox, de EPIC-PORTAL-V2 függ EPIC-IDENTITY-V1-től.

1. Conductor látja a Frontend inbox-ot
2. Check: `EPIC-PORTAL-V2.depends_on = ["EPIC-IDENTITY-V1"]`
3. Check: `EPIC-IDENTITY-V1.status = "active"` (nem done!)
4. **Döntés:** Ne indítsd el a Frontend session-t
5. **Log:** "Frontend blocked: EPIC-PORTAL-V2 waits for EPIC-IDENTITY-V1"
6. Várj, amíg EPIC-IDENTITY-V1 = done

### Hibakezelés

| Helyzet | Teendő |
|---------|--------|
| Epic nem található EPICS.yaml-ban | Warning log, proceed (assume no deps) |
| Circular dependency | Error log, ne dispatch semmit |
| EPICS.yaml parse error | Error log, fallback to no-epic mode |
```

---

## Módosítandó meglévő szekciók

### 1. "FELADATTÍPUSOK" szekció kiegészítése

Az "1. Planning queue feldolgozás" részhez add hozzá:

```markdown
6. **Epic dependency ellenőrzés dispatch előtt:**
   - Keresd meg a konsenzus target project epic-jét
   - Ellenőrizd, hogy az epic depends_on listája mind `done`
   - Ha nem → ne adj ki inbox-ot, log warning
```

### 2. "FONTOS SZABÁLYOK" szekció kiegészítése

Új szabály:

```markdown
6. **Epic dependency tisztelet** — Ne dispatch-elj taskot, ha az epic blocked
```

---

## Implementációs megjegyzés

Ez a frissítés a **Phase 1 TASK-006** része.
A tényleges CLAUDE.md módosítást a Backend terminál végzi el, miután a
`checkEpicDependencies()` function implementálva van (TASK-005).

---

**Létrehozva:** 2026-06-22 Architect session (MSG-ARCHITECT-005)
