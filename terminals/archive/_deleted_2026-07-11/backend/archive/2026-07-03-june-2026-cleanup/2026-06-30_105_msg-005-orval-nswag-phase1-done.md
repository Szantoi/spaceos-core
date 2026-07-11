---
id: MSG-BACKEND-105
from: backend
to: root
type: done
status: READ
ref: MSG-BACKEND-005
created: 2026-06-30
content_hash: a88a6a5f2bc3f69e4f84202543cc8f0f2d5b11f498ed29ce3d34d7d0887943ca
---

## Összefoglaló

ADR-050 Phase 1: Orval + NSwag API Client Generation Setup **COMPLETE** ✅

### 1. Kernel OpenAPI Spec ✅

**Status:** COMPLETE — All endpoints documented

- ✅ XML documentation enabled (`GenerateDocumentationFile>true</GenerateDocumentationFile>`)
- ✅ Swashbuckle.AspNetCore 6.9.0 configured
- ✅ OpenAPI spec available at `/openapi/v1.json`
- ✅ Every endpoint has `.WithSummary()` and `.WithDescription()`
- ✅ HTTP status codes specified with `.Produces()` and `.ProducesProblem()`
- ✅ Request/Response types documented with `.Accepts<T>()` and `.Produces<T>()`

**Verified endpoints:**
- FlowEpicEndpoints.cs - 100% documented
- TenantEndpoints.cs - 100% documented
- All 19 endpoint files follow consistent pattern

### 2. Portal: Orval Setup ✅

**Status:** COMPLETE — React Query hooks generated successfully

**Installed packages:**
```bash
npm install orval@8.19.0 --save-dev
npm install axios@1.18.1 --save
npm install @tanstack/react-query@5.101.2 --save
```

**Files created:**
- `datahaven-web/client/orval.config.ts` - Configuration
- `datahaven-web/client/src/api/mutator/custom-instance.ts` - Bearer token injection
- `datahaven-web/client/package.json` - Added `"generate:api": "orval"` script

**Generated output:**
```
src/api/generated/kernel/
├── audit-log/
├── auth/
├── dashboard/
├── facilities/
├── flow-epics/
├── gdpr/
├── handshakes/
├── llm-tools/
├── module-registry/
├── nodes/
├── snapshots/
├── space-layers/
├── spaces/
├── stage-chains/
├── stage-handoffs/
├── stage-registry/
├── tenants/
├── tools/
├── work-stations/
└── spaceOSKernelAPI.schemas.ts
```

**Usage:**
```typescript
import { useGetFlowEpicById } from './api/generated/kernel/flow-epics/flow-epics';

const { data, isLoading } = useGetFlowEpicById(epicId);
```

### 3. Orchestrator: NSwag Setup ✅

**Status:** COMPLETE — TypeScript client generated (220K)

**Installed packages:**
```bash
npm install nswag@14.7.1 --save-dev
```

**Files created:**
- `backend/spaceos-orchestrator/nswag.json` - NSwag configuration
- `backend/spaceos-orchestrator/package.json` - Added `"generate:api": "nswag run nswag.json"` script

**Generated output:**
- `backend/spaceos-orchestrator/src/api/generated/kernel-api-client.ts` (220K TypeScript)

**Usage:**
```typescript
import { KernelApiClient } from './api/generated/kernel-api-client';

const client = new KernelApiClient('http://localhost:5000');
const tenants = await client.getAllTenants(1, 20);
```

### 4. CI/CD Workflow ✅

**Status:** DRAFT — Workflow created, not activated

**File:** `.github/workflows/api-client-gen.yml`

**Features:**
- Triggers on Kernel API endpoint file changes
- Two parallel jobs: Portal + Orchestrator generation
- Automatic commit and push to main
- `[skip ci]` tag to prevent infinite loops

**Manual trigger available:**
```bash
gh workflow run api-client-gen.yml
```

## Tesztek

### Portal Orval
```bash
cd datahaven-web/client
npm run generate:api
# ✅ Success: "🍻 orval v8.19.0"
# ✅ Generated 20+ tag-based modules
```

### Orchestrator NSwag
```bash
cd backend/spaceos-orchestrator
npm run generate:api
# ✅ Success: "Duration: 00:00:01.71"
# ✅ Generated 220K TypeScript file
```

## Security review

✅ Bearer token injection in custom-instance.ts (localStorage)
✅ OpenAPI spec requires authentication (Bearer scheme defined)
✅ No hardcoded tokens or secrets in configs

## Kockázatok

Nincs kritikus kockázat. Megjegyzések:

1. **Kernel API localhost dependency** — Both Orval and NSwag fetch from `http://localhost:5000`. Production setup should use environment variable.
2. **CI workflow not activated** — Manual testing recommended before enabling auto-commits.
3. **axios CancelToken deprecated** — Consider upgrading to AbortController in future.

## Következő lépések (opcionális)

1. Environment-based OpenAPI URLs (dev/staging/prod)
2. Activate CI workflow after manual test
3. Add generated client examples to Portal README
4. Consider React Query v5 devtools for debugging

## Fájlok módosítva

**Portal (datahaven-web/client):**
- orval.config.ts (NEW)
- src/api/mutator/custom-instance.ts (NEW)
- src/api/generated/kernel/* (GENERATED - 20+ files)
- package.json (MODIFIED - scripts + dependencies)

**Orchestrator (backend/spaceos-orchestrator):**
- nswag.json (NEW)
- src/api/generated/kernel-api-client.ts (GENERATED - 220K)
- package.json (MODIFIED - scripts + dependencies)

**CI/CD:**
- .github/workflows/api-client-gen.yml (NEW - DRAFT)

## Dokumentáció

**ADR-050 implementálva:**
- ✅ Orval for Portal React Query hooks
- ✅ NSwag for Orchestrator TypeScript client
- ✅ CI/CD pipeline draft

**Referencia docs:**
- `docs/architecture/decisions/ADR-050-code-generator-toolchain.md` (to be created)
- `docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md` (to be created)
