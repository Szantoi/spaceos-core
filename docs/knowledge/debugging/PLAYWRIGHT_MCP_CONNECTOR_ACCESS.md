# Playwright MCP Connector Access — Debugging Guide

**Date:** 2026-07-10
**Issue:** Designer terminal cannot access Playwright MCP tools
**Status:** RESOLVED
**Task:** MSG-NEXUS-020

---

## Problem Summary

Designer terminal reported:
```
Error: No such tool available: mcp__spaceos-connector__playwright_navigate
```

**Expected tools:**
- `mcp__spaceos-connector__playwright_navigate`
- `mcp__spaceos-connector__playwright_screenshot`
- `mcp__spaceos-connector__playwright_click`
- `mcp__spaceos-connector__playwright_fill`

**Workaround used:** Playwright CLI directly (`npx playwright screenshot`)

---

## Root Cause

The MCP connector is configured with `MCP_TERMINAL: "root"` in `~/.claude/settings.json`:

```json
{
  "mcpServers": {
    "spaceos-connector": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/mcp-connector/bin/stdio-bridge.js"],
      "env": {
        "MCP_CONNECTOR_HOST": "localhost",
        "MCP_CONNECTOR_PORT": "3457",
        "MCP_TERMINAL": "root"  // <-- Only root terminal has access
      }
    }
  }
}
```

**This means:**
- ✅ Root terminal sessions can access spaceos-connector tools
- ❌ Other terminals (designer, backend, frontend, etc.) **cannot** access spaceos-connector tools
- ❌ Designer session tried to use `mcp__spaceos-connector__playwright_navigate` but the MCP server was not available in that session

---

## MCP Connector Status

### Backend Status (Port 3457)

The MCP connector **is running** and **is healthy**:

```bash
$ lsof -i :3457
COMMAND     PID  USER FD   TYPE   DEVICE SIZE/OFF NODE NAME
node    1624422 gabor 24u  IPv6 12933705      0t0  TCP *:3457 (LISTEN)

$ curl -s http://localhost:3457/health
{
  "status": "healthy",
  "version": "1.0",
  "backends": {
    "knowledge": true,
    "playwright": true,
    "ref": true,
    "context7": true,
    "brave-search": true
  },
  "uptime": 2465.255081424
}
```

### Available Playwright Tools

```bash
$ curl -s http://localhost:3457/tools | grep playwright
"playwright_navigate"
"playwright_screenshot"
"playwright_click"
"playwright_fill"
"playwright_select"
"playwright_hover"
"playwright_evaluate"
"playwright_console_logs"
"playwright_close"
"playwright_resize"
```

**All 10 Playwright tools are available via the connector.**

### Permissions Configuration

From `spaceos-nexus/mcp-connector/config.yaml`:

```yaml
permissions:
  designer:
    backends: [knowledge, ref, playwright]
    tools:
      - search_knowledge
      - read_memory
      - register_working
      - register_idle
      - ref_search_documentation
      - playwright_navigate       # ✅ Allowed
      - playwright_screenshot     # ✅ Allowed
```

**Designer has permission to use Playwright tools** via the connector.

---

## Solution Options

### Option 1: Use Playwright Plugin Directly

**Enable the official Playwright plugin** in `~/.claude/settings.json`:

```json
{
  "enabledPlugins": {
    "telegram@claude-plugins-official": true,
    "playwright@claude-plugins-official": true  // <-- Add this
  }
}
```

**Tool names will be:**
- `mcp__playwright__playwright_navigate`
- `mcp__playwright__playwright_screenshot`
- etc.

**Pros:**
- ✅ Available in all terminal sessions
- ✅ No connector dependency
- ✅ Official Microsoft package

**Cons:**
- ❌ No centralized permissions (designer would have full Playwright access)
- ❌ No audit logging via connector
- ❌ Separate process per terminal (higher memory usage)

### Option 2: Configure Connector Per Terminal

**Modify settings.json** to use terminal-specific connector config:

```json
{
  "mcpServers": {
    "spaceos-connector": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/mcp-connector/bin/stdio-bridge.js"],
      "env": {
        "MCP_CONNECTOR_HOST": "localhost",
        "MCP_CONNECTOR_PORT": "3457",
        "MCP_TERMINAL": "${CLAUDE_TERMINAL}"  // <-- Dynamic terminal name
      }
    }
  }
}
```

**Requires:** Claude Code support for environment variable substitution in MCP config.

**Status:** Not currently supported (as of Claude Code 2026-07).

### Option 3: Root Session Only (Current State)

**Keep current configuration** — Playwright tools only available in root terminal.

**Workflow:**
1. Designer requests screenshots → creates inbox for root
2. Root terminal uses Playwright MCP tools
3. Root delivers screenshots back to designer

**Pros:**
- ✅ Centralized control
- ✅ Audit trail via connector
- ✅ Permission enforcement

**Cons:**
- ❌ Manual coordination required
- ❌ Slower turnaround time

### Option 4: CLI Workaround (Current Workaround)

**Use Playwright CLI** directly in any terminal:

```bash
npx playwright screenshot --browser chromium --full-page URL OUTPUT.png
```

**Pros:**
- ✅ Works in all terminals
- ✅ No configuration changes needed

**Cons:**
- ❌ No programmatic control (click, fill, etc.)
- ❌ Screenshot-only
- ❌ No integration with test flows

---

## Recommended Solution

**Short-term (Q3 2026):** Continue using **Option 4 (CLI workaround)** for designer screenshot tasks.

**Long-term (Q4 2026):** Implement **Option 1 (Playwright plugin)** once terminal-specific MCP permissions are available in the connector architecture (ADR-061).

**Reasoning:**
- Designer primarily needs screenshots for UI/UX verification
- Full browser automation (click, fill) is rarely needed for design tasks
- Backend/Frontend terminals handle E2E testing with full Playwright access

---

## Tool Name Convention

When using MCP tools via the spaceos-connector:

| Backend | Tool Name in Connector | Tool Name in Claude Code |
|---------|------------------------|--------------------------|
| knowledge | `search_knowledge` | `mcp__spaceos-knowledge__search_knowledge` |
| playwright | `playwright_navigate` | `mcp__spaceos-connector__playwright_navigate` |
| ref | `ref_search_documentation` | `mcp__spaceos-connector__ref_search_documentation` |
| context7 | `resolve-library-id` | `mcp__spaceos-connector__resolve-library-id` |
| brave-search | `brave_web_search` | `mcp__spaceos-connector__brave_web_search` |

**Note:** The spaceos-knowledge tools have their own MCP server prefix (`mcp__spaceos-knowledge__`) because they are exposed directly via the knowledge-service HTTP endpoint, not via the connector.

---

## Documentation References

- **MCP Connector Config:** `/opt/spaceos/spaceos-nexus/mcp-connector/config.yaml`
- **MCP Connector Implementation Plan:** `/opt/spaceos/docs/projects/EPIC-MCP-CONNECTOR-IMPLEMENTATION-PLAN.md`
- **ADR-061:** MCP Connector Pattern
- **Playwright Official MCP:** `@playwright/mcp@latest` (npm package)

---

## Testing Verification

```bash
# 1. Check connector is running
curl -s http://localhost:3457/health

# 2. List available tools
curl -s http://localhost:3457/tools | grep playwright

# 3. Test Playwright backend health
curl -s http://localhost:3457/health | jq '.backends.playwright'

# 4. Check audit logs
tail -50 /opt/spaceos/logs/mcp-connector-audit.log
```

---

**Issue Status:** RESOLVED — Documented workaround + long-term solution path
**Next Action:** Continue using CLI workaround until terminal-specific MCP permissions available
