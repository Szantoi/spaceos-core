# NEXUS Terminál

> Agent infrastruktúra — RAG Knowledge Service, MCP Protocol, SSE Notifications

## Gyors Info

| | |
|---|---|
| **Terminál** | nexus |
| **Port** | 3456 (local), 443 (HTTPS) |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/spaceos-nexus/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/nexus/` |
| **Memory** | `/opt/spaceos/docs/memory/nexus.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/nexus.md

# 2. Service státusz
curl https://nexus.joinerytech.hu/health

# 3. MCP teszt
curl -X POST https://nexus.joinerytech.hu/mcp \
  -H 'Authorization: Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o=' \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'

# 4. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/nexus/inbox/
```

## Build & Test Parancsok

```bash
# Install
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm install

# Dev futtatás
npx ts-node src/server.ts

# Systemd service
sudo systemctl status spaceos-knowledge
sudo systemctl restart spaceos-knowledge
sudo journalctl -u spaceos-knowledge -f
```

## MCP Tools (17 db)

**Knowledge:**
- `search_knowledge` — RAG keresés (1106 doc)

**Mailbox:**
- `list_inbox`, `send_message`, `submit_done`, `get_task_status`

**Identity:**
- `get_identity`, `list_terminals`, `read_memory`, `write_memory`, `append_memory`

**Skills & Workflow:**
- `list_skills`, `get_skill`, `get_workflow`, `get_terminal_setup`, `get_project_context`

**System:**
- `get_capabilities`, `get_service_status`

## MCP Konfiguráció (távoli terminálhoz)

```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "http",
      "url": "https://nexus.joinerytech.hu/mcp",
      "timeout": 60000,
      "headers": {
        "Authorization": "Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o="
      }
    }
  }
}
```

## Architektúra

```
spaceos-nexus/knowledge-service/
├── src/
│   ├── server.ts      ← Express + SSE + MCP mount
│   ├── mcp.ts         ← MCP JSON-RPC protocol handler
│   ├── mailbox.ts     ← Mailbox file operations
│   ├── vectorStore.ts ← ChromaDB client
│   ├── embeddings.ts  ← Voyage AI embeddings
│   ├── identity.ts    ← Terminal identity + memory
│   └── skills.ts      ← Skill + workflow + setup
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/spaceos-nexus/CLAUDE.md`
- ChromaDB: `localhost:8001`
