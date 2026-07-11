---
id: MSG-BACKEND-005
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
ref: ADR-050
content_hash: 9a3952248f5cbf2bfba1099a1705b904512528b654a9036203c59f5d1ba6ed94
---

# Phase 1: Orval + NSwag API Client Generation Setup

## Kontextus

ADR-050 alapján bevezetjük az automatikus API client generálást:
- **Orval** — Portal React Query hooks (OpenAPI → TypeScript)
- **NSwag** — Orchestrator TypeScript client

## Feladat

### 1. Kernel OpenAPI Docs Kiegészítése

Ellenőrizd és egészítsd ki a Kernel API XML dokumentációt:

```csharp
/// <summary>
/// Creates a new flow epic
/// </summary>
/// <param name="command">Flow epic creation command</param>
/// <returns>Created flow epic ID</returns>
[HttpPost]
public async Task<ActionResult<Guid>> Create(CreateFlowEpicCommand command)
```

**Ellenőrizendő:**
- Minden public endpoint dokumentált
- Request/Response típusok leírva
- HTTP status kódok specifikálva

### 2. Portal: Orval Setup

```bash
cd datahaven-web/client
npm install orval --save-dev
```

Konfiguráció (`orval.config.ts`):
```typescript
export default {
  kernel: {
    input: '../api/openapi/kernel.json', // vagy URL
    output: {
      mode: 'tags-split',
      target: 'src/api/generated/kernel',
      client: 'react-query',
      override: {
        mutator: {
          path: 'src/api/mutator/custom-instance.ts',
          name: 'customInstance',
        },
      },
    },
  },
};
```

Package.json script:
```json
{
  "scripts": {
    "generate:api": "orval"
  }
}
```

### 3. Orchestrator: NSwag Setup

```bash
cd spaceos-orchestrator
npm install nswag --save-dev
```

Vagy dotnet tool:
```bash
dotnet tool install NSwag.ConsoleCore
```

### 4. CI/CD Integráció

GitHub Actions workflow (`.github/workflows/api-client-gen.yml`):
```yaml
name: API Client Generation
on:
  push:
    paths:
      - 'spaceos-kernel/**/openapi.json'
jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Generate Portal clients
        run: cd datahaven-web/client && npm run generate:api
      - name: Commit changes
        run: |
          git add src/api/generated/
          git commit -m "chore: regenerate API clients" || true
          git push
```

## Elfogadási Kritériumok

- [ ] Kernel OpenAPI spec teljes (minden endpoint dokumentált)
- [ ] Portal: `npm run generate:api` sikeresen fut
- [ ] Generated hooks használhatók (pl. `useGetFlowEpics()`)
- [ ] Orchestrator: NSwag client generálás működik
- [ ] CI workflow elkészítve (draft OK, nem kell aktív)

## Dokumentáció

- `docs/architecture/decisions/ADR-050-code-generator-toolchain.md`
- `docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md`

## Megjegyzés

Ha a Kernel OpenAPI spec nem elérhető vagy hiányos, jelezd BLOCKED-ként és sorold fel a hiányzó endpoint-okat.
