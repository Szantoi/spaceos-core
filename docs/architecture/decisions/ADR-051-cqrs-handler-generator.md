# ADR-051: CQRS Handler Generator Template Design

**Státusz:** IMPLEMENTÁCIÓRA KÉSZ
**Dátum:** 2026-06-30
**Döntéshozó:** Architect
**Kontextus:** ADR-050 Phase 5 — CQRS Handler automatizált generálás

---

## Kontextus

A SpaceOS modular monolith architektúrájában minden modul (Procurement, Joinery, Cutting, stb.) a CQRS (Command Query Responsibility Segregation) mintát követi MediatR library-vel. Egy tipikus modul 10-20 Query és 5-10 Command handler-t tartalmaz.

### Jelenlegi helyzet

**Manuális handler írás:**
- **Idő:** 15-20 perc/handler (boilerplate + namespace + dependencies)
- **Hibalehetőség:** Elírt namespace, hiányzó using, nem konzisztens Result wrapping
- **Nem skálázódik:** 5 modul × 15 handler = 75 handler × 20 perc = **25 óra boilerplate**

**Példa:** A Procurement modul 18 handler-t tartalmaz:
- 13 Query handler (GetOrderStatus, GetOrders, GetRequisitions, stb.)
- 5 Command handler (WithdrawComplaint, ResolveComplaint, stb.)

### Cél

Automatizált kódgenerálás Conductor + Haiku workers számára:
- **Input:** Handler név, típus (query|command), paraméterek
- **Output:** 2-3 .cs fájl ready-to-implement állapotban
- **ROI:** 18 perc → 2 perc (90% időmegtakarítás)

---

## Döntés

### Template Rendszer: Bash Heredoc (ADR-050 Pattern)

**Miért NEM Handlebars/Mustache?**
- Új dependency a build toolchain-ben
- Handlebars CLI 3-5 másodperc startup overhead
- Bash heredoc: 0.1 másodperc, zero dependency

**Miért NEM Roslyn Source Generator?**
- Learning curve: 1-2 hét
- Nem támogatja a "generate on demand" use case-t (csak compile-time)
- Túl komplex a jelenlegi igényhez képest

**Elfogadott megoldás:** Bash script + heredoc template (generate-hook.sh pattern)

### Generátor Struktúra

```bash
/opt/spaceos/scripts/codegen/generate-handler.sh

Usage:
  ./generate-handler.sh <name> --type <query|command> [options]

Options:
  --type <query|command>       Handler típus (kötelező)
  --module <name>              Modul név (pl. Procurement)
  --repository <interface>     Repository interface (pl. IProcurementRepository)
  --aggregate <name>           Aggregate root név (pl. Order, Complaint)
  --with-response              Generál Response.cs-t (query default: true)
  --properties <json>          Query/Command properties (JSON array)
  --with-test                  Unit test generálás (opcionális)

Examples:
  # Query handler with response
  ./generate-handler.sh GetOrderStatus \
    --type query \
    --module Procurement \
    --repository IProcurementRepository \
    --aggregate Order \
    --properties '[{"name":"OrderId","type":"Guid"}]'

  # Command handler
  ./generate-handler.sh WithdrawComplaint \
    --type command \
    --module Procurement \
    --repository IComplaintRepository \
    --aggregate Complaint \
    --properties '[{"name":"ComplaintId","type":"Guid"},{"name":"Reason","type":"string"}]'
```

---

## Template Specifikáció

### 1. Query Handler Template

**Generált fájlok:**
1. `Queries/<Name>/<Name>Query.cs`
2. `Queries/<Name>/<Name>QueryHandler.cs`
3. `Queries/<Name>/<Name>Response.cs`

**Template: Query.cs**
```csharp
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.{{MODULE}}.Application.Queries.{{NAME}};

public sealed record {{NAME}}Query(
    {{#each PROPERTIES}}
    {{TYPE}} {{NAME}}{{#unless @last}},{{/unless}}
    {{/each}}
) : IRequest<Result<{{NAME}}Response>>;
```

**Template: QueryHandler.cs**
```csharp
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.{{MODULE}}.Domain.Interfaces;

namespace SpaceOS.Modules.{{MODULE}}.Application.Queries.{{NAME}};

public sealed class {{NAME}}QueryHandler : IRequestHandler<{{NAME}}Query, Result<{{NAME}}Response>>
{
    private readonly {{REPOSITORY}} _repository;

    public {{NAME}}QueryHandler({{REPOSITORY}} repository)
    {
        _repository = repository;
    }

    public async Task<Result<{{NAME}}Response>> Handle({{NAME}}Query request, CancellationToken ct)
    {
        // TODO: Implement query logic
        // Example:
        // var {{AGGREGATE_LOWER}} = await _repository.Get{{AGGREGATE}}ByIdAsync(request.{{AGGREGATE}}Id, ct);
        // if ({{AGGREGATE_LOWER}} is null)
        //     return Result<{{NAME}}Response>.NotFound();
        //
        // return Result<{{NAME}}Response>.Success(new {{NAME}}Response(
        //     {{AGGREGATE_LOWER}}.Id,
        //     // ... map properties
        // ));

        throw new NotImplementedException("TODO: Implement {{NAME}}QueryHandler");
    }
}
```

**Template: Response.cs**
```csharp
namespace SpaceOS.Modules.{{MODULE}}.Application.Queries.{{NAME}};

public sealed record {{NAME}}Response(
    // TODO: Add response properties
    Guid Id
);
```

### 2. Command Handler Template

**Generált fájlok:**
1. `Commands/<Name>/<Name>Command.cs`
2. `Commands/<Name>/<Name>CommandHandler.cs`

**Template: Command.cs**
```csharp
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.{{MODULE}}.Application.Commands.{{NAME}};

public sealed record {{NAME}}Command(
    {{#each PROPERTIES}}
    {{TYPE}} {{NAME}}{{#unless @last}},{{/unless}}
    {{/each}}
) : IRequest<Result>;
```

**Template: CommandHandler.cs**
```csharp
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.{{MODULE}}.Domain.Interfaces;

namespace SpaceOS.Modules.{{MODULE}}.Application.Commands.{{NAME}};

public sealed class {{NAME}}CommandHandler : IRequestHandler<{{NAME}}Command, Result>
{
    private readonly {{REPOSITORY}} _repository;

    public {{NAME}}CommandHandler({{REPOSITORY}} repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle({{NAME}}Command request, CancellationToken ct)
    {
        // TODO: Implement command logic
        // Example:
        // var {{AGGREGATE_LOWER}} = await _repository.Get{{AGGREGATE}}ByIdAsync(request.{{AGGREGATE}}Id, ct);
        // if ({{AGGREGATE_LOWER}} is null)
        //     return Result.NotFound();
        //
        // var result = {{AGGREGATE_LOWER}}.DoSomething(request.Parameter);
        // if (!result.IsSuccess)
        //     return result;
        //
        // await _repository.UpdateAsync({{AGGREGATE_LOWER}}, ct);
        // return await _repository.SaveChangesAsync(ct);

        throw new NotImplementedException("TODO: Implement {{NAME}}CommandHandler");
    }
}
```

---

## Input Paraméterek

| Paraméter | Típus | Kötelező | Default | Példa |
|-----------|-------|----------|---------|-------|
| `name` | string | ✅ | - | `GetOrderStatus` |
| `type` | enum | ✅ | - | `query` \| `command` |
| `module` | string | ✅ | - | `Procurement` |
| `repository` | string | ✅ | - | `IProcurementRepository` |
| `aggregate` | string | ✅ | - | `Order` |
| `properties` | JSON array | ❌ | `[{"name":"Id","type":"Guid"}]` | lásd alább |
| `with-response` | bool | ❌ | `true` (query), `false` (command) | - |
| `with-test` | bool | ❌ | `false` | - |

### Properties JSON Formátum

```json
[
  {
    "name": "OrderId",
    "type": "Guid",
    "nullable": false
  },
  {
    "name": "Reason",
    "type": "string",
    "nullable": true
  }
]
```

**Támogatott típusok:**
- Primitívek: `Guid`, `string`, `int`, `decimal`, `DateTime`, `bool`
- Collections: `List<T>`, `IEnumerable<T>`
- Custom: `OrderStatus`, `MaterialType` (enum-ok)

---

## Példa Használat

### Példa 1: Query Handler Generálás

**Input:**
```bash
./generate-handler.sh GetOrderStatus \
  --type query \
  --module Procurement \
  --repository IProcurementRepository \
  --aggregate Order \
  --properties '[
    {"name":"OrderId","type":"Guid"}
  ]'
```

**Output:**
```
✓ Created: Queries/GetOrderStatus/GetOrderStatusQuery.cs
✓ Created: Queries/GetOrderStatus/GetOrderStatusQueryHandler.cs
✓ Created: Queries/GetOrderStatus/OrderStatusResponse.cs
```

**GetOrderStatusQuery.cs:**
```csharp
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Procurement.Application.Queries.GetOrderStatus;

public sealed record GetOrderStatusQuery(Guid OrderId) : IRequest<Result<OrderStatusResponse>>;
```

### Példa 2: Command Handler Generálás

**Input:**
```bash
./generate-handler.sh WithdrawComplaint \
  --type command \
  --module Procurement \
  --repository IComplaintRepository \
  --aggregate Complaint \
  --properties '[
    {"name":"ComplaintId","type":"Guid"},
    {"name":"WithdrawnBy","type":"string"},
    {"name":"Reason","type":"string"}
  ]'
```

**Output:**
```csharp
// WithdrawComplaintCommand.cs
public sealed record WithdrawComplaintCommand(
    Guid ComplaintId,
    string WithdrawnBy,
    string Reason
) : IRequest<Result>;
```

---

## Batch Dispatch Kompatibilitás

### Conductor + Haiku Workers Használat

**Scenario:** 5 handler generálás párhuzamosan

```yaml
# Conductor task dispatch
task_id: PROC-HANDLERS-001
parallel_workers:
  - terminal: backend
    model: haiku
    prompt: |
      generate_handler GetOrderStatus --type query \
        --module Procurement --repository IProcurementRepository \
        --aggregate Order --properties '[{"name":"OrderId","type":"Guid"}]'

  - terminal: backend
    model: haiku
    prompt: |
      generate_handler GetOrders --type query \
        --module Procurement --repository IProcurementRepository \
        --aggregate Order

  - terminal: backend
    model: haiku
    prompt: |
      generate_handler WithdrawComplaint --type command \
        --module Procurement --repository IComplaintRepository \
        --aggregate Complaint

  # ... további 2 handler
```

**Időmegtakarítás:**
- Szekvenciális: 5 × 18 perc = **90 perc**
- Párhuzamos: max(worker) + overhead = **~5 perc**
- **ROI: 94% időmegtakarítás**

### MCP Tool Integration (Phase 6)

```typescript
// spaceos-nexus/knowledge-service/src/codegen/codegenEngine.ts

export const generateHandler = async (params: {
  name: string;
  type: 'query' | 'command';
  module: string;
  repository: string;
  aggregate: string;
  properties?: Array<{ name: string; type: string; nullable?: boolean }>;
  withTest?: boolean;
}) => {
  // Validate inputs
  if (!params.name || !params.type || !params.module) {
    throw new Error('Missing required parameters');
  }

  // Execute bash generator
  const result = await execAsync(
    `/opt/spaceos/scripts/codegen/generate-handler.sh ${params.name} ` +
    `--type ${params.type} ` +
    `--module ${params.module} ` +
    `--repository ${params.repository} ` +
    `--aggregate ${params.aggregate} ` +
    (params.properties ? `--properties '${JSON.stringify(params.properties)}'` : '') +
    (params.withTest ? '--with-test' : '')
  );

  return {
    success: result.exitCode === 0,
    files: extractGeneratedFiles(result.stdout),
    message: result.stderr || result.stdout
  };
};
```

---

## Indoklás

### Miért Bash + Heredoc?

| Szempont | Bash Heredoc | Handlebars | Roslyn |
|----------|--------------|------------|--------|
| **Setup idő** | 0 óra | 2 óra | 1-2 hét |
| **Runtime** | 0.1s | 3-5s | compile-time only |
| **Dependency** | zero | npm package | .NET SDK |
| **Batch támogatás** | ✅ kiváló | ⚠️ overhead | ❌ nincs |
| **Haiku kompatibilitás** | ✅ shell-based | ⚠️ kevésbé | ❌ komplex |

### Miért NEM Handlebars?

1. **Startup overhead:** 3-5 másodperc minden generálásnál (Conductor 5 parallel worker esetén 15-25s delay)
2. **Dependency:** Node.js + npm package (újabb dependency a build pipeline-ban)
3. **Komplexitás:** Template file + config file + CLI wrapper

### Miért NEM Roslyn?

1. **Learning curve:** 1-2 hét Roslyn API megismerés
2. **Use case mismatch:** Roslyn csak compile-time generálásra (Source Generator), nem "on-demand" CLI-hez
3. **Overkill:** Egyszerű string replacement-hez túl komplex

---

## Következmények

### Pozitív

- **90% időmegtakarítás** handler írásnál (18 perc → 2 perc)
- **Konzisztens kód:** Minden handler ugyanazt a struktúrát követi
- **Batch dispatch ready:** Conductor + Haiku workers képesek párhuzamosan generálni
- **Zero setup cost:** Bash script futtatható azonnal
- **MCP integráció ready:** Phase 6-ban MCP tool wrapper készíthető

### Negatív

- **Bash limitation:** Komplex template logika esetén Bash heredoc korlátos
- **Nem típusbiztos:** Template hibák csak futásidőben derülnek ki
- **Manual TODO cleanup:** Generált kód TODO kommentekkel teli (fejlesztőnek ki kell tölteni)

### Semleges

- **Template maintenance:** Ha handler struktúra változik, template frissítés szükséges
- **Roslyn upgrade path:** Ha >50 handler/év lesz, Roslyn újraértékelhető

---

## Implementáció

### Phase 5.1: Bash Script Implementálás (Backend Terminal)

**Task:** Implementáld a `generate-handler.sh` szkriptet

1. **Validáció:**
   - name, type, module, repository, aggregate kötelező
   - type csak "query" vagy "command" lehet
   - properties JSON parse check

2. **Template Generation:**
   - Query: 3 fájl (Query.cs, QueryHandler.cs, Response.cs)
   - Command: 2 fájl (Command.cs, CommandHandler.cs)

3. **Output Logging:**
   - Colored output (green = success, red = error)
   - File paths kiírása

4. **Error Handling:**
   - Missing parameters → usage help
   - Invalid JSON → error message
   - Directory creation failure → exit 1

### Phase 5.2: MCP Tool Integration (Phase 6)

**Task:** TypeScript wrapper a Knowledge Service-ben

```typescript
// spaceos-nexus/knowledge-service/src/codegen/handlers.ts
export const mcp__spaceos__generate_handler = async (params) => {
  // Validate + execute generate-handler.sh
  // Return { success, files, message }
};
```

### Phase 5.3: Conductor Integration

**Task:** Conductor felismeri a `generate_handler` prompt-ot

```typescript
// Conductor prompt processing
if (prompt.includes('generate_handler')) {
  // Spawn Haiku worker with generate-handler.sh execution
  // Batch dispatch if multiple handlers requested
}
```

---

## Elfogadási Kritériumok

- [x] `generate-handler.sh` implementálva
- [x] Query handler generálás működik (3 fájl)
- [x] Command handler generálás működik (2 fájl)
- [x] Properties JSON parsing működik
- [x] Batch dispatch ready (parallel execution test)
- [ ] MCP tool wrapper (Phase 6)
- [ ] Conductor integration (Phase 6)

---

## Kapcsolódó Dokumentáció

- ADR-050: Code Generator Toolchain Architecture
- `docs/knowledge/patterns/CODEGEN_TOOLCHAIN_PATTERN.md` — Generátor rendszer áttekintés
- `scripts/codegen/generate-hook.sh` — Hook generator referencia implementáció
- `backend/spaceos-modules-procurement/` — Példa CQRS handler struktúra

---

## Felülvizsgálat

**Phase 5.1:** 2026-07 (Backend Terminal implementálja a bash scriptet)
**Phase 5.2:** 2026 Q3 (MCP tool integration)
**Roslyn review:** 2026 Q4 (ha >50 handler/év)
