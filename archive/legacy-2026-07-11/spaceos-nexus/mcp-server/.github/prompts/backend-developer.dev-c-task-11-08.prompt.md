---
role: backend_developer
task: TASK-11-08
epic: EPIC-11
milestone: M02
phase: phase-2
effort: 8h
execution_time: 2026-03-11 to 2026-03-12
priority: P0
---

# Backend Developer Execution Guide — TASK-11-08: Two-Track Routing

## 🎯 Mission

Implement **two-track routing** that restricts agents to either discovery or delivery tools based on session workflow context. Combine RBAC + context middleware to create role-based tool visibility. Delivery agents see FSM-tracked tools; discovery agents see ideation/iteration tools.

**Why this matters:**
- Discovery agents iterate/validate ideas (no FSM constraint)
- Delivery agents execute workflows under FSM state machine (plan→in_progress→submit)
- Agent cannot see tools outside their track (immutable per session)
- Pairs with RBAC to create **dual-layer permission model**

**Success Signal:** Agent logs in with discovery workflow → can ONLY call discovery tools; switching tracks requires new session (immutable).

---

## 📋 Acceptance Criteria (26 AC)

### Track Definition (Functional)

**AC-1:** Track enum defined (DISCOVERY | DELIVERY)
- [ ] Type: `enum WorkflowTrack { DISCOVERY = "discovery", DELIVERY = "delivery" }`
- [ ] Location: `src/mcp/routing/TrackEnums.ts` (new file)
- [ ] Export from: `src/mcp/index.ts`

**AC-2:** Discovery track tools documented
- [ ] Tools: agent_context, search_knowledge_base, list_workflows, validate_input, brainstorm
- [ ] These tools enabled ONLY when workflow_id → track === "discovery"
- [ ] Schema ref: `workflow_definitions.track = "discovery"`

**AC-3:** Delivery track tools documented
- [ ] Tools: bootstrap_agent, submit_workflow, get_workflow_state, list_artifacts, record_result
- [ ] These tools enabled ONLY when workflow_id → track === "delivery"
- [ ] Schema ref: `workflow_definitions.track = "delivery"`

**AC-4:** Shared tools available in both tracks
- [ ] Tools: get_session_context, get_audit_log
- [ ] No restriction: both tracks can access anytime

**AC-5:** Restricted tools not accessible in either track
- [ ] Tools: admin only, internal use (e.g., database bypass tools)
- [ ] Error code: `FORBIDDEN_TOOL`

### Router Implementation (Functional)

**AC-6:** `twoTrackRouter.ts` created at `src/mcp/routing/twoTrackRouter.ts`
- [ ] Class: `TwoTrackRouter`
- [ ] Constructor: `new TwoTrackRouter(agentDb: AgentDb, rbacFilter: RbacFilter)`
- [ ] Exports: `TwoTrackRouter, UnauthorizedTrackAccessError`

**AC-7:** Method `getAvailableTools(session_id: string): Promise<string[]>` implemented
- [ ] Step 1: Query session → WorkflowStateTracker.getState(session_id)
- [ ] Step 2: Get workflow_id from session state
- [ ] Step 3: Query workflow_definitions table → extract track
- [ ] Step 4: Filter tools by track (discovery_tools or delivery_tools)
- [ ] Step 5: Apply RBAC filter (role permissions)
- [ ] Final: Return intersection of (track_tools AND role_permissions)
- [ ] Return type: `string[]` with tool names

**AC-8:** Method `canAccessTool(session_id: string, tool_name: string): Promise<boolean>` implemented
- [ ] Step 1: Get available tools via getAvailableTools()
- [ ] Step 2: Check if tool_name in available_tools
- [ ] Step 3: Return boolean
- [ ] Throws: `UnauthorizedTrackAccessError` if tool not in track (configured via flag)

**AC-9:** Track determination from session
- [ ] Query: Session → workflow_id → workflow_definitions.track
- [ ] Session object: `{ session_id, workflow_id, track, state, role, domain }`
- [ ] If track === "discovery": discovery_tools set
- [ ] If track === "delivery": delivery_tools set

**AC-10:** Tool categorization constants defined
- [ ] Constant: `DISCOVERY_TOOLS = ["agent_context", "search_knowledge_base", "list_workflows", "validate_input", "brainstorm"]`
- [ ] Constant: `DELIVERY_TOOLS = ["bootstrap_agent", "submit_workflow", "get_workflow_state", "list_artifacts", "record_result"]`
- [ ] Constant: `SHARED_TOOLS = ["get_session_context", "get_audit_log"]`
- [ ] Location: `src/mcp/routing/toolCategories.ts`

**AC-11:** Custom error class `UnauthorizedTrackAccessError` created
- [ ] Properties: tool_name, session_id, current_track, required_track
- [ ] Error code: `UNAUTHORIZED_TRACK_ACCESS`
- [ ] Message template: "Tool `{tool_name}` not available in `{current_track}` track. Use: {alternatives}"
- [ ] Location: `src/mcp/routing/errors.ts`

### Filter Integration (Functional)

**AC-12:** RbacFilter refactored to accept optional twoTrackRouter
- [ ] Constructor: `new RbacFilter(agentDb: AgentDb, twoTrackRouter?: TwoTrackRouter)`
- [ ] Method: `hasPermission(tool_name, role_name, session_id?)` signature updated
- [ ] If session_id provided AND twoTrackRouter available:
  - Check track permission first (via twoTrackRouter)
  - Then check role permission (via RbacFilter logic)
  - Return: roleHasPermission AND toolInTrack
- [ ] If session_id not provided: return roleHasPermission (backward compat)

**AC-13:** Tool list endpoint filtered by track + role
- [ ] Endpoint: `GET /mcp/http` (tools/list_tools method)
- [ ] Query params: session_id (in header: `mcp-session-id`)
- [ ] Logic: Filter tools by track + role, merge results
- [ ] Response: `{ tools: [...filtered tool names...] }`

**AC-14:** Unauthorized access attempt returns 403 FORBIDDEN
- [ ] Try to call tool outside track → 403 response
- [ ] Error body: `{ code: "UNAUTHORIZED_TRACK_ACCESS", message: "...", context: { current_track, required_track } }`
- [ ] Logged: tool_name, session_id, role, timestamp in audit_log

**AC-15:** Error logging for track violations
- [ ] Table: audit_log (extended)
- [ ] New columns: attempted_tool, violation_type (e.g., "TRACK_MISMATCH")
- [ ] Query: Find all violations for session via `audit_log` search

### Session Lifecycle & Track Immutability (Functional) — P1 Mitigation

**AC-16:** Session created with immutable track
- [ ] Method: `WorkflowStateTracker.createSession(domain, role, workflow_id)`
- [ ] Track determined from: workflow_id → workflow_definitions.track
- [ ] Stored in: `agent_sessions.track` (new column, NOT NULL)
- [ ] Immutable constraint: track cannot change for session lifetime

**AC-17:** Track immutable AFTER session creation
- [ ] Update logic: track column locked (no UPDATE allowed on track field)
- [ ] Alternative: Validation in code (refuse updateState if track changes requested)
- [ ] Enforce: WorkflowStateTracker.updateState() — never allows track change

**AC-18:** AC-20 Explicit Clarification — P1 Mitigation
- [ ] Once session track is DISCOVERY, agent CANNOT call DELIVERY tools
- [ ] Once session track is DELIVERY, agent CANNOT call DISCOVERY tools
- [ ] Attempt to switch: 403 FORBIDDEN, logged as violation

**AC-19:** Track switch attempt returns 403 + helpful error
- [ ] Error code: `UNAUTHORIZED_TRACK_ACCESS`
- [ ] Error message: "Tool `{tool}` not available in discovery track. Alternative tools: [...]"
- [ ] Context: include current_track + allowed_next_tools (discovery only)

**AC-20:** New session required to switch tracks (P1 Mitigation)
- [ ] Design: Agent cannot mid-session switch from discovery → delivery
- [ ] Solution: End session → create new session with different workflow_id
- [ ] Workflow: Submit current session → new session request → both have audit trail

**AC-21:** Tool availability changes ONLY via FSM state change (delivery) or skill unlock (discovery)
- [ ] Delivery: FSM state change (e.g., TODO → IN_DEV) unlocks new tools for IN_DEV agents
- [ ] Discovery: Skill unlock (validation complete) enables next phase tools
- [ ] Track NEVER changes — only tool availability within track
- [ ] Immutability preserved: track === delivery and state === IN_DEV → locked to DELIVERY tools

**AC-22:** Track immutability tested with multi-session scenario
- [ ] Test: Create discovery session → call discovery tool ✅
- [ ] Test: Same session → try DELIVERY tool → 403 ✅
- [ ] Test: New session (delivery track) → call DELIVERY tool ✅
- [ ] Test: New session (delivery) → try DISCOVERY tool → 403 ✅

### Error Handling (Functional)

**AC-23:** `UnauthorizedTrackAccessError` structured response
- [ ] Fields: code, message, context { tool_name, session_id, current_track, required_track }
- [ ] Serializable: JSON.stringify(error) works (includes all fields)
- [ ] HTTP: maps to 403 FORBIDDEN

**AC-24:** Invalid track in workflow_definitions
- [ ] Error: `InvalidTrackError`
- [ ] Trigger: workflow_id references undefined track (not discovery/delivery)
- [ ] Message: "Workflow {workflow_id} has invalid track: {bad_track}. Valid: [discovery, delivery]"

**AC-25:** Track query failure graceful fallback
- [ ] If AgentDb.findWorkflowDefinition() returns null:
  - Error code: `WORKFLOW_NOT_FOUND`
  - Message: "Workflow {workflow_id} not found. Cannot determine available tools."
  - Action: Return empty tool list (deny-by-default)

**AC-26:** All errors structured with code, message, details
- [ ] Pattern: `{ code, message, details: { ... } }`
- [ ] Logged: error_code, error_message in audit_log
- [ ] LLM-friendly: no stack traces in message

---

## 🔧 Implementation Path (8 hours)

### Phase 1: Setup (1.5 hours)
**Checkpoint 1 (09:00 → 10:30 UTC)**

```typescript
// src/mcp/routing/TrackEnums.ts
export enum WorkflowTrack {
  DISCOVERY = "discovery",
  DELIVERY = "delivery",
}

export type TrackType = WorkflowTrack;
export const VALID_TRACKS = Object.values(WorkflowTrack);

// src/mcp/routing/toolCategories.ts
export const DISCOVERY_TOOLS = [
  "agent_context",
  "search_knowledge_base",
  "list_workflows",
  "validate_input",
  "brainstorm",
];

export const DELIVERY_TOOLS = [
  "bootstrap_agent",
  "submit_workflow",
  "get_workflow_state",
  "list_artifacts",
  "record_result",
];

export const SHARED_TOOLS = [
  "get_session_context",
  "get_audit_log",
];

export const ALL_TOOLS = [...DISCOVERY_TOOLS, ...DELIVERY_TOOLS, ...SHARED_TOOLS];

export function getToolsByTrack(track: WorkflowTrack): string[] {
  if (track === WorkflowTrack.DISCOVERY) {
    return [...DISCOVERY_TOOLS, ...SHARED_TOOLS];
  } else if (track === WorkflowTrack.DELIVERY) {
    return [...DELIVERY_TOOLS, ...SHARED_TOOLS];
  }
  return SHARED_TOOLS; // Fallback: only shared
}

// src/mcp/routing/errors.ts
export class UnauthorizedTrackAccessError extends Error {
  code = "UNAUTHORIZED_TRACK_ACCESS";

  constructor(
    public tool_name: string,
    public session_id: string,
    public current_track: string,
    public required_track: string,
  ) {
    const alternatives = getToolsByTrack(required_track as WorkflowTrack).slice(0, 3);
    super(`Tool \`${tool_name}\` not available in \`${current_track}\` track. Use: ${alternatives.join(", ")}`);
  }

  toJSON() {
    return {
      code: this.code,
      message: this.message,
      context: {
        tool_name: this.tool_name,
        session_id: this.session_id,
        current_track: this.current_track,
        required_track: this.required_track,
      },
    };
  }
}

export class InvalidTrackError extends Error {
  code = "INVALID_TRACK";

  constructor(workflow_id: string, bad_track: string) {
    super(`Workflow ${workflow_id} has invalid track: ${bad_track}. Valid: [discovery, delivery]`);
  }

  toJSON() {
    return { code: this.code, message: this.message };
  }
}

export class WorkflowNotFoundError extends Error {
  code = "WORKFLOW_NOT_FOUND";

  constructor(workflow_id: string) {
    super(`Workflow ${workflow_id} not found. Cannot determine available tools.`);
  }

  toJSON() {
    return { code: this.code, message: this.message };
  }
}
```

**Verification at 10:30 UTC:**
- [ ] TrackEnums.ts compiles, exports enum + type
- [ ] toolCategories.ts has 5 discovery tools + 5 delivery tools
- [ ] getToolsByTrack() returns correct array
- [ ] All 3 error classes defined

### Phase 2: Router Core (2 hours)
**Checkpoint 2 (10:30 → 12:30 UTC)**

```typescript
// src/mcp/routing/twoTrackRouter.ts
import { AgentDb } from "../database/AgentDb";
import { RbacFilter } from "../RbacFilter";
import { WorkflowTrack, VALID_TRACKS } from "./TrackEnums";
import { getToolsByTrack } from "./toolCategories";
import { UnauthorizedTrackAccessError, InvalidTrackError, WorkflowNotFoundError } from "./errors";

export interface SessionWithTrack {
  session_id: string;
  workflow_id: string;
  track: WorkflowTrack;
  state: string;
  role: string;
  domain: string;
}

export class TwoTrackRouter {
  constructor(
    private agentDb: AgentDb,
    private rbacFilter: RbacFilter,
  ) {}

  /**
   * AC-7: Get available tools for a session based on track + role
   */
  async getAvailableTools(session_id: string): Promise<string[]> {
    // Step 1-3: Get session + workflow + track
    const session = await this.getSessionWithTrack(session_id);

    // Step 4: Filter tools by track
    const track_tools = getToolsByTrack(session.track);

    // Step 5: Apply RBAC filter (assuming role-based filtering exists)
    const role_allowed = this.rbacFilter.getAllowedTools(session.role);

    // Step 6: Return intersection
    const available = track_tools.filter(tool => role_allowed.has(tool));
    return available;
  }

  /**
   * AC-8: Check if session can access a specific tool
   */
  async canAccessTool(session_id: string, tool_name: string): Promise<boolean> {
    try {
      const available = await this.getAvailableTools(session_id);
      return available.includes(tool_name);
    } catch (e) {
      // If session not found or track invalid, deny access
      return false;
    }
  }

  /**
   * Internal: Get session with resolved track
   */
  private async getSessionWithTrack(session_id: string): Promise<SessionWithTrack> {
    // Query: session → workflow_id → workflow_definitions.track
    const session = await this.agentDb.getSession(session_id);
    if (!session) {
      throw new WorkflowNotFoundError(session_id);
    }

    const workflow = await this.agentDb.findWorkflowDefinition(session.workflow_id);
    if (!workflow) {
      throw new WorkflowNotFoundError(session.workflow_id);
    }

    if (!VALID_TRACKS.includes(workflow.track)) {
      throw new InvalidTrackError(session.workflow_id, workflow.track);
    }

    return {
      session_id: session.session_id,
      workflow_id: session.workflow_id,
      track: workflow.track as WorkflowTrack,
      state: session.state,
      role: session.role,
      domain: session.domain,
    };
  }

  /**
   * Validate tool access and throw if not allowed (for middleware)
   */
  async validateToolAccess(session_id: string, tool_name: string): Promise<void> {
    const allowed = await this.canAccessTool(session_id, tool_name);
    if (!allowed) {
      const session = await this.getSessionWithTrack(session_id);
      throw new UnauthorizedTrackAccessError(
        tool_name,
        session_id,
        session.track,
        "unknown", // Could determine required track from tool
      );
    }
  }
}

export * from "./errors";
```

**Verification at 12:30 UTC:**
- [ ] class TwoTrackRouter instantiates
- [ ] getAvailableTools() returns array of strings
- [ ] canAccessTool() returns boolean
- [ ] validateToolAccess() throws UnauthorizedTrackAccessError on denied access
- [ ] getSessionWithTrack() queries session + workflow + validates track

### Phase 3: RBAC Integration (2 hours)
**Checkpoint 3 (12:30 → 14:30 UTC) — After lunch**

```typescript
// src/mcp/RbacFilter.ts — REFACTORED
import { TwoTrackRouter } from "./routing/twoTrackRouter";

export class RbacFilter {
  private twoTrackRouter?: TwoTrackRouter;

  constructor(
    private agentDb: AgentDb,
    twoTrackRouter?: TwoTrackRouter,
  ) {
    this.twoTrackRouter = twoTrackRouter;
  }

  /**
   * AC-12: Check permission with optional track validation
   */
  async hasPermission(
    tool_name: string,
    role_name: string,
    session_id?: string,
  ): Promise<boolean> {
    // Step 1: Check role permission (existing logic)
    const roleAllowed = this.getAllowedTools(role_name).has(tool_name);

    if (!roleAllowed) {
      return false;
    }

    // Step 2: If session_id provided, also check track
    if (session_id && this.twoTrackRouter) {
      return await this.twoTrackRouter.canAccessTool(session_id, tool_name);
    }

    return roleAllowed;
  }

  /**
   * Existing logic — no change
   */
  getAllowedTools(role_name: string): Set<string> {
    // Query role_schemas from AgentDb, parse JSON permissions
    // Return Set<string> of allowed tool names
    // ... existing implementation
  }
}

// src/mcp/mcpServer.ts — INTEGRATION
import { TwoTrackRouter } from "./routing/twoTrackRouter";

async function setupMcp() {
  const agentDb = new AgentDb(/* ... */);
  const rbacFilter = new RbacFilter(agentDb);
  const twoTrackRouter = new TwoTrackRouter(agentDb, rbacFilter);

  // Pass router to RbacFilter
  rbacFilter.setTwoTrackRouter(twoTrackRouter); // or update constructor

  // Use in middleware:
  app.use(async (req, res, next) => {
    const sessionId = req.headers["mcp-session-id"] as string;
    const tool = req.body.params.name;

    if (sessionId && tool) {
      try {
        await twoTrackRouter.validateToolAccess(sessionId, tool);
      } catch (e) {
        return res.status(403).json(e.toJSON());
      }
    }

    next();
  });
}
```

**Verification at 14:30 UTC:**
- [ ] RbacFilter.hasPermission() checks role first, then track
- [ ] twoTrackRouter integrated into McpServer
- [ ] Middleware validates track access before tool call
- [ ] 403 error returned on track violation

### Phase 4: Testing + Error Handling (2 hours)
**Checkpoint 4 (14:30 → 16:30 UTC)**

```typescript
// src/tests/unit/twoTrackRouter.test.ts
import { describe, it, expect, beforeEach } from "jest";
import { TwoTrackRouter } from "../../mcp/routing/twoTrackRouter";
import { AgentDb } from "../../database/AgentDb";
import { RbacFilter } from "../../mcp/RbacFilter";
import { WorkflowTrack } from "../../mcp/routing/TrackEnums";

describe("TwoTrackRouter", () => {
  let router: TwoTrackRouter;
  let agentDb: AgentDb;
  let rbacFilter: RbacFilter;

  beforeEach(() => {
    agentDb = new AgentDb(/* mock or test db */);
    rbacFilter = new RbacFilter(agentDb);
    router = new TwoTrackRouter(agentDb, rbacFilter);
  });

  // AC-7: getAvailableTools
  it("should return discovery tools for discovery session", async () => {
    const session_id = "test-discovery-session";
    // Setup: create session with discovery workflow

    const tools = await router.getAvailableTools(session_id);
    expect(tools).toContain("agent_context");
    expect(tools).toContain("search_knowledge_base");
    expect(tools).not.toContain("bootstrap_agent"); // Delivery tool
  });

  it("should return delivery tools for delivery session", async () => {
    const session_id = "test-delivery-session";
    // Setup: create session with delivery workflow

    const tools = await router.getAvailableTools(session_id);
    expect(tools).toContain("bootstrap_agent");
    expect(tools).toContain("submit_workflow");
    expect(tools).not.toContain("agent_context"); // Discovery tool
  });

  // AC-8: canAccessTool
  it("should allow discovery tool in discovery session", async () => {
    const session_id = "test-discovery-session";
    const allowed = await router.canAccessTool(session_id, "agent_context");
    expect(allowed).toBe(true);
  });

  it("should deny delivery tool in discovery session", async () => {
    const session_id = "test-discovery-session";
    const allowed = await router.canAccessTool(session_id, "bootstrap_agent");
    expect(allowed).toBe(false);
  });

  // AC-18/19/23: Error handling
  it("should throw UnauthorizedTrackAccessError on track violation", async () => {
    const session_id = "test-discovery-session";
    expect(() => router.validateToolAccess(session_id, "bootstrap_agent")).rejects.toThrow(
      "UnauthorizedTrackAccessError",
    );
  });

  it("should structure error with code, message, context", async () => {
    try {
      await router.validateToolAccess("session", "bootstrap_agent");
    } catch (e) {
      const json = e.toJSON();
      expect(json.code).toBe("UNAUTHORIZED_TRACK_ACCESS");
      expect(json.message).toBeDefined();
      expect(json.context.current_track).toBeDefined();
    }
  });

  // AC-16/17: Track immutability
  it("should fail when attempting to change session track", async () => {
    const session_id = "test-discovery-session";
    // Attempt to change track mid-session
    expect(() =>
      agentDb.updateSession(session_id, { track: WorkflowTrack.DELIVERY }),
    ).rejects.toThrow();
  });
});

// src/tests/e2e/twoTrackRouter.e2e.test.ts
describe("Two-Track Routing E2E", () => {
  // AC-1/2/3/4: Discovery workflow
  it("should restrict discovery agent to discovery tools only", async () => {
    // 1. Create discovery session
    const session = await createSession("discovery_workflow");
    // 2. Try discovery tool → ✅
    const result = await callTool(session.id, "agent_context");
    expect(result.ok).toBe(true);
    // 3. Try delivery tool → ❌ 403
    expect(() => callTool(session.id, "bootstrap_agent")).rejects.toThrow(403);
  });

  // AC-1/2/3/4: Delivery workflow
  it("should restrict delivery agent to delivery tools only", async () => {
    const session = await createSession("delivery_workflow");
    // ✅ Delivery tool works
    const result = await callTool(session.id, "bootstrap_agent");
    expect(result.ok).toBe(true);
    // ❌ Discovery tool fails
    expect(() => callTool(session.id, "agent_context")).rejects.toThrow(403);
  });

  // AC-20/22: Track immutability
  it("should require new session to switch tracks", async () => {
    const discovery_session = await createSession("discovery_workflow");
    // ❌ Cannot switch to delivery mid-session
    expect(() =>
      callTool(discovery_session.id, "bootstrap_agent"),
    ).rejects.toThrow("UNAUTHORIZED_TRACK_ACCESS");

    // ✅ New session needed
    const delivery_session = await createSession("delivery_workflow");
    const result = await callTool(delivery_session.id, "bootstrap_agent");
    expect(result.ok).toBe(true);
  });

  // AC-4: Shared tools available
  it("should allow shared tools in both tracks", async () => {
    const discovery = await createSession("discovery_workflow");
    const delivery = await createSession("delivery_workflow");

    // ✅ Both can call shared tools
    expect(await callTool(discovery.id, "get_session_context")).toJSON();
    expect(await callTool(delivery.id, "get_session_context")).toJSON();
  });

  // AC-18: Invalid transition attempt
  it("should log track violation with clear context", async () => {
    const session = await createSession("discovery_workflow");
    try {
      await callTool(session.id, "bootstrap_agent");
    } catch (e) {
      // Check error structure
      expect(e.code).toBe("UNAUTHORIZED_TRACK_ACCESS");
      expect(e.context.current_track).toBe("discovery");

      // Check audit log
      const audit = await searchAuditLog({ session_id: session.id });
      expect(audit).toContainEqual({
        tool_name: "bootstrap_agent",
        violation_type: "TRACK_MISMATCH",
      });
    }
  });
});
```

**Verification at 16:30 UTC:**
- [ ] 12+ unit tests passing (95%+ coverage of public methods)
- [ ] 4 E2E tests validating workflows
- [ ] Error structure: code + message + context
- [ ] Audit logging working (track violations logged)

### Phase 5: Integration + Polish (0.5 hours)
**Checkpoint 5 (16:30 → 17:00 UTC)**

- [ ] Update `src/mcp/index.ts` exports (TwoTrackRouter, errors)
- [ ] Update `src/index.ts` server setup (instantiate TwoTrackRouter, pass to RbacFilter)
- [ ] Add to `src/tests/unit/index.ts` test suite
- [ ] Verify all 26 AC met (checklist below)
- [ ] Draft implementation summary

---

## ✅ Acceptance Criteria Verification Checklist

**Phase 2 Completion (16:30 UTC):**

- [ ] AC-1: Track enum defined (DISCOVERY | DELIVERY) ✅
- [ ] AC-2: Discovery tools listed (5 tools) ✅
- [ ] AC-3: Delivery tools listed (5 tools) ✅
- [ ] AC-4: Shared tools available (2 tools) ✅
- [ ] AC-5: Restricted tools documented ✅
- [ ] AC-6: twoTrackRouter.ts created ✅
- [ ] AC-7: getAvailableTools() works (unit test ✅) ✅
- [ ] AC-8: canAccessTool() works (unit test ✅) ✅
- [ ] AC-9: Track determined from session (query ✅) ✅
- [ ] AC-10: Tool categorization constants defined ✅
- [ ] AC-11: UnauthorizedTrackAccessError class created ✅
- [ ] AC-12: RbacFilter refactored + integrated ✅
- [ ] AC-13: Tool list endpoint filtered by track + role ✅
- [ ] AC-14: Unauthorized access → 403 FORBIDDEN ✅
- [ ] AC-15: Track violations logged in audit_log ✅
- [ ] AC-16: Session track immutable ✅
- [ ] AC-17: Track locked after creation ✅
- [ ] AC-18: Discovery ⊥ DELIVERY tools (immutable) ✅
- [ ] AC-19: Error message helpful (alternatives shown) ✅
- [ ] AC-20: New session required to switch tracks ✅
- [ ] AC-21: FSM state unlocks tools (within track) ✅
- [ ] AC-22: Multi-session immutability tested (E2E) ✅
- [ ] AC-23: Error structured (code, message, context) ✅
- [ ] AC-24: InvalidTrackError on bad track ✅
- [ ] AC-25: Graceful fallback (null workflow → empty tools) ✅
- [ ] AC-26: All errors structured properly ✅

**All 26 AC verified → Task complete**

---

## 🎯 Time Budget Breakdown

| Phase | Duration | Checkpoint | Status |
|:------|:---------|:-----------|:------:|
| 1. Setup | 1.5h | 10:30 UTC | Enums + Constants + Errors |
| 2. Router | 2h | 12:30 UTC | Core logic + session tracking |
| 3. RBAC Integration | 2h | 14:30 UTC | RbacFilter + McpServer setup |
| 4. Testing | 2h | 16:30 UTC | 12+ unit + 4 E2E tests |
| 5. Polish | 0.5h | 17:00 UTC | Exports + summary |
| **Total** | **8h** | **17:00 UTC** | ✅ Ready for merge |

---

## 📤 Standup Template for Phase 2 (TASK-11-08)

**Morning (09:00 UTC):**
```
DEV-C-STANDUP-2026-03-11-MORNING.md
===================================
Completed today:
- [x] TrackEnums.ts + toolCategories.ts + errors.ts (1.5h checkpoint)

Planned rest of day:
- TwoTrackRouter core implementation (Phase 2)
- RBAC integration (Phase 3)

Blockers: None currently

Test status: Setting up Jest + Playwright tests

Questions:
- Confirm: AgentDb.findWorkflowDefinition() exists or do I need to add it?
```

**Midday (12:00 UTC):**
```
DEV-C-STANDUP-2026-03-11-MIDDAY.md
==================================
Completed since morning:
- [x] TwoTrackRouter methods (getAvailableTools, canAccessTool)
- [x] getSessionWithTrack() internal resolver

Still on track:
- RBAC integration next (30 min setup)
- Unit tests (1h)

Blockers: None

Test status: 3/12 unit tests passing (setup phase)

Questions: Any guidance on error handling in middleware layer?
```

**Merge Ready:**
```
DEV-C-COMPLETION-TASK-11-08.md
==============================
Task: TASK-11-08 Two-Track Routing
Status: ✅ COMPLETE

Deliverables:
- [x] src/mcp/routing/twoTrackRouter.ts (280 lines)
- [x] src/mcp/routing/TrackEnums.ts (15 lines)
- [x] src/mcp/routing/toolCategories.ts (40 lines)
- [x] src/mcp/routing/errors.ts (60 lines)
- [x] src/tests/unit/twoTrackRouter.test.ts (150 lines, 12 tests)
- [x] src/tests/e2e/twoTrackRouter.e2e.test.ts (120 lines, 4 tests)

AC Summary: 26/26 ✅ All verified
- Discovery/Delivery track immutability ✅
- 403 FORBIDDEN on track violation ✅
- Audit logging for violations ✅
- RBAC + track combined filtering ✅
- Error handling structured ✅

Test Coverage:
- Unit: 95%+ (12 tests, all critical paths)
- E2E: 4 workflows tested (discovery, delivery, shared tools, immutability)

PR: Ready for merge
Branch: feature/task-11-08-two-track-routing

Next: EPIC-11 Phase 3 Phase (EPIC-12 guardrail kicked off)
```

---

## 🚀 Launch Sequence

**Day 1 (TASK-11-08, 09:00-17:00 UTC):**
1. 09:00 UTC — Morning standup + Phase 1 setup (enums, errors, constants)
2. 10:30 UTC — Checkpoint 1: All infrastructure ready
3. 12:00 UTC — Midday standup + TwoTrackRouter core (2h)
4. 12:30 UTC — Checkpoint 2: Router methods complete
5. 14:00 UTC — Lunch + RBAC refactor (2h)
6. 14:30 UTC — Checkpoint 3: Integration complete
7. 15:00 UTC — Unit + E2E testing (2h)
8. 16:30 UTC — Checkpoint 4: All 26 AC verified
9. 17:00 UTC — Polish + summary draft

**Post-standup (18:00 UTC):** Prepare completion report for coordinator review.

---

**Questions or blockers during execution?** Post in `epic_11/dev-c/feedback/` or include in standups.
Coordinator checks: 09:00, 12:00, 18:00 UTC daily.

🎯 **Success Signal:** All 26 AC passing, track immutability proven, error handling structured, merge-ready code.
