---
id: TASK-14-08
title: "TASK-14-08: Resource Template Support"
epic: EPIC-14
phase: "Phase 2: Advanced Features"
type: task
created: 2026-03-11
status: "✅ COMPLETE"
effort: "10 hours (~3 days)"
owner: "TBD"
---

# TASK-14-08: Resource Template Support

## Overview

Implement **dynamic Resource URI patterns** in the MCP server to support role-based, workflow-based, and template-based **resource discovery**. This enables the MCP server to serve resources dynamically without hardcoding paths.

**Status:** ✅ COMPLETE — Resource templates implemented and covered by unit + integration tests
**Owner:** TBD (Architecture-focused developer)
**Duration:** 10 hours (~3 development days)
**Predecessor:** TASK-14-03 (Plugin System foundation)
**Successor:** TASK-14-11 (E2E tests can verify resource resolution)
**Blockers:** None — Can start in parallel with 14-07, 14-09, 14-10

---

## Problem Statement

Currently, MCP resources (role definitions, workflows, templates) are:

- **Statically registered** — Must be hardcoded or loaded at startup
- **File-path based** — Exposes filesystem structure; brittle to reorganization
- **Non-discoverable** — Clients don't know what resources exist without documentation

**Goal:** Create a `ResourceTemplate` pattern allowing resources to be **discovered dynamically**, queried by **semantic URIs** (e.g., `resource://role/{domain}/{role}`), and resolved at request time.

---

## Acceptance Criteria

### AC-1: ResourceTemplate Base Class

**Requirement:** Define `ResourceTemplate<T>` base class for creating templated resources.

**Input:** None (interface definition)

**Output:**

```typescript
interface ResourceTemplate<T> {
  id: string;              // Unique identifier
  uriPattern: string;      // URI pattern: resource://type/{placeholder}
  name: string;
  mimeType: string;        // text/plain, application/json, etc.
  resolve(params: Record<string, string>): Promise<T>;
  validate(params: Record<string, string>): Promise<boolean>;
}
```

**Validation:**

- [ ] Base class created at `src/mcp/resources/ResourceTemplate.ts`
- [ ] Generic type parameter T allows any resource type
- [ ] Pattern matching via URI parameter extraction
- [ ] Async resolve() method for lazy loading
- [ ] Optional validate() method for parameter validation

**Test Case:** UT-01 — Base class interface validation

---

### AC-2: URI Pattern Matching

**Requirement:** Implement pattern matcher to convert URI templates to regex and extract parameters.

**Input:**

- Pattern: `resource://role/{domain}/{role}`
- URI: `resource://role/engineering/agent-coordinator`

**Output:**

- Matched: true
- Extracted params: `{ domain: 'engineering', role: 'agent-coordinator' }`

**Validation:**

- [ ] Pattern → Regex conversion works
- [ ] Parameter extraction is accurate
- [ ] Case sensitivity configurable (default: case-sensitive)
- [ ] Special chars escaped properly (`{`, `}`, `/`)
- [ ] Wildcards supported (optional): `resource://role/*/{role}`

**Test Case:** UT-02..04 — Pattern matching for 3 URI types

---

### AC-3: Resource Resolver Implementations

**Requirement:** Create resolver functions for MCP server resource types.

**Output:** Four resolver templates:

1. **RoleTemplate** — `resource://role/{domain}/{role}`
   - Loads role definition from `database/roles/{domain}/{role.md}`
   - Validates domain + role exist
   - Returns role manifest (name, capabilities, tools)

2. **WorkflowTemplate** — `resource://workflow/{workflow_type}`
   - Loads workflow definition from `database/knowledge/{workflow_type}.md`
   - Returns workflow FSM definition

3. **TemplateTemplate** — `resource://template/{category}`
   - Loads template from `database/knowledge/templates/{category}.md`
   - Returns template content

4. **TaskTemplate** — `resource://task/{task_id}`
   - Loads task from EPIC milestone docs
   - Returns task AC + deliverables

**Validation:**

- [ ] All 4 resolvers created
- [ ] Each implements ResourceTemplate interface
- [ ] Parameter validation in validate() method
- [ ] Returns correct MIME type
- [ ] Handles file not found gracefully

**Test Case:** UT-05..08 — One test per resolver type

---

### AC-4: server.registerResource() Integration

**Requirement:** Add `registerResource()` method to MCP server to register resource templates.

**Signature:**

```typescript
server.registerResource(template: ResourceTemplate<any>): void;
server.listResources(): ResourceTemplate<any>[];
```

**Behavior:**

- Stores template in server registry
- No file loading until resource is requested (lazy)
- Multiple templates can share same URI prefix (exact match wins)

**Validation:**

- [ ] registerResource() method exists on server
- [ ] Templates stored in registry
- [ ] listResources() returns registered templates
- [ ] Duplicate URI patterns prevented (or first wins)
- [ ] No startup time impact (lazy loading only)

**Test Case:** INT-01 — Register template + list returns template

---

### AC-5: No File Paths in URIs

**Requirement:** All resource URIs are semantic, never expose filesystem paths.

**Valid URIs:**

- `resource://role/engineering/agent-coordinator` ✅
- `resource://workflow/discovery` ✅

**Invalid (should be rejected):**

- `resource://file/../../database/roles/...` ❌
- `resource://path/src/mcp/tools/...` ❌

**Validation:**

- [ ] URI whitelist enforced (only `role://`, `workflow://`, `template://`, `task://`)
- [ ] Path traversal attempts rejected
- [ ] Slashes in parameters escape properly
- [ ] Error message: "Invalid resource URI" (no exposing actual paths)

**Test Case:** UT-09..10 — URI validation + path traversal attack prevention

---

### AC-6: Error Handling (404, Invalid Params)

**Requirement:** Graceful error handling for missing resources + invalid parameters.

**Scenarios:**

1. Resource not found (parameter validation fails)
   - Return: `ResourceNotFoundError`
   - Message: "Resource role://engineering/nonexistent not found"

2. Invalid parameters
   - Return: `InvalidParameterError`
   - Message: "Parameter 'domain' must be alphanumeric"

3. Resolver throws
   - Return: `ResourceResolutionError`
   - Message: Generic error (no stack trace exposed)

**Validation:**

- [ ] Custom error types created
- [ ] Error messages don't expose file paths
- [ ] HTTP 404 for not found (if HTTP transport)
- [ ] Logging includes session ID for audit trail
- [ ] No exception stack traces in client response

**Test Case:** UT-11..13 — Error scenarios

---

## Deliverables

### Code

- [ ] `src/mcp/resources/ResourceTemplate.ts` — Base class + interface (80 lines)
- [ ] `src/mcp/resources/ResourceResolver.ts` — Pattern matching + resolution (150 lines)
- [ ] `src/mcp/resources/resolvers/` — Resolver implementations
  - [ ] `RoleResolver.ts` (50 lines)
  - [ ] `WorkflowResolver.ts` (50 lines)
  - [ ] `TemplateResolver.ts` (50 lines)
  - [ ] `TaskResolver.ts` (50 lines)
- [ ] `src/mcp/index.ts` — Update to integrate resource registration
- [ ] `src/tests/unit/resources/` — Unit tests (400+ lines)
- [ ] `src/tests/integration/resources-integration.test.ts` — E2E (200 lines)

### Documentation

- [ ] `docs/RESOURCE-TEMPLATES.md` — Developer guide (100 lines)
  - How to create custom resourceTemplate
  - Pattern syntax reference
  - Example: adding new resource type
- [ ] `docs/RESOURCE-URIS.md` — Resource URI reference (50 lines)
  - Valid URI patterns
  - Parameter constraints
  - Examples

### Tests (Definition of Done)

- [ ] Unit tests: 13+ test cases (cover all AC)
- [ ] Integration tests: Resolver workflows (4+ tests)
- [ ] No regression: Phase 1 tests (159) + Phase 2 tests (58 memory) still pass
- [ ] 80%+ code coverage for resource module

---

## File Inventory

| File | Type | Purpose | Status |
|:-----|:-----|:---------|:-------|
| `src/mcp/resources/ResourceTemplate.ts` | NEW | Base class + interface | Create |
| `src/mcp/resources/ResourceResolver.ts` | NEW | Pattern matching + resolution | Create |
| `src/mcp/resources/resolvers/RoleResolver.ts` | NEW | Role resource resolver | Create |
| `src/mcp/resources/resolvers/WorkflowResolver.ts` | NEW | Workflow resolver | Create |
| `src/mcp/resources/resolvers/TemplateResolver.ts` | NEW | Template resolver | Create |
| `src/mcp/resources/resolvers/TaskResolver.ts` | NEW | Task resolver | Create |
| `src/tests/unit/resources/ResourceTemplate.test.ts` | NEW | Base class tests | Create |
| `src/tests/unit/resources/ResourceResolver.test.ts` | NEW | Pattern matching tests | Create |
| `src/tests/unit/resources/UriValidation.test.ts` | NEW | URI validation + security | Create |
| `src/tests/integration/resources-integration.test.ts` | NEW | E2E resolver workflows | Create |
| `docs/RESOURCE-TEMPLATES.md` | NEW | Developer guide | Create |
| `docs/RESOURCE-URIS.md` | NEW | URI reference | Create |
| `src/mcp/index.ts` | MODIFY | Add server.registerResource() | Update |

---

## Technical Approach

### 1. Define ResourceTemplate Base Class (1 hour)

```typescript
export abstract class ResourceTemplate<T> {
  abstract id: string;
  abstract uriPattern: string;
  abstract name: string;
  abstract mimeType: string;

  abstract resolve(params: Record<string, string>): Promise<T>;
  validate(params: Record<string, string>): Promise<boolean> {
    return Promise.resolve(true); // Override in subclasses
  }
}
```

### 2. Implement ResourceResolver (2 hours)

- Convert `uriPattern` to regex: `resource://role/{domain}/{role}` → `/resource:\/\/role\/([^/]+)\/([^/]+)/`
- Test: `resource://role/engineering/agent-coordinator` → Matches, extract params
- Error handling: Throw `InvalidUriError` for malformed patterns

### 3. Create Resolver Implementations (3 hours)

- RoleResolver: Load from `database/roles/{domain}/{role.md}`
- WorkflowResolver: Load from `database/knowledge/{workflow_type}.md`
- TemplateResolver: Load from `database/knowledge/templates/{category}.md`
- TaskResolver: Load from EPIC milestone docs

### 4. Write Tests (2 hours)

- Unit: Pattern matching (3), Validators (3), Error handling (3)
- Integration: Full resolver workflows (4+)

### 5. Documentation (2 hours)

- Developer guide for creating custom templates
- URI reference for all built-in types
- Security notes (path traversal prevention)

---

## Blocked On

| Blocker | Task | Status | Impact |
|:--------|:-----|:-------|:-------|
| Plugin System | TASK-14-03 | ✅ Done | No impact — ready to start |

**No blockers. Can start immediately in parallel with 14-07, 14-09, 14-10.**

---

## Unblocks

- **TASK-14-11** (E2E Tests): Resource resolution can be tested end-to-end

---

## Success Criteria Checklist

- [ ] ResourceTemplate base class created + generic type support
- [ ] URI pattern matcher implemented + tested
- [ ] 4 resolver implementations created (Role, Workflow, Template, Task)
- [ ] server.registerResource() method added
- [ ] AC-5 security checks (no file paths, path traversal prevention)
- [ ] AC-6 error handling (404, invalid params, safe messages)
- [ ] 13+ unit tests, all passing
- [ ] 4+ integration tests, all passing
- [ ] No regression: Phase 1 + Memory Plugin tests (217) still pass
- [ ] 80%+ code coverage for resource module
- [ ] Code review approved
- [ ] Merged to feature branch

---

## Effort Breakdown

| Phase | Duration | Notes |
|:------|:---------|:------|
| ResourceTemplate base class | 1 hour | Interface design |
| ResourceResolver implementation | 2 hours | Pattern matching + resolution logic |
| Resolver implementations (4x) | 3 hours | Load role, workflow, template, task |
| Unit tests | 2 hours | Pattern + validators + error handling (13 cases) |
| Integration tests | 1.5 hours | Full workflows (4+ cases) |
| Documentation | 0.5 hours | Developer guide + URI reference |
| **Total** | **10 hours** | Done by +3 days |

---

## Definition of Done

- [x] TASK-14-08-IMPLEMENTATION-SUMMARY.md created (post-implementation)
- [x] AC verification matrix (6/6 AC passing)
- [x] Test results: 13 unit + 4 integration, all passing
- [x] Code review sign-off
- [x] Git commit: `feat(TASK-14-08): Dynamic resource templates with semantic URIs`
- [x] Merged to feature branch
- [x] Phase 1 + Memory Plugin regression check (217 tests still passing)

---

## Next Tasks After Completion

Parallelize remaining Phase 2:

- **TASK-14-09: Sampling & Completion** (10h)
- **TASK-14-10: Notification Debouncing** (6h)
- **TASK-14-11: E2E Test Suite** (12h, after 14-02 + 14-07 complete)
