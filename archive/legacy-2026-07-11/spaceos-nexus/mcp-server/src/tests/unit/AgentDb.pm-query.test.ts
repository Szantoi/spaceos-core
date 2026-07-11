import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { join } from 'path';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { tmpdir } from 'os';

describe('EPIC-15: AgentDb PM query methods', () => {
  let tempDir: string;
  let dbPath: string;
  let manager: DatabaseConnectionManager;
  let agentDb: AgentDb;

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), 'agentdb-pm-'));
    dbPath = join(tempDir, 'metadata.db');
    manager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(manager);
    agentDb.initSchema();
  });

  afterEach(() => {
    try {
      agentDb.close();
    } catch {
      // Ignore close errors in test teardown.
    }

    try {
      if (existsSync(tempDir)) {
        rmSync(tempDir, { recursive: true, force: true });
      }
    } catch {
      // Ignore cleanup errors.
    }
  });

  it('getProjectState returns aggregated values from PM tables', () => {
    const db = agentDb.getRawDatabase();
    db.exec(`
      CREATE TABLE projects (id TEXT PRIMARY KEY, due_date TEXT);
      CREATE TABLE epics (id TEXT PRIMARY KEY, project_id TEXT, state TEXT);
      CREATE TABLE tasks (
        id TEXT PRIMARY KEY,
        epic_id TEXT,
        title TEXT,
        status TEXT,
        owner TEXT
      );

      INSERT INTO projects (id, due_date) VALUES ('PROJ-1', '2026-04-01');
      INSERT INTO epics (id, project_id, state) VALUES ('EPIC-1', 'PROJ-1', 'IN_PROGRESS');
      INSERT INTO tasks (id, epic_id, title, status, owner) VALUES
        ('TASK-1', 'EPIC-1', 'Do A', 'Open', 'alice'),
        ('TASK-2', 'EPIC-1', 'Do B', 'Done', 'bob');
    `);

    const state = agentDb.getProjectState('PROJ-1');
    expect(state).toBeTruthy();
    expect(state?.project_id).toBe('PROJ-1');
    expect(state?.milestone).toBe('EPIC-1');
    expect(state?.open_tasks_count).toBe(1);
    expect(state?.due_date).toBe('2026-04-01');
  });

  it('listPmTasks supports query and status filtering', () => {
    const db = agentDb.getRawDatabase();
    db.exec(`
      CREATE TABLE tasks (
        id TEXT PRIMARY KEY,
        title TEXT,
        description TEXT,
        status TEXT,
        owner TEXT
      );

      INSERT INTO tasks (id, title, description, status, owner) VALUES
        ('TASK-10', 'Fix cache issue', 'Investigate cache miss path', 'Open', 'alice'),
        ('TASK-11', 'Write docs', 'Delivery notes', 'Done', 'bob');
    `);

    const rows = agentDb.listPmTasks({ query: 'cache', status: 'Open', limit: 10 });
    expect(rows).toHaveLength(1);
    expect(rows[0].id).toBe('TASK-10');
    expect(rows[0].assigned_to).toBe('alice');
  });

  it('getTaskContext returns detailed task projection when table exists', () => {
    const db = agentDb.getRawDatabase();
    db.exec(`
      CREATE TABLE tasks (
        id TEXT PRIMARY KEY,
        title TEXT,
        status TEXT,
        assigned_to TEXT,
        domain TEXT,
        track TEXT,
        description TEXT,
        acceptance_criteria TEXT,
        workflow TEXT,
        template TEXT
      );

      INSERT INTO tasks (
        id, title, status, assigned_to, domain, track, description, acceptance_criteria, workflow, template
      ) VALUES (
        'TASK-CTX',
        'Implement query tool',
        'Open',
        'backend_dev',
        'engineering',
        'delivery',
        'Need PM query read model',
        'Given/When/Then AC',
        'delivery_workflow',
        'implementation_summary'
      );
    `);

    const task = agentDb.getTaskContext('TASK-CTX');
    expect(task).toBeTruthy();
    expect(task?.title).toBe('Implement query tool');
    expect(task?.acceptance_criteria).toContain('Given/When/Then');
    expect(task?.workflow).toBe('delivery_workflow');
    expect(task?.template).toBe('implementation_summary');
  });
});
