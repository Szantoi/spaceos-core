# JoineryTech.McpServer

The **single source of truth** MCP (Model Context Protocol) server for JoineryTech AI agents.

## What is this?

An Express/TypeScript server implementing the Model Context Protocol. It controls what tools AI agents can access, serves role definitions, validates agent behaviour, and tracks workflow state.

## Quick start

```bash
# Install dependencies
npm install

# Start ChromaDB (for RAG)
npm run create-db    # first time
npm run start-db     # subsequent times

# Index the knowledge base
npm run index-kb

# Development server
npm run dev

# Run tests
npm test
npm run test:e2e:rbac
npm run test:e2e:guardrail
```

## Environment setup

Copy `.env.example` to `.env` and fill in the required values (Google API key for Guardrail LLM evaluation).

## Structure

```
src/
  mcp/          <- MCP router, RBAC filter, document server, Express server
  roles/        <- Guardrail service, role loader, compliance evaluator
  metadata/     <- WorkflowStateTracker (FSM/SQLite), ResourceTracker
  rag/          <- VectorStore, knowledge base indexer (ChromaDB)
  tests/        <- Unit, E2E, evaluator tests
database/       <- LIVE SSOT: role definitions, schemas, standards, knowledge
meta-security/  <- Core system protection scripts
```

## Architecture

```
AI Agent (Copilot/Claude)
    │ HTTP / MCP protocol
    ▼
RbacFilter ──► only allowed tools visible
    │
    ▼
McpRouter ──► DocumentServer (reads database/)
    │
    ├── GuardrailService (post-hoc LLM compliance check)
    ├── WorkflowStateTracker (FSM state in SQLite)
    └── ResourceTracker (artifact registry in SQLite)
```

## Related projects

- **JoineryTech.AgentScripts** — PowerShell workflow runner (calls this server)
- **JoineryTech.Flow** — The .NET API + React application
