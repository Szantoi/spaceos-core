import * as fs from 'node:fs';
import * as path from 'node:path';

export interface ResourceContent {
    uri: string;
    name: string;
    description?: string;
    mimeType: 'application/json';
    contents: string;
}

export interface MatchResult {
    matched: boolean;
    params?: Record<string, string>;
}

export interface ResourceListing {
    uriPattern: string;
    description: string;
}

export class ResourceResolutionError extends Error {
    public readonly statusCode: number;

    constructor(message: string, statusCode = 404) {
        super(message);
        this.name = 'ResourceResolutionError';
        this.statusCode = statusCode;
    }
}

export abstract class ResourceTemplate {
    abstract readonly uriPattern: string;
    abstract readonly description: string;

    public matchUri(uri: string): MatchResult {
        const regex = this.patternToRegex();
        const match = regex.exec(uri);
        if (!match) {
            return { matched: false };
        }

        const variables = this.extractPatternVariables();
        const params: Record<string, string> = {};
        for (let i = 0; i < variables.length; i += 1) {
            const value = match[i + 1];
            if (!value) {
                return { matched: false };
            }
            params[variables[i]] = decodeURIComponent(value);
        }

        return { matched: true, params };
    }

    abstract resolve(params: Record<string, string>): Promise<ResourceContent>;

    protected extractPatternVariables(): string[] {
        const regex = /{([^}]+)}/g;
        const variables: string[] = [];
        let current: RegExpExecArray | null = regex.exec(this.uriPattern);

        while (current !== null) {
            variables.push(current[1]);
            current = regex.exec(this.uriPattern);
        }

        return variables;
    }

    protected patternToRegex(): RegExp {
        const tokenRegex = /{([^}]+)}/g;
        let built = '^';
        let cursor = 0;
        let match = tokenRegex.exec(this.uriPattern);

        while (match !== null) {
            const staticPart = this.uriPattern.slice(cursor, match.index);
            built += staticPart.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            built += '([^/]+)';
            cursor = match.index + match[0].length;
            match = tokenRegex.exec(this.uriPattern);
        }

        const trailingStatic = this.uriPattern.slice(cursor);
        built += trailingStatic.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        built += '$';

        return new RegExp(built);
    }
}

function safeSegment(value: string, name: string): string {
    if (!value || value.includes('..') || value.includes('/') || value.includes('\\')) {
        throw new ResourceResolutionError(`Invalid resource parameter: ${name}`, 400);
    }
    return value;
}

function readFileOr404(filePath: string, notFoundMessage: string): string {
    if (!fs.existsSync(filePath)) {
        throw new ResourceResolutionError(notFoundMessage, 404);
    }
    return fs.readFileSync(filePath, 'utf8');
}

function toJsonContent(payload: unknown): string {
    return JSON.stringify(payload, null, 2);
}

export class RoleResourceTemplate extends ResourceTemplate {
    readonly uriPattern = 'resource://role/{domain}/{role}';
    readonly description = 'Role definition by domain and role name';

    constructor(private readonly databaseRoot: string = path.resolve(process.cwd(), 'database')) {
        super();
    }

    async resolve(params: Record<string, string>): Promise<ResourceContent> {
        const domain = safeSegment(params.domain, 'domain');
        const role = safeSegment(params.role, 'role');

        const roleFile = path.join(this.databaseRoot, 'roles', domain, role, `${role}.role.md`);
        const content = readFileOr404(roleFile, `Role not found for ${domain}/${role}`);
        const uri = `resource://role/${domain}/${role}`;

        return {
            uri,
            name: `${domain}/${role}`,
            description: 'Role definition document',
            mimeType: 'application/json',
            contents: toJsonContent({ domain, role, content })
        };
    }
}

export class WorkflowResourceTemplate extends ResourceTemplate {
    readonly uriPattern = 'resource://workflow/{type}';
    readonly description = 'Workflow content by workflow type';

    constructor(private readonly databaseRoot: string = path.resolve(process.cwd(), 'database')) {
        super();
    }

    async resolve(params: Record<string, string>): Promise<ResourceContent> {
        const type = safeSegment(params.type, 'type');
        const candidates = [
            path.join(this.databaseRoot, 'roles', 'discovery', 'workflows', `${type}.workflow.md`),
            path.join(this.databaseRoot, 'roles', 'discovery', 'workflows', `DWI.workflow.md`)
        ];

        const discovered = this.findWorkflowByType(type);
        if (discovered) {
            candidates.unshift(discovered);
        }

        let selectedPath: string | null = null;
        for (const filePath of candidates) {
            if (fs.existsSync(filePath)) {
                selectedPath = filePath;
                break;
            }
        }

        if (!selectedPath) {
            throw new ResourceResolutionError(`Workflow not found for type: ${type}`, 404);
        }

        const content = fs.readFileSync(selectedPath, 'utf8');
        const fileName = path.basename(selectedPath);
        return {
            uri: `resource://workflow/${type}`,
            name: type,
            description: 'Workflow resource',
            mimeType: 'application/json',
            contents: toJsonContent({ type, file: fileName, content })
        };
    }

    private findWorkflowByType(type: string): string | null {
        const workflowsRoot = path.join(this.databaseRoot, 'roles');
        if (!fs.existsSync(workflowsRoot)) {
            return null;
        }

        const roleDomains = fs.readdirSync(workflowsRoot, { withFileTypes: true }).filter(d => d.isDirectory());
        for (const domainEntry of roleDomains) {
            const domainPath = path.join(workflowsRoot, domainEntry.name);
            const roleEntries = fs.readdirSync(domainPath, { withFileTypes: true }).filter(d => d.isDirectory());
            for (const roleEntry of roleEntries) {
                const workflowsPath = path.join(domainPath, roleEntry.name, 'workflows');
                if (!fs.existsSync(workflowsPath)) {
                    continue;
                }
                const files = fs.readdirSync(workflowsPath);
                const found = files.find(file => file.endsWith('.workflow.md') && file.includes(type));
                if (found) {
                    return path.join(workflowsPath, found);
                }
            }
        }

        return null;
    }
}

export class TemplateCategoryResourceTemplate extends ResourceTemplate {
    readonly uriPattern = 'resource://template/{category}';
    readonly description = 'Template resources by category';

    constructor(private readonly databaseRoot: string = path.resolve(process.cwd(), 'database')) {
        super();
    }

    async resolve(params: Record<string, string>): Promise<ResourceContent> {
        const category = safeSegment(params.category, 'category');
        const templatesRoot = path.join(this.databaseRoot, 'roles', 'discovery', 'templates');

        if (!fs.existsSync(templatesRoot)) {
            throw new ResourceResolutionError('Template directory not found', 404);
        }

        const files = fs.readdirSync(templatesRoot).filter(file => file.endsWith('.md'));
        const selected = files.filter(file => file.includes(category));
        if (selected.length === 0) {
            throw new ResourceResolutionError(`Template category not found: ${category}`, 404);
        }

        const templates = selected.map(file => ({
            name: file,
            content: fs.readFileSync(path.join(templatesRoot, file), 'utf8')
        }));

        return {
            uri: `resource://template/${category}`,
            name: category,
            description: 'Template category resources',
            mimeType: 'application/json',
            contents: toJsonContent({ category, templates })
        };
    }
}

export class DiscoveryPhaseResourceTemplate extends ResourceTemplate {
    readonly uriPattern = 'resource://discovery/{phase}';
    readonly description = 'Discovery workflow guide by phase';

    constructor(private readonly databaseRoot: string = path.resolve(process.cwd(), 'database')) {
        super();
    }

    async resolve(params: Record<string, string>): Promise<ResourceContent> {
        const phase = safeSegment(params.phase, 'phase');
        const workflowFile = path.join(this.databaseRoot, 'roles', 'discovery', 'workflows', 'DWI.workflow.md');
        const workflow = readFileOr404(workflowFile, 'Discovery workflow not found');

        const templatesRoot = path.join(this.databaseRoot, 'roles', 'discovery', 'templates');
        const templateFiles = fs.existsSync(templatesRoot)
            ? fs.readdirSync(templatesRoot).filter(file => file.endsWith('.md'))
            : [];

        const templates = templateFiles
            .filter(file => file.includes(phase) || phase === 'all')
            .map(file => ({ name: file, content: fs.readFileSync(path.join(templatesRoot, file), 'utf8') }));

        if (templates.length === 0 && phase !== 'all') {
            throw new ResourceResolutionError(`No discovery templates found for phase: ${phase}`, 404);
        }

        return {
            uri: `resource://discovery/${phase}`,
            name: phase,
            description: 'Discovery phase context',
            mimeType: 'application/json',
            contents: toJsonContent({ phase, workflow, templates })
        };
    }
}

export class TaskContextResourceTemplate extends ResourceTemplate {
    readonly uriPattern = 'resource://task/{task_id}';
    readonly description = 'Task context document by task identifier';

    constructor(private readonly workspaceRoot: string = process.cwd()) {
        super();
    }

    async resolve(params: Record<string, string>): Promise<ResourceContent> {
        const taskId = safeSegment(params.task_id, 'task_id');
        const docsRoot = path.join(this.workspaceRoot, 'Docs');

        if (!fs.existsSync(docsRoot)) {
            throw new ResourceResolutionError('Docs directory not found', 404);
        }

        const match = this.findTaskFile(docsRoot, taskId.toLowerCase());
        if (!match) {
            throw new ResourceResolutionError(`Task context not found: ${taskId}`, 404);
        }

        const content = fs.readFileSync(match, 'utf8');
        return {
            uri: `resource://task/${taskId}`,
            name: taskId,
            description: 'Task context resource',
            mimeType: 'application/json',
            contents: toJsonContent({ task_id: taskId, content })
        };
    }

    private findTaskFile(rootDir: string, loweredTaskId: string): string | null {
        const stack = [rootDir];
        while (stack.length > 0) {
            const current = stack.pop();
            if (!current) {
                continue;
            }

            const entries = fs.readdirSync(current, { withFileTypes: true });
            for (const entry of entries) {
                const fullPath = path.join(current, entry.name);
                if (entry.isDirectory()) {
                    stack.push(fullPath);
                    continue;
                }

                if (entry.isFile() && entry.name.toLowerCase().includes(loweredTaskId) && entry.name.endsWith('.md')) {
                    return fullPath;
                }
            }
        }

        return null;
    }
}

export class ResourceTemplateRegistry {
    private templates: ResourceTemplate[] = [];

    public register(template: ResourceTemplate): void {
        this.templates.push(template);
    }

    public registerDefaults(databaseRoot: string = path.resolve(process.cwd(), 'database'), workspaceRoot: string = process.cwd()): void {
        this.register(new RoleResourceTemplate(databaseRoot));
        this.register(new WorkflowResourceTemplate(databaseRoot));
        this.register(new TemplateCategoryResourceTemplate(databaseRoot));
        this.register(new DiscoveryPhaseResourceTemplate(databaseRoot));
        this.register(new TaskContextResourceTemplate(workspaceRoot));
    }

    public listResources(): ResourceListing[] {
        return this.templates.map(template => ({
            uriPattern: template.uriPattern,
            description: template.description
        }));
    }

    public async resolveUri(uri: string): Promise<ResourceContent> {
        for (const template of this.templates) {
            const match = template.matchUri(uri);
            if (match.matched && match.params) {
                return template.resolve(match.params);
            }
        }

        throw new ResourceResolutionError(`No resource template matched URI: ${uri}`, 404);
    }
}
