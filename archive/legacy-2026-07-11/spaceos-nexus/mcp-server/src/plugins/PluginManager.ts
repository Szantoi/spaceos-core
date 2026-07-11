import { IToolModule } from '../mcp/tools/IToolModule';
import { PluginStatus, PluginManifest, CircularDependencyError, DependencyNotFoundError, SystemContext } from './PluginTypes';
import { PluginDependencyResolver } from './PluginDependencyResolver';
import * as path from 'node:path';
import { pathToFileURL } from 'node:url';

/**
 * Main manager for the JoineryTech Plugin System.
 * Orchestrates loading, unloading, and lifecycle management of plugins.
 */
export class PluginManager {
    private pluginRegistry = new Map<string, IToolModule>();
    private pluginStatus = new Map<string, PluginStatus>();
    private loadedPlugins = new Set<string>();
    private failedPlugins = new Map<string, Error>();
    private manifests = new Map<string, PluginManifest>();
    private resolver = new PluginDependencyResolver();
    private loadPromises = new Map<string, Promise<void>>();
    private systemContext: SystemContext;

    constructor(systemContext: SystemContext) {
        this.systemContext = systemContext;
    }

    /**
     * Loads a plugin and its dependencies.
     * @param pluginName Name of the plugin to load.
     * @param isCritical If true, failure to load this plugin will throw an error.
     */
    public async loadPlugin(pluginName: string, isCritical?: boolean): Promise<void> {
        // Avoid concurrent loads for the same plugin
        if (this.loadPromises.has(pluginName)) {
            return this.loadPromises.get(pluginName)!;
        }

        const loadPromise = (async () => {
            let manifest: PluginManifest | undefined;
            let instance: IToolModule | null = null;

            try {
                // If already loaded, we do nothing (idempotent)
                if (this.loadedPlugins.has(pluginName)) return;

                this.pluginStatus.set(pluginName, PluginStatus.LOADING);

                // In a real scenario, manifests would be loaded from files.
                // For this implementation, we assume they are pre-registered or discovered.
                manifest = this.manifests.get(pluginName);
                if (!manifest) {
                    throw new Error(`Manifest not found for plugin: ${pluginName}`);
                }

                const critical = isCritical ?? manifest.critical ?? false;

                // Resolve and load dependencies first
                const loadOrder = this.resolver.resolveDependencies(pluginName, this.manifests);
                for (const dep of loadOrder) {
                    if (dep !== pluginName && !this.loadedPlugins.has(dep)) {
                        // Dependencies of any plugin are considered critical for that plugin's operation
                        await this.loadPlugin(dep, true);
                    }
                }

                // Dynamic import of the plugin module
                // The entry point is defined in the manifest
                const modulePath = manifest.entry;

                // Use pathToFileURL for absolute paths to handle Windows drive letters correctly in import()
                const importUrl = path.isAbsolute(modulePath) ? pathToFileURL(modulePath).href : modulePath;

                let pluginModule;
                try {
                    // console.log(`[PluginManager] Attempting to import from: ${importUrl}`);
                    pluginModule = await import(importUrl);
                } catch (e: any) {
                    console.warn(`[PluginManager] Dynamic import failed for ${importUrl}. Error: ${e.message}`);
                    throw new Error(`Failed to load plugin module at ${modulePath}. Error: ${e.message}`);
                }

                // In CJS, the import() might return the module directly or wrapped in a 'default' property
                const PluginClass = pluginModule.default?.[manifest.className] || pluginModule[manifest.className];
                if (!PluginClass) {
                    throw new Error(`No plugin class found in module at ${modulePath}`);
                }

                instance = new PluginClass(this.systemContext);

                // AC-33: lifecycle.onInit() called before registration
                if (instance.lifecycle?.onInit) {
                    await instance.lifecycle.onInit();
                }

                // Register the plugin
                this.pluginRegistry.set(pluginName, instance);
                this.loadedPlugins.add(pluginName);
                this.pluginStatus.set(pluginName, PluginStatus.LOADED);

            } catch (error: any) {
                this.pluginStatus.set(pluginName, PluginStatus.FAILED);
                this.failedPlugins.set(pluginName, error as Error);

                // AC-09: Plugin lifecycle onError hook
                if (instance?.lifecycle?.onError) {
                    try {
                        await instance.lifecycle.onError(error as Error);
                    } catch (nested) {
                        console.error(`Plugin onError handler threw for ${pluginName}:`, nested);
                    }
                }

                // AC-34: Optional Plugin Error Recovery
                const critical = isCritical ?? manifest?.critical ?? false;
                if (critical) {
                    // Preserve specific plugin errors for testing and better error handling
                    if (error instanceof CircularDependencyError || error instanceof DependencyNotFoundError) {
                        throw error;
                    }
                    throw new Error(`Critical plugin failed to load: ${pluginName}`, { cause: error });
                } else {
                    console.warn(`Optional plugin failed to load: ${pluginName}. Continuing...`, error);
                }

            } finally {
                // Ensure we don't hold onto stale promises after completion.
                this.loadPromises.delete(pluginName);
            }
        })();

        this.loadPromises.set(pluginName, loadPromise);
        return loadPromise;
    }

    /**
     * Unloads a plugin and calls its onDestroy hook.
     */
    public async unloadPlugin(pluginName: string): Promise<void> {
        const plugin = this.pluginRegistry.get(pluginName);
        if (!plugin) return;

        this.pluginStatus.set(pluginName, PluginStatus.UNLOADING);

        try {
            if (plugin.lifecycle?.onDestroy) {
                await plugin.lifecycle.onDestroy();
            }
        } catch (error) {
            console.error(`Error during onDestroy for plugin ${pluginName}:`, error);
        } finally {
            this.pluginRegistry.delete(pluginName);
            this.loadedPlugins.delete(pluginName);
            this.pluginStatus.set(pluginName, PluginStatus.NOT_LOADED);
        }
    }

    /**
     * Manually registers a manifest for a plugin (simulating discovery).
     */
    public registerManifest(manifest: PluginManifest): void {
        this.manifests.set(manifest.name, manifest);
    }

    /**
     * Routes a tool call to the appropriate plugin.
     */
    public async invokeTool(toolName: string, args: any, context: any): Promise<any> {
        for (const [pluginName, plugin] of this.pluginRegistry) {
            if (plugin.handlers[toolName]) {
                return await plugin.handlers[toolName](args, context);
            }
        }
        throw new Error(`Tool "${toolName}" not found in any loaded plugin.`);
    }

    /**
     * Lists all tools from all loaded plugins.
     */
    public listTools(): any[] {
        const allTools: any[] = [];
        for (const [pluginName, plugin] of this.pluginRegistry) {
            allTools.push(...plugin.tools.map(t => ({ ...t, plugin: pluginName })));
        }
        return allTools;
    }

    /**
     * Returns a read-only view of the plugin registry.
     * This prevents external code from mutating the internal registry map.
     */
    public getPluginRegistry(): ReadonlyMap<string, IToolModule> {
        // Return a shallow copy to prevent external mutation of the internal registry.
        return new Map(this.pluginRegistry);
    }

    /**
     * Returns all successfully loaded plugin modules.
     */
    public getLoadedPluginModules(): IToolModule[] {
        return Array.from(this.pluginRegistry.values()).filter((t): t is IToolModule => t !== null);
    }

    /**
     * AC-15: Returns current health status of all plugins.
     */
    public getPluginStatus() {
        const loaded = Array.from(this.loadedPlugins);
        const failed = Array.from(this.failedPlugins.keys());
        const notLoaded = Array.from(this.manifests.keys()).filter(name => !loaded.includes(name) && !failed.includes(name));

        return {
            loaded,
            failed,
            notLoaded,
            details: Array.from(this.pluginStatus.entries()).map(([name, status]) => ({
                name,
                status,
                error: this.failedPlugins.get(name)?.message
            }))
        };
    }
}
