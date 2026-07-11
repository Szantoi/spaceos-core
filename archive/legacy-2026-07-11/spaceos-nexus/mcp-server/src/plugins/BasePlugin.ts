import { IToolModule, IToolDefinition, ToolHandler } from '../mcp/tools/IToolModule';
import { SystemContext } from './PluginTypes';

/**
 * Base class for all plugins.
 * Automatically extracts metadata from @Plugin and @Tool decorators.
 */
export abstract class BasePlugin implements IToolModule {
    public name: string;
    public version: string;
    public dependencies?: string[];
    public tools: IToolDefinition[] = [];
    public handlers: Record<string, ToolHandler> = {};

    constructor(protected context: SystemContext) {
        // Extract plugin config from constructor
        const config = (this.constructor as any)._pluginConfig;
        if (!config) {
            throw new Error(`Plugin configuration missing for ${this.constructor.name}. Did you forget @Plugin?`);
        }

        this.name = config.name;
        this.version = config.version;
        this.dependencies = config.dependencies;

        // Extract tools from prototype
        const decoratedTools = (this.constructor.prototype as any)._tools || [];
        for (const tool of decoratedTools) {
            this.tools.push({
                name: tool.name,
                description: tool.description,
                inputSchema: tool.schema
            });

            this.handlers[tool.name] = (args, mcpContext) => {
                return (this as any)[tool.method](args, mcpContext);
            };
        }
    }
}
