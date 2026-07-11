# Error Code Catalog

This document centralizes all error codes used in the JoineryTech MCP Server.

## MCP Tool Errors

| Code | Message | Description |
|:-----|:--------|:------------|
| `INVALID_DOMAIN` | Invalid domain format. | Domain does not match `/^[a-z-]+$/` (Max 64 chars) |
| `INVALID_ROLE` | Invalid role format. | Role does not match `/^[a-z_]+$/` (Max 64 chars) |
| `ROLE_NOT_FOUND` | Requested role not found in database. | The (domain, role) pair is missing from AgentDb. |
| `SESSION_NOT_FOUND` | Session with ID X not found. | Attempted to resume a non-existent session. |
| `PAYLOAD_TOO_LARGE` | Extended bootstrap payload exceeds 5MB limit. | Total response size exceeds safety limits. |
| `SESSION_CREATION_FAILED` | Failed to create or persist session record. | Critical database failure during session generation. |
| `UNKNOWN_ERROR` | An unexpected error occurred. | Generic fallback for unhandled exceptions. |

## HTTP Status Mappings

| Error Code | HTTP Status |
|:-----------|:------------|
| `INVALID_DOMAIN` | 400 Bad Request |
| `INVALID_ROLE` | 400 Bad Request |
| `ROLE_NOT_FOUND` | 404 Not Found |
| `SESSION_NOT_FOUND` | 404 Not Found |
| `PAYLOAD_TOO_LARGE` | 413 Payload Too Large |
| `SESSION_CREATION_FAILED` | 500 Internal Server Error |
| `UNKNOWN_ERROR` | 500 Internal Server Error |

---
*Generated as part of EPIC-10 Documentation (TASK-10-08)*
