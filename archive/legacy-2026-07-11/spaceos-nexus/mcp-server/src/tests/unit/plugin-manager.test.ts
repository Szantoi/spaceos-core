import { describe, test, expect, beforeEach } from 'vitest';
import { PluginManager } from '../../plugins/PluginManager';
import { SystemContext } from '../../plugins/PluginTypes';
import { AgentDb } from '../../mcp/AgentDb';
import { SessionManager } from '../../mcp/SessionManager';
import { RbacFilter } from '../../mcp/RbacFilter';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { GuardrailService } from '../../roles/GuardrailService';
import path from 'node:path';

function makeMockSystemContext(): SystemContext {
  return {
    agentDb: {} as any,
    sessionManager: {} as any,
    rbacFilter: {} as any,
    workflowTracker: {} as any,
    guardrailService: {} as any
  };
}

describe('PluginManager', () => {
  let manager: PluginManager;
  let ctx: SystemContext;

  beforeEach(() => {
    ctx = makeMockSystemContext();
    manager = new PluginManager(ctx);
  });

  test('loads a plugin and invokes a tool handler', async () => {
    const entry = path.resolve(__dirname, '../fixtures/plugins/testPlugin.js');
    manager.registerManifest({ name: 'test', version: '1.0.0', entry, className: 'TestPlugin' });

    await manager.loadPlugin('test');
    const status = manager.getPluginStatus();

    expect(status.loaded).toContain('test');

    const result = await manager.invokeTool('test_tool', { foo: 'bar' }, {} as any);
    expect(result.ok).toBe(true);
    expect(result.args).toEqual({ foo: 'bar' });
  });

  test('loads dependencies in correct order', async () => {
    const baseDir = path.resolve(__dirname, '../fixtures/plugins');
    const order: string[] = [];

    // Create two temporary plugins in memory via dynamic module generation (simple approach)
    // We'll create files on disk for this test.
    const pluginAPath = path.join(baseDir, 'pluginA.js');
    const pluginBPath = path.join(baseDir, 'pluginB.js');

    const fs = await import('fs/promises');
    await fs.writeFile(pluginAPath, `
      const { z } = require('zod');
      module.exports = { PluginA: class PluginA { constructor() { global.__pluginOrder = global.__pluginOrder || []; global.__pluginOrder.push('A'); } name='A'; version='1.0.0'; tools=[]; handlers={}; } };
    `);
    await fs.writeFile(pluginBPath, `
      const { z } = require('zod');
      module.exports = { PluginB: class PluginB { constructor() { global.__pluginOrder = global.__pluginOrder || []; global.__pluginOrder.push('B'); } name='B'; version='1.0.0'; tools=[]; handlers={}; } };
    `);

    manager.registerManifest({ name: 'A', version: '1.0.0', entry: pluginAPath, className: 'PluginA' });
    manager.registerManifest({ name: 'B', version: '1.0.0', entry: pluginBPath, className: 'PluginB', dependencies: ['A'] });

    global.__pluginOrder = [];
    await manager.loadPlugin('B');
    expect(global.__pluginOrder).toEqual(['A', 'B']);
  });

  test('optional plugin failure does not throw', async () => {
    const entry = path.resolve(__dirname, '../fixtures/plugins/failingPlugin.js');
    manager.registerManifest({ name: 'failing', version: '1.0.0', entry, className: 'FailingPlugin', critical: false });

    await manager.loadPlugin('failing');
    const status = manager.getPluginStatus();
    expect(status.failed).toContain('failing');
    expect(status.loaded).not.toContain('failing');
  });

  test('calls plugin onError hook when init fails', async () => {
    const entry = path.resolve(__dirname, '../fixtures/plugins/onInitFailPlugin.js');
    manager.registerManifest({ name: 'onInitFail', version: '1.0.0', entry, className: 'OnInitFailPlugin', critical: false });

    // Reset global marker used by the fixture plugin
    (global as any).__pluginErrorHandled = [];

    await manager.loadPlugin('onInitFail');
    const status = manager.getPluginStatus();

    expect(status.failed).toContain('onInitFail');
    expect((global as any).__pluginErrorHandled).toContain('onInit failed');
  });

  test('critical plugin failure throws', async () => {
    const entry = path.resolve(__dirname, '../fixtures/plugins/failingPlugin.js');
    manager.registerManifest({ name: 'failing', version: '1.0.0', entry, className: 'FailingPlugin', critical: true });

    await expect(manager.loadPlugin('failing')).rejects.toThrow();
  });
});
