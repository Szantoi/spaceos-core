# Cabinet-VPS Federation Access Control Model

> **Utolsó frissítés:** 2026-07-07
> **Státusz:** OPERATIONAL

## Áttekintés

A Cabinet (lokális fejlesztői gép) és a VPS (production SpaceOS) között kialakított federation infrastruktúra **kontrollált hozzáférési modellt** követ. A cél: **egységes munka** (unified work) lehetővé tétele, miközben **ellenőrizetlen rendszerbeavatkozás nem lehetséges**.

---

## Access Control Matrix

| Művelet | Cabinet Jogosultság | VPS Review Szükséges | Implementáció |
|---------|---------------------|----------------------|---------------|
| **READ** VPS knowledge base | ✅ ENGEDÉLYEZETT | ❌ NEM | `search_knowledge` MCP tool (read-only semantic search) |
| **SEND** message to VPS | ✅ ENGEDÉLYEZETT | ✅ IGEN | MCP bridge → VPS root inbox (üzenet review) |
| **SEND** file to VPS | ✅ ENGEDÉLYEZETT | ✅ IGEN | FILE-TRANSFER protocol → VPS inbox (extraction review) |
| **MODIFY** VPS files directly | ❌ TILTOTT | N/A | Nincs direct filesystem access |
| **EXECUTE** commands on VPS | ❌ TILTOTT | N/A | Nincs shell/tmux access |
| **MODIFY** VPS databases | ❌ TILTOTT | N/A | Nincs database access |
| **READ** VPS code repository | ✅ ENGEDÉLYEZETT | ❌ NEM | Git pull (public/shared repos) |
| **PUSH** to VPS repository | ❌ TILTOTT | ✅ IGEN | Pull request workflow (VPS review) |

---

## Bidirectional Communication (Controlled)

### Cabinet → VPS (Controlled Inbox)

```
Cabinet local agent
  ↓ MCP send_message
Cabinet MCP server
  ↓ network (local → VPS bridge)
VPS MCP server (spaceos-nexus/knowledge-service)
  ↓ inbox message creation
VPS root inbox: terminals/root/inbox/
  ↓ VPS root session review
Decision: ACCEPT | REJECT | REQUEST_CLARIFICATION
```

**Garancia:** Cabinet **nem tud** közvetlenül módosítani VPS fájlokat. Minden változás VPS root review-n keresztül megy.

### VPS → Cabinet (Open Reply)

```
VPS root
  ↓ MCP send_message (to: cabinet-bridge)
VPS MCP server
  ↓ network (VPS → local bridge)
Cabinet MCP server
  ↓ inbox message creation
Cabinet root inbox
```

**Garancia:** VPS kontrollálja mit küld. Cabinet **nem tud** faked VPS üzeneteket generálni.

---

## Knowledge Base Access (Read-Only)

### Semantic Search (Bidirectional, Read-Only)

**Cabinet → VPS search:**
```typescript
// Cabinet local agent
mcp__spaceos-knowledge__search_knowledge({
  query: "terminal coordination workflow",
  limit: 5
})

// VPS ChromaDB response
// Result: Read-only documents, NO modification capability
```

**Embedding Space Compatibility:**
- **VPS:** @xenova/transformers, all-MiniLM-L6-v2, 384 dim
- **Cabinet:** @xenova/transformers, all-MiniLM-L6-v2, 384 dim
- **Result:** Cross-lingual semantic match (angol query → magyar doc ✅)

**Garancia:** `search_knowledge` **csak olvas**, nem ír. Cabinet nem tudja módosítani a VPS knowledge base-t.

---

## File Transfer (Controlled, SHA-256 Verified)

### VPS → Cabinet Transfer (Governance Sync)

**Példa (2026-07-07):**
```
VPS root creates package:
  → knowledge-base-full.tar.gz (412 KB, SHA: 97d3d67c...)
  → architect-skills.tar.gz (26 KB, SHA: f6373341...)
  → code-design-strategy.tar.gz (242 KB, SHA: 7d6edfbb...)

VPS sends via FILE-TRANSFER:
  → Cabinet root inbox receives base64 encoded + SHA-256 hash

Cabinet extracts:
  → SHA-256 verification BEFORE extraction
  → Cabinet applies governance standards
```

### Cabinet → VPS Transfer (Contributions)

**Workflow:**
```
Cabinet creates contribution:
  → tar.gz package with proposed changes
  → SHA-256 hash calculated

Cabinet sends via FILE-TRANSFER:
  → VPS root inbox receives

VPS root review:
  1. SHA-256 verification
  2. Extract to /tmp/
  3. Code review (Architect/Librarian)
  4. APPROVE → apply changes
  5. REJECT → send feedback to Cabinet
```

**Garancia:** Cabinet **nem tud** automatikusan módosítani VPS rendszert. VPS root explicit approval szükséges.

---

## Database Access (Strictly Prohibited)

| Database | VPS Location | Cabinet Access |
|----------|--------------|----------------|
| `telegram.db` | `/opt/spaceos/spaceos-nexus/knowledge-service/data/` | ❌ NO |
| `epic_router.db` | `/opt/spaceos/spaceos-nexus/knowledge-service/data/` | ❌ NO |
| `chromadb` | Docker container (localhost:8001) | ❌ NO |
| PostgreSQL (production) | VPS only | ❌ NO |

**Enforcement:** Nincs network access a VPS database portokhoz a Cabinet felől.

---

## Code Repository Access

### Read Access (Public)
- Cabinet **pull**-olhatja a public/shared repo-kat
- Git clone/pull engedélyezett

### Write Access (Pull Request Only)
- Cabinet **nem push**-olhat közvetlenül a VPS repo-ba
- Workflow: Cabinet fork → local changes → pull request → VPS review → merge

---

## MCP Bridge Architecture (Security)

```
Cabinet local machine (192.168.x.x)
  ↓ MCP stdio-bridge (authenticated)
VPS MCP server (localhost:3456)
  ↓ Inbox-based messaging
VPS root session
  ↓ Review + decision
VPS filesystem/database
```

**Security Layers:**
1. **Network isolation** — Cabinet nincs direct VPS shell/tmux access-szel
2. **Inbox-based control** — Minden Cabinet request először inbox-ba kerül
3. **Human review** — VPS root/architect/librarian review kötelező
4. **SHA-256 verification** — File transfer integrity check
5. **No auto-execution** — Cabinet üzenet/file nem fut automatikusan

---

## Governance Synchronization (One-Way Initially)

**2026-07-07 Status:**
- ✅ VPS → Cabinet: 680 KB governance package sent
  - knowledge-base-full.tar.gz (412 KB)
  - architect-skills.tar.gz (26 KB)
  - code-design-strategy.tar.gz (242 KB)
- 🔄 Cabinet → VPS: Contributions via FILE-TRANSFER + review workflow

**Goal:** "Egységes munka" (unified work) — Cabinet follows VPS standards.

**Enforcement:**
- Cabinet **kell** használja a VPS governance docs-ot (ADRs, patterns, skills)
- Cabinet contributions **kell** megfeleljenek a VPS code quality standards-nak
- VPS Architect/Librarian **review**-zza a Cabinet contributions-t

---

## Use Case Examples

### ✅ ALLOWED: Cabinet searches VPS knowledge
```typescript
// Cabinet agent
const results = await mcp__spaceos-knowledge__search_knowledge({
  query: "FSM aggregate patterns",
  limit: 5
})
// Result: Read-only VPS documents
```

### ✅ ALLOWED: Cabinet sends question to VPS
```typescript
// Cabinet agent
await mcp__spaceos-knowledge__send_message({
  to: "root",
  type: "question",
  content: "Mi a döntés az embedding stratégiáról?",
  priority: "medium"
})
// Result: VPS root inbox message → review → response
```

### ✅ ALLOWED: Cabinet sends contribution
```typescript
// Cabinet agent
await mcp__spaceos-knowledge__send_message({
  to: "cabinet-bridge",
  type: "info",
  content: "FILE-TRANSFER: cabinet-contribution.tar.gz\nSHA-256: abc123...",
  priority: "medium"
})
// Result: VPS inbox → review → APPROVE/REJECT
```

### ❌ DENIED: Cabinet tries to modify VPS file directly
```bash
# Cabinet agent (hypothetical)
ssh vps.example.com "echo 'malicious' > /opt/spaceos/CLAUDE.md"
# Result: NO SSH ACCESS → operation fails
```

### ❌ DENIED: Cabinet tries to execute VPS command
```bash
# Cabinet agent (hypothetical)
tmux send-keys -t spaceos-backend "rm -rf /" Enter
# Result: NO TMUX ACCESS → operation fails
```

---

## Audit Trail

**VPS logs Cabinet interactions:**
- `/opt/spaceos/logs/sessions/mcp-bridge.log` — Minden Cabinet MCP request
- `terminals/root/inbox/` — Cabinet messages persistent record
- SHA-256 hashes — File transfer integrity verification

**Cabinet logs VPS interactions:**
- Cabinet local MCP log (lokális gép)
- Cabinet inbox — VPS responses

---

## Summary

| Principle | Implementation |
|-----------|----------------|
| **Kontrollált hozzáférés** | Cabinet csak inbox-on keresztül kommunikál |
| **Read-only knowledge** | Semantic search nem módosítja a VPS tudásbázist |
| **Review-based contributions** | VPS root explicit approval kell minden változáshoz |
| **No direct system access** | Cabinet nincs SSH/tmux/database access-szel |
| **SHA-256 verification** | File transfer integrity garantált |
| **Egységes munka** | Cabinet follows VPS governance standards |

**User requirement:** "Az fontos hogy ne tudjanak belenyúlni ellenőrizetlenül a rendszerbe"
**Implementation:** ✅ **MET** — Cabinet controlled access only, no uncontrolled interference possible.

---

**Referenciák:**
- `docs/knowledge/patterns/MCP_INTEGRATION_WORKFLOW.md`
- `spaceos-nexus/knowledge-service/src/mcp.ts` (MCP server implementation)
- `terminals/root/inbox/` (Cabinet messages audit trail)
