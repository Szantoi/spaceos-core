import { describe, expect, test } from 'vitest';
import {
    ResourceResolutionError,
    ResourceTemplateRegistry,
    RoleResourceTemplate,
    WorkflowResourceTemplate,
    TemplateCategoryResourceTemplate,
    DiscoveryPhaseResourceTemplate,
    TaskContextResourceTemplate
} from '../../mcp/resources/resourceTemplates';

describe('TASK-14-08 Resource Templates', () => {
    test('AC-2: role URI pattern matches and extracts params', () => {
        const template = new RoleResourceTemplate();
        const match = template.matchUri('resource://role/engineering/backend_developer');

        expect(match.matched).toBe(true);
        expect(match.params).toEqual({
            domain: 'engineering',
            role: 'backend_developer'
        });
    });

    test('AC-4: registry lists registered templates', () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());
        registry.register(new WorkflowResourceTemplate());

        const resources = registry.listResources();
        const patterns = resources.map(item => item.uriPattern);

        expect(patterns).toContain('resource://role/{domain}/{role}');
        expect(patterns).toContain('resource://workflow/{type}');
    });

    test('AC-3: role resource resolves content JSON', async () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());

        const resource = await registry.resolveUri('resource://role/engineering/backend_developer');
        const parsed = JSON.parse(resource.contents) as { domain: string; role: string; content: string };

        expect(resource.uri).toBe('resource://role/engineering/backend_developer');
        expect(resource.mimeType).toBe('application/json');
        expect(parsed.domain).toBe('engineering');
        expect(parsed.role).toBe('backend_developer');
        expect(parsed.content.length).toBeGreaterThan(0);
    });

    test('AC-5: resolved resource metadata does not expose file paths', async () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());

        const resource = await registry.resolveUri('resource://role/engineering/backend_developer');
        expect(resource.uri.includes('database')).toBe(false);
        expect(resource.name.includes('database')).toBe(false);
        expect(resource.description?.includes('database') ?? false).toBe(false);
    });

    test('AC-6: missing role returns 404 ResourceResolutionError', async () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());

        await expect(registry.resolveUri('resource://role/engineering/not_a_real_role'))
            .rejects
            .toMatchObject<ResourceResolutionError>({
                name: 'ResourceResolutionError',
                statusCode: 404
            });
    });

    test('default registration includes all required template categories', () => {
        const registry = new ResourceTemplateRegistry();
        registry.registerDefaults();

        const patterns = registry.listResources().map(item => item.uriPattern);
        expect(patterns).toContain('resource://role/{domain}/{role}');
        expect(patterns).toContain('resource://workflow/{type}');
        expect(patterns).toContain('resource://template/{category}');
        expect(patterns).toContain('resource://discovery/{phase}');
        expect(patterns).toContain('resource://task/{task_id}');
    });

    test('individual templates are constructible for registry usage', () => {
        expect(new TemplateCategoryResourceTemplate()).toBeDefined();
        expect(new DiscoveryPhaseResourceTemplate()).toBeDefined();
        expect(new TaskContextResourceTemplate()).toBeDefined();
    });
});
