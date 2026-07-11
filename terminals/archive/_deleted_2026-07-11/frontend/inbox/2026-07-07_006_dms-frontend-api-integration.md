---
id: MSG-FRONTEND-006
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-FRONTEND
ref: CP-DMS-BACKEND
created: 2026-07-07
estimated_nwt: 15
completed: 2026-07-07
---

# DMS Frontend API Integration

**Epic:** EPIC-JT-DMS
**Checkpoint:** CP-DMS-FRONTEND
**Backend Checkpoint:** CP-DMS-BACKEND ✅ DONE (MSG-BACKEND-168)
**Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion)
**Estimated:** 15 NWT (~30 min with pattern reuse, 67% acceleration validated)

---

## Context — Pattern Reuse Validated

CRM Frontend (MSG-FRONTEND-001) completed in **15 minutes** (vs 45 NWT estimate = **67% acceleration**).

**Key finding:** All integration code already existed. Only needed:
1. `.env` file with `VITE_USE_MOCK_API=false`
2. Backend API endpoints ready ✅

**Backend DMS API Ready:**
- **MSG-BACKEND-168**: Week 4 API Layer (10 endpoints, 0 errors/warnings)
- **Endpoints:** Document CRUD, Folder management, Document versioning, Entity linking, Document search
- **Patterns:** Immutable versioning, blob storage, entity linking (document-to-order, document-to-project)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml` (MSG-ARCHITECT-066, 36 endpoints)

---

## Task — Apply CRM Pattern to DMS

**Expected outcome:** DMS Dashboard with File browser, Document preview, Version history, Entity linking UI, and document search.

### Pattern Reuse (from CRM)

**1. TanStack Query Hook Pattern**
Create `src/hooks/useDMS.ts` with query and mutation hooks (following `useCRM.ts` structure):

```typescript
// Query Hooks
export const useFolders = (parentId?: string) => {
  return useQuery({
    queryKey: ['dms', 'folders', parentId],
    queryFn: () => dmsApi.getFolders(parentId),
    staleTime: 30000
  });
};

export const useDocuments = (filters?: DocumentFilters) => {
  return useQuery({
    queryKey: ['dms', 'documents', filters],
    queryFn: () => dmsApi.getDocuments(filters),
    staleTime: 30000
  });
};

export const useDocumentById = (id: string) => {
  return useQuery({
    queryKey: ['dms', 'document', id],
    queryFn: () => dmsApi.getDocumentById(id),
    enabled: !!id
  });
};

export const useDocumentVersions = (documentId: string) => {
  return useQuery({
    queryKey: ['dms', 'versions', documentId],
    queryFn: () => dmsApi.getVersions(documentId),
    enabled: !!documentId
  });
};

export const useDocumentSearch = (query: string) => {
  return useQuery({
    queryKey: ['dms', 'search', query],
    queryFn: () => dmsApi.searchDocuments(query),
    enabled: query.length >= 3, // min 3 chars
    staleTime: 60000 // 1 min
  });
};

export const useLinkedDocuments = (entityType: string, entityId: string) => {
  return useQuery({
    queryKey: ['dms', 'links', entityType, entityId],
    queryFn: () => dmsApi.getLinkedDocuments(entityType, entityId),
    enabled: !!entityType && !!entityId
  });
};

// Mutation Hooks
export const useUploadDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dmsApi.uploadDocument,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dms', 'documents'] });
    }
  });
};

export const useCreateFolder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dmsApi.createFolder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dms', 'folders'] });
    }
  });
};

export const useLinkDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dmsApi.linkDocument,
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['dms', 'links', variables.entityType, variables.entityId]
      });
    }
  });
};

export const useDeleteDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dmsApi.deleteDocument,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dms', 'documents'] });
    }
  });
};
```

**2. File Upload Pattern**
Implement multipart/form-data file upload:
- Drag & Drop zone (react-dropzone)
- Progress bar during upload
- File size validation (max 50 MB for MVP)
- File type validation (PDF, DOCX, XLSX, images)

**3. Feature Flag Pattern**
Reuse `.env` file approach:
- `VITE_USE_MOCK_API=false` (already set from CRM)
- `dmsApi.ts` checks feature flag to toggle mock/real API

**4. Dashboard Page Structure**
Create `src/pages/DMSDashboardPage.tsx` (follow `CRMLeadsPage.tsx` structure):
- Route: `/dms/browse`
- Layout: File browser (left sidebar: folders, right pane: documents)
- State management: React Query only (no local state)

**5. Activity Logging Pattern**
Log DMS actions (document uploaded, folder created, document linked) using `useActivityLog` hook.

---

## Acceptance Criteria

### 1. ✅ DMS Dashboard Page Created
- **File:** `src/pages/DMSDashboardPage.tsx`
- **Route:** `/dms/browse` (default), `/dms/search`, `/dms/linked/:entityType/:entityId`
- **Layout:** File browser with folder tree + document grid
- **Integration:** Real Backend API (not mock)

### 2. ✅ 4 DMS Components Implemented

**FileBrowser** (`src/components/DMS/FileBrowser.tsx`):
- Props: `{ currentFolderId?: string }`
- API: `useFolders(currentFolderId)`, `useDocuments({ folderId: currentFolderId })`
- Display: Folder tree (left sidebar), Document grid (right pane)
- Actions: Create folder, Upload document, Navigate folders
- Pagination: 20 documents per page
- Sorting: By name, date, size

**DocumentPreview** (`src/components/DMS/DocumentPreview.tsx`):
- Props: `{ documentId: string }`
- API: `useDocumentById(documentId)`
- Display: File preview (PDF embed, image thumbnail, DOCX text extract)
- Actions: Download, View versions, Link to entity, Delete
- Version history: Expandable section showing all versions

**EntityLinkingPanel** (`src/components/DMS/EntityLinkingPanel.tsx`):
- Props: `{ documentId: string }`
- API: `useLinkDocument()`
- Display: Modal/drawer for linking document to entities
- Entity types: Order, Project, Employee, Asset (dropdown)
- Entity selection: Autocomplete search
- Existing links: List of current links with unlink button

**DocumentSearch** (`src/components/DMS/DocumentSearch.tsx`):
- Props: None
- API: `useDocumentSearch(query)`
- Display: Search input + results grid
- Search: Full-text search (min 3 chars)
- Filters: By folder, by linked entity type, by date range
- Highlight: Search terms highlighted in results

### 3. ✅ TanStack Query Hooks Created
- **File:** `src/hooks/useDMS.ts` (follow `useCRM.ts` pattern)
- **Query hooks:** 6 (Folders, Documents, Document by ID, Versions, Search, Linked Documents)
- **Mutation hooks:** 4 (Upload Document, Create Folder, Link Document, Delete Document)
- **Pattern:** Query invalidation on mutation success

### 4. ✅ API Service Layer
- **File:** `src/services/dmsApi.ts` (follow `crmApi.ts` pattern)
- **Feature flag:** Check `VITE_USE_MOCK_API` env var
- **Endpoints:** 10 total (from MSG-BACKEND-168)
- **File upload:** Multipart/form-data support

### 5. ✅ Error Handling + Loading States
- Loading: Skeleton loaders (follow CRM pattern)
- Error: Error alert with retry button
- Empty: "No documents" placeholder with "Upload" CTA

### 6. ✅ File Upload UX
- Drag & Drop zone (react-dropzone)
- Progress bar during upload
- File size validation (max 50 MB)
- File type validation (PDF, DOCX, XLSX, PNG, JPG)
- Multi-file upload support

### 7. ✅ Entity Linking
- Link document to entities (Order, Project, Employee, Asset)
- Autocomplete entity search
- Visual indicator on documents that have links
- Quick unlink action

### 8. ✅ Activity Logging
- Log document upload, folder creation, entity linking
- Reuse `useActivityLog` hook from CRM

### 9. ✅ Build Gates
```bash
npm run build  # 0 errors, 0 warnings
npm run typecheck  # PASS
npm run lint  # PASS (or warnings only, no errors)
```

---

## Files to Create/Modify

**New files (10 files):**
```
src/pages/DMSDashboardPage.tsx                   (NEW)
src/pages/DMSDashboardPage.module.css           (NEW, optional)
src/components/DMS/FileBrowser.tsx              (NEW)
src/components/DMS/DocumentPreview.tsx          (NEW)
src/components/DMS/EntityLinkingPanel.tsx       (NEW)
src/components/DMS/DocumentSearch.tsx           (NEW)
src/components/DMS/index.ts                     (NEW, barrel export)
src/hooks/useDMS.ts                             (NEW)
src/services/dmsApi.ts                          (NEW)
```

**Modified files (1 file, if routes need update):**
```
src/main.tsx  (add routes for /dms/*)
```

**Optional dependencies:**
```
npm install react-dropzone  # File upload drag & drop
```

---

## Backend API Endpoints

**Folder Endpoints (3):**
1. `GET /api/dms/folders` — List folders (with optional parentId filter)
2. `GET /api/dms/folders/{id}` — Get folder details
3. `POST /api/dms/folders` — Create folder

**Document Endpoints (7):**
1. `GET /api/dms/documents` — List documents with filters
2. `GET /api/dms/documents/{id}` — Get document details
3. `POST /api/dms/documents` — Upload document (multipart/form-data)
4. `DELETE /api/dms/documents/{id}` — Delete document
5. `GET /api/dms/documents/{id}/versions` — Get version history
6. `POST /api/dms/documents/{id}/link` — Link document to entity
7. `POST /api/dms/search` — Full-text document search

**Full OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml` (36 endpoints, MSG-ARCHITECT-066)

---

## Expected Timeline

**Baseline (CRM):** 15 minutes (67% acceleration from 45 NWT estimate)

**DMS estimate:** ~15-30 minutes (4 components, file upload similar complexity to FSM patterns)

---

## DONE Outbox Format

**File:** `terminals/frontend/outbox/2026-07-07_NNN_msg-frontend-006-dms-done.md`

**Frontmatter:**
```yaml
---
id: MSG-FRONTEND-006-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-006
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-FRONTEND
created: 2026-07-07
content_hash: <auto>
---
```

**Content sections:**
1. **Executive Summary** — What was completed in how much time
2. **Deliverables** — 4 components + hooks + API service
3. **Build Status** — 0 errors/warnings, TypeScript clean
4. **Pattern Reuse** — Which patterns from CRM/HR/Maintenance/QA were applied
5. **File Upload** — Multipart/form-data implementation with drag & drop
6. **Entity Linking** — Document-to-entity linking UI
7. **Files Changed** — List of created/modified files
8. **Next Steps** — All 5 frontend modules complete, ready for parallel dispatch

---

## References

- **Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion report)
- **Backend Checkpoint:** CP-DMS-BACKEND (MSG-BACKEND-168, 10 endpoints, 0E/0W)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-dms-v1.yaml` (36 endpoints)
- **Epic:** EPIC-JT-DMS
- **Design System:** Datahaven Bento Grid (ADR-048)
- **File Upload:** Multipart/form-data with react-dropzone
- **Entity Linking:** Document-to-Order, Document-to-Project, Document-to-Employee, Document-to-Asset

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
