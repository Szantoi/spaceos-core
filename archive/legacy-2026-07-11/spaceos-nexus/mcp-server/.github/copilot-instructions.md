# GitHub Copilot Instructions — JoineryTech.McpServer

You are an expert Senior Architect and Software Engineer assisting with the **JoineryTech MCP Server**.

## What is this project?
This is the **Model Context Protocol (MCP) server** — the single source of truth for all JoineryTech AI agents.
It provides:
- **RBAC**: Role-based tool access control (agents only see tools allowed for their role)
- **Document serving**: Role definitions, schemas, workflows, templates
- **RAG**: Semantic search over the knowledge base (ChromaDB)
- **Guardrail**: LLM-based compliance validation of agent responses
- **Workflow state**: FSM-based state tracking per session (SQLite)

## Core Preferences
1. **Language:** Communicate in Hungarian for explanations, but use English for all code, comments, and commit messages.
2. **Stack:** TypeScript / Node.js / Express. Use strict typing, no `any` unless unavoidable.
3. **Architecture:** Clean separation — `mcp/` (routing/RBAC), `roles/` (guardrail), `metadata/` (state), `rag/` (search).

## Directory Structure
- `src/mcp/` — MCP router, RBAC filter, document server, main server
- `src/roles/` — Guardrail service, role loader, guardrail evaluator
- `src/metadata/` — WorkflowStateTracker (FSM/SQLite), ResourceTracker
- `src/rag/` — Vector store, knowledge base indexer (ChromaDB)
- `src/tests/` — Unit, E2E, evaluator tests
- `database/` — LIVE SSOT: role definitions, schemas, standards, knowledge
- `meta-security/` — Core system protection scripts

## Key principle
The `database/` folder is the **single source of truth**. Never hardcode role data in TypeScript — always read from `database/roles/<domain>/<role>/`.
