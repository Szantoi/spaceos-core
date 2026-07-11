---
id: DEV-B-TASK-14-08-QUICKSTART
title: "TASK-14-08 — Day 1 Quickstart (Resource Template Support)"
developer: Dev B
date: 2026-03-12
status: "✅ COMPLETE"
---

# TASK-14-08 Day 1 Quickstart

## 🎯 What You're Doing Today (3-4 hours)

Building the **Resource Template system** — a clean way for agents to discover and access role definitions, workflows, and templates via URI patterns instead of file paths.

**By EOD Today:** Design doc + skeleton code ready to review

---

## 📋 Today's Checklist

### Phase 1: Context Reading (30-45 min)

- [ ] Read `EPIC-14-TASK-MATRIX.md` — lines 300-350 (TASK-14-08 full spec)
- [ ] Review `PLUGIN-SYSTEM-API-REFERENCE.md` — understand plugin registration
- [ ] Skim `EPIC-14-PHASE-1-COMPLETION.md` — see what Phase 1 built
- [ ] Review your own HTTP transport (`src/mcp/transports/HTTPTransport.ts`) — 10 min refresh

### Phase 2: Design (1-1.5 hours)

- [ ] Create `TASK-14-08-DESIGN.md` (see template below)
- [ ] Sketch URI pattern matching algorithm
- [ ] Design ResourceTemplate interface + base class
- [ ] Plan resolver chain for roles → workflows → templates
- [ ] Identify 3-5 edge cases (invalid URIs, missing resources, etc.)

### Phase 3: Skeleton Code (1-1.5 hours)

- [ ] Create `src/mcp/resources/resourceTemplates.ts`
- [ ] Define `ResourceTemplate` abstract base class
- [ ] Define `ResourceContent` interface
- [ ] Stub out role template resolver
- [ ] Create test skeleton: `src/tests/unit/resourceTemplates.test.ts`

### Phase 4: Sign-Off & Prep (30 min)

- [ ] Create this file: ✓ You're reading it
- [ ] Create day-1 summary: `TASK-14-08-DAY-1-SUMMARY.md`
- [ ] Flag any blockers or questions
- [ ] Stage for Tech Lead review tomorrow morning

---

## 🔧 What NOT to Do Today

- Don't build full resolver implementations yet (save for Day 2)
- Don't write integration tests (save for Day 3)
- Don't worry about HTTP transport implementation details (transports already work)
- Don't optimize performance yet (correctness first)

---

## 📝 Design Template: TASK-14-08-DESIGN.md

Create this file in `dev-b/TASK-14-08/`:

```markdown
---
id: TASK-14-08-DESIGN
title: "Design — Resource Template Support"
developer: Dev B
date: 2026-03-12
---

# TASK-14-08 Design

## Problem Statement
Currently, role definitions live in `database/roles/` but agents have no clean way to discover and access them. We need a URI-based resource system.

## Solution Overview
Implement ResourceTemplate pattern:
- Base class: ResourceTemplate (abstract)
- URI pattern: `resource://role/{domain}/{role}`
- Resolver: Matches URI → extracts params → resolves content

## Content Model
- Resource URI: `resource://role/engineering/backend_developer`
- Pattern: `resource://role/{domain}/{role}`
- Content: JSON stringified role definition

## State Diagram
```

Request URI
→ Match against patterns
→ Extract parameters
→ Call resolver function
→ Return ResourceContent (JSON)
   OR 404 if not found

```

## API Design
```typescript
abstract class ResourceTemplate {
  abstract uriPattern: string;
  abstract matchUri(uri: string): MatchResult;
  abstract resolve(params): Promise<ResourceContent>;
}

interface ResourceContent {
  uri: string;
  name: string;
  mimeType: 'application/json';
  contents: string;
}
```

## Implementation Plan (Days 2-3)

1. Base class + interface definitions
2. Role template resolver
3. Workflow template resolver
4. Template library resolver
5. Server integration + both transports
6. Full test suite

## Edge Cases

1. Invalid URI format → 400 Bad Request
2. Missing resource (e.g., non-existent role) → 404 Not Found
3. Ambiguous pattern match → Error
4. Malformed parameters → 400 Bad Request
5. File path traversal attempts → Rejected

## Success Metrics

- [ ] All 6 AC implemented
- [ ] 100% unit test coverage
- [ ] Both transports support URIs
- [ ] No file paths leaked in responses

```

---

## 💻 Skeleton Code: src/mcp/resources/resourceTemplates.ts

Create this file with base structure:

```typescript
// src/mcp/resources/resourceTemplates.ts

/**
 * Resource Template System
 *
 * Enables URI-based discovery and access to role definitions,
 * workflows, templates, and other resources without hardcoding paths.
 *
 * URI Patterns:
 *   resource://role/{domain}/{role}
 *   resource://workflow/{type}
 *   resource://template/{category}
 *   resource://discovery/{phase}
 */

export interface ResourceContent {
  uri: string;
  name: string;
  description?: string;
  mimeType: 'application/json';
  contents: string;  // JSON stringified
}

export interface MatchResult {
  matched: boolean;
  params?: Record<string, string>;
}

/**
 * Abstract base class for resource templates
 */
export abstract class ResourceTemplate {
  abstract readonly uriPattern: string;
  abstract readonly description: string;

  /**
   * Match URI against this template's pattern
   * Returns matched params if successful
   */
  abstract matchUri(uri: string): MatchResult;

  /**
   * Resolve resource content for matched parameters
   * Throws if params don't exist
   */
  abstract resolve(params: Record<string, string>): Promise<ResourceContent>;

  /**
   * Helper: Extract pattern variables
   * E.g., "resource://role/{domain}/{role}" → ["domain", "role"]
   */
  protected extractPatternVariables(): string[] {
    const regex = /{([^}]+)}/g;
    const matches = [];
    let match;
    while ((match = regex.exec(this.uriPattern)) !== null) {
      matches.push(match[1]);
    }
    return matches;
  }

  /**
   * Helper: Convert pattern to regex
   * E.g., "resource://role/{domain}/{role}"
   *       → /^resource:\/\/role\/([^\/]+)\/([^\/]+)$/
   */
  protected patternToRegex(): RegExp {
    let regex = this.uriPattern
      .replace(/[.*+?^${}()|[\]\\]/g, '\\$&')  // Escape special chars
      .replace(/{[^}]+}/g, '([^\\/]+)');        // Replace {var} with regex groups
    return new RegExp(`^${regex}$`);
  }
}

/**
 * Role template: resource://role/{domain}/{role}
 */
export class RoleTemplate extends ResourceTemplate {
  readonly uriPattern = 'resource://role/{domain}/{role}';
  readonly description = 'Role definition by domain and role type';

  matchUri(uri: string): MatchResult {
    const regex = this.patternToRegex();
    const match = uri.match(regex);
    if (!match) return { matched: false };

    const [domain, role] = match.slice(1);
    return { matched: true, params: { domain, role } };
  }

  async resolve(params: Record<string, string>): Promise<ResourceContent> {
    const { domain, role } = params;

    // TODO: Load from database/roles/{domain}/{role}.json
    // For now, throw 404
    throw new Error(`Role not found: ${domain}/${role}`);
  }
}

// TODO: Implement WorkflowTemplate, TemplateLibraryTemplate, etc.

export class ResourceTemplateRegistry {
  private templates: ResourceTemplate[] = [];

  register(template: ResourceTemplate): void {
    this.templates.push(template);
  }

  /**
   * Find template and resolve resource
   */
  async resolve(uri: string): Promise<ResourceContent> {
    for (const template of this.templates) {
      const match = template.matchUri(uri);
      if (match.matched && match.params) {
        return template.resolve(match.params);
      }
    }
    throw new Error(`No resource template matched URI: ${uri}`);
  }

  /**
   * List all available resource URIs
   */
  async listResources(): Promise<Array<{ uri: string; description: string }>> {
    // TODO: Enumerate all resources from each template
    return [];
  }
}
```

---

## 🧪 Test Skeleton: src/tests/unit/resourceTemplates.test.ts

```typescript
import { describe, it, expect } from 'vitest';
import { ResourceTemplate, RoleTemplate, ResourceTemplateRegistry } from '../../src/mcp/resources/resourceTemplates';

describe('ResourceTemplate', () => {
  describe('URI Pattern Matching', () => {
    it('should match valid role URI', () => {
      const template = new RoleTemplate();
      const match = template.matchUri('resource://role/engineering/backend_developer');

      expect(match.matched).toBe(true);
      expect(match.params).toEqual({ domain: 'engineering', role: 'backend_developer' });
    });

    it('should not match role URI with extra segments', () => {
      const template = new RoleTemplate();
      const match = template.matchUri('resource://role/engineering/backend_developer/extra');

      expect(match.matched).toBe(false);
    });

    it('should not match invalid URI format', () => {
      const template = new RoleTemplate();
      const match = template.matchUri('invalid-uri');

      expect(match.matched).toBe(false);
    });
  });

  describe('Resource Resolution', () => {
    it('should throw 404 for missing role (TODO: implement)', async () => {
      const template = new RoleTemplate();

      await expect(template.resolve({ domain: 'nonexistent', role: 'nonexistent' }))
        .rejects.toThrow();
    });
  });
});

describe('ResourceTemplateRegistry', () => {
  it('should resolve resource via registered template', async () => {
    // TODO: Implement when templates built
  });

  it('should throw when no template matches URI', async () => {
    // TODO: Implement when templates built
  });
});
```

---

## 🎯 What to Commit Today

Push these files to a feature branch:

```bash
git checkout -b dev-b/epic-14-phase2-task-14-08

# Add today's work
git add Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-b/TASK-14-08/
git add src/mcp/resources/resourceTemplates.ts
git add src/tests/unit/resourceTemplates.test.ts

git commit -m "feat(epic-14): TASK-14-08 Day 1 — Resource Template design + skeleton code

- Designed ResourceTemplate base class + URI pattern matching
- Sketched resolver architecture (role/workflow/template)
- Implemented skeleton code + test structure
- Ready for Tech Lead design review (2026-03-12 09:00 UTC)

Files:
- src/mcp/resources/resourceTemplates.ts (skeleton + interfaces)
- src/tests/unit/resourceTemplates.test.ts (test scaffolding)
- Docs/.../TASK-14-08-DESIGN.md (design doc)
- Docs/.../TASK-14-08-DAY-1-SUMMARY.md (progress summary)
"
```

---

## ✅ EOD Checklist

Before wrapping up today:

- [ ] Design doc (`TASK-14-08-DESIGN.md`) complete
- [ ] Skeleton code files created (ResourceTemplate base class)
- [ ] Test structure in place (can scaffold, not run yet)
- [ ] Day-1 summary doc created
- [ ] Feature branch pushed (ready for Tech Lead review)
- [ ] No compilation errors (run `npx tsc --noEmit`)
- [ ] Questions/blockers documented (if any)

---

## 🔗 Reference Files

| File | Purpose |
|------|---------|
| `EPIC-14-TASK-MATRIX.md` | Full task spec (lines 300-350) |
| `PLUGIN-SYSTEM-API-REFERENCE.md` | How plugins register resources |
| `src/mcp/transports/HTTPTransport.ts` | Your HTTP transport (as reference) |
| `src/mcp/index.ts` | Entry point for resource registration |

---

**Next: Tomorrow 09:00 UTC → Tech Lead design review + Day 2 implementation kickoff**

Good luck, Dev B! 🚀
