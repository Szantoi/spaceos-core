# ADR-009: MCP RBAC Scope & Session Role Switching Limitations

## Status
Accepted

## Context
We introduced Role-Based Access Control (RBAC) to the MCP server. Tools are now filtered based on the `x-active-role` header to prevent agents from accessing tools outside their domain or responsibilities (e.g., `explorer` shouldn't mutate `workflow_state`).

However, E2E testing (`mcp-session-roleswitch.test.ts`) revealed that the MCP server and clients have an architectural limitation regarding mid-session role switching:
1. When an MCP HTTP connection is established, an `McpServer` connection transport is created and bound to the session ID.
2. The permitted tool list is evaluated and registered to that `McpServer` instance *during initialization* based on the initial `x-active-role`.
3. If a client sends a `tools/list` request using the same `mcp-session-id` but a different `x-active-role` header later in the session, the tools are NOT re-evaluated. The server retrieves the existing session which still holds the original tool registry.
4. Client-side caching (e.g., Cursor, Claude) exacerbates this; clients typically pull the tool list only once upon initialization and rarely ask for it again.

## Decision
We accept this limitation as a boundary of the current architecture. The MCP server will **not** attempt to dynamically reload or mutate tool registries mid-session for an existing client ID.

1. An MCP session's tool surface is strictly bound to the `x-active-role` provided during the initial handshake.
2. If an agent (or human) switches roles and requires a different toolset, they **must initialize a new MCP session**.

## Consequences
* **Positive:** Simpler and more performant implementation in `mcpServer.ts` because tool registries don't need real-time state synchronization, teardown, or complex invalidation logic.
* **Negative:** Developers and agents must be aware that cross-role context switching within a single, persistent IDE session won't update the tool surface automatically. The MCP client server must be restarted, or a new session initiated to pick up new permissions.
