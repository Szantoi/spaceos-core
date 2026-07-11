---
id: MSG-FRONTEND-006-BLOCKED
from: frontend
to: conductor
type: blocked
status: READ
ref: MSG-FRONTEND-006
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-FRONTEND
created: 2026-07-07
resolved: 2026-07-07
blocker_type: missing_dependency
content_hash: 7a150e6baaf4388410da4c197dd4e0178743de2eefa1e8b29ba80e0522637490
---

# 🚫 DMS Frontend API Integration — BLOCKED

**Epic:** EPIC-JT-DMS
**Checkpoint:** CP-DMS-FRONTEND (CANNOT START)
**Estimated:** 15 NWT (~30 min)
**Status:** BLOCKED

---

## Executive Summary

**DMS Frontend implementation is BLOCKED** due to missing OpenAPI specification. Cannot generate Orval API client without the spec file.

**Blocker:** OpenAPI spec `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml` does not exist (expected from MSG-ARCHITECT-066).

---

## Blocker Details

### Missing Dependency
- **File:** `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml`
- **Expected From:** MSG-ARCHITECT-066 (referenced in task spec)
- **Required For:** Orval code generation
- **Impact:** Cannot generate React Query hooks without OpenAPI spec

### Verification Performed
```bash
$ ls -la /opt/spaceos/docs/api/joinerytech-dms-v1.yaml
ls: cannot access '/opt/spaceos/docs/api/joinerytech-dms-v1.yaml': No such file or directory

$ ls -la /opt/spaceos/datahaven-web/client/src/api/generated/
total 24
drwxrwx---  6 gabor gabor 4096 Jul  4 21:14 .
drwxrwx---  4 gabor gabor 4096 Jun 30 15:08 ..
drwxrwx---  6 gabor gabor 4096 Jul  4 21:14 hr
drwxrwx--- 22 gabor gabor 4096 Jun 30 15:08 kernel
drwxrwx---  6 gabor gabor 4096 Jul  4 20:18 kontrolling
drwxrwx---  6 gabor gabor 4096 Jul  4 21:06 maintenance
# NO 'dms' directory!
```

### Backend Status
- **MSG-BACKEND-168:** ✅ DONE (10 endpoints, 0 errors/warnings)
- **Backend API:** Ready and deployed
- **OpenAPI Spec:** ❌ NOT FOUND (MSG-ARCHITECT-066 not completed or not placed in expected location)

---

## Pattern Context

**Successful Pattern (4 modules):**
1. **CRM:** Custom hooks (no Orval, manual implementation)
2. **Kontrolling:** Orval-generated from `joinerytech-kontrolling-v1.yaml` ✅
3. **HR:** Orval-generated from `joinerytech-hr-v1.yaml` ✅
4. **Maintenance:** Orval-generated from `joinerytech-maintenance-v1.yaml` ✅

**DMS Expected Pattern:**
5. **DMS:** Orval-generated from `joinerytech-dms-v1.yaml` ❌ **MISSING**

**Orval Generation Process:**
1. OpenAPI spec exists at `/opt/spaceos/docs/api/joinerytech-<module>-v1.yaml`
2. Orval config references the spec
3. Run `npm run generate:api-client:dms` (or similar)
4. Generated hooks appear in `src/api/generated/dms/`

**Current Status:** Step 1 failed — spec does not exist.

---

## Resolution Required

### Option 1: Architect Creates OpenAPI Spec (RECOMMENDED)
**Action:** Dispatch MSG-ARCHITECT-066 to create `joinerytech-dms-v1.yaml`
**Estimated Time:** 30-45 min (based on Maintenance spec creation)
**Owner:** Architect terminal
**Unblocks:** MSG-FRONTEND-006 can proceed after spec created

### Option 2: Backend Exports OpenAPI Spec
**Action:** Backend terminal exports OpenAPI spec from .NET controllers
**Estimated Time:** 15 min (if Swashbuckle/NSwag already configured)
**Owner:** Backend terminal
**Unblocks:** MSG-FRONTEND-006 can proceed after spec export

### Option 3: Manual Hook Creation (NOT RECOMMENDED)
**Action:** Manually create `useDMS.ts` hooks without Orval
**Estimated Time:** 2-3 hours (no type safety, error-prone)
**Owner:** Frontend terminal
**Issues:** Type safety loss, manual maintenance, slower than Orval pattern

---

## Recommended Action

**Dispatch to Architect:**
```yaml
---
id: MSG-ARCHITECT-066
from: conductor
to: architect
type: task
priority: critical
ref: MSG-FRONTEND-006-BLOCKED
---

# Create DMS Module OpenAPI Spec

**Blocker:** MSG-FRONTEND-006 is BLOCKED waiting for OpenAPI spec.

**Required:**
- File: `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml`
- Endpoints: 10 base + 26 extended = 36 total (from MSG-BACKEND-168)
- Patterns: Document CRUD, Folder management, Versioning, Entity linking, Document search

**Reference:**
- Backend: MSG-BACKEND-168 (10 endpoints, 0E/0W)
- Similar specs: joinerytech-maintenance-v1.yaml (31 endpoints)
```

**After Spec Created:**
1. Frontend runs `npm run generate:api-client:dms` (or creates Orval config if missing)
2. Verifies generated hooks in `src/api/generated/dms/`
3. Proceeds with MSG-FRONTEND-006 implementation

---

## Impact Analysis

### Checkpoint Status
- **CP-DMS-FRONTEND:** CANNOT START (blocked before first step)
- **CP-DMS-BACKEND:** ✅ DONE (MSG-BACKEND-168)
- **Epic EPIC-JT-DMS:** STALLED (frontend blocked)

### Timeline Impact
- **Original Estimate:** 15 NWT (~30 min)
- **Blocked Since:** 2026-07-07
- **Estimated Delay:** 1-2 hours (depending on resolution option)

---

## Next Steps

**Immediate:**
1. ✅ Mark MSG-FRONTEND-006 inbox as READ
2. ✅ Create BLOCKED outbox (this message)
3. ⏸️ Await resolution (Architect or Backend provides OpenAPI spec)

**After Unblocked:**
1. Generate Orval API client
2. Create DMS Dashboard components (File browser, Document preview, Version history)
3. Build verification
4. DONE outbox

---

## References

- **Backend Checkpoint:** CP-DMS-BACKEND (MSG-BACKEND-168, 10 endpoints)
- **Missing Spec:** `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml` (MSG-ARCHITECT-066)
- **Pattern Source:** MSG-FRONTEND-003-DONE (HR completion with Orval pattern)
- **Epic:** EPIC-JT-DMS
- **Similar Modules:** Kontrolling, HR, Maintenance (all using Orval successfully)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
