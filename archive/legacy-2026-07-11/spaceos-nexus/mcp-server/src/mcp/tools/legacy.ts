import { z } from 'zod';
import { McpContext } from '../middleware/contextMiddleware';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';
import { SystemContext } from '../../plugins/PluginTypes';

/**
 * LegacyPlugin
 *
 * Provides thin backward-compatibility wrappers around older MCP tools that
 * existed prior to the decorator/plugin refactor.  Each legacy handler simply
 * logs a deprecation warning and then forwards the call to the current tool
 * implementation (if available) via PluginManager.invokeTool().
 *
 * This plugin is marked as deprecated so the PluginManager will include the
 * appropriate metadata; actual removal is scheduled for v2.0 (2026-06-01).
 */
@Plugin({
    id: 'legacy',
    name: 'Legacy Tools Adapter',
    version: '1.0.0',
    deprecated: true,
    deprecated_reason: 'Legacy compatibility layer; migrate to decorated tools',
    deprecation_removal: '2026-06-01'
})
export class LegacyPlugin extends BasePlugin {
    private readonly legacyMap: Record<string, string> = {
        // mapping of old tool names to their modern equivalents
        'search_knowledge': 'search_knowledge_base',
        'brainstorm': 'brainstorm',       // still same name, just wrapped
        // add additional legacy names here as needed
    };

    private logDeprecation(toolName: string) {
        const modern = this.legacyMap[toolName] || toolName;
        console.warn(`[DEPRECATED] Tool ${toolName} is provided via LegacyPlugin and will be removed in v2.0. Use ${modern} instead.`);
    }

    @Tool({
        name: 'search_knowledge',
        description: 'Legacy alias for search_knowledge_base (deprecated)',
        schema: z.any()
    })
    async searchKnowledge(args: any, context: McpContext) {
        this.logDeprecation('search_knowledge');
        if (this.context.pluginManager) {
            return this.context.pluginManager.invokeTool('search_knowledge_base', args, context);
        }
        return { success: false, error: { code: 503, message: 'PluginManager not available' } };
    }

    @Tool({
        name: 'brainstorm',
        description: 'Legacy brainstorming tool (deprecated but still functional)',
        schema: z.any()
    })
    async brainstorm(args: any, context: McpContext) {
        this.logDeprecation('brainstorm');
        // forward to whatever implementation exists, may be identity
        if (this.context.pluginManager) {
            try {
                return await this.context.pluginManager.invokeTool('brainstorm', args, context);
            } catch (e) {
                // if underlying tool missing, just return a generic error
                return { success: false, error: { code: 404, message: 'brainstorm tool not found' } };
            }
        }
        return { success: false, error: { code: 503, message: 'PluginManager not available' } };
    }
}

export default LegacyPlugin;
