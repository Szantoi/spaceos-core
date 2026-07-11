import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import type { AgentDb, RoleRow, WorkflowRow, TemplateRow, RunbookRow, StandardRow } from '../../mcp/AgentDb';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { DocumentServer } from '../../mcp/DocumentServer';
import { ResourceTracker } from '../../metadata/ResourceTracker';

// Helper to create a temporary directory for each test (use system temp)
function makeTempDir(): string {
    return fs.mkdtempSync(path.join(os.tmpdir(), 'ds-test-'));
}

describe('DocumentServer.getRoleContext', () => {
    let tmp: string;
    let ds: DocumentServer;

    beforeEach(() => {
        tmp = makeTempDir();
        // use a separate metadata.db inside the temp folder so ResourceTracker can construct
        const dbPath = path.join(tmp, 'metadata.db');
        ds = new DocumentServer(tmp, tmp, new ResourceTracker(dbPath));
    });

// ─────────────────────────────────────────────────────────────────────────────
// EPIC-16: DB-first unit tests for DocumentServer
// ─────────────────────────────────────────────────────────────────────────────
/** Creates a minimal AgentDb stub using vi.fn(); all methods return null/[] by default. */
function makeAgentDbStub(overrides: Partial<Record<keyof AgentDb, any>> = {}): AgentDb {
    return {
        getRole: vi.fn().mockReturnValue(null),
        getWorkflow: vi.fn().mockReturnValue(null),
        getTemplate: vi.fn().mockReturnValue(null),
        getTemplatesByRole: vi.fn().mockReturnValue([]),
        getRunbook: vi.fn().mockReturnValue(null),
        getStandard: vi.fn().mockReturnValue(null),
        listDomains: vi.fn().mockReturnValue([]),
        listRoleNames: vi.fn().mockReturnValue([]),
        ...overrides,
    } as unknown as AgentDb;
}

function makeRow<T extends object>(base: T): T { return base; }

describe('EPIC-16: DocumentServer DB-first — getRole', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('returns DB content when AgentDb has record — file is absent', () => {
        const db = makeAgentDbStub({
            getRole: vi.fn().mockReturnValue(
                makeRow<RoleRow>({ id: 1, domain: 'eng', role_name: 'dev', content: 'DB_ROLE_CONTENT', created_at: '', last_updated: '' })
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getRole('eng', 'dev')).toBe('DB_ROLE_CONTENT');
    });

    it('falls back to file when DB returns null', () => {
        const db = makeAgentDbStub({ getRole: vi.fn().mockReturnValue(null) });
        const roleDir = path.join(tmp, 'roles', 'eng', 'dev');
        fs.mkdirSync(roleDir, { recursive: true });
        fs.writeFileSync(path.join(roleDir, 'dev.role.md'), 'FILE_ROLE_CONTENT');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getRole('eng', 'dev')).toBe('FILE_ROLE_CONTENT');
    });

    it('DB content takes precedence when both DB and file exist', () => {
        const db = makeAgentDbStub({
            getRole: vi.fn().mockReturnValue(
                makeRow<RoleRow>({ id: 1, domain: 'eng', role_name: 'dev', content: 'DB_WINS', created_at: '', last_updated: '' })
            ),
        });
        const roleDir = path.join(tmp, 'roles', 'eng', 'dev');
        fs.mkdirSync(roleDir, { recursive: true });
        fs.writeFileSync(path.join(roleDir, 'dev.role.md'), 'FILE_CONTENT');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getRole('eng', 'dev')).toBe('DB_WINS');
    });
});

describe('EPIC-16: DocumentServer DB-first — getWorkflow', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('uses DB record for default workflow (type omitted)', () => {
        const db = makeAgentDbStub({
            getWorkflow: vi.fn().mockReturnValue(
                makeRow<WorkflowRow>({ id: 1, domain: 'eng', role_name: 'dev', workflow_type: 'dev', content: 'DB_WORKFLOW', created_at: '', last_updated: '' })
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getWorkflow('eng', 'dev')).toBe('DB_WORKFLOW');
        // Verify correct workflowType key passed: for 'default', expects role name
        expect((db.getWorkflow as ReturnType<typeof vi.fn>).mock.calls[0]).toEqual(['eng', 'dev', 'dev']);
    });

    it('uses DB record for named workflow type', () => {
        const db = makeAgentDbStub({
            getWorkflow: vi.fn().mockReturnValue(
                makeRow<WorkflowRow>({ id: 2, domain: 'eng', role_name: 'dev', workflow_type: 'dev_multi', content: 'DB_MULTI_WORKFLOW', created_at: '', last_updated: '' })
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getWorkflow('eng', 'dev', 'multi')).toBe('DB_MULTI_WORKFLOW');
        expect((db.getWorkflow as ReturnType<typeof vi.fn>).mock.calls[0]).toEqual(['eng', 'dev', 'dev_multi']);
    });

    it('falls back to file when DB returns null', () => {
        const db = makeAgentDbStub({ getWorkflow: vi.fn().mockReturnValue(null) });
        const wfDir = path.join(tmp, 'roles', 'eng', 'dev', 'workflows');
        fs.mkdirSync(wfDir, { recursive: true });
        fs.writeFileSync(path.join(wfDir, 'dev.workflow.md'), 'FILE_WORKFLOW');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getWorkflow('eng', 'dev')).toBe('FILE_WORKFLOW');
    });
});

describe('EPIC-16: DocumentServer DB-first — getTemplate', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('returns DB template content', () => {
        const db = makeAgentDbStub({
            getTemplate: vi.fn().mockReturnValue(
                makeRow<TemplateRow>({ id: 1, domain: 'eng', role_name: 'dev', template_name: 'impl_report', content: 'DB_TEMPLATE', created_at: '', last_updated: '' })
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getTemplate('eng', 'dev', 'impl_report')).toBe('DB_TEMPLATE');
    });

    it('falls back to file when DB returns null', () => {
        const db = makeAgentDbStub({ getTemplate: vi.fn().mockReturnValue(null) });
        const tplDir = path.join(tmp, 'roles', 'eng', 'dev', 'templates');
        fs.mkdirSync(tplDir, { recursive: true });
        fs.writeFileSync(path.join(tplDir, 'impl_report.template.md'), 'FILE_TEMPLATE');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getTemplate('eng', 'dev', 'impl_report')).toBe('FILE_TEMPLATE');
    });
});

describe('EPIC-16: DocumentServer DB-first — listTemplates', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('returns DB template names when DB has records', () => {
        const db = makeAgentDbStub({
            getTemplatesByRole: vi.fn().mockReturnValue([
                makeRow<TemplateRow>({ id: 1, domain: 'eng', role_name: 'dev', template_name: 'tpl_a', content: '', created_at: '', last_updated: '' }),
                makeRow<TemplateRow>({ id: 2, domain: 'eng', role_name: 'dev', template_name: 'tpl_b', content: '', created_at: '', last_updated: '' }),
            ]),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listTemplates('eng', 'dev')).toEqual(['tpl_a', 'tpl_b']);
    });

    it('falls back to file-system names when DB returns empty', () => {
        const db = makeAgentDbStub({ getTemplatesByRole: vi.fn().mockReturnValue([]) });
        const tplDir = path.join(tmp, 'roles', 'eng', 'dev', 'templates');
        fs.mkdirSync(tplDir, { recursive: true });
        fs.writeFileSync(path.join(tplDir, 'file_tpl.template.md'), '');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listTemplates('eng', 'dev')).toEqual(['file_tpl']);
    });
});

describe('EPIC-16: DocumentServer DB-first — getCore', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('returns runbook from DB when doc=runbook and domain+role given', () => {
        const db = makeAgentDbStub({
            getRunbook: vi.fn().mockReturnValue(
                makeRow<RunbookRow>({ id: 1, domain: 'eng', role_name: 'dev', content: 'DB_RUNBOOK', created_at: '', last_updated: '' })
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getCore('runbook', 'eng', 'dev')).toBe('DB_RUNBOOK');
    });

    it('returns standard from DB when doc is not runbook', () => {
        const db = makeAgentDbStub({
            getStandard: vi.fn().mockImplementation((id: string) =>
                id === 'constraints'
                    ? makeRow<StandardRow>({ id: '1', std_id: 'constraints', content: 'DB_CONSTRAINTS', created_at: '', last_updated: '' })
                    : null
            ),
        });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getCore('constraints')).toBe('DB_CONSTRAINTS');
    });

    it('falls back to file when runbook not in DB', () => {
        const db = makeAgentDbStub({ getRunbook: vi.fn().mockReturnValue(null) });
        const roleDir = path.join(tmp, 'roles', 'eng', 'dev');
        fs.mkdirSync(roleDir, { recursive: true });
        fs.writeFileSync(path.join(roleDir, 'dev.runbook.md'), 'FILE_RUNBOOK');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getCore('runbook', 'eng', 'dev')).toBe('FILE_RUNBOOK');
    });

    it('falls back to standards file when standard not in DB', () => {
        const db = makeAgentDbStub({ getStandard: vi.fn().mockReturnValue(null) });
        const stdDir = path.join(tmp, 'standards');
        fs.mkdirSync(stdDir, { recursive: true });
        fs.writeFileSync(path.join(stdDir, 'constraints.md'), 'FILE_CONSTRAINTS');
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.getCore('constraints')).toBe('FILE_CONSTRAINTS');
    });
});

describe('EPIC-16: DocumentServer DB-first — listDomains & listRoles', () => {
    let tmp: string;

    beforeEach(() => { tmp = makeTempDir(); });
    afterEach(() => { try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (_e) { } });

    it('listDomains returns DB list when non-empty', () => {
        const db = makeAgentDbStub({ listDomains: vi.fn().mockReturnValue(['domain_a', 'domain_b']) });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listDomains()).toEqual(['domain_a', 'domain_b']);
    });

    it('listDomains falls back to file-system when DB empty', () => {
        const db = makeAgentDbStub({ listDomains: vi.fn().mockReturnValue([]) });
        fs.mkdirSync(path.join(tmp, 'roles', 'fs_domain'), { recursive: true });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listDomains()).toContain('fs_domain');
    });

    it('listRoles returns DB list when non-empty', () => {
        const db = makeAgentDbStub({ listRoleNames: vi.fn().mockReturnValue(['role_x', 'role_y']) });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listRoles('domain_a')).toEqual(['role_x', 'role_y']);
    });

    it('listRoles falls back to file-system when DB empty', () => {
        const db = makeAgentDbStub({ listRoleNames: vi.fn().mockReturnValue([]) });
        fs.mkdirSync(path.join(tmp, 'roles', 'domain_a', 'fs_role'), { recursive: true });
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')), db);
        expect(ds.listRoles('domain_a')).toContain('fs_role');
    });

    it('DocumentServer without agentDb still uses file-system (no regression)', () => {
        const ds = new DocumentServer(tmp, tmp, new ResourceTracker(path.join(tmp, 'meta.db')));
        fs.mkdirSync(path.join(tmp, 'roles', 'd1', 'r1'), { recursive: true });
        fs.writeFileSync(path.join(tmp, 'roles', 'd1', 'r1', 'r1.role.md'), 'FS_ONLY');
        expect(ds.getRole('d1', 'r1')).toBe('FS_ONLY');
        expect(ds.listDomains()).toContain('d1');
        expect(ds.listRoles('d1')).toContain('r1');
    });
});

    afterEach(() => {
        try {
            fs.rmSync(tmp, { recursive: true, force: true });
        } catch (_e) { }
    });

    it('returns full context when all files exist', () => {
        const roleDir = path.join(tmp, 'roles', 'test', 'tester');
        fs.mkdirSync(roleDir, { recursive: true });
        fs.writeFileSync(path.join(roleDir, 'tester.role.md'), 'role content');
        fs.writeFileSync(path.join(roleDir, 'tester.schema.yaml'), 'schema: yep');
        fs.writeFileSync(path.join(roleDir, 'tester.runbook.md'), 'runbook content');

        const out = ds.getRoleContext('test', 'tester');
        expect(out).toContain('## Role');
        expect(out).toContain('role content');
        expect(out).toContain('## Schema');
        expect(out).toContain('schema: yep');
        expect(out).toContain('## Runbook');
        expect(out).toContain('runbook content');
    });

    it('inserts warning text when files are missing', () => {
        const roleDir = path.join(tmp, 'roles', 'test', 'tester');
        fs.mkdirSync(roleDir, { recursive: true });
        fs.writeFileSync(path.join(roleDir, 'tester.role.md'), 'role content');
        // intentionally omit schema and runbook

        const out = ds.getRoleContext('test', 'tester');
        expect(out).toContain('> ⚠️ Not found');
        expect(out).toContain('role content');
    });
});

// simple helper to write a fake state.md table for summary tests
function writeStateMd(epicDir: string, rows: string[]) {
    const content = [
        '---',
        'fsm_state: "IN_DEV"',
        '---',
        '',
        '| Task ID | Cím | Státusz | Felelős |',
        '| :--- | :--- | :--- | :--- |',
        ...rows,
    ].join('\n');
    fs.writeFileSync(path.join(epicDir, 'state.md'), content);
}

describe('DocumentServer SSOT summary methods', () => {
    let tmp: string;
    let ds: DocumentServer;

    beforeEach(() => {
        tmp = makeTempDir();
        const dbPath = path.join(tmp, 'metadata.db');
        ds = new DocumentServer(tmp, tmp, new ResourceTracker(dbPath));
    });

    afterEach(() => {
        try {
            fs.rmSync(tmp, { recursive: true, force: true });
        } catch (_e) { }
    });

    it('getProjectSummaryFs scans epics folder', () => {
        const epicsDir = path.join(tmp, 'docs', 'joinerytech-flow', 'epics');
        const epic1 = path.join(epicsDir, 'EPIC-XX');
        const epic2 = path.join(epicsDir, 'EPIC-YY');
        fs.mkdirSync(epic1, { recursive: true });
        fs.mkdirSync(epic2, { recursive: true });
        writeStateMd(epic1, ['| [TASK-01]() | title | Pending | QA |']);
        writeStateMd(epic2, ['| [TASK-02]() | title2 | Done | Dev |']);
        const summary = ds.getProjectSummaryFs();
        expect(summary).toHaveProperty('epics');
        expect(Array.isArray(summary.epics)).toBe(true);
        expect(summary.epics.length).toBe(2);
        expect(summary.epics[0]).toHaveProperty('epic');
        expect(summary.epics[0]).toHaveProperty('fsm_state');
    });

    it('getEpicSummaryFs parses task table correctly', () => {
        const epicsDir = path.join(tmp, 'docs', 'joinerytech-flow', 'epics');
        const epic = path.join(epicsDir, 'EPIC-AB');
        fs.mkdirSync(epic, { recursive: true });
        writeStateMd(epic, [
            '| [TASK-11]() | Do stuff | 📋 Pending | Architect |',
            '| [TASK-12]() | More work | ✅ Done | QA |',
        ]);
        const result = ds.getEpicSummaryFs('EPIC-AB');
        expect(result.epic).toBe('EPIC-AB');
        expect(Array.isArray(result.tasks)).toBe(true);
        expect(result.tasks.length).toBe(2);
        expect(result.tasks[0]).toMatchObject({ id: 'TASK-11', status: '📋 Pending' });
    });

    it('database path takes precedence when tables exist', () => {
        const db = new (require('better-sqlite3'))(path.join(tmp, 'metadata.db'));
        db.exec(`
            CREATE TABLE projects (id TEXT PRIMARY KEY);
            CREATE TABLE epics (id TEXT PRIMARY KEY, project_id TEXT, state TEXT);
            CREATE TABLE tasks (id TEXT PRIMARY KEY, epic_id TEXT, title TEXT, status TEXT, owner TEXT);
            INSERT INTO projects (id) VALUES ('proj1');
            INSERT INTO epics (id, project_id, state) VALUES ('EPIC-DB', 'proj1', 'QA_WAITING');
            INSERT INTO tasks (id, epic_id, title, status, owner) VALUES ('TDB1','EPIC-DB','foo','Done','X');
        `);
        db.close();

        const ps = ds.getProjectSummary();
        expect(ps.epics.find((e: any) => e.epic === 'EPIC-DB')).toBeDefined();
        const es = ds.getEpicSummary('EPIC-DB');
        expect(es.tasks.length).toBe(1);
        expect(es.tasks[0].id).toBe('TDB1');
    });
});
