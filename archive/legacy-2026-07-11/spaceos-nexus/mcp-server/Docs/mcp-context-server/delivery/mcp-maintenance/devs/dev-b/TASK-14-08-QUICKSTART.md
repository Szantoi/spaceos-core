---
id: TASK-14-08-QUICKSTART
title: "TASK-14-08 Day-1 Quickstart — Resource Template Support"
type: developer-quickstart
owner: "Dev B (or TBD)"
duration: "10 hours"
created: 2026-03-11
---

# 🚀 TASK-14-08 Quickstart — Resource Template Support

**Your mission:** Implement dynamic Resource URI patterns for role-based, workflow-based resource discovery (without hardcoding file paths).

**Duration:** 10 hours (~3 dev days)
**Files you'll touch:** `src/mcp/resources/resourceTemplates.ts`, `src/tests/unit/resource-templates.test.ts`
**Predecessor:** TASK-14-03 ✅
**No blockers** → Can run in parallel!

---

## Today's Checklist

**Day 1:**
- [ ] **09:00-09:30** — Understand problem + review task spec (30 min)
- [ ] **09:30-11:00** — Design ResourceTemplate base class (1h30m)
- [ ] **11:00-12:00** — Implement URI pattern matcher (1h)
- [ ] **12:00-13:00** — Implement Zod schema validation (1h)

**Day 2:**
- [ ] **09:00-10:30** — Implement resource resolvers (role, workflow, template) (1h30m)
- [ ] **10:30-12:00** — Write unit tests (1h30m)
- [ ] **12:00-13:00** — Create resource registrant + listing (1h)

**Day 3:**
- [ ] **09:00-11:00** — Integration tests (2h)
- [ ] **11:00-12:00** — Documentation + examples (1h)
- [ ] **12:00-13:00** — Final validation (1h)

**Total: 10 hours** (spread over 3 days)

---

## Problem & Solution

**Problem:** Resources currently hardcoded or file-path based
- Tight coupling to filesystem
- No semantic URIs
- Not discoverable

**Solution:** ResourceTemplate pattern
- URI: `resource://type/{placeholder}/next`
- Dynamic resolver at request-time
- Registered with PluginManager

**Example:**
```
Pattern:  resource://role/{domain}/{role}
Request:  resource://role/engineering/agent-coordinator
Extract:  { domain: "engineering", role: "agent-coordinator" }
Resolve:  Load from DB + format
```

---

## Architecture Pattern

```typescript
// src/mcp/resources/resourceTemplates.ts

interface IResourceTemplate<T> {
  id: string;
  uriPattern: string;               // e.g., resource://role/{domain}/{role}
  name: string;
  mimeType: string;                 // text/plain, application/json
  resolve(params: Record<string, string>): Promise<T | null>;
  validate(params: Record<string, string>): Promise<boolean>;
}

// Example: Role resource template
export class RoleResourceTemplate implements IResourceTemplate<YamlContent> {
  id = "role-template";
  uriPattern = "resource://role/{domain}/{role}";
  name = "Role Definition Resource";
  mimeType = "text/yaml";

  async resolve(params: { domain: string; role: string }): Promise<string> {
    // Fetch from database: SELECT * FROM roles WHERE domain = ... AND name = ...
    // Format as YAML
    // Return content
  }

  async validate(params: { domain: string; role: string }): Promise<boolean> {
    // Check domain exists, role exists, etc.
    return true; // or false
  }
}
```

---

## Step-by-Step

### Step 1: Base Class Design (1h30m)

```typescript
// Define interface
interface IResourceTemplate<T> {
  id: string;
  uriPattern: string;
  name: string;
  mimeType: string;
  resolve(params: Record<string, string>): Promise<T>;
  validate(params: Record<string, string>): Promise<boolean>;
}

// Create base abstract class
abstract class BaseResourceTemplate<T> implements IResourceTemplate<T> {
  abstract id: string;
  abstract uriPattern: string;
  abstract name: string;
  abstract mimeType: string;
  abstract resolve(params: Record<string, string>): Promise<T>;
  abstract validate(params: Record<string, string>): Promise<boolean>;
}
```

### Step 2: URI Pattern Matcher (1h)

```typescript
class UriPatternMatcher {
  constructor(pattern: string) {
    // e.g., resource://role/{domain}/{role}
    // Extract placeholders: [domain, role]
    // Build regex: resource://role/([^/]+)/([^/]+)
  }

  match(uri: string): { matched: boolean; params?: Record<string, string> } {
    // Try regex match
    // Extract param values
    // Return { matched: true, params: {...} }
  }
}
```

### Step 3: Resource Resolvers (1h30m)

Create 3 template implementations:

```typescript
// RoleResourceTemplate
export class RoleResourceTemplate extends BaseResourceTemplate<string> {
  id = "role";
  uriPattern = "resource://role/{domain}/{role}";
  name = "Role Definition";
  mimeType = "text/yaml";

  async resolve(params: { domain: string; role: string }): Promise<string> {
    // Query DB: SELECT content FROM roles WHERE domain = ... AND name = ...
    return content;
  }

  async validate(params: { domain: string; role: string }): Promise<boolean> {
    // Check role exists
    return true;
  }
}

// WorkflowResourceTemplate
// TemplateResourceTemplate
// ... similar structure
```

### Step 4: Registration & Listing (1h)

```typescript
export class ResourceTemplateRegistry {
  private templates: Map<string, IResourceTemplate<any>> = new Map();

  register(template: IResourceTemplate<any>): void {
    this.templates.set(template.id, template);
  }

  async resolveUri(uri: string): Promise<string | null> {
    // Iterate templates
    for (const template of this.templates.values()) {
      const matcher = new UriPatternMatcher(template.uriPattern);
      const match = matcher.match(uri);
      if (match.matched) {
        return template.resolve(match.params!);
      }
    }
    return null;
  }

  listResources(): Array<{ id: string; uriPattern: string; name: string }> {
    // Return all registered templates
  }
}
```

### Step 5: Unit Tests (1h30m)

```typescript
describe("ResourceTemplates", () => {
  it("should match URI pattern", () => {
    const matcher = new UriPatternMatcher("resource://role/{domain}/{role}");
    const result = matcher.match("resource://role/engineering/agent");
    expect(result.matched).toBe(true);
    expect(result.params).toEqual({ domain: "engineering", role: "agent" });
  });

  it("should resolve role resource", async () => {
    const template = new RoleResourceTemplate();
    const content = await template.resolve({ domain: "engineering", role: "agent" });
    expect(content).toContain("role:");
  });

  // 2-3 more tests
});
```

---

## Testing Your Work

```bash
npm test -- --match "*resource*"
```

**Expected:** All pattern matching, resolution, and validation tests pass ✅

---

## Supported Resource URIs

After implementation, these should work:

```
resource://role/{domain}/{role}              → Role YAML
resource://workflow/{type}                   → Workflow definition
resource://template/{category}               → Artifact template
resource://discovery/{phase}                 → Discovery workflow
resource://task/{task_id}                    → Task context
```

---

## Completion Sign-Off

When done:

1. All 6 AC from `TASK-14-08-RESOURCE-TEMPLATES.md` passing ✅
2. All tests green ✅
3. At least 3 resource templates working ✅
4. URI matching tested ✅
5. Post completion report!

