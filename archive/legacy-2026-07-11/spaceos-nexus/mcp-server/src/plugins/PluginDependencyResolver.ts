import { PluginManifest, CircularDependencyError, DependencyNotFoundError } from './PluginTypes';

/**
 * Logic for resolving plugin dependencies and determining load order.
 */
export class PluginDependencyResolver {
    private visited = new Set<string>();
    private recursionStack = new Set<string>();

    /**
     * Resolves dependencies for a given plugin and returns the correct load order (dependencies first).
     */
    public resolveDependencies(pluginName: string, manifests: Map<string, PluginManifest>): string[] {
        this.visited.clear();
        this.recursionStack.clear();

        const order: string[] = [];
        this.buildLoadOrder(pluginName, manifests, order, []);

        return order;
    }

    /**
     * Internal recursive method for topological sorting and cycle detection.
     */
    private buildLoadOrder(
        name: string,
        manifests: Map<string, PluginManifest>,
        order: string[],
        path: string[]
    ): void {
        if (this.recursionStack.has(name)) {
            const cycleStart = path.indexOf(name);
            const cycle = [...path.slice(cycleStart), name];
            throw new CircularDependencyError(cycle);
        }

        if (this.visited.has(name)) return;

        const manifest = manifests.get(name);
        if (!manifest) {
            // If we're in a dependency check and the manifest is missing,
            // it means a required dependency is not registered.
            if (path.length > 0) {
                const parent = path[path.length - 1];
                throw new DependencyNotFoundError(parent, name);
            }
            throw new Error(`Plugin manifest not found: ${name}`);
        }

        this.recursionStack.add(name);
        path.push(name);

        if (manifest.dependencies) {
            for (const dep of manifest.dependencies) {
                this.buildLoadOrder(dep, manifests, order, path);
            }
        }

        this.recursionStack.delete(name);
        this.visited.add(name);
        order.push(name);
    }
}
