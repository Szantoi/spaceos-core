import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import express from 'express';
import request from 'supertest';
import { join } from 'path';
import { mkdirSync, rmSync } from 'fs';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { SessionManager } from '../../mcp/SessionManager';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { createMcpServerRouter } from '../../mcp/mcpServer';
import { DocumentServer } from '../../mcp/DocumentServer';
import { RbacFilter } from '../../mcp/RbacFilter';
import { GuardrailService } from '../../roles/GuardrailService';

describe('EPIC-17 TASK-17-06: Multi-domain E2E', () => {
    const testDir = join(__dirname, '../../test_data/multi-domain-e2e');
    const dbPath = join(testDir, 'metadata.db');

    let app: express.Express;
    let connectionManager: DatabaseConnectionManager;
    let agentDb: AgentDb;
    let sessionManager: SessionManager;
    let workflowTracker: WorkflowStateTracker;

    let engineeringSessionId: string;
    let adminSessionId: string;
    let engineeringTransportSessionId: string;
    let adminTransportSessionId: string;

    beforeEach(async () => {
        mkdirSync(testDir, { recursive: true });

        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();

        sessionManager = new SessionManager(dbPath);
        workflowTracker = new WorkflowStateTracker(':memory:');

        const db = connectionManager.getAdminPool();
        db.exec(`
      INSERT INTO domains (id, name, description) VALUES
        ('eng', 'engineering', 'Engineering domain'),
        ('mgt', 'management', 'Management domain');

      CREATE TABLE IF NOT EXISTS projects (
        id TEXT PRIMARY KEY,
        due_date TEXT
      );

      CREATE TABLE IF NOT EXISTS epics (
        id TEXT PRIMARY KEY,
        project_id TEXT,
        state TEXT
      );

      CREATE TABLE IF NOT EXISTS tasks (
        id TEXT PRIMARY KEY,
        epic_id TEXT,
        title TEXT,
        status TEXT,
        owner TEXT,
        domain TEXT,
        track TEXT,
        description TEXT,
        acceptance_criteria TEXT,
        workflow TEXT,
        template TEXT
      );

      INSERT INTO projects (id, due_date) VALUES ('PROJ-17', '2026-06-01');
      INSERT INTO epics (id, project_id, state) VALUES ('EPIC-17', 'PROJ-17', 'IN_PROGRESS');

      INSERT INTO tasks (id, epic_id, title, status, owner, domain, track, description, acceptance_criteria, workflow, template)
      VALUES
        ('TASK-ENG-1', 'EPIC-17', 'Engineering Task', 'Open', 'eng_owner', 'engineering', 'delivery', 'Eng work', 'AC-ENG', 'wf_eng', 'tpl_eng'),
        ('TASK-MGT-1', 'EPIC-17', 'Management Task', 'Open', 'mgt_owner', 'management', 'delivery', 'Mgt work', 'AC-MGT', 'wf_mgt', 'tpl_mgt');
    `);

        engineeringSessionId = sessionManager.register(
            'backend_developer',
            'engineering',
            'eng-agent',
            'eng-session',
            'eng'
        ).id;

        adminSessionId = sessionManager.register(
            'admin_agent',
            'engineering',
            'admin-agent',
            'admin-session',
            'eng'
        ).id;

        workflowTracker.createSession({
            sessionId: engineeringSessionId,
            domain: 'engineering',
            roleName: 'backend_developer',
            workflowId: 'agile-epic-lifecycle-v1',
            track: 'delivery',
        });

        workflowTracker.createSession({
            sessionId: adminSessionId,
            domain: 'engineering',
            roleName: 'admin_agent',
            workflowId: 'agile-epic-lifecycle-v1',
            track: 'delivery',
        });

        const documentServer = new DocumentServer(testDir, testDir, undefined, agentDb);
        const rbacFilter = new RbacFilter(agentDb);
        const guardrailService = new GuardrailService(testDir);

        app = express();
        app.use(express.json());
        app.use('/mcp', createMcpServerRouter(
            documentServer,
            workflowTracker,
            rbacFilter,
            sessionManager,
            undefined,
            agentDb,
            undefined,
            guardrailService
        ));

        const initialize = async (sessionId: string, activeRole: string) => {
            const initPayload = {
                jsonrpc: '2.0',
                id: `init-${sessionId}`,
                method: 'initialize',
                params: {
                    protocolVersion: '2024-11-05',
                    capabilities: {},
                    clientInfo: { name: 'multi-domain-e2e-client', version: '1.0.0' },
                },
            };

            const initRes = await request(app)
                .post('/mcp/http')
                .set('Accept', 'application/json, text/event-stream')
                .set('x-active-role', activeRole)
                .set('x-session-id', sessionId)
                .send(initPayload);

            expect(initRes.status).toBe(200);
            const transportSessionId = initRes.headers['mcp-session-id'] as string;
            expect(transportSessionId).toBeTruthy();

            const notifRes = await request(app)
                .post('/mcp/http')
                .set('Accept', 'application/json, text/event-stream')
                .set('x-active-role', activeRole)
                .set('mcp-session-id', transportSessionId)
                .send({ jsonrpc: '2.0', method: 'notifications/initialized' });

            expect([200, 202]).toContain(notifRes.status);
            return transportSessionId;
        };

        engineeringTransportSessionId = await initialize(engineeringSessionId, 'backend_developer');
        adminTransportSessionId = await initialize(adminSessionId, 'admin_agent');
    });

    afterEach(() => {
        try {
            agentDb.close();
        } catch {
            // Ignore teardown close errors.
        }
        try {
            sessionManager.close();
        } catch {
            // Ignore teardown close errors.
        }
        try {
            if ((workflowTracker as any)?.db) {
                (workflowTracker as any).db.close();
            }
        } catch {
            // Ignore teardown close errors.
        }
        rmSync(testDir, { recursive: true, force: true });
    });

    it('enforces domain isolation and supports admin domain switch at runtime', async () => {
        const extractJsonPayload = (value: any): any => {
            if (!value) {
                return value;
            }

            if (typeof value === 'string') {
                try {
                    return extractJsonPayload(JSON.parse(value));
                } catch {
                    return value;
                }
            }

            if (Array.isArray(value?.content)) {
                for (const item of value.content) {
                    if (item?.type === 'text' && typeof item.text === 'string') {
                        try {
                            return extractJsonPayload(JSON.parse(item.text));
                        } catch {
                            // Try next content item.
                        }
                    }
                }
            }

            return value;
        };

        const extractData = (response: any) => {
            const payload = response?.data?.data ?? response?.data;
            return extractJsonPayload(payload);
        };

        const normalizeToolResult = (value: any) => {
            if (!value) {
                return value;
            }

            if (typeof value === 'string') {
                const bracketedError = value.match(/^\[([A-Z_]+)\]\s*(.*)$/s);
                if (bracketedError) {
                    const [, code, message] = bracketedError;
                    return {
                        success: false,
                        code,
                        error: {
                            code,
                            message,
                        },
                    };
                }

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
                return { success: false, error: value.error, code: value.code };
            }

            return { success: true, data: value };
        };

        const callTool = async (
            toolName: string,
            args: Record<string, unknown>,
            sessionId: string,
            transportSessionId: string
        ) => {
            const activeRole = sessionId === adminSessionId ? 'admin_agent' : 'backend_developer';
            const payload = {
                jsonrpc: '2.0',
                id: `${toolName}-${sessionId}`,
                method: 'tools/call',
                params: {
                    name: toolName,
                    arguments: { ...args, session_id: sessionId },
                },
            };

            const res = await request(app)
                .post('/mcp/http')
                .set('Accept', 'application/json, text/event-stream')
                .set('x-active-role', activeRole)
                .set('mcp-session-id', transportSessionId)
                .set('x-session-id', sessionId)
                .send(payload);

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

            if (!mcpResponse?.result && typeof res.text === 'string' && res.text.trim().length > 0) {
                try {
                    mcpResponse = JSON.parse(res.text);
                } catch {
                    // Keep fallback to body if plain JSON parsing fails.
                }
            }

            const rawResult = mcpResponse?.result;
            if (!rawResult) {
                return rawResult;
            }

            if (rawResult.structuredContent) {
                return normalizeToolResult(rawResult.structuredContent);
            }

            const textContents = Array.isArray(rawResult?.content)
                ? rawResult.content
                    .filter((item: { type: string; text?: string }) => item?.type === 'text' && typeof item.text === 'string')
                    .map((item: { text: string }) => item.text)
                : [];

            for (const textContent of textContents) {
                try {
                    return normalizeToolResult(JSON.parse(textContent));
                } catch {
                    // Continue and try the next text block.
                }
            }

            if (textContents.length > 0) {
                return normalizeToolResult(textContents[0]);
            }

            return normalizeToolResult(rawResult);
        };

        // Scenario A: engineering agent sees domains and only engineering tasks.
        const availableDomains = await callTool(
            'list_available_domains',
            {},
            engineeringSessionId,
            engineeringTransportSessionId
        );
        const availableDomainsData = extractData(availableDomains);

        expect(availableDomains.success).toBe(true);
        expect(Array.isArray(availableDomainsData?.domains)).toBe(true);
        expect(availableDomainsData.domains.length).toBeGreaterThanOrEqual(2);

        const engineeringTasks = await callTool(
            'list_my_team_tasks',
            { track: 'delivery', status: 'Open' },
            engineeringSessionId,
            engineeringTransportSessionId
        );
        const engineeringTasksData = extractData(engineeringTasks);

        expect(engineeringTasks.success).toBe(true);
        expect(engineeringTasksData.tasks).toHaveLength(1);
        expect(engineeringTasksData.tasks[0].id).toBe('TASK-ENG-1');

        // Scenario C: non-admin cannot switch domain (403/forbidden).
        const forbiddenSwitch = await callTool(
            'switch_domain',
            { domain_name: 'management' },
            engineeringSessionId,
            engineeringTransportSessionId
        );

        expect(forbiddenSwitch.success).toBe(false);
        expect(forbiddenSwitch.code).toBe('FORBIDDEN');

        // Scenario B: admin switches to management and receives management tasks.
        const switchResult = await callTool(
            'switch_domain',
            { domain_name: 'management' },
            adminSessionId,
            adminTransportSessionId
        );
        const switchResultData = extractData(switchResult);

        expect(switchResult.success).toBe(true);
        expect(switchResultData.domain_name).toBe('management');
        expect(switchResultData.domain_id).toBe('mgt');

        const managementTasks = await callTool(
            'list_my_team_tasks',
            { track: 'delivery', status: 'Open' },
            adminSessionId,
            adminTransportSessionId
        );
        const managementTasksData = extractData(managementTasks);

        expect(managementTasks.success).toBe(true);
        expect(managementTasksData.tasks).toHaveLength(1);
        expect(managementTasksData.tasks[0].id).toBe('TASK-MGT-1');

        // Scenario C: no cross-contamination, engineering session still sees engineering only.
        const engineeringTasksAfterAdminSwitch = await callTool(
            'list_my_team_tasks',
            { track: 'delivery', status: 'Open' },
            engineeringSessionId,
            engineeringTransportSessionId
        );
        const engineeringTasksAfterAdminSwitchData = extractData(engineeringTasksAfterAdminSwitch);

        expect(engineeringTasksAfterAdminSwitch.success).toBe(true);
        expect(engineeringTasksAfterAdminSwitchData.tasks).toHaveLength(1);
        expect(engineeringTasksAfterAdminSwitchData.tasks[0].id).toBe('TASK-ENG-1');
    });
});
