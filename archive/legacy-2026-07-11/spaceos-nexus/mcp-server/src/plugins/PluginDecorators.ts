/**

/**
 * Decorator for plugin metadata.
 */
export function Plugin(config: { id?: string; name: string; version: string; dependencies?: string[]; deprecated?: boolean; deprecated_reason?: string; deprecation_removal?: string }) {
    return function <T extends { new(...args: any[]): {} }>(constructor: T) {
        (constructor as any)._pluginConfig = config;
        return constructor;
    };
}

/**
 * Decorator for tagging methods as MCP tools.
 */
export function Tool(config: { name: string; description: string; schema: any }) {
    return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
        if (!target._tools) target._tools = [];
        target._tools.push({
            name: config.name,
            method: propertyKey,
            description: config.description,
            schema: config.schema
        });
        return descriptor;
    };
}
