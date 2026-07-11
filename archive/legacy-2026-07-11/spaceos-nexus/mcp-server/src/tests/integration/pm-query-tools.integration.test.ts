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

describe('EPIC-15: PM Query Tools integration', () => {
  const testDir = join(__dirname, '../../test_data/pm-query-tools');
  const dbPath = join(testDir, 'metadata.db');

  let app: express.Express;
  let connectionManager: DatabaseConnectionManager;
  let agentDb: AgentDb;
  let sessionManager: SessionManager;
  let workflowTracker: WorkflowStateTracker;
  let activeSession: string;
  let transportSessionId: string;

  beforeEach(async () => {
    mkdirSync(testDir, { recursive: true });

    connectionManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connectionManager);
    agentDb.initSchema();

    sessionManager = new SessionManager(dbPath);
    workflowTracker = new WorkflowStateTracker(':memory:');

    const db = connectionManager.getAdminPool();
    db.exec(`
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

      INSERT INTO projects (id, due_date) VALUES ('PROJ-15', '2026-04-01');
      INSERT INTO epics (id, project_id, state) VALUES ('EPIC-15', 'PROJ-15', 'IN_PROGRESS');
      INSERT INTO tasks (id, epic_id, title, status, owner, domain, track, description, acceptance_criteria, workflow, template)
      VALUES
        ('TASK-15-API', 'EPIC-15', 'Implement PM query API', 'Open', 'backend_dev', 'engineering', 'delivery', 'Add PM query read APIs', 'Given/When/Then checks', 'delivery_workflow', 'implementation_summary'),
        ('TASK-15-DOCS', 'EPIC-15', 'Write usage guide', 'Done', 'tech_writer', 'engineering', 'delivery', 'Document tool usage', 'Docs reviewed', 'delivery_workflow', 'documentation_template');
    `);

    activeSession = sessionManager.register('backend_developer', 'engineering', 'pm-query-int').id;
    workflowTracker.createSession({
      sessionId: activeSession,
      domain: 'engineering',
      roleName: 'backend_developer',
      workflowId: 'agile-epic-lifecycle-v1',
      track: 'delivery'
    });

    const documentServer = new DocumentServer(testDir, testDir, undefined, agentDb);

    app = express();
    app.use(express.json());
    app.use('/mcp', createMcpServerRouter(documentServer, workflowTracker, undefined, sessionManager, undefined, agentDb));

    const initPayload = {
      jsonrpc: '2.0',
      id: 'init-pm-query-int',
      method: 'initialize',
      params: {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: { name: 'pm-query-int-client', version: '1.0.0' }
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
    try {
      agentDb.close();
    } catch {
      // Ignore teardown close errors.
    }
    try {
      if ((sessionManager as any)?.db) {
        (sessionManager as any).db.close();
      }
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

  it('supports PM query flow over MCP HTTP transport', async () => {
    const normalizeToolResult = (value: any) => {
      if (value && typeof value === 'object' && 'success' in value) {
        return value;
      }

      return {
        success: true,
        data: value,
      };
    };

    const callTool = async (toolName: string, args: Record<string, unknown>) => {
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

      const textContent = rawResult?.content?.find((item: { type: string; text: string }) => item.type === 'text')?.text;
      if (typeof textContent === 'string') {
        try {
          return normalizeToolResult(JSON.parse(textContent));
        } catch {
          return normalizeToolResult({ success: true, data: textContent });
        }
      }

      return normalizeToolResult(rawResult);
    };

    const listResult = await callTool('list_my_team_tasks', { track: 'delivery', status: 'Open' });
    expect(listResult.success).toBe(true);
    expect(Array.isArray(listResult.data.tasks)).toBe(true);
    expect(listResult.data.tasks.length).toBe(1);
    expect(listResult.data.tasks[0].id).toBe('TASK-15-API');

    const taskContext = await callTool('get_task_context', { task_id: 'TASK-15-API' });
    expect(taskContext.success).toBe(true);
    expect(taskContext.data.id).toBe('TASK-15-API');
    expect(taskContext.data.acceptance_criteria).toContain('Given/When/Then');

    const searchResult = await callTool('search_tasks', {
      query: 'query',
      filters: { status: 'Open', track: 'delivery' }
    });
    expect(searchResult.success).toBe(true);
    expect(searchResult.data.total).toBeGreaterThanOrEqual(1);

    const projectState = await callTool('get_project_state', { project_id: 'PROJ-15' });
    expect(projectState.success).toBe(true);
    expect(projectState.data.project_id).toBe('PROJ-15');
    expect(projectState.data.open_tasks_count).toBe(1);
  });
});
