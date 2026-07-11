import { describe, it, expect } from 'vitest';
import { RoleLoader } from '../../roles/RoleLoader';
import * as fs from 'fs';

const databaseRoot = __dirname + '/../../../database';

describe('Discovery role definitions', () => {
    const loader = new RoleLoader(databaseRoot);

    it('should load researcher role correctly', () => {
        const schema = loader.loadRole('discovery', 'researcher');
        expect(schema).toBeDefined();
        expect(schema.role).toBe('researcher');
        expect(schema.domain).toBe('discovery');
        expect(schema.mcp_tool_permissions).toContain('get_workflow');
    });

    it('should load architect role (sanity check)', () => {
        const schema = loader.loadRole('discovery', 'architect');
        expect(schema).toBeDefined();
        expect(schema.role).toBe('architect');
        // architect is allowed to request context
        expect(schema.mcp_tool_permissions).toContain('request_context');
        expect(schema.mcp_tool_permissions).toContain('session_register');
    });

    it('tools are phase-restricted: researcher cannot request context', () => {
        const arch = loader.loadRole('discovery', 'architect');
        const res = loader.loadRole('discovery', 'researcher');
        expect(arch.mcp_tool_permissions).toContain('request_context');
        expect(res.mcp_tool_permissions).not.toContain('request_context');
    });
});

describe('Discovery workflow and templates', () => {
    const base = __dirname + '/../../../database/roles/discovery';
    const workflowPath = base + '/workflows/DWI.workflow.md';

    it('DWI workflow file exists and defines four phases', () => {
        expect(fs.existsSync(workflowPath)).toBe(true);
        const content = fs.readFileSync(workflowPath, 'utf8');
        const match = content.match(/phases:[\s\S]*(- ideation)[\s\S]*(- validation)[\s\S]*(- iteration)[\s\S]*(- delivery_handoff)/);
        expect(match).not.toBeNull();
        // also ensure entrance and exit criteria are documented
        expect(content).toMatch(/Entrance Criteria:/i);
        expect(content).toMatch(/Exit Criteria:/i);
    });

    it('all four artifact templates exist', () => {
        const templates = [
            'ideation-artifact.md',
            'validation-report.md',
            'refined-design.md',
            'handoff-ticket.md',
        ];
        for (const t of templates) {
            const p = `${base}/templates/${t}`;
            expect(fs.existsSync(p)).toBe(true);
            const txt = fs.readFileSync(p, 'utf8');
            expect(txt.length).toBeGreaterThan(20);
        }
    });
});
