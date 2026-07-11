import { z } from 'zod';
import { McpContext } from '../middleware/contextMiddleware';

/**
 * Interface representing a single MCP tool definition.
 */
export interface IToolDefinition {
    name: string;
    description: string;
    inputSchema: z.ZodObject<any>;
}

/**
 * Type for tool handler functions.
 */
export type ToolHandler = (args: any, context: McpContext) => Promise<any>;

/**
 * Interface for modular MCP tool plugins.
 * Each module defines a set of tools and their handlers.
 */
export interface IToolModule {
    name: string;
    version: string;
    dependencies?: string[];
    lifecycle?: {
        onInit?: () => Promise<void>;
        onDestroy?: () => Promise<void>;
        onError?: (error: Error) => void;
    };
    tools: IToolDefinition[];
    handlers: Record<string, ToolHandler>;
}
