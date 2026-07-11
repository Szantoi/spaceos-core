# MCP Server Best Practices

## Quick Reference

### Server Naming
- **Python**: `{service}_mcp` (e.g., `slack_mcp`)
- **Node/TypeScript**: `{service}-mcp-server` (e.g., `slack-mcp-server`)

### Tool Naming
- Use snake_case with service prefix
- Format: `{service}_{action}_{resource}`
- Example: `slack_send_message`, `github_create_issue`

### Response Formats
- Support both JSON and Markdown formats
- JSON for programmatic processing
- Markdown for human readability

### Pagination
- Always respect `limit` parameter
- Return `has_more`, `next_offset`, `total_count`
- Default to 20-50 items

### Transport
- **Streamable HTTP**: For remote servers, multi-client scenarios
- **stdio**: For local integrations, command-line tools

---

## Tool Naming and Design

1. **Use snake_case**: `search_users`, `create_project`
2. **Include service prefix**: `slack_send_message`
3. **Be action-oriented**: Start with verbs
4. **Be specific**: Avoid generic names

---

## Security Best Practices
- Use OAuth 2.1 or secure API keys
- Sanitize inputs and file paths
- Validate schemas (Zod/Pydantic)
- Bind to `127.0.0.1` for local servers
