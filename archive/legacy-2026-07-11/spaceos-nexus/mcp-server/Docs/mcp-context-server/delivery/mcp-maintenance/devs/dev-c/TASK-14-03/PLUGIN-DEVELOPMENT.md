# Plugin Development Guide

A JoineryTech Plugin System lehetővé teszi a rendszer funkcióinak moduláris bővítését.

## Plugin felépítése

Egy pluginnek implementálnia kell az `IToolModule` interfészt, és használnia kell a `@Plugin` dekorátort.

```typescript
import { Plugin, Tool } from "../plugins/PluginDecorators";
import { IToolModule } from "./IToolModule";

@Plugin({
    name: "my-plugin",
    version: "1.0.0",
    dependencies: ["bootstrap"] // Opcionális függőségek
})
export class MyPlugin implements IToolModule {
    name = "my-plugin";
    version = "1.0.0";
    tools = [
        {
            name: "my_tool",
            description: "Leírás",
            inputSchema: z.object({})
        }
    ];
    handlers = {
        "my_tool": async (args, context) => {
            // Logika
        }
    };

    lifecycle = {
        onInit: async () => {
            console.log("Plugin inicializálva");
        },
        onDestroy: async () => {
            console.log("Plugin leállítva");
        }
    };
}
```

## Függőségkezelés

A rendszer automatikusan feloldja a függőségeket:
1. Topológiai sorrendben tölt be (függőségek előbb).
2. Detektálja a körkörös függőségeket (`CircularDependencyError`).
3. Jelzi a hiányzó függőségeket (`DependencyNotFoundError`).

## Lifecycle Hookok

- `onInit`: A plugin betöltésekor, de a regisztráció előtt fut le. Itt végezhetők el a DB kapcsolódások, inicializálások.
- `onDestroy`: A plugin eltávolításakor fut le. Erőforrások felszabadítására szolgál.
- `onError`: Plugin-szintű hibák kezelésére.

## Plugin betöltése

```typescript
const manager = new PluginManager();
manager.registerManifest({
    name: "my-plugin",
    version: "1.0.0",
    entry: "./path/to/plugin"
});

await manager.loadPlugin("my-plugin", true); // true = kritikus hiba esetén megáll
```
