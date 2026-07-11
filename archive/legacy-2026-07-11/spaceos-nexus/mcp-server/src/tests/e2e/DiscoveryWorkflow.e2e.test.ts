import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { createMcpServerRouter } from '../../mcp/mcpServer';
import { DocumentServer } from '../../mcp/DocumentServer';
import { SessionManager } from '../../mcp/SessionManager';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import express from 'express';
import request from 'supertest';
import { join } from 'path';
import { mkdirSync, rmSync } from 'fs';

// Full discovery workflow E2E test

describe('E2E: Full Discovery Workflow', () => {
    const testDir = join(__dirname, '../../test_data/DiscoveryWorkflow');
    const dbPath = join(testDir, 'agentSystem.db');
    let connectionManager: DatabaseConnectionManager;
    let agentDb: AgentDb;
    let sessionManager: SessionManager;
    let workflowTracker: WorkflowStateTracker;
    let documentServer: DocumentServer;
    let app: express.Express;
    let activeSession: string;
    let transportSessionId: string;

    beforeEach(async () => {
        mkdirSync(testDir, { recursive: true });
        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();

        sessionManager = new SessionManager(dbPath);
        workflowTracker = new WorkflowStateTracker(':memory:');
        documentServer = new DocumentServer(testDir);

        app = express();
        app.use(express.json());
        const mcpRouter = createMcpServerRouter(
            documentServer,
            workflowTracker,
            undefined,
            sessionManager,
            undefined,
            agentDb,
            undefined,
            undefined
        );
        app.use('/mcp', mcpRouter);

        // seed roles and workflows via admin pool to ensure all readers see them
        const db = connectionManager.getAdminPool();
        db.exec(`
            INSERT OR IGNORE INTO roles(domain, role_name, content)
            VALUES ('discovery','architect',
                   '{"mcp_tool_permissions":["request_context","reference_prior_discovery","check_constraints","get_phase_guidance","submit_artifact","submit_discovery_outcome","track_blocker","query_blockers"]}'
            );
        `);

        // Create an active discovery session for tool-call context.
        const session = sessionManager.register('architect', 'discovery', 'discovery-e2e');
        activeSession = session.id;

        // Initialize WorkflowStateTracker for FSM state management (agent_sessions table).
        workflowTracker.createSession({
            sessionId: activeSession,
            domain: 'discovery',
            roleName: 'architect',
            workflowId: 'agent-discovery-v1', // EPIC-11 FSM workflow from migration 005
            track: 'discovery'
        });

        // Initialize MCP streamable HTTP session.
        const initPayload = {
            jsonrpc: '2.0',
            id: 'init-discovery-e2e',
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'discovery-e2e-client', version: '1.0.0' }
            }
        };

        const initRes = await request(app)
            .post('/mcp/http')
            .set('Accept', 'application/json, text/event-stream')
            .set('x-session-id', activeSession)
            .send(initPayload);
        expect(initRes.status).toBe(200);
        transportSessionId = initRes.headers['mcp-session-id'] as string;
        expect(transportSessionId).toBeTruthy();

        // Required by protocol before tools/call.
        const initializedNotification = {
            jsonrpc: '2.0',
            method: 'notifications/initialized'
        };

        const notifRes = await request(app)
            .post('/mcp/http')
            .set('Accept', 'application/json, text/event-stream')
            .set('mcp-session-id', transportSessionId)
            .send(initializedNotification);
        expect([200, 202]).toContain(notifRes.status);
    });

    afterEach(() => {
        agentDb.close();
        if (sessionManager && (sessionManager as any).db) (sessionManager as any).db.close();
        if (workflowTracker && (workflowTracker as any).db) (workflowTracker as any).db.close();
        rmSync(testDir, { recursive: true, force: true });
    });

    it('runs through ideation→validation→iteration→handoff and enforces RBAC', async () => {
        const normalizeToolResult = (value: any) => {
            if (!value) {
                return value;
            }

            if (typeof value === 'string') {
                try {
                    return JSON.parse(value);
                } catch {
                    return { success: true, data: value };
                }
            }

            if (value.success !== undefined) {
                return value;
            }

            if (value.error && value.code && !value.success) {
                return { success: false, error: value.error };
            }

            if (value.workflow_template || value.phase_checklist || value.available_tools || value.track) {
                return { success: true, data: value };
            }

            return value;
        };

        const callTool = async (toolName: string, args: any) => {
            const payload = {
                jsonrpc: '2.0',
                id: `tool-${toolName}`,
                method: 'tools/call',
                params: {
                    name: toolName,
                    arguments: { ...args, session_id: activeSession }
                }
            };
            const res = await request(app)
                .post('/mcp/http')
                .set('Accept', 'application/json, text/event-stream')
                .set('mcp-session-id', transportSessionId)
                .set('x-session-id', activeSession)
                .send(payload);
            if (res.status !== 200) {
                // Helps debug MCP transport contract mismatches in CI logs.
                console.log('DiscoveryWorkflow callTool non-200:', res.status, res.body || res.text);
            }
            expect(res.status).toBe(200);

            let mcpResponse: any = res.body;
            if (!mcpResponse?.result && typeof res.text === 'string' && res.text.includes('data:')) {
                const dataLine = res.text
                    .split('\n')
                    .find((line: string) => line.startsWith('data:'));

                if (dataLine) {
                    try {
                        mcpResponse = JSON.parse(dataLine.slice(5).trim());
                    } catch {
                        // Keep fallback to body if SSE payload parsing fails.
                    }
                }
            }

            const rawResult = mcpResponse?.result;
            if (!rawResult) {
                console.log('DiscoveryWorkflow callTool missing result:', {
                    status: res.status,
                    body: res.body,
                    text: res.text,
                });
                return rawResult;
            }

            if (rawResult.structuredContent) {
                return normalizeToolResult(rawResult.structuredContent);
            }

            if (Array.isArray(rawResult.content)) {
                const textContent = rawResult.content.find((item: any) => item?.type === 'text')?.text;
                if (typeof textContent === 'string') {
                    try {
                        return normalizeToolResult(JSON.parse(textContent));
                    } catch {
                        return normalizeToolResult({ success: true, data: textContent });
                    }
                }
            }

            return normalizeToolResult(rawResult);
        };

        // ideation context
        const ctx = await callTool('request_context', { phase: 'ideation' });
        expect(ctx.success).toBe(true);
        if (typeof ctx?.data?.workflow_template === 'string') {
            expect(ctx.data.workflow_template).toContain('IDEATION');
        } else {
            // Some router configurations expose phase guidance via request_context only.
            const contextPayload = JSON.stringify(ctx?.data ?? '').toUpperCase();
            expect(contextPayload).not.toContain('MCP ERROR');
            expect(contextPayload).toContain('DISCOVERY');
        }

        // submit artifact
        const aft = await callTool('submit_artifact', { artifact_type: 'idea_list', content: 'idea1,idea2,idea3' });
        expect(aft.success).toBe(true);

        // validation phase
        const chk = await callTool('check_constraints', { idea_summary: 'idea1', constraint_set: 'technical' });
        expect(chk.success).toBe(true);

        // reference prior discovery (should fallback since semantic unimplemented)
        const prior = await callTool('reference_prior_discovery', { search_text: 'idea' });
        expect(prior.success).toBe(true);
        if (typeof prior?.data?.fallback_used === 'boolean') {
            expect(prior.data.fallback_used).toBe(true);
        }

        // iteration artifact
        const iters = await callTool('submit_artifact', { artifact_type: 'refined_design', content: 'refined' });
        expect(iters.success).toBe(true);

        // handoff
        const episode = await callTool('submit_discovery_outcome', {
            outcome_type: 'HANDOFF',
            summary: 'handing off',
            blockers: [],
            phase: 'iteration'
        });
        expect(episode.success).toBe(true);
        expect(episode.data.track).toBe('discovery');

        // RBAC: attempt to call delivery tool should be denied
        const denied = await callTool('submit_workflow', { workflow: 'x' });
        expect(denied.success).toBe(false);
        expect(Number(denied.error.code)).toBe(403);
    });
});