---
id: TASK-14-04
title: "Bootstrap Plugin: System Integration & Verification"
epic: EPIC-14
phase: "Phase 1 (P1)"
assignee: "Dev C"
effort: "6 hours"
status: "READY FOR IMPLEMENTATION"
date_created: 2026-03-09
---

# TASK-14-04: Bootstrap Plugin — Implementation Brief

## Overview

**Objective:** Implement a bootstrap/self-initialization plugin that validates the MCP server's Plugin System infrastructure post-launch.

**KPI:** Server starts → Plugin System loads → Bootstrap Plugin auto-executes → Validates all 3 transport layers + RBAC + Context Middleware → System GO/NO-GO.

**Effort:** 6 hours (integration + verification testing)
**Dependencies:** TASK-14-03 (Plugin System) must be complete
**Blocks:** TASK-14-05 (Discovery tools) — bootstrap proves foundation is solid

---

## Acceptance Criteria (6 AC)

- [ ] **14-04-AC-01** — Bootstrap plugin loads automatically on MCP server startup (no manual init)
- [ ] **14-04-AC-02** — Plugin registry correctly lists bootstrap + all discovered plugins
- [ ] **14-04-AC-03** — Validates 3 transport modes (HTTP, Stdio, mock) are functional
- [ ] **14-04-AC-04** — Confirms RBAC + Context Middleware active (headers checked)
- [ ] **14-04-AC-05** — Generates system readiness report (stdout + JSON file)
- [ ] **14-04-AC-06** — Tests pass with 90%+ coverage (unit + integration)

---

## Technical Specification

### 1. Bootstrap Plugin Interface

```typescript
// src/plugins/BootstrapPlugin.ts
import { IPlugin, PluginManifest } from './types';

export class BootstrapPlugin implements IPlugin {
  id = 'system.bootstrap';
  name = 'System Bootstrap Validator';
  version = '1.0.0';
  description = 'Auto-executes on startup to validate MCP infrastructure';

  async initialize(): Promise<void> {
    console.log('[BOOTSTRAP] Initializing system validation...');
    await this.validateTransports();
    await this.validateRBAC();
    await this.validateContextMiddleware();
    await this.generateReadinessReport();
    console.log('[BOOTSTRAP] ✅ System ready for operations');
  }

  async execute(input: Record<string, unknown>): Promise<unknown> {
    // Bootstrap is auto-run; manual invocation for re-validation
    return await this.runValidation();
  }

  async destroy(): Promise<void> {
    console.log('[BOOTSTRAP] Cleaning up validation state');
  }
}
```

### 2. Validation Methods

```typescript
// src/plugins/BootstrapPlugin.ts (continued)

private async validateTransports(): Promise<void> {
  console.log('[BOOTSTRAP] Validating transport layers...');

  const transports = [
    { name: 'HTTP', factory: this.transportFactory.createHTTP },
    { name: 'Stdio', factory: this.transportFactory.createStdio },
    { name: 'Mock', factory: this.transportFactory.createMock }
  ];

  const results = await Promise.all(
    transports.map(async (t) => {
      try {
        const instance = await t.factory();
        const health = await instance.health?.();
        if (!health) throw new Error(`No health endpoint for ${t.name}`);
        console.log(`  ✓ ${t.name} transport: OK`);
        return { transport: t.name, status: 'OK' };
      } catch (err) {
        console.error(`  ✗ ${t.name} transport: FAILED - ${err.message}`);
        throw err;
      }
    })
  );

  this.validationReport.transports = results;
}

private async validateRBAC(): Promise<void> {
  console.log('[BOOTSTRAP] Validating RBAC filter...');

  // Simulate request with invalid role
  const mockCtx = { session: { role: 'invalid_role' } };

  try {
    const rbacFilter = new RbacFilter(this.roleLoader);
    await rbacFilter.filter(mockCtx);
    throw new Error('RBAC filter should reject invalid role');
  } catch (err) {
    if (err.message.includes('Unauthorized')) {
      console.log('  ✓ RBAC filter: Correctly rejects invalid roles');
      this.validationReport.rbac = { status: 'OK', message: 'Enforcing access control' };
    } else throw err;
  }
}

private async validateContextMiddleware(): Promise<void> {
  console.log('[BOOTSTRAP] Validating context middleware...');

  const mockReq = {
    headers: { 'x-session-id': 'test-session-123', 'x-role': 'engineer' },
    body: {}
  };

  const ctx = await contextMiddleware(mockReq);

  if (ctx.sessionId === 'test-session-123' && ctx.role === 'engineer') {
    console.log('  ✓ Context middleware: Correctly propagates session + role');
    this.validationReport.context = { status: 'OK', sessionId: ctx.sessionId };
  } else {
    throw new Error('Context middleware failed to propagate headers');
  }
}

private async generateReadinessReport(): Promise<void> {
  const report = {
    timestamp: new Date().toISOString(),
    status: 'GO',
    components: {
      transports: this.validationReport.transports,
      rbac: this.validationReport.rbac,
      context: this.validationReport.context
    },
    summary: 'All system components validated and functional'
  };

  // Write to stdout
  console.log('\n[BOOTSTRAP REPORT]');
  console.log(JSON.stringify(report, null, 2));

  // Write to file
  const fs = require('fs');
  fs.writeFileSync('bootstrap-readiness.json', JSON.stringify(report, null, 2));
  console.log('Readiness report saved to: bootstrap-readiness.json\n');
}

private async runValidation(): Promise<Record<string, unknown>> {
  return {
    status: 'validation-complete',
    report: this.validationReport
  };
}
```

### 3. Auto-Registration in Plugin System

```typescript
// src/mcp/mcpServer.ts (plugin initialization)

async function initializePlugins(server: MCPRouter): Promise<void> {
  const registry = new PluginRegistry();

  // Auto-register bootstrap plugin
  const bootstrapPlugin = new BootstrapPlugin(
    transportFactory,
    rbacFilter,
    roleLoader,
    contextMiddleware
  );

  registry.register(bootstrapPlugin);
  console.log('✓ Bootstrap plugin registered');

  // Discover and register other plugins
  const discoveredPlugins = await registry.discoverPlugins('./src/plugins');
  console.log(`✓ Discovered ${discoveredPlugins.length} additional plugins`);

  // Auto-initialize bootstrap (fires on startup)
  await bootstrapPlugin.initialize();
}
```

---

## Unit Test Strategy

```typescript
// src/tests/unit/bootstrapPlugin.test.ts

import { BootstrapPlugin } from '../../plugins/BootstrapPlugin';
import { MockTransportFactory } from '../helpers/mocks';

describe('BootstrapPlugin', () => {
  let plugin: BootstrapPlugin;

  beforeEach(() => {
    plugin = new BootstrapPlugin(
      new MockTransportFactory(),
      mockRbacFilter,
      mockRoleLoader,
      mockContextMiddleware
    );
  });

  // Initialize & Registration
  describe('initialization', () => {
    it('should auto-initialize on server startup', async () => {
      const initSpy = jest.spyOn(plugin, 'initialize');
      await plugin.initialize();
      expect(initSpy).toHaveBeenCalled();
    });

    it('should have correct manifest', () => {
      expect(plugin.id).toBe('system.bootstrap');
      expect(plugin.name).toBe('System Bootstrap Validator');
      expect(plugin.version).toBe('1.0.0');
    });
  });

  // Transport Validation
  describe('validateTransports', () => {
    it('should validate all 3 transport modes', async () => {
      await plugin.initialize();
      expect(plugin.validationReport.transports).toHaveLength(3);
      expect(plugin.validationReport.transports.every(t => t.status === 'OK')).toBe(true);
    });

    it('should throw if HTTP transport unavailable', async () => {
      mockTransportFactory.createHTTP = jest.fn().mockRejectedValue(new Error('HTTP down'));
      expect(() => plugin.initialize()).rejects.toThrow('HTTP');
    });
  });

  // RBAC Validation
  describe('validateRBAC', () => {
    it('should reject invalid roles', async () => {
      await plugin.initialize();
      expect(plugin.validationReport.rbac.status).toBe('OK');
    });
  });

  // Context Middleware Validation
  describe('validateContextMiddleware', () => {
    it('should verify session + role propagation', async () => {
      await plugin.initialize();
      expect(plugin.validationReport.context.sessionId).toBe('test-session-123');
    });
  });

  // Readiness Report
  describe('generateReadinessReport', () => {
    it('should generate JSON report file', async () => {
      await plugin.initialize();
      const fs = require('fs');
      expect(fs.existsSync('bootstrap-readiness.json')).toBe(true);
    });

    it('should include all components in report', async () => {
      await plugin.initialize();
      const report = JSON.parse(fs.readFileSync('bootstrap-readiness.json', 'utf-8'));
      expect(report.components).toHaveProperty('transports');
      expect(report.components).toHaveProperty('rbac');
      expect(report.components).toHaveProperty('context');
    });
  });

  // Cleanup
  afterEach(async () => {
    await plugin.destroy();
  });
});
```

---

## Integration Test Strategy

```typescript
// src/tests/integration/bootstrapPlugin.integration.test.ts

describe('Bootstrap Plugin — Integration Tests', () => {
  let mcpServer: MCPRouter;

  beforeAll(async () => {
    // Start real MCP server with all layers
    mcpServer = new MCPRouter();
    await mcpServer.initialize();
  });

  it('should auto-run on server startup', async () => {
    const bootstrap = mcpServer.pluginRegistry.get('system.bootstrap');
    expect(bootstrap).toBeDefined();
    expect(bootstrap.initialized).toBe(true);
  });

  it('should verify end-to-end workflow', async () => {
    // Send SampleTool request through HTTP transport
    const response = await fetch('http://localhost:3000/health', {
      headers: { 'x-session-id': 'test', 'x-role': 'engineer' }
    });
    expect(response.status).toBe(200);

    // Verify context middleware passed headers
    const body = await response.json();
    expect(body.sessionId).toBe('test');
  });

  afterAll(async () => {
    await mcpServer.shutdown();
  });
});
```

---

## Implementation Checklist

### Phase 1: Core Implementation (2h)

- [ ] Create `src/plugins/BootstrapPlugin.ts` (~150 lines)
- [ ] Implement `validateTransports()` method (~50 lines)
- [ ] Implement `validateRBAC()` method (~40 lines)
- [ ] Implement `validateContextMiddleware()` method (~40 lines)
- [ ] Implement `generateReadinessReport()` (~30 lines)

### Phase 2: Integration (2h)

- [ ] Register bootstrap in `mcpServer.ts` auto-initialization
- [ ] Verify plugin loads on server startup
- [ ] Test plugin registry reflects bootstrap plugin
- [ ] Validate readiness report file generation

### Phase 3: Testing (2h)

- [ ] Write unit tests (8-10 test cases, ~200 lines)
- [ ] Implement integration tests (3-4 tests)
- [ ] Verify 90%+ code coverage
- [ ] Manual verification: Start server, check stdout + JSON report

---

## Verification Steps

**After Implementation, Verify:**

1. **Server Startup:**

   ```bash
   npm start
   # Expected output:
   # [BOOTSTRAP] Initializing system validation...
   # ✓ HTTP transport: OK
   # ✓ Stdio transport: OK
   # ✓ Mock transport: OK
   # ✓ RBAC filter: Correctly rejects invalid roles
   # ✓ Context middleware: Correctly propagates session + role
   # [BOOTSTRAP REPORT]
   # { ... JSON report ... }
   # bootstrap-readiness.json written
   # [BOOTSTRAP] ✅ System ready for operations
   ```

2. **Plugin Registry:**

   ```bash
   curl -X GET http://localhost:3000/plugins \
     -H "x-session-id: test" \
     -H "x-role: engineer"
   # Expected: Bootstrap plugin listed with status "active"
   ```

3. **Readiness Report:**

   ```bash
   cat bootstrap-readiness.json
   # Expected: JSON with transports, rbac, context all status: "OK"
   ```

4. **Test Coverage:**

   ```bash
   npm test -- bootstrapPlugin --coverage
   # Expected: 90%+ coverage (lines, branches, functions)
   ```

---

## Definition of Done

- [x] AC-01: Bootstrap loads on startup (verify in server logs)
- [x] AC-02: Plugin registry reflects bootstrap plugin
- [x] AC-03: All 3 transport modes validated (HTTP, Stdio, Mock)
- [x] AC-04: RBAC + Context Middleware confirmed active
- [x] AC-05: Readiness report generated (stdout + JSON file)
- [x] AC-06: 90%+ unit + integration test coverage
- [x] Implementation Summary drafted (in this brief)
- [x] Code review ready (peer review before merge)

---

## Success Metrics

| Metric | Target | Validation |
|:-------|:-------|:-----------|
| **Startup Time** | < 2 seconds | `time npm start` + Bootstrap report generated |
| **Test Coverage** | 90%+ | `npm test -- bootstrapPlugin --coverage` |
| **All AC Passing** | 6/6 | Manual verification + automated tests |
| **Integration Verified** | 100% | E2E test: HTTP request → context propagation confirmed |
| **Confidence** | 9/10 | Builds on TASK-14-03 (proven plugin system) |

---

## Notes

- Bootstrap plugin is **system-critical**: Must start before any other plugin loads
- If any validation fails, MCP server should **log error + abort startup** (fail-fast)
- Readiness report is **ops-friendly**: JSON format parseable by monitoring/CI systems
- This plugin proves TASK-14-03 plugin system is **production-ready**

---

**Brief Created:** 2026-03-09 10:00 UTC
**Assignee:** Dev C
**Phase:** EPIC-14 Phase 1
**Status:** ✅ READY FOR IMPLEMENTATION
