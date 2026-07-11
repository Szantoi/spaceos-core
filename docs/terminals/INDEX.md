# SpaceOS Terminálok

> Minden terminál saját mappával rendelkezik ami tartalmazza az összes szükséges artifaktot.
> Ez a dokumentum gyors áttekintést ad az összes terminálról.

## Terminál Architektúra

```
┌──────────────────────────────────────────────────────────────────┐
│  ROOT terminál  (/opt/spaceos)                  [persistent]     │
│  Feladat: stratégiai döntések, Datahaven/Resonance építés        │
└──┬──────────┬──────────┬──────────┬──────────┬───────────────────┘
   │          │          │          │           │
   ▼          ▼          ▼          ▼           ▼
KERNEL     ORCH       JOINERY    CUTTING    INVENTORY   ... (on-demand)
:5000      :3000      :5002      :5005      :5004

┌──────────────────────────────────────────────────────────────────┐
│  CONDUCTOR terminál  (/opt/spaceos/spaceos-conductor/)           │
│  [persistent — tervezés, feladatkiosztás, pipeline kezelés]      │
└──────────────────────────────────────────────────────────────────┘
```

## Terminálok Listája

| Terminál | Port | Típus | Könyvtár | Leírás |
|----------|------|-------|----------|--------|
| **root** | - | persistent | `/opt/spaceos/` | Stratégiai koordináció |
| **conductor** | - | persistent | `/opt/spaceos/spaceos-conductor/` | Tervezés, feladatkiosztás |
| **architect** | - | persistent | `/opt/spaceos/spaceos-architect/` | Konzultatív arch partner |
| **librarian** | - | on-demand | `/opt/spaceos/spaceos-librarian/` | Tudásbázis gondozás |
| **nexus** | 3456 | on-demand | `/opt/spaceos/spaceos-nexus/` | Agent infrastruktúra |
| **kernel** | 5000 | on-demand | `/opt/spaceos/backend/spaceos-kernel/` | .NET 8 backend core |
| **orch** | 3000 | on-demand | `/opt/spaceos/backend/spaceos-orchestrator/` | Node.js BFF |
| **fe** | - | on-demand | `/opt/spaceos/frontend/joinerytech-portal/` | React frontend |
| **joinery** | 5002 | on-demand | `/opt/spaceos/backend/spaceos-modules-joinery/` | Joinery modul |
| **abstractions** | 5003 | on-demand | `/opt/spaceos/backend/spaceos-modules-abstractions/` | Abstractions modul |
| **cutting** | 5005 | on-demand | `/opt/spaceos/backend/spaceos-modules-cutting/` | Cutting modul |
| **inventory** | 5004 | on-demand | `/opt/spaceos/backend/spaceos-modules-inventory/` | Inventory modul |
| **procurement** | 5006 | on-demand | `/opt/spaceos/backend/spaceos-modules-procurement/` | Procurement modul |
| **sales** | 5007 | on-demand | `/opt/spaceos/backend/spaceos-modules-sales/` | Sales modul |
| **identity** | 5008 | on-demand | `/opt/spaceos/backend/spaceos-modules-identity/` | Identity modul |
| **infra** | - | on-demand | `/opt/spaceos/infra/` | VPS, nginx, systemd |
| **e2e** | - | on-demand | `/opt/spaceos/e2e/` | E2E teszt suite |

## MCP Hozzáférés (távoli termináloknak)

```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "http",
      "url": "https://nexus.joinerytech.hu/mcp",
      "timeout": 60000,
      "headers": {
        "Authorization": "Bearer <TOKEN>"
      }
    }
  }
}
```

### Hasznos MCP Toolok

| Tool | Leírás |
|------|--------|
| `get_terminal_setup` | Terminál CLAUDE.md + skill + MCP config |
| `get_identity` | Terminál identity + memory |
| `search_knowledge` | RAG keresés a tudásbázisban |
| `list_inbox` | Terminál inbox lekérdezése |
| `get_workflow` | WORKFLOW.md teljes pipeline |
| `get_project_context` | Vízió, knowledge index, codebase status |

## Terminál Mappa Struktúra

Minden terminál mappája:
```
docs/terminals/<terminal>/
├── README.md          ← Gyors áttekintés (ez a fő fájl)
├── QUICKSTART.md      ← Session indítási útmutató
├── COMMANDS.md        ← Gyakori parancsok
└── CONTEXT.md         ← Domain kontextus (opcionális)
```
