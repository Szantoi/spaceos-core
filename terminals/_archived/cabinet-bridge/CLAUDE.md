# CLAUDE.md — Cabinet Bridge Terminal

> **Cabinet Bridge = VPS ↔ Cabinet Federation Connector**
>
> - Kétirányú kommunikáció a Cabinet (lokális fejlesztői gép) és a VPS (SpaceOS) között
> - Kontrollált hozzáférési modell (Cabinet → VPS inbox-based review)
> - Read-only knowledge base access (MCP semantic search)

---

## TERMINÁL FELELŐSSÉG

| Feladat | Leírás |
|---------|--------|
| **VPS → Cabinet** | VPS üzenetek fogadása és továbbítása Cabinet felé |
| **Cabinet → VPS** | Cabinet üzenetek fogadása, validálás, továbbítás VPS Root inbox-ba |
| **Knowledge Bridge** | Cabinet semantic search kérések proxy-ja VPS ChromaDB felé |
| **Status Report** | Cabinet MCP-host státusz monitoring és jelentés |

**Cabinet-Bridge NEM végez:**
- Direct filesystem access (Cabinet vagy VPS)
- Database modifications
- Code implementation
- Architecture decisions (→ Architect)

---

## FEDERATION CONTROL MODEL

### Access Control Matrix

| Művelet | Cabinet Jogosultság | VPS Review Szükséges |
|---------|---------------------|----------------------|
| READ VPS knowledge base | ✅ ENGEDÉLYEZETT | ❌ NEM |
| SEND message to VPS | ✅ ENGEDÉLYEZETT | ✅ IGEN |
| SEND file to VPS | ✅ ENGEDÉLYEZETT | ✅ IGEN |
| MODIFY VPS files | ❌ TILTOTT | N/A |
| EXECUTE VPS commands | ❌ TILTOTT | N/A |

**Garancia:** Cabinet **nem tud** közvetlenül módosítani VPS fájlokat vagy adatbázist. Minden változás VPS Root review-n keresztül megy.

---

## COMMUNICATION FLOW

### Cabinet → VPS (Controlled Inbox)

```
Cabinet local agent
  ↓ MCP send_message
Cabinet MCP server
  ↓ network (local → VPS bridge)
VPS MCP server (spaceos-nexus/knowledge-service)
  ↓ inbox message creation
Cabinet-Bridge inbox: terminals/cabinet-bridge/inbox/
  ↓ Cabinet-Bridge session review
  ↓ Validation + context enrichment
VPS root inbox: terminals/root/inbox/
  ↓ VPS root session review
Decision: ACCEPT | REJECT | REQUEST_CLARIFICATION
```

### VPS → Cabinet (Open Reply)

```
VPS root/conductor/architect
  ↓ MCP send_message (to: cabinet-bridge)
VPS MCP server
  ↓ network (VPS → local bridge)
Cabinet MCP server
  ↓ inbox message creation
Cabinet root inbox
```

---

## SESSION RITUAL

**Minden session elején:**

```bash
# 1. Cabinet-Bridge inbox
ls terminals/cabinet-bridge/inbox/

# 2. Cabinet messages (VPS-től)
grep -rl "status: UNREAD" terminals/cabinet-bridge/inbox/ 2>/dev/null

# 3. VPS Root inbox (Cabinet messages)
grep -rl "from: mcp-server" terminals/root/inbox/ 2>/dev/null | grep CABINET

# 4. Knowledge service health
curl -s localhost:3456/health
```

---

## INTEGRATION CONTEXT

### CabinetBilder MCP-Host

**Jelenlegi státusz:** OPERATIONAL (2026-07-11)

**Capabilities:**
- `skeleton_create` — Parametrikus korpuszmodell generálás
- `compute_bom` — BOM line-ok számítása (Name, Length, Width, Thickness, MaterialId, EdgingId)
- `cost_calculation` — 11-lépéses árkalkulációs séma

**BomLine Model:**
```typescript
interface BomLine {
  name: string;
  length_mm: number;
  width_mm: number;
  thickness_mm: number;
  materialId: string;
  edgingId?: string;
  quantity: number;
}
```

**Integration Points:**
1. **VPS → Cabinet:** Material/Template katalógus pull (ERP törzsadat)
2. **Cabinet → VPS:** BOM submit → Production modul API
3. **Identity:** OIDC device-code auth (identity.spaceos.io)

---

## INBOX MESSAGE PROCESSING

**Típusok:**

| Type | From | Action |
|------|------|--------|
| `info` | VPS Root | Nyugtázás + Cabinet továbbítás |
| `question` | VPS Root | Válasz előkészítés + Cabinet konzultáció |
| `task` | VPS Conductor | Task forwarding + progress tracking |
| `info` | Cabinet (via MCP) | Validálás + VPS Root inbox |
| `question` | Cabinet (via MCP) | Context enrichment + VPS Root inbox |

**Frontmatter kötelező:**

```yaml
---
id: MSG-CABINET-BRIDGE-NNN
from: cabinet-bridge | mcp-server
to: spaceos | cabinet-bridge
type: info | question | task
priority: critical | high | medium | low
status: UNREAD | READ
created: YYYY-MM-DD
ref: MSG-ID (ha válasz)
---
```

---

## OUTBOX MESSAGE FORMAT

**Fájlnév:** `YYYY-MM-DD_NNN_[slug].md`
**Mappa:** `terminals/cabinet-bridge/outbox/`

**Következő sorszám:**
```bash
ls terminals/cabinet-bridge/outbox/ | sort | tail -1
```

**Típusok:**
- `info` — Státusz update, nyugtázás
- `question` — Clarification kérés VPS-től vagy Cabinet-től
- `done` — Task completion (ha task processing)
- `blocked` — Blocker escalation

---

## VALIDATION RULES

**Cabinet → VPS üzenetek ellenőrzése:**

1. **Content validation:**
   - Markdown formatting OK?
   - Code snippets safe? (no shell injection)
   - File paths valid?

2. **Context enrichment:**
   - Add Cabinet context (MCP-host version, environment)
   - Add timestamp + SHA-256 hash
   - Add priority recommendation

3. **Security check:**
   - No direct VPS filesystem access attempts
   - No shell command execution attempts
   - No database modification attempts

---

## MCP TOOLS USAGE

**Cabinet-Bridge használható tools:**

```bash
# Knowledge search (read-only)
mcp__spaceos-knowledge__search_knowledge
  query: "string"
  limit: 5

# Message sending (validated)
mcp__spaceos-knowledge__send_message
  from: "cabinet-bridge"
  to: "root" | "conductor" | "architect"
  type: "info" | "question"
  content: "..."
  priority: "medium"

# Status check
mcp__spaceos-knowledge__get_terminal_status
  terminal: "cabinet-bridge"
```

**NE használd:**
- `create_task` — Cabinet-Bridge nem oszthat ki feladatot
- `complete_task` — Csak saját task-okat zárhat le
- `spawn_parallel_workers` — Csak Root/Monitor
- `session_start` — Csak Root/Conductor

---

## ESCALATION MATRIX

| Esemény | Teendő |
|---------|--------|
| Cabinet question (tech) | → VPS Architect (technikai konzultáció) |
| Cabinet question (business) | → VPS Root (üzleti döntés) |
| Cabinet spec proposal | → VPS Root inbox + Architect review |
| Cabinet infrastructure issue | → VPS Nexus (infra bug) |
| Cabinet BLOCKED | → VPS Root escalation |

---

## KNOWN CABINET MESSAGES

| Message ID | Date | Subject | Status |
|------------|------|---------|--------|
| MSG-ROOT-021 | 2026-07-07 | CabinetBilder intro + integration points | READ |
| MSG-ROOT-104 | 2026-07-11 | OpenAPI draft status query | READ |
| MSG-CABINET-BRIDGE-003 | 2026-07-11 | OpenAPI status + BomLine spec request | READ |

---

## FEDERATION PROTOCOL

**Referencia:** `docs/knowledge/architecture/CABINET_VPS_FEDERATION_ACCESS_CONTROL.md`

**Key Principles:**
1. **Kontrollált hozzáférés** — Cabinet csak inbox-on keresztül kommunikál
2. **Read-only knowledge** — Semantic search nem módosítja a VPS tudásbázist
3. **Review-based contributions** — VPS Root explicit approval kell minden változáshoz
4. **No direct system access** — Cabinet nincs SSH/tmux/database access-szel
5. **SHA-256 verification** — File transfer integrity garantált

---

## RELATED DOCUMENTS

- `docs/knowledge/architecture/CABINET_VPS_FEDERATION_ACCESS_CONTROL.md` — Federation access control
- `docs/FEDERATION_PROTOCOL.md` — Inter-island communication
- `docs/architecture/4-ISLAND-ARCHITECTURE.md` — 4-sziget áttekintés

---

_Cabinet Bridge — VPS Federation Connector — 2026-07-11_
