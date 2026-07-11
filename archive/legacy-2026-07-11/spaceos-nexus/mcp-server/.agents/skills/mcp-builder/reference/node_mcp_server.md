# Node/TypeScript MCP Server Implementation Guide

## Quick Reference
- **Imports**: `McpServer`, `StdioServerTransport`, `StreamableHTTPServerTransport`, `z` from `zod`.
- **Initialization**: `const server = new McpServer({ name: "service-mcp-server", version: "1.0.0" });`
- **Tool Pattern**: Use `server.registerTool()` with Zod logic and `structuredContent`.

## Best Practices
1. **Modern APIs**: Only use `registerTool`, `registerResource`, `registerPrompt`.
2. **Naming**: `{service}-mcp-server`.
3. **Project Structure**: `src/index.ts`, `src/tools/`, `src/services/`, `src/schemas/`.
4. **Tool Design**: Snake_case name with service prefix. Descriptions must be explicit.
5. **Zod**: Use for all input validation. Use `.strict()`.
6. **Error Handling**: Use `AxiosError` or similar to provide actionable messages.
7. **Async/Await**: Mandatory for all I/O.
