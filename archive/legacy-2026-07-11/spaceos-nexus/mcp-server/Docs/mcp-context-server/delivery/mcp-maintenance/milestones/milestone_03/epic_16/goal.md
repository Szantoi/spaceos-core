---
id: goal-epic-16
title: "EPIC-16 Goal: Legacy Tool Refactor - DB Wrapper Architecture"
type: goal
epic: EPIC-16
sphere: mcp-context-server
milestone: M03
created: 2026-03-04
---

# EPIC-16 Goal: Legacy Tool Refactor — DB Wrapper Architecture

## Executive Summary

Refactor **file-based tools** (`get_role`, `get_workflow`, `get_template`, `get_core`) into backward-compatible
**DB wrapper tools** that query `agent.db` instead of the filesystem. External API remains unchanged;
internal implementation switches from file I/O to SQLite queries.

**End result:** Server operates fully on `agent.db`; `database/` folder is read-only seed input.**

---

## Strategic Context (Vision Goals Addressed)

- **Goal #2:** Database-first runtime — 100% of queries use agent.db (not filesystem)
- **Broader goal:** Deployment flexibility — Server deployable without `database/` folder present

---

## Key Principles

1. **Backward compatible API** — External tool signatures unchanged; LLMs don't need updates
2. **Transparent wrapper** — Agents don't know queries are DB-backed (not file-backed)
3. **Better performance** — Indexed DB queries faster than file I/O + parsing YAML
4. **Future-proof** — DB allows easier changes (schema migration) vs. file versioning

---

## Success Criteria

### Tool Wrapping
- [ ] `get_role(domain, role)` → queries `roles` table, returns same JSON structure
- [ ] `get_workflow(domain, role, type)` → queries `workflows` table
- [ ] `get_template(domain, category, name)` → queries `templates` table
- [ ] `get_core(std_id)` → queries standards table (new, M03)
- [ ] `list_roles(domain)` → queries `roles` table filtered by domain
- [ ] All tools return identical output format (no breaking changes)

### Query Performance
- [ ] Legacy tool query latency < 50ms (indexed lookups)
- [ ] No performance regression vs. M01 baseline
- [ ] Bulk lookups (list_roles with 50 roles) < 200ms

### Data Integrity
- [ ] Seeded data (EPIC-09) matches original file content bit-for-bit (if applicable)
- [ ] Character encoding preserved (UTF-8 for special chars, etc.)
- [ ] Markdown formatting preserved (JSON stringified, not mangled)

### Filesystem Independence
- [ ] Server boots successfully with `database/` folder deleted
- [ ] Agent can call `get_role()` → returns data from agent.db (not error)
- [ ] E2E tests pass without filesystem dependency

### Integration Points
- [ ] RBAC filter (EPIC-11) uses DB queries (no YAML scanning)
- [ ] Bootstrap (EPIC-10) queries agent.db for role definitions
- [ ] Request context (EPIC-11) uses agent.db workflows
- [ ] All existing E2E tests pass unmodified

### Testing
- [ ] Unit test: `get_role()` wraps DB lookup correctly
- [ ] Unit test: output format matches original JSON structure
- [ ] E2E test: agent calls legacy tools → data returned correctly
- [ ] E2E test: `list_roles()` returns correct count
- [ ] Regression test: comparison of legacy (YAML) vs. new (DB) output for sample data

---

## Deliverables

| Deliverable | Type | Location |
|:-----------|:-----|:---------|
| DB Wrapper Tool Module | Code | `src/mcp/tools/legacy.ts` |
| Standards Table Schema | Code | `src/metadata/initAgentDb.ts` (standards table) |
| Tool Implementations | Code | get_role, get_workflow, get_template, get_core, list_roles |
| Seeder Extensions | Code | `src/rag/seedAgentDb.ts` (standards seeding) |
| Query Helpers | Code | `src/metadata/queryLegacyTools.ts` |
| Regression Test Suite | Tests | `src/tests/e2e/epic-16-legacy-wrapper.spec.ts` |
| Migration Guide | Docs | `Docs/mcp-context-server/migration/legacy-to-db.md` |
| Implementation Summary | Report | `implementation-summary/EPIC-16-<date>.md` |

---

## Database Schema (New / Extensions)

```sql
-- Standards table (for get_core queries)
CREATE TABLE standards (
  id TEXT PRIMARY KEY,
  category TEXT, -- "foundation", "delivery", "discovery", "agent-system"
  name TEXT,
  content TEXT, -- Full Markdown content
  version TEXT,
  created_at TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE INDEX idx_standards_category ON standards(category);

-- Extension to existing tables (from EPIC-09)
-- roles, workflows, templates already exist + populated
-- Just ensure all indexes + query optimization in place

CREATE INDEX idx_roles_domain_name ON roles(domain, name);
CREATE INDEX idx_workflows_domain_type ON workflows(domain, workflow_type);
CREATE INDEX idx_templates_domain_category ON templates(domain, category, name);
```

---

## Tool Wrapper Examples

### Before (YAML file I/O)
```typescript
async function get_role(domain: string, role: string): Promise<RoleDefinition> {
  const path = `database/roles/${domain}/${role}/${role}.role.md`;
  const content = fs.readFileSync(path, 'utf-8');
  // parse markdown, extract YAML frontmatter, return object
  return parseRoleMarkdown(content);
}
```

### After (DB Query)
```typescript
async function get_role(domain: string, role: string, context: RequestContext): Promise<RoleDefinition> {
  const row = db.prepare(
    `SELECT * FROM roles WHERE domain = ? AND name = ?`
  ).get(domain, role);

  if (!row) {
    return errorResponse("NOT_FOUND", `Role ${role} not found in domain ${domain}`);
  }

  // Return same structure as before (role_definition JSON + metadata)
  return {
    content: [{type: "text", text: row.content}],
    structuredContent: {
      role: row.name,
      domain: row.domain,
      definition: JSON.parse(row.definition_json)
    }
  };
}
```

---

## Tool API (Unchanged)

```typescript
// get_role (backward compatible)
request: { domain: string, role: string }
response: {
  role: string,
  domain: string,
  definition: {
    name: string,
    description: string,
    responsibilities: string[],
    rules: Record<string, any>
  }
}

// get_workflow (backward compatible)
request: { domain: string, role: string, type: string }
response: {
  workflow: {
    type: string,
    steps: Step[],
    transitions: Transition[]
  }
}

// get_template (backward compatible)
request: { domain: string, category: string, name: string }
response: {
  template: {
    category: string,
    name: string,
    content: string
  }
}

// get_core
request: { std_id: string }
response: {
  standard: {
    id: string,
    category: string,
    content: string,
    version: string
  }
}

// list_roles
request: { domain: string }
response: {
  roles: [{name: string, description: string}]
}
```

---

## Task Breakdown

- **TASK-16-01:** Standards table schema design
- **TASK-16-02:** Seeder extensions: standards loading
- **TASK-16-03:** `get_role()` wrapper implementation
- **TASK-16-04:** `get_workflow()` wrapper
- **TASK-16-05:** `get_template()` + `list_roles()` wrappers
- **TASK-16-06:** `get_core()` implementation
- **TASK-16-07:** Query optimization + indexing
- **TASK-16-08:** Regression testing (YAML vs. DB comparison)
- **TASK-16-09:** Filesystem independence test
- **TASK-16-10:** Performance benchmarking

---

## Blocks/Enablers

| Dependency | Status | Impact |
|:-----------|:-------|:--------|
| M02 Complete | ✅ M02 → M03 | ✅ prerequisite (bootstrap, context ready) |
| EPIC-09 (SQLite) | ✅ M02 EPIC-09 | ✅ Base tables populated |
| EPIC-11 (middleware) | ✅ M02 EPIC-11 | ✅ RBAC queries available |
| EPIC-17 (multi-domain) | 🔄 M03 EPIC-17 | ✅ Domain queries compatible |

---

## Design Decisions

| Decision | Rationale |
|:---------|:----------|
| Wrapper (not replacement) | Backward compatibility; agents don't change |
| DB queries for all legacy | Consistency + performance (no hybrid file/DB) |
| Exact output format match | Regression testing verifies no behavior change |
| Standards table new | Generalizes knowledge base storage |
| Indexed queries | Performance: DB lookups faster than disk I/O |

---

## Migration Path

1. **M03 start:** agent.db fully seeded (EPIC-09 complete)
2. **M03 work:** Legacy wrappers implemented + tested
3. **M03 end:** All queries DB-backed; `database/` folder optional
4. **M04 onward:** `database/` only used for seeding if needed; runtime independent

---

## Success Metrics

- ✅ EPIC-16 tasks complete + all tests green
- ✅ `get_role()` latency < 50ms (DB vs. < 100ms file I/O)
- ✅ Existing E2E tests pass unmodified (100% backward compat)
- ✅ `list_roles()` returns identical results to filesystem scan
- ✅ Server boots successfully with `database/` folder deleted
- ✅ Regression test: DB output == YAML output (byte-comparison for content)
- ✅ Performance benchmark: DB queries outperform file I/O

---

## Reverse Compatibility Test (Critical)

```typescript
// For each legacy tool:
// 1. Read result from old YAML-based method
// 2. Read result from new DB wrapper
// 3. Verify identical (or acceptable differences, documented)

const yamlResult = await legacyYamlTool(...);
const dbResult = await legacyDbWrapper(...);
expect(JSON.stringify(dbResult)).toEqual(JSON.stringify(yamlResult));
```

---

## Related Documentation

- `Docs/goal.md` § Vision Goal #2 — Database-first runtime
- EPIC-09 goal.md — Seeded data foundation
- EPIC-11 goal.md — RBAC DB queries
- EPIC-17 goal.md — Multi-domain DB support
- Existing E2E tests (unchanged, to stay green)
