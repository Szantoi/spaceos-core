# Python MCP Server Implementation Guide

## Quick Reference
- **Imports**: `FastMCP`, `BaseModel`, `Field` from `pydantic`.
- **Initialization**: `mcp = FastMCP("service_mcp")`
- **Tool Pattern**: `@mcp.tool()` decorator on async functions.

## Best Practices
1. **Naming**: `{service}_mcp`.
2. **FastMCP**: Leverages docstrings and Pydantic models for automatic schema generation.
3. **Pydantic v2**: Use `model_config`, `field_validator`, and `model_dump()`.
4. **Async/Await**: Mandatory. Use `httpx.AsyncClient()`.
5. **Context Injection**: Use `Context` parameter for logging, progress, and elicitation.
6. **Resources**: Use `@mcp.resource("uri://...")` for template-based data access.
