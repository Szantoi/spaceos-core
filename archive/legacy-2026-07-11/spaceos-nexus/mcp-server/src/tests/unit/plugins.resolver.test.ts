import { describe, test, expect, vi, beforeEach } from 'vitest';
import { PluginManager } from '../../plugins/PluginManager';
import { PluginStatus, CircularDependencyError, DependencyNotFoundError } from '../../plugins/PluginTypes';

// Mock types to satisfy the ToolModule interface
class MockPlugin {
    name = "mock";
    version = "1.0.0";
    tools = [];
    handlers = {};
}

describe('Plugin System - Unit Tests', () => {
    let manager: PluginManager;

    beforeEach(() => {
        manager = new PluginManager();
        // In Vitest, we need to mock the dynamic import if we want to test loadPlugin
        // without real files, but since we are in a dev environment,
        // let's try to test the Resolver and status logic first.
    });

    test('UT-09: Plugin health status reporting', async () => {
        const status = manager.getPluginStatus();
        expect(status.loaded).toEqual([]);
        expect(status.failed).toEqual([]);
        expect(status.notLoaded).toEqual([]);
    });

    test('UT-11: Circular dependency detection', async () => {
        manager.registerManifest({
            name: "A",
            version: "1.0.0",
            entry: "./A",
            dependencies: ["B"]
        });
        manager.registerManifest({
            name: "B",
            version: "1.0.0",
            entry: "./B",
            dependencies: ["A"]
        });

        // Use isCritical=true to ensure the error propagates
        await expect(manager.loadPlugin("A", true)).rejects.toThrow(CircularDependencyError);
    });

    test('UT-12: Missing dependency throws error', async () => {
        manager.registerManifest({
            name: "plugin-with-missing-dep",
            version: "1.0.0",
            entry: "./entry",
            dependencies: ["ghost-dependency"]
        });

        // Use isCritical=true to ensure the error propagates
        await expect(manager.loadPlugin("plugin-with-missing-dep", true)).rejects.toThrow(DependencyNotFoundError);
    });

    test('UT-18: Optional plugin failure recovery', async () => {
        // Critical plugin missing entry -> should throw
        manager.registerManifest({ name: "critical", version: "1.0.0", entry: "./non-existent" });
        await expect(manager.loadPlugin("critical", true)).rejects.toThrow();

        // Optional plugin missing entry -> should just warn (log) and mark as failed
        manager.registerManifest({ name: "optional", version: "1.0.0", entry: "./non-existent-opt" });
        await manager.loadPlugin("optional", false);

        const status = manager.getPluginStatus();
        expect(status.failed).toContain("optional");
        expect(status.loaded).not.toContain("optional");
    });
});
