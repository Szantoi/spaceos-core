import 'reflect-metadata';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { PluginManager } from '../../plugins/PluginManager';
import { SystemContext } from '../../plugins/PluginTypes';
import * as path from 'path';
import { v4 as uuidv4 } from 'uuid';

describe('BootstrapPlugin Integration', () => {
    let pluginManager: PluginManager;
    let mockContext: SystemContext;

    beforeEach(() => {
        // Mock SystemContext services
        mockContext = {
            agentDb: {
                getRolesByDomain: vi.fn().mockReturnValue([
                    { role_name: 'framer', domain: 'discovery' }
                ]),
                getWorkflowsByRole: vi.fn().mockReturnValue([
                    { workflow_type: 'discovery-agile', content: '{}' }
                ]),
                getTemplatesByRole: vi.fn().mockReturnValue([
                    { template_name: 'prd-template', content: 'PRD context' }
                ])
            } as any,
            sessionManager: {
                get: vi.fn().mockReturnValue(null),
                register: vi.fn().mockImplementation((role, domain, agentId, sessionId) => ({
                    session_id: sessionId,
                    agent_id: agentId,
                    role,
                    domain
                }))
            } as any,
            rbacFilter: {
                filterTools: vi.fn((tools) => Promise.resolve(tools)),
                getAllowedTools: vi.fn().mockReturnValue(new Set(['tool1', 'tool2']))
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                updateState: vi.fn(),
                createSession: vi.fn()
            } as any,
            guardrailService: {
                checkCompliance: vi.fn()
            } as any
        };

        pluginManager = new PluginManager(mockContext);
    });

    it('should load the bootstrap plugin successfully', async () => {
        const pluginPath = path.resolve(__dirname, '../../mcp/tools/bootstrap.ts');
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: pluginPath,
            className: 'BootstrapPlugin'
        });
        await pluginManager.loadPlugin('bootstrap', true);

        const loadedModules = pluginManager.getLoadedPluginModules();
        expect(loadedModules.length).toBe(1);
        expect(loadedModules[0].name).toBe('bootstrap');
    });

    it('should execute bootstrap_agent and return complete context (AC-01 to AC-09)', async () => {
        const pluginPath = path.resolve(__dirname, '../../mcp/tools/bootstrap.ts');
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: pluginPath,
            className: 'BootstrapPlugin'
        });
        await pluginManager.loadPlugin('bootstrap', true);
        const plugin = pluginManager.getLoadedPluginModules()[0];

        const args = {
            agentId: uuidv4(),
            discoveryPhase: 'discovery'
        };

        const result = await plugin.handlers['bootstrap_agent'](args, {} as any);

        expect(result.success).toBe(true);
        expect(result.data.agentContext).toBeDefined();
        expect(result.data.agentContext.agentId).toBe(args.agentId);
        expect(result.data.agentContext.sessionId).toBeDefined();
        expect(result.data.agentContext.roles).toContain('framer');
        expect(result.data.agentContext.permissions).toContain('tool1');
        expect(result.data.agentContext.workflows.length).toBeGreaterThan(0);
        expect(result.data.agentContext.templates.length).toBeGreaterThan(0);
        expect(result.data.agentContext.schemaVersion).toBe('1.0');
    });

    it('should recover an existing session if sessionId is provided (AC-10)', async () => {
        const sessionId = uuidv4();
        const agentId = uuidv4();

        // Mock existing session
        (mockContext.sessionManager.get as any).mockReturnValue({
            session_id: sessionId,
            agent_id: agentId,
            role: 'framer',
            domain: 'discovery'
        });

        const pluginPath = path.resolve(__dirname, '../../mcp/tools/bootstrap.ts');
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: pluginPath,
            className: 'BootstrapPlugin'
        });
        await pluginManager.loadPlugin('bootstrap', true);
        const plugin = pluginManager.getLoadedPluginModules()[0];

        const args = {
            agentId,
            sessionId,
            discoveryPhase: 'discovery'
        };

        const result = await plugin.handlers['bootstrap_agent'](args, {} as any);

        expect(result.success).toBe(true);
        expect(result.data.agentContext.sessionId).toBe(sessionId);
        expect(mockContext.sessionManager.register).not.toHaveBeenCalled();
    });

    it('should create a new session if provided sessionId does not exist (AC-09)', async () => {
        const sessionId = uuidv4();
        const agentId = uuidv4();

        const pluginPath = path.resolve(__dirname, '../../mcp/tools/bootstrap.ts');
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: pluginPath,
            className: 'BootstrapPlugin'
        });
        await pluginManager.loadPlugin('bootstrap', true);
        const plugin = pluginManager.getLoadedPluginModules()[0];

        const args = {
            agentId,
            sessionId,
            discoveryPhase: 'discovery'
        };

        const result = await plugin.handlers['bootstrap_agent'](args, {} as any);

        expect(result.success).toBe(true);
        expect(result.data.agentContext.sessionId).toBe(sessionId);
        expect(mockContext.sessionManager.register).toHaveBeenCalled();
    });

    it('should return error if no roles found for phase (AC-13)', async () => {
        (mockContext.agentDb.getRolesByDomain as any).mockReturnValue([]);

        const pluginPath = path.resolve(__dirname, '../../mcp/tools/bootstrap.ts');
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: pluginPath,
            className: 'BootstrapPlugin'
        });
        await pluginManager.loadPlugin('bootstrap', true);
        const plugin = pluginManager.getLoadedPluginModules()[0];

        const args = {
            agentId: uuidv4(),
            discoveryPhase: 'discovery'
        };

        const result = await plugin.handlers['bootstrap_agent'](args, {} as any);

        expect(result.success).toBe(false);
        expect(result.message).toContain('No roles found');
    });
});
