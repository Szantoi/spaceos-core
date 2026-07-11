import * as fs from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';
import { ResourceTracker, ResourceRecord } from '../metadata/ResourceTracker';
import { AgentDb } from './AgentDb'; // EPIC-16: DB-first tool refactor

/**
 * DocumentServer
 *
 * Serves COMPLETE documents for structured content types that MUST NOT be RAG-indexed.
 * Per MCP_Server_Architecture.md: role, workflow, template, core docs must be loaded fully.
 *
 * Database structure:
 *   src/agent-system/database/roles/{domain}/{role}/
 *     - {role}.role.md
 *     - {role}.schema.yaml
 *     - {role}.runbook.md
 *     - workflows/{role}[_suffix].workflow.md
 *     - templates/{name}.template.md
 *
 *   src/agent-system/database/standards/
 *     - constraints.md  (or similar core files)
 */

// ────────────────────────────────────────────────────────────────────────────
// Artifact submission types
// ────────────────────────────────────────────────────────────────────────────

export interface ArtifactSubmitRequest {
    /** Domain of the submitting role, e.g. "engineering" */
    domain: string;
    /** Role name, e.g. "backend_developer" */
    role: string;
    /**
     * Template name (without .template.md).
     * Used to verify that a matching template exists for this artifact type.
     * e.g. "implementation_report"
     */
    type: string;
    /** Full Markdown content including YAML frontmatter (--- blocks). */
    content: string;
    /**
     * Target path relative to workspace root.
     * e.g. "docs/joinerytech-flow/epics/EPIC-01/implementation-summary/TASK-01-auth.md"
     * Sanitized server-side — directory traversal is rejected.
     */
    target_path: string;
    /** Project identifier for the ResourceTracker. */
    project: string;
    /** Submitting user / agent identifier. */
    user: string;
    /** Optional: session_id from SessionManager to link artifact to session. */
    session_id?: string;
}

export interface ArtifactSubmitResult {
    ok: boolean;
    message: string;
    resource?: ResourceRecord;
    /** List of missing required frontmatter fields, if validation failed. */
    missing_fields?: string[];
}

export class DocumentServer {
    private rolesBaseDir: string;
    private standardsBaseDir: string;
    private workspaceRoot: string;
    private resourceTracker: ResourceTracker;
    private agentDb: AgentDb | undefined;

    /**
     * @param databaseRoot    - absolute path to the agent-system/database directory
     * @param workspaceRoot   - absolute path to repo root (optional, will try to infer)
     * @param resourceTracker - shared ResourceTracker instance (optional, creates own if omitted)
     * @param agentDb         - AgentDb instance for DB-first reads (EPIC-16, optional, file-fallback otherwise)
     */
    constructor(
        databaseRoot: string,
        workspaceRoot?: string,
        resourceTracker?: ResourceTracker,
        agentDb?: AgentDb
    ) {
        this.rolesBaseDir = path.join(databaseRoot, 'roles');
        this.standardsBaseDir = path.join(databaseRoot, 'standards');
        this.workspaceRoot = workspaceRoot || path.join(databaseRoot, '..', '..', '..');
        this.resourceTracker = resourceTracker ?? new ResourceTracker();
        this.agentDb = agentDb;
    }

    /**
     * GET ROLE — returns the full .role.md content
     * Tool: get_role(domain, role)
     */
    public getRole(domain: string, role: string): string {
        // EPIC-16: DB-first — use AgentDb when available, file-system as fallback
        if (this.agentDb) {
            const record = this.agentDb.getRole(domain, role);
            if (record) return record.content;
            // If not in DB, fall through to file-system for graceful degradation
        }
        const filePath = path.join(this.rolesBaseDir, domain, role, `${role}.role.md`);
        return this.readFile(filePath, `Role not found: ${domain}/${role}`);
    }

    /**
     * GET SCHEMA — returns the full .schema.yaml content
     */
    public getSchema(domain: string, role: string): string {
        const filePath = path.join(this.rolesBaseDir, domain, role, `${role}.schema.yaml`);
        return this.readFile(filePath, `Schema not found: ${domain}/${role}`);
    }

    /**
     * GET WORKFLOW — returns the full .workflow.md content
     * Tool: get_workflow(domain, role, type?)
     * type: 'multi_workspace' | 'default' (omit = default workflow)
     */
    public getWorkflow(domain: string, role: string, type?: string): string {
        // EPIC-16: DB-first — AgentDb.getWorkflow uses workflowType
        if (this.agentDb) {
            const workflowType = (!type || type === 'default') ? role : `${role}_${type}`;
            const record = this.agentDb.getWorkflow(domain, role, workflowType);
            if (record) return record.content;
            // Fall through to file-system if not found in DB
        }
        const workflowDir = path.join(this.rolesBaseDir, domain, role, 'workflows');

        if (!fs.existsSync(workflowDir)) {
            throw new Error(`Workflow directory not found: ${domain}/${role}/workflows`);
        }

        let targetFile: string;

        if (type && type !== 'default') {
            // e.g. type='multi_workspace' -> backend_developer_multi_workspace.workflow.md
            targetFile = path.join(workflowDir, `${role}_${type}.workflow.md`);
        } else {
            // Default workflow
            targetFile = path.join(workflowDir, `${role}.workflow.md`);
        }

        if (!fs.existsSync(targetFile)) {
            // Fallback: list available workflows and return error with options
            const available = fs.readdirSync(workflowDir)
                .filter(f => f.endsWith('.workflow.md'))
                .map(f => f.replace('.workflow.md', '').replace(`${role}_`, '').replace(role, 'default'));
            throw new Error(
                `Workflow not found: ${domain}/${role}/${type || 'default'}. Available: [${available.join(', ')}]`
            );
        }

        return this.readFile(targetFile, `Workflow not found: ${domain}/${role}/${type || 'default'}`);
    }

    /**
     * GET TEMPLATE — returns the full .template.md content
     * Tool: get_template(domain, role, name)
     */
    public getTemplate(domain: string, role: string, name: string): string {
        // EPIC-16: DB-first
        if (this.agentDb) {
            const record = this.agentDb.getTemplate(domain, role, name);
            if (record) return record.content;
        }
        const templatePath = path.join(
            this.rolesBaseDir, domain, role, 'templates', `${name}.template.md`
        );
        return this.readFile(templatePath, `Template not found: ${domain}/${role}/${name}`);
    }

    /**
     * LIST TEMPLATES — lists available templates for a role
     */
    public listTemplates(domain: string, role: string): string[] {
        // EPIC-16: DB-first
        if (this.agentDb) {
            const templates = this.agentDb.getTemplatesByRole(domain, role);
            if (templates.length > 0) return templates.map(t => t.template_name);
        }
        const templateDir = path.join(this.rolesBaseDir, domain, role, 'templates');
        if (!fs.existsSync(templateDir)) return [];
        return fs.readdirSync(templateDir)
            .filter(f => f.endsWith('.template.md'))
            .map(f => f.replace('.template.md', ''));
    }


    /**
     * GET CORE — returns a full core/standards document
     * Tool: get_core(doc: 'constraints' | 'runbook' | 'dod' | 'error_recovery', domain?, role?)
     *
     * Lookup order:
     *   1. If domain+role: role-specific file (e.g. roles/engineering/backend_developer/backend_developer.runbook.md)
     *   2. Fallback: standards directory (e.g. standards/constraints.md)
     */
    public getCore(doc: string, domain?: string, role?: string): string {
        // EPIC-16: DB-first — runbook from AgentDb.getRunbook(), standards from AgentDb.getStandard()
        if (this.agentDb) {
            if (doc === 'runbook' && domain && role) {
                const record = this.agentDb.getRunbook(domain, role);
                if (record) return record.content;
            } else if (doc !== 'runbook') {
                // Try as standard (std_id convention: "<doc>.md" or "00-foundation/<doc>.md")
                const record = this.agentDb.getStandard(doc)
                    ?? this.agentDb.getStandard(`${doc}.md`)
                    ?? this.agentDb.getStandard(`00-foundation/${doc}.md`);
                if (record) return record.content;
            }
            // Fall through to file-system for graceful degradation
        }
        // Try role-specific first
        if (domain && role) {
            const roleSpecificMap: Record<string, string> = {
                runbook: path.join(this.rolesBaseDir, domain, role, `${role}.runbook.md`),
            };
            if (roleSpecificMap[doc] && fs.existsSync(roleSpecificMap[doc])) {
                return fs.readFileSync(roleSpecificMap[doc], 'utf8');
            }
        }

        // Fallback: standards directory
        const standardsFilePath = path.join(this.standardsBaseDir, `${doc}.md`);
        if (fs.existsSync(standardsFilePath)) {
            return fs.readFileSync(standardsFilePath, 'utf8');
        }

        // List available standards for helpful error
        const available = fs.existsSync(this.standardsBaseDir)
            ? fs.readdirSync(this.standardsBaseDir).filter(f => f.endsWith('.md')).map(f => f.replace('.md', ''))
            : [];

        throw new Error(
            `Core document not found: "${doc}". Available standards: [${available.join(', ')}]`
        );
    }

    /**
     * GET POLICY — returns a general policy file from the docs folder
     * Tool: get_policy(name)
     * e.g. name = "orchestrator" -> docs/context/orchestrator.policy.md
     * or docs/joinerytech-flow/agent-system-v2/orchestrator.policy.md
     */
    public getPolicy(name: string): string {
        // Sanitize name to prevent directory traversal
        const safeName = path.basename(name).replace(/\.policy\.md$/, '');

        // Potential paths — ordered by priority
        const pathsToTry = [
            // Highest priority: database/standards/ (canonical policy location)
            path.join(this.standardsBaseDir, `${safeName}.policy.md`),
            // Workspace docs locations (for future doc-level policies)
            path.join(this.workspaceRoot, 'docs', 'context', `${safeName}.policy.md`),
            path.join(this.workspaceRoot, 'docs', 'agent-system-v2', `${safeName}.policy.md`),
            path.join(this.workspaceRoot, 'docs', 'joinerytech-flow', 'context', `${safeName}.policy.md`),
        ];

        for (const p of pathsToTry) {
            if (fs.existsSync(p)) {
                return fs.readFileSync(p, 'utf8');
            }
        }

        const searched = pathsToTry.join('\n  - ');
        throw new Error(`Policy document not found: "${safeName}.policy.md". Searched:\n  - ${searched}`);
    }

    /**
     * GET ROLE CONTEXT — returns a combined context bundle: role + schema + runbook
     * Tool: get_role_context(domain, role)
     *
     * Missing files produce a warning section instead of throwing, so the agent
     * always receives a partial (but usable) context.
     */
    public getRoleContext(domain: string, role: string): string {
        const readSafe = (filePath: string, label: string): string => {
            if (fs.existsSync(filePath)) {
                return fs.readFileSync(filePath, 'utf8');
            }
            return `> ⚠️ Not found: \`${path.relative(this.workspaceRoot, filePath)}\``;
        };

        const roleFile = path.join(this.rolesBaseDir, domain, role, `${role}.role.md`);
        const schemaFile = path.join(this.rolesBaseDir, domain, role, `${role}.schema.yaml`);
        const runbookFile = path.join(this.rolesBaseDir, domain, role, `${role}.runbook.md`);

        const roleContent = readSafe(roleFile, 'role');
        const schemaContent = readSafe(schemaFile, 'schema');
        const runbookContent = readSafe(runbookFile, 'runbook');

        return [
            `# Role Context Bundle: ${domain}/${role}`,
            '',
            '## Role',
            roleContent,
            '',
            '## Schema',
            '```yaml',
            schemaContent,
            '```',
            '',
            '## Runbook',
            runbookContent,
        ].join('\n');
    }

    // ─── SSOT SUMMARY HELPERS ─────────────────────────────────────────────────

    /**
     * Return a high‑level list of epics in the project along with their FSM state.
     * For demonstration purposes we attempt a JOIN query against the SQLite
     * `projects`/`epics` tables if they exist; otherwise we fall back to a file‑
     * system scan of `docs/joinerytech-flow/epics`.
     */
    public getProjectSummary(): any {
        // first try the database path (AC-03: include JOIN query)
        try {
            // eslint-disable-next-line @typescript-eslint/no-var-requires
            const Database: any = require('better-sqlite3');
            const db = new Database(path.resolve(this.workspaceRoot, 'metadata.db'));
            // ensure tables exist so the JOIN below never errors
            db.exec(`
                CREATE TABLE IF NOT EXISTS projects (id TEXT PRIMARY KEY);
                CREATE TABLE IF NOT EXISTS epics (
                    id TEXT PRIMARY KEY,
                    project_id TEXT,
                    state TEXT
                );
            `);
            const rows = db.prepare(`
                SELECT p.id AS project, e.id AS epic, e.state AS epic_state
                FROM projects p
                JOIN epics e ON e.project_id = p.id
            `).all();
            db.close();
            return { epics: rows };
        } catch (_err) {
            // fall through to filesystem-based implementation
        }
        return this.getProjectSummaryFs();
    }

    private getProjectSummaryFs(): any {
        const epicsDir = path.join(this.workspaceRoot, 'docs', 'joinerytech-flow', 'epics');
        if (!fs.existsSync(epicsDir)) return { epics: [] };
        const epics = fs.readdirSync(epicsDir)
            .filter(d => d.startsWith('EPIC-') && fs.statSync(path.join(epicsDir, d)).isDirectory());
        const summary = epics.map(e => {
            const stateFile = path.join(epicsDir, e, 'state.md');
            let fsmState = '';
            if (fs.existsSync(stateFile)) {
                const content = fs.readFileSync(stateFile, 'utf8');
                const m = content.match(/fsm_state:\s*"(.*?)"/);
                if (m) fsmState = m[1];
            }
            return { epic: e, fsm_state: fsmState };
        });
        return { epics: summary };
    }

    /**
     * Return task-level details for a given epic. Attempts DB query first,
     * falling back to parsing the `state.md` table under the epic directory.
     */
    public getEpicSummary(epic: string): any {
        try {
            const Database: any = require('better-sqlite3');
            const db = new Database(path.resolve(this.workspaceRoot, 'metadata.db'));
            db.exec(`
                CREATE TABLE IF NOT EXISTS tasks (
                    id TEXT PRIMARY KEY,
                    epic_id TEXT,
                    title TEXT,
                    status TEXT,
                    owner TEXT
                );
            `);
            const tasks = db.prepare(`SELECT id, title, status, owner FROM tasks WHERE epic_id = ?`).all(epic);
            db.close();
            return { epic, tasks };
        } catch (_e) {
            // fall through
        }
        return this.getEpicSummaryFs(epic);
    }

    private getEpicSummaryFs(epic: string): any {
        const stateFile = path.join(this.workspaceRoot, 'docs', 'joinerytech-flow', 'epics', epic, 'state.md');
        if (!fs.existsSync(stateFile)) {
            throw new Error(`Epic not found: ${epic}`);
        }
        const content = fs.readFileSync(stateFile, 'utf8');
        const lines = content.split(/\r?\n/);
        const tasks: Array<{ id: string; title: string; status: string; owner: string }> = [];
        let inTable = false;
        for (const line of lines) {
            if (line.startsWith('| Task ID')) {
                inTable = true;
                continue;
            }
            if (inTable) {
                // skip table separator lines (e.g. "| :--- | :--- |" or "| --- |")
                if (/^\|\s*:?-{3}/.test(line)) continue;
                if (!line.startsWith('|')) break;
                const cols = line.split('|').map(s => s.trim());
                if (cols.length < 5) continue;
                const id = cols[1].replace(/\[|\]/g, '').split('(')[0];
                const title = cols[2];
                const status = cols[3];
                const owner = cols[4];
                tasks.push({ id, title, status, owner });
            }
        }
        return { epic, tasks };
    }

    /**
     * LIST DOMAINS — returns available domains
     */
    public listDomains(): string[] {
        // EPIC-16: DB-first
        if (this.agentDb) {
            const domains = this.agentDb.listDomains();
            if (domains.length > 0) return domains;
        }
        if (!fs.existsSync(this.rolesBaseDir)) return [];
        return fs.readdirSync(this.rolesBaseDir, { withFileTypes: true })
            .filter(d => d.isDirectory())
            .map(d => d.name);
    }

    /**
     * LIST ROLES — returns available roles in a domain
     */
    public listRoles(domain: string): string[] {
        // EPIC-16: DB-first
        if (this.agentDb) {
            const roles = this.agentDb.listRoleNames(domain);
            if (roles.length > 0) return roles;
        }
        const domainDir = path.join(this.rolesBaseDir, domain);
        if (!fs.existsSync(domainDir)) return [];
        return fs.readdirSync(domainDir, { withFileTypes: true })
            .filter(d => d.isDirectory())
            .map(d => d.name);
    }

    // ─── Artifact Write Layer (ADR-007) ───────────────────────────────────────

    /**
     * SUBMIT ARTIFACT — validates, writes, and registers a document produced by an agent.
     *
     * Flow (per ADR-007 + workflow-protocol analysis):
     *   1. Validate that a template exists for the requested type (domain/role/type).
     *   2. Parse the YAML frontmatter from `content`.
     *   3. Check required frontmatter fields (type, role, at minimum).
     *   4. Sanitize target_path and resolve to absolute path inside workspaceRoot.
     *   5. Write the file (creates parent directories as needed).
     *   6. Register the resource in ResourceTracker (with session_id for audit).
     */
    public submitArtifact(req: ArtifactSubmitRequest): ArtifactSubmitResult {
        const { domain, role, type, content, target_path, project, user, session_id } = req;

        // ── Step 1: Validate template exists for this type ───────────────────
        try {
            this.getTemplate(domain, role, type);
        } catch {
            return {
                ok: false,
                message: `No template registered for type "${type}" under domain="${domain}" role="${role}". ` +
                    `Available: [${this.listTemplates(domain, role).join(', ')}]`
            };
        }

        // ── Step 2: Parse frontmatter ─────────────────────────────────────────
        const frontmatter = this.extractFrontmatter(content);
        if (!frontmatter) {
            return {
                ok: false,
                message: 'Content is missing a valid YAML frontmatter block (--- ... ---). ' +
                    'All submitted artifacts must begin with a frontmatter section.'
            };
        }

        // ── Step 3: Required field check ──────────────────────────────────────
        const requiredFields = this.getRequiredFrontmatterFields(domain, role, type);
        const missingFields = requiredFields.filter(f => !(f in frontmatter));
        if (missingFields.length > 0) {
            return {
                ok: false,
                message: `Frontmatter validation failed. Missing required fields: [${missingFields.join(', ')}]`,
                missing_fields: missingFields
            };
        }

        // ── Step 4: Sanitize and resolve path ─────────────────────────────────
        const sanitized = this.sanitizeTargetPath(target_path);
        if (!sanitized.ok) {
            return { ok: false, message: sanitized.error! };
        }
        const absolutePath = path.join(this.workspaceRoot, sanitized.relativePath!);

        // ── Step 5: Write file ────────────────────────────────────────────────
        try {
            fs.mkdirSync(path.dirname(absolutePath), { recursive: true });
            fs.writeFileSync(absolutePath, content, 'utf8');
        } catch (err: any) {
            return { ok: false, message: `File write failed: ${err.message}` };
        }

        // ── Step 6: Register in ResourceTracker ───────────────────────────────
        const resource = this.resourceTracker.addOrUpdate({
            project,
            user,
            type,
            relative_path: sanitized.relativePath!,
            session_id
        });

        return {
            ok: true,
            message: `Artifact written and registered: ${sanitized.relativePath}`,
            resource
        };
    }

    /**
     * Extracts and parses the YAML frontmatter from a Markdown string.
     * Returns null if no valid frontmatter block is found.
     */
    private extractFrontmatter(content: string): Record<string, unknown> | null {
        const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
        if (!match) return null;
        try {
            const parsed = yaml.load(match[1]);
            if (parsed && typeof parsed === 'object') return parsed as Record<string, unknown>;
            return null;
        } catch {
            return null;
        }
    }

    /**
     * Returns the list of required frontmatter fields for a given template type.
     *
     * Lookup strategy:
     *   1. Role schema YAML: `required_frontmatter_fields.<type>` array
     *   2. Fallback: minimal global requirements [type, role]
     */
    private getRequiredFrontmatterFields(domain: string, role: string, type: string): string[] {
        try {
            const schemaPath = path.join(this.rolesBaseDir, domain, role, `${role}.schema.yaml`);
            if (fs.existsSync(schemaPath)) {
                const schema = yaml.load(fs.readFileSync(schemaPath, 'utf8')) as Record<string, any>;
                if (schema?.required_frontmatter_fields?.[type]) {
                    return schema.required_frontmatter_fields[type] as string[];
                }
            }
        } catch {
            // Fall through to defaults
        }
        // Minimal global requirements per Operative_Process_Framework_Standard
        return ['type', 'role'];
    }

    /**
     * Sanitizes the target_path to prevent directory traversal.
     * Returns the clean relative path or an error message.
     */
    private sanitizeTargetPath(rawPath: string): { ok: boolean; relativePath?: string; error?: string } {
        // Normalize separators first, then resolve — do NOT strip dots before traversal check
        const withForwardSlashes = rawPath.replace(/\\/g, '/');
        const resolved = path.normalize(withForwardSlashes);

        // Reject anything that escapes the workspace root
        if (resolved.startsWith('..') || path.isAbsolute(resolved)) {
            return { ok: false, error: `Rejected target_path — directory traversal detected: "${rawPath}"` };
        }

        // Strip any remaining leading slashes after the traversal check
        const clean = resolved.replace(/^\/+/, '');

        // Only allow .md files (artifact documents)
        if (!clean.endsWith('.md')) {
            return { ok: false, error: `Rejected target_path — only .md artifacts are accepted: "${rawPath}"` };
        }

        return { ok: true, relativePath: clean };
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private readFile(filePath: string, errorMessage: string): string {
        if (!fs.existsSync(filePath)) {
            throw new Error(`${errorMessage}\nPath: ${filePath}`);
        }
        return fs.readFileSync(filePath, 'utf8');
    }
}
