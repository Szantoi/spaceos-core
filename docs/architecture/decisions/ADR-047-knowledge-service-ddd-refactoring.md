# ADR-047: Knowledge Service DDD Refactoring

**Status:** Accepted  
**Date:** 2026-06-24  
**Author:** Root Terminal  

## Context

A `spaceos-nexus/knowledge-service` kódbázis gyorsan nőtt, és két fő fájl (server.ts: 3159 sor, mcp.ts: 2035 sor) nehezen karbantarthatóvá vált. A kód nem követett egységes architektúra mintákat, ami megnehezítette:

- Új fejlesztők onboardingját
- Unit tesztek írását
- Funkciók izolált módosítását
- Üzleti logika és infrastruktúra elkülönítését

## Decision

A knowledge-service kódbázist Domain-Driven Design (DDD) és Clean Architecture elvek szerint refaktoráljuk, 4 rétegű struktúrába:

### 1. Core Layer (`src/core/`)

Framework-független típusdefiníciók és hibaosztályok:

```
core/
├── types/
│   ├── common.ts      # MessageStatus, Priority, ModelType, BoxType enums
│   ├── terminal.ts    # TerminalName, TerminalState, FocusItem, FocusQueue
│   └── message.ts     # MessageFrontmatter, InboxMessageInput, MessageQuery
└── errors/
    └── domain-error.ts  # DomainError, NotFoundError, ValidationError, stb.
```

**Szabályok:**
- Nincs külső függőség (csak TypeScript built-in típusok)
- Readonly interfészek az immutabilitáshoz
- Union type-ok a domain fogalmakhoz (TerminalName, Priority)

### 2. Domain Layer (`src/domain/`)

Üzleti logika, entitások és domain service-ek:

```
domain/
├── terminal/
│   ├── terminal.entity.ts     # Terminal aggregate root, factory methods
│   ├── terminal.repository.ts # ITerminalRepository interface (port)
│   └── terminal.service.ts    # TerminalService (koordináció)
└── mailbox/
    ├── message.entity.ts      # MessageEntity, InboxMessageBuilder
    ├── mailbox.repository.ts  # IMailboxRepository interface (port)
    └── mailbox.service.ts     # MailboxService (koordináció)
```

**Szabályok:**
- Csak core layer-re hivatkozhat
- Repository interfészek (portok), nem implementációk
- Aggregate root-ok factory method-okkal
- Domain service-ek a több entitást érintő logikához

### 3. Infrastructure Layer (`src/infrastructure/`)

Külső rendszerek integrációja (file system, SQLite, ChromaDB):

```
infrastructure/
└── persistence/
    └── file-system/
        └── file-terminal.repository.ts  # ITerminalRepository implementáció
```

**Szabályok:**
- Repository interfészek implementációi
- Külső szolgáltatások (DB, file system, API) wrapperek
- Caching, connection pooling

### 4. Interfaces Layer (`src/interfaces/`)

HTTP route-ok és MCP tool-ok:

```
interfaces/
├── http/
│   └── routes/
│       ├── health.routes.ts    # GET /health, /ready, /live
│       ├── pipeline.routes.ts  # SSE /pipeline/events
│       └── index.ts
└── mcp/
    └── tools/
        ├── base-tool.ts        # ToolRegistry, ToolDefinition
        └── index.ts
```

**Szabályok:**
- Express Router-alapú route modulok
- MCP tool definíciók és handlerek
- Input validáció és error handling

## Consequences

### Pozitív

1. **Tesztelhetőség**: Domain logika izoláltan tesztelhető mock repository-kkal
2. **Karbantarthatóság**: Kisebb, fókuszált fájlok (~100-300 sor)
3. **Onboarding**: Egyértelmű struktúra, rétegenkénti felelősségek
4. **Rugalmasság**: Repository implementáció cserélhető (file → SQLite → PostgreSQL)
5. **Type Safety**: Core típusok újrafelhasználhatók minden rétegben

### Negatív

1. **Komplexitás**: Több fájl és mappa kezelése
2. **Boilerplate**: Repository interfészek + implementációk duplikáció
3. **Tanulási görbe**: DDD/Clean Architecture fogalmak elsajátítása

### Migráció

A refaktorálás inkrementálisan történik:

1. ✅ **Phase 1**: Core és domain rétegek létrehozása (2026-06-24)
2. ✅ **Phase 2**: Interfaces layer struktúra (health, pipeline routes)
3. ⏳ **Phase 3**: server.ts route-ok migrálása
4. ⏳ **Phase 4**: mcp.ts tool-ok migrálása
5. ⏳ **Phase 5**: Infrastructure repository implementációk

## Implementation Notes

### Build Configuration

`tsconfig.json` módosítva:
```json
{
  "exclude": ["node_modules", "dist", "src/__tests__", "src/_wip"]
}
```

### Export Structure

Minden réteg `index.ts`-en keresztül exportál:
```typescript
// src/core/index.ts
export * from './types';
export * from './errors';

// src/domain/index.ts
export * from './terminal';
export * from './mailbox';

// src/interfaces/index.ts
export * from './http/routes';
export * from './mcp';
```

## References

- Clean Architecture (Robert C. Martin)
- Domain-Driven Design (Eric Evans)
- Ports and Adapters / Hexagonal Architecture (Alistair Cockburn)
