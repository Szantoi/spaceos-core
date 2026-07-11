---
id: goal-epic-17
title: "EPIC-17 Goal: Multi-Domain Configuration & Onboarding"
type: goal
epic: EPIC-17
sphere: mcp-context-server
milestone: M03
created: 2026-03-04
---

# EPIC-17 Goal: Multi-Domain Configuration & Onboarding

## Executive Summary

Generalize the MCP server to support **arbitrary domains** (not hardcoded to engineering/discovery/management).
Enable **domain onboarding workflows** where new domains (e.g., "product," "data-science," "compliance")
can be registered + seeded at runtime. Session context is domain-aware; agents switch domains
without server restart.

**This realizes server portability and enables multi-domain demonstrations.**

---

## Strategic Context (Vision Goals Addressed)

- **Goal #2:** Database-first runtime — Domains stored in agent.db; no hardcoded strings
- **Broader goal:** Domain portability — Same server code serves multiple verticals

---

## Key Principles

1. **Domain registry in DB** — `domains` table; no hardcoding
2. **Runtime domain switching** — Agents can change active domain within session
3. **Onboarding flow** — New domain → register → seeder loads roles + workflows
4. **Domain isolation** — Queries filtered by domain; no cross-domain data leakage

---

## Success Criteria

### Domain Registry
- [ ] `domains` SQLite table: id, name, description, config_json, created_at, updated_at
- [ ] Seeder populates `domains` table from `database/roles/` directory structure
- [ ] Each domain has unique ID + name

### Runtime Domain Switching
- [ ] `switch_domain(domain_name)` MCP tool (admin-only)
- [ ] Session context includes `current_domain_id`
- [ ] Session stores user's domain preference
- [ ] Tool calls routed to current domain's tools + workflows

### Onboarding
- [ ] `register_domain(name, description, config)` MCP tool (admin-only)
- [ ] Tool triggers seeder for new domain
- [ ] Error handling: duplicate domain names rejected
- [ ] Documentation: step-by-step onboarding guide

### Multi-Tenant Safety
- [ ] Queries include `WHERE domain_id = ?` filtering
- [ ] RBAC: agent restricted to their domain's roles
- [ ] No cross-domain information leakage
- [ ] Audit log tracks domain-switch events

### Integration Points
- [ ] EPIC-14 tool plugins can query by domain
- [ ] EPIC-11 middleware propagates domain context
- [ ] Bootstrap (EPIC-10) returns domain-scoped context
- [ ] Legacy tools (EPIC-16 M03) wrapped DB queries with domain filter

### Testing
- [ ] Unit test: domain registration + seeder
- [ ] Unit test: queries filtered by domain
- [ ] E2E test: agent registers new domain + bootstraps
- [ ] E2E test: switch_domain() changes session context
- [ ] E2E test: cross-domain data isolation verified

---

## Deliverables

| Deliverable | Type | Location |
|:-----------|:-----|:---------|
| Domain Registry Schema | Code | `src/metadata/initAgentDb.ts` (domains table) |
| Domain Management Tools | Code | `src/mcp/tools/admin.ts` (register_domain, switch_domain, list_domains) |
| Seeder Refactor | Code | `src/rag/seedAgentDb.ts` (domain-aware loading) |
| Session + Domain Binding | Code | `src/metadata/WorkflowStateTracker.ts` (extend session) |
| Query Helpers | Code | `src/metadata/queryAgentDb.ts` (add domain filtering) |
| Domain Config Docs | Docs | `database/standards/03-agent-system/domain-config.md` |
| Onboarding Guide | Docs | `Docs/mcp-context-server/onboarding/add-new-domain.md` |
| E2E Test Suite | Tests | `src/tests/e2e/epic-17-multi-domain.spec.ts` |
| Implementation Summary | Report | `implementation-summary/EPIC-17-<date>.md` |

---

## Database Schema Additions

```sql
CREATE TABLE domains (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL UNIQUE,
  description TEXT,
  config_json TEXT, -- Domain-specific configuration
  parent_domain_id TEXT, -- Optional: for domain hierarchy
  created_at TIMESTAMP,
  updated_at TIMESTAMP,
  FOREIGN KEY (parent_domain_id) REFERENCES domains(id)
);

-- FK extension to roles table (from EPIC-09)
ALTER TABLE roles ADD COLUMN domain_id TEXT REFERENCES domains(id);

-- FK extension to sessions table (from EPIC-08/09)
ALTER TABLE sessions ADD COLUMN current_domain_id TEXT REFERENCES domains(id);

-- Domain seeding metadata
CREATE TABLE domain_seed_history (
  id TEXT PRIMARY KEY,
  domain_id TEXT,
  seeded_at TIMESTAMP,
  roles_count INTEGER,
  workflows_count INTEGER,
  FOREIGN KEY (domain_id) REFERENCES domains(id)
);
```

---

## Domain Configuration Example

```markdown
# domain-config.md (stores in domains.config_json)

{
  "id": "product",
  "name": "Product Domain",
  "description": "Product management + feature discovery",
  "roles": [
    "product_manager",
    "designer",
    "researcher"
  ],
  "workflows": {
    "discovery": ["ideation", "validation", "user_feedback"],
    "delivery": ["planning", "development", "qa", "launch"]
  },
  "templates": [
    "product_brief.template",
    "feature_spec.template",
    "launch_checklist.template"
  ]
}
```

---

## Tool API

```typescript
// register_domain (admin-only)
request: {
  name: string,
  description: string,
  config?: Record<string, any>
}
response: {
  domain_id: string,
  name: string,
  seeding_status: "queued" | "in_progress" | "complete"
}

// switch_domain (admin-only, or user in multi-domain session)
request: {
  domain_name: string
}
response: {
  current_domain_id: string,
  domain_name: string,
  available_roles: string[],
  switched_at: string (ISO8601)
}

// list_available_domains
request: {}
response: {
  domains: [
    {
      id: string,
      name: string,
      description: string,
      roles_count: number
    }
  ]
}

// get_domain_config (admin-only)
request: {
  domain_name: string
}
response: {
  config: Record<string, any>
}
```

---

## Task Breakdown

- **TASK-17-01:** Domain schema design + ERD
- **TASK-17-02:** SQLite migration: add domains table
- **TASK-17-03:** Seeder refactor: domain-aware loading
- **TASK-17-04:** `register_domain()` tool + auto-seeder trigger
- **TASK-17-05:** `switch_domain()` tool + session binding
- **TASK-17-06:** `list_available_domains()` tool
- **TASK-17-07:** Query filtering: add domain WHERE clause
- **TASK-17-08:** E2E test: multi-domain isolation
- **TASK-17-09:** Onboarding documentation + demo

---

## Blocks/Enablers

| Dependency | Status | Impact |
|:-----------|:-------|:--------|
| EPIC-09 (SQLite) | ✅ M02 EPIC-09 | ✅ Base tables ready |
| EPIC-16 (refactor) | 🔄 M03 EPIC-16 | ✅ Query isolation compatible |
| EPIC-14 (plugins) | ✅ M02 EPIC-14 | ✅ Admin tools plugins ready |

---

## Design Decisions

| Decision | Rationale |
|:---------|:----------|
| Domain registry in DB | No hardcoding; runtime flexibility |
| Session domain binding | Per-user domain context; no global state |
| Admin-only tools | Prevent accidental domain switches |
| Domain hierarchy (optional) | Allows nested domains (product/mobile, product/web) |
| Seed history tracking | Audit trail + observability |

---

## Success Metrics

- ✅ EPIC-17 tasks complete + all tests green
- ✅ New domain registration < 30 seconds (seeder included)
- ✅ Multi-domain E2E test passes (2+ domains)
- ✅ Query isolation verified: agent in domain A cannot read domain B data
- ✅ M03 demo: 3 domains live + queryable
- ✅ Onboarding doc clear enough for new domains to be added without code changes

---

## Related Documentation

- `Docs/goal.md` § Program Hierarchy — Multi-domain vision
- `database/roles/` — Current domain structure (template)
- `database/standards/03-agent-system/domain-config.md` (new, to be created)
- EPIC-16 goal.md — Query refactoring for isolation
