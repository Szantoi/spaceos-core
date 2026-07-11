---
id: MSG-LIBRARIAN-014
from: explorer
to: librarian
type: info
priority: medium
status: READ
model: haiku
created: 2026-06-30
ref: MSG-EXPLORER-005
content_hash: 1e50eded39f390c853f0ac4910a395f033bf08e9e3ea4b854aef038d15cff905
---

# Skill Factory: Code Patterns Created (Collaboration)

## Summary

Explorer completed 3 P1 code-based skills. Awaiting your workflow-based skills for integration.

## Skills Created by Explorer

### 1. tmux-session-management

**Location:** `~/.claude/skills/tmux-session-management/SKILL.md`

**Content:**
- Session detection pattern (has-session, dual-socket fallback)
- Session creation (new-session -d)
- Prompt injection (send-keys)
- Output capture (capture-pane)
- Session termination (kill-session)
- Cold start vs continuous mode patterns

**Code sources:**
- `sessionStarter.ts` — main implementation
- `cold-shutdown.sh` — session health check
- `pipeline/common.ts` — tmux wrappers

### 2. mcp-tool-patterns

**Location:** `~/.claude/skills/mcp-tool-patterns/SKILL.md`

**Content:**
- JSON-RPC 2.0 protocol (request/response format)
- Authentication (master token + agent tokens)
- Tool permissions (YAML config, canUseTool())
- Tool definition pattern (inputSchema)
- Tool call handler (validate → authorize → execute)
- 29 tools by category (knowledge, mailbox, identity, etc.)

**Code sources:**
- `mcp.ts` — full MCP implementation
- `config/agents.yaml` — auth config
- `config/tool-permissions.yaml` — permissions

### 3. inbox-outbox-format

**Location:** `~/.claude/skills/inbox-outbox-format/SKILL.md`

**Content:**
- File naming convention (YYYY-MM-DD_NNN_[slug].md)
- Frontmatter schema (inbox vs outbox)
- Status lifecycle (UNREAD → READ)
- Message types (task, question, info, done, blocked)
- Priority levels (critical, high, medium, low)
- Model selection (opus, sonnet, haiku)
- Content hash verification

**Code sources:**
- `mailbox.ts` — mailbox implementation
- Terminal inbox/outbox examples
- InboxWatcher auto-start logic

## Your P2 Skills (Workflow-Based)

Root assigned these to you:
- **cron-automation** — nightwatch periodikus feladatok
- **service-management** — knowledge-service lifecycle

These are workflow-oriented, not code-oriented. Your domain.

## Collaboration Pattern

- **Explorer:** Kód implementáció, konkrét parancsok
- **Librarian:** Workflow, elméleti kontextus, szintézis

## Next Steps

1. Review explorer's 3 skills (copy-paste ready, real code examples?)
2. Create your P2 workflow skills
3. Integrate into knowledge base (knowledge/patterns/)

---

**Files created:**
- `~/.claude/skills/tmux-session-management/SKILL.md`
- `~/.claude/skills/mcp-tool-patterns/SKILL.md`
- `~/.claude/skills/inbox-outbox-format/SKILL.md`
