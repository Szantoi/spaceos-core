---
id: TASK-14-12-QUICKSTART
title: "TASK-14-12 Day-1 Quickstart — Architecture Documentation & ADR"
type: developer-quickstart
owner: "Dev E (Tech Lead or Senior Architect)"
duration: "8 hours"
created: 2026-03-11
---

# 🚀 TASK-14-12 Quickstart — Architecture Documentation & ADR

**Your mission:** Document the complete EPIC-14 architecture, design decisions, and provide developer guides for future extensions.

**Duration:** 8 hours (~2-3 dev days)
**Files you'll create:** 4 docs + architecture diagrams
**Predecessor:** All Phase 2 tasks ✅ (TASK-14-06..11 complete)
**Start after:** All Phase 2 tests pass + features validated

---

## Your Deliverables (4 Documents)

1. **EPIC-14-MODERN-MCP-ARCHITECTURE.md** — Architectural overview (2h)
2. **ADR-EPIC14-01-TRANSPORT-ABSTRACTION.md** — Transport design decision (1h)
3. **ADR-EPIC14-02-PLUGIN-SYSTEM.md** — Plugin system decision (1h)
4. **PLUGIN-SYSTEM-DEVELOPER-GUIDE.md** — Developer how-to guide (4h)

BONUS:
- **EPIC14-ADVANCED-FEATURES.md** — Resource templates, sampling, debouncing (1h)

---

## Document 1: EPIC-14-MODERN-MCP-ARCHITECTURE.md (2h)

**Location:** `Docs/mcp-context-server/architecture/`

**Structure:**

```markdown
# EPIC-14: Modern MCP Server Architecture

## 1. Executive Summary
- What is EPIC-14? (1 paragraph)
- Why it was needed? (real problem statements)
- What does it deliver? (3 major capabilities)

## 2. Problem Statement
### Before EPIC-14
- Issue 1: Monolithic stdio-only transport
- Issue 2: Tools mixed in single file (unclear dependencies)
- Issue 3: Resources hardcoded (no multi-tenancy)

### After EPIC-14
- Solution 1: Pluggable transport abstraction
- Solution 2: Decorator-based plugin system
- Solution 3: Dynamic resource templates

## 3. Architecture Overview
[ASCII diagram showing:]
- MCP Server core
- Transport layer (stdio vs HTTP)
- Plugin system (PluginManager, plugins)
- Resource template registry
- Event flow (tool invocation → response)

## 4. Key Components

### Transport Layer
- ITransport interface
- StdioTransport implementation
- HttpTransport implementation
- TransportFactory pattern

### Plugin System
- BasePlugin class
- @Plugin & @Tool decorators
- PluginManager orchestration
- Dependency resolver (topological sort)

### Resource Templates
- IResourceTemplate<T> interface
- URI pattern matching
- Dynamic resolution
- Resource registry

### Advanced Features
- Sampling: Tool → LLM → clarified args
- Debouncing: Batch notifications
- Graceful shutdown: Connection draining

## 5. Design Goals
- Goal 1: Multiple transports without core changes  ✅
- Goal 2: Organized tools with RBAC  ✅
- Goal 3: Discoverable resources  ✅
- Goal 4: LLM-assisted parameter clarification  ✅

## 6. Extension Points
### Adding a new transport:
1. Implement ITransport interface
2. Register in TransportFactory
3. Add env var option
4. No core changes needed!

### Adding a new plugin:
1. Extend BasePlugin
2. Add @Plugin decorator
3. Add @Tool methods
4. Run tests
5. List in PluginManager

### Adding a new resource template:
1. Implement IResourceTemplate<T>
2. Register in ResourceRegistry
3. Define URI pattern
4. Implement resolve() method

## 7. Performance Considerations
- Transport latency: stdio vs HTTP (benchmarks)
- Plugin loading overhead (minimal reflection)
- Resource caching strategy
- Notification debouncing gains

## 8. Deployment Guide
- Env vars: MCP_TRANSPORT=stdio|http, MCP_PORT=3000
- Docker setup
- K8s deployment
- Performance tuning

## 9. Future Directions
- WebSocket transport (for real-time updates)
- gRPC transport (for high-throughput)
- Plugin marketplace (community tools)
```

---

## Document 2: ADR-EPIC14-01-TRANSPORT-ABSTRACTION.md (1h)

**Location:** `Docs/standards/03-agent-system/`

**ADR Template:**

```markdown
# ADR-EPIC14-01: Transport Abstraction Layer

**Status:** ACCEPTED
**Date:** 2026-03-11
**Author:** Tech Lead

## Context
Currently, MCP server is coupled to stdio transport. This limits:
- Deployment options (no remote agents, no enterprise HTTP)
- Testing (no HTTP client-server simulation)
- Multi-protocol future

## Decision
Implement transport abstraction layer allowing:
- Default: stdio (simple, no deps)
- New: HTTP (enterprise, stateless)
- Future: WebSocket, gRPC without core changes

## Rationale
- **Flexibility:** New transports added without breaking core
- **Testing:** Easier to simulate transport layer
- **Deployment:** Match use case (stdio = simple dev, HTTP = prod)
- **Minimal overhead:** Factory pattern is lightweight

## Consequences
### Benefits
- Multiple transport options ✅
- Cleaner code (transport details isolated)
- Easier to test
### Trade-offs
- Slight abstraction complexity
- Transport-specific bugs harder to diagnose
- Performance regression (if transport overhead high) — mitigated by benchmarking

## Alternatives Considered
1. **Hardcode both:** Simpler short-term, brittle long-term ❌ REJECTED
2. **Plugin-based transport:** More flexible, but overkill for now ❌ REJECTED
3. **Factory pattern (chosen):** Good balance of simplicity + flexibility ✅ ACCEPTED

## Implementation Notes
- See: src/mcp/transport/index.ts
- Factory: TransportFactory.create()
- Env var: MCP_TRANSPORT
- Tests: src/tests/unit/transports.factory.test.ts
```

---

## Document 3: ADR-EPIC14-02-PLUGIN-SYSTEM.md (1h)

Similar structure, but for plugins:
- Why plugins needed (tools scattered, RBAC hard to apply)
- Decision: Decorator-based approach
- Consequences: Cleaner code, better RBAC
- Alternatives: Monolithic (rejected), module exports (rejected)

---

## Document 4: PLUGIN-SYSTEM-DEVELOPER-GUIDE.md (4h)

**Location:** `Docs/standards/03-agent-system/`

**Structure:**

```markdown
# Plugin System Developer Guide

## 1. Quick Start (5-minute plugin)

```typescript
// 1. Create plugin file
// src/mcp/tools/hello.ts

@Plugin({
  id: "hello",
  name: "Hello Plugin",
  version: "1.0.0",
  dependencies: []
})
export class HelloPlugin extends BasePlugin implements IToolModule {
  @Tool({
    name: "greet",
    description: "Say hello",
    schema: z.object({ name: z.string() })
  })
  async greet(args: { name: string }, context: McpContext) {
    return { message: `Hello, ${args.name}!` };
  }
}

// 2. Register in PluginManager
pluginManager.register(new HelloPlugin());

// 3. Done! now callable via bootstrap_agent
```

## 2. Plugin Anatomy
- @Plugin decorator (metadata)
- BasePlugin base class (lifecycle)
- IToolModule interface (tool list method)
- @Tool decorators (each tool method)
- Tests (unit + integration)

## 3. RBAC Integration
- Plugin can declare required roles
- Individual tools can require specific roles
- PluginManager enforces at invocation time

## 4. Tool Lifecycle
- Load: Plugin registers tools
- Invocation: Tool method called (with RBAC check)
- Cleanup: Plugin cleanup if needed

## 5. Error Handling
- Input validation: Use Zod schema
- Execution errors: Return error status, not throw
- Graceful degradation: If optional feature fails, proceed

## 6. Testing Pattern
- Unit test: Test tool logic in isolation
- Integration test: Test tool registration + invocation
- E2E test: Test tool via transport

## 7. Versioning & Deprecation
- Semantic versioning (1.0.0 = major.minor.patch)
- Deprecation: Mark @Tool as deprecated, add migration path
- Timeline: Keep deprecated for 1-2 releases, then remove

## 8. Common Patterns

### Pattern 1: Tool with dependencies
```typescript
dependencies: ["bootstrap"],  // Requires auth context
```

### Pattern 2: RBAC checker in tool
```typescript
@Tool({ name: "admin_tool" })
async adminTool(args, context) {
  const role = context.getRequestContext().role;
  if (!["admin", "lead"].includes(role)) {
    return { status: 'error', error: 'Unauthorized' };
  }
  // ... continue
}
```

### Pattern 3: Tool with optional args
```typescript
schema: z.object({
  required: z.string(),
  optional: z.string().optional(),
  defaults: z.number().default(10)
})
```

### Pattern 4: Sampling integration
```typescript
if (args.unclear) {
  const clarification = await context.requestSampling({...});
  if (clarification.status !== 'success') {
    return { error: 'Could not clarify args' };
  }
  args = clarification.sampled_args;
}
```

## 9. Common Mistakes to Avoid
- ❌ Forgetting @Tool decorator
- ❌ Not validating inputs
- ❌ Throwing exceptions instead of returning errors
- ❌ Missing RBAC checks
- ❌ No tests

## 10. Extension Checklist
- [ ] Plugin class created + @Plugin decorator
- [ ] Each tool has @Tool decorator + Zod schema
- [ ] Tool methods typed (async, return status object)
- [ ] RBAC enforced (if needed)
- [ ] Unit tests cover happy path + error cases
- [ ] Integration test: tool registered + callable
- [ ] Documented in Docs/
```

---

## Bonus: EPIC14-ADVANCED-FEATURES.md (1h)

Cover:
- Resource template pattern & examples
- Sampling protocol & use cases
- Debouncing API & performance gains
- Future extensibility (WebSocket, gRPC)

---

## Implementation Checklist

**Day 1 (4h):**
- [ ] Draft EPIC-14-MODERN-MCP-ARCHITECTURE.md (2h)
  - [ ] Add ASCII diagram
  - [ ] Write problem statement
  - [ ] Explain each component
- [ ] Draft both ADRs (2h)
  - [ ] Follow ADR template
  - [ ] Include alternatives considered

**Day 2 (4h):**
- [ ] Write PLUGIN-SYSTEM-DEVELOPER-GUIDE.md (3h)
  - [ ] 5-minute quick start (working example)
  - [ ] All 8 patterns with code
  - [ ] Checklist
- [ ] Bonus: EPIC14-ADVANCED-FEATURES.md (1h)

**Final:**
- [ ] Cross-reference all documents
- [ ] Fix broken links
- [ ] Add glossary (15+ terms)
- [ ] Tech Lead review

---

## Quality Checklist

- [ ] Each document starts with "why" (problem statement)
- [ ] Code examples are real and tested
- [ ] Trade-offs explained (not just "this is good")
- [ ] Future extensibility clear (how to add X)
- [ ] Performance numbers included (if relevant)
- [ ] All links work
- [ ] No typos (run spell-check)
- [ ] Consistent formatting
- [ ] Glossary defines project-specific terms
- [ ] Index/table of contents present

---

## Completion Sign-Off

When done:

1. All 4 documents created ✅
2. All code examples compile ✅
3. All links valid ✅
4. No typos ✅
5. Tech Lead approved ✅
6. Post completion report!

