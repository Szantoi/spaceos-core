---
id: MSG-ARCHITECT-066-DONE
from: architect
to: conductor
type: done
status: READ
ref: MSG-ARCHITECT-066
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: 2026-07-04
content_hash: 94ea11abf07e2a5f8c9173568579e9ea5f1d65fc21e034059ca1f2a8a3bc4c6b
---

# JoineryTech DMS Week 0 — OpenAPI Contract Specification DONE

## Summary

DMS OpenAPI 3.1 Contract specification completed successfully. The specification defines a comprehensive Document Management System API with:

- **36 HTTP operations** across 27 path groups
- **30 schema definitions** (DTOs, Commands, Enums, Responses)
- **Immutable versioning** with SHA-256 integrity verification
- **Entity linking** to Orders, Projects, Assets, Employees, etc.
- **Permission-based access control** (View, Edit, Delete, Share)
- **Full-text search** via PostgreSQL tsvector

## Deliverable

**File:** `/opt/spaceos/spaceos-modules-dms/docs/openapi.yaml`

## OpenAPI Specification Statistics

| Category | Count |
|----------|-------|
| **OpenAPI Version** | 3.1.0 |
| **Path Groups** | 27 |
| **HTTP Operations** | 36 |
| **Schemas (DTOs)** | 30 |
| **Tag Groups** | 6 |

## Endpoint Groups (6 Tags)

### 1. Documents (8 operations)
- `GET /api/dms/documents` — List documents (paged, filtered)
- `POST /api/dms/documents` — Upload document (multipart/form-data)
- `GET /api/dms/documents/{id}` — Get document details
- `PUT /api/dms/documents/{id}` — Update metadata
- `DELETE /api/dms/documents/{id}` — Soft delete
- `POST /api/dms/documents/{id}/archive` — Archive document
- `POST /api/dms/documents/{id}/unarchive` — Unarchive document
- `POST /api/dms/documents/{id}/restore` — Restore from trash

### 2. Versions (7 operations)
- `GET /api/dms/documents/{id}/versions` — List all versions
- `POST /api/dms/documents/{id}/versions` — Add new version (immutable)
- `GET /api/dms/documents/{id}/versions/{versionNumber}` — Get version metadata
- `GET /api/dms/documents/{id}/versions/{versionNumber}/download` — Download file
- `GET /api/dms/documents/{id}/versions/latest/download` — Download latest
- `POST /api/dms/documents/{id}/versions/{versionNumber}/rollback` — Rollback to version
- `GET /api/dms/documents/{id}/versions/{versionNumber}/verify` — Verify SHA-256 integrity

### 3. EntityLinks (4 operations)
- `GET /api/dms/documents/{id}/links` — List entity links
- `POST /api/dms/documents/{id}/links` — Link to entity
- `DELETE /api/dms/documents/{id}/links/{linkId}` — Unlink entity
- `GET /api/dms/entity-documents/{entityType}/{entityId}` — Documents by entity

### 4. Permissions (4 operations)
- `GET /api/dms/documents/{id}/permissions` — List permissions
- `POST /api/dms/documents/{id}/permissions` — Grant permission
- `DELETE /api/dms/documents/{id}/permissions/{permissionId}` — Revoke permission
- `GET /api/dms/documents/{id}/permissions/my` — Current user permissions

### 5. Search (5 operations)
- `GET /api/dms/documents/search` — Full-text search (PostgreSQL tsvector)
- `GET /api/dms/documents/by-tags` — Search by tags
- `GET /api/dms/documents/recent` — Recently uploaded
- `GET /api/dms/documents/expiring` — Expiring soon (compliance)
- `GET /api/dms/documents/expired` — Already expired

### 6. Folders (6 operations) — Phase 2
- `GET /api/dms/folders` — List folders
- `POST /api/dms/folders` — Create folder
- `GET /api/dms/folders/{id}` — Get folder
- `PUT /api/dms/folders/{id}` — Rename folder
- `DELETE /api/dms/folders/{id}` — Delete folder
- `POST /api/dms/folders/{id}/move` — Move folder

## Schema Definitions

### DTOs (Response)
| Schema | Purpose |
|--------|---------|
| `DocumentDto` | Document summary (list view) |
| `DocumentDetailDto` | Full document with versions, links, permissions |
| `DocumentVersionDto` | Version metadata + download info |
| `EntityLinkDto` | Entity link record |
| `DocumentPermissionDto` | Permission grant record |
| `UserPermissionsDto` | Current user's permission flags |
| `FolderDto` | Folder metadata |
| `SearchResultItemDto` | Search result with relevance |
| `IntegrityVerificationResult` | SHA-256 verification result |

### Commands (Request)
| Schema | Purpose |
|--------|---------|
| `UploadDocumentCommand` | File upload (multipart/form-data) |
| `AddVersionCommand` | Add new version |
| `UpdateDocumentMetadataCommand` | Update tags, description, expiry |
| `AddTagCommand` | Add single tag |
| `LinkToEntityCommand` | Link document to entity |
| `GrantPermissionCommand` | Grant view/edit/delete/share |
| `CreateFolderCommand` | Create folder |
| `RenameFolderCommand` | Rename folder |
| `MoveFolderCommand` | Move folder to parent |

### Enums
| Enum | Values |
|------|--------|
| `DocumentStatus` | Active, Archived, Deleted |
| `EntityType` | Order, Project, Asset, Employee, WorkOrder, Ticket, Lead, Opportunity, Supplier, PurchaseOrder |
| `PermissionType` | View, Edit, Delete, Share |
| `MimeTypeCategory` | PDF, Image, Word, Excel, Archive, Unknown |

## Validation Results

### Redocly Lint

```
$ npx @redocly/cli lint openapi.yaml

validating openapi.yaml...
openapi.yaml: validated in 159ms

Woohoo! Your API description is valid. 🎉
```

**Result:** PASS (zero errors, zero warnings)

## Key Patterns Documented

### 1. Blob Storage Integration
- File upload via `multipart/form-data` (max 50MB)
- Download via presigned URLs (Azure Blob SAS, S3, MinIO)
- SHA-256 hash in response header for integrity verification
- `Content-Disposition: attachment` for downloads

### 2. Full-Text Search
- PostgreSQL `tsvector` column for FileName, Description, Tags
- Query parameter: `q` (search text)
- Relevance ranking: `ts_rank(search_vector, query)`
- Response includes `relevance` score and `matchedFields`

### 3. Entity Linking Pattern
- Link documents to any domain entity (Order, Project, Asset, etc.)
- Cross-module reference via EntityType + EntityId
- Query documents by entity: `GET /api/dms/entity-documents/{entityType}/{entityId}`

### 4. Security & RLS
- JWT Bearer authentication required on all endpoints
- TenantId extracted from JWT claims
- PostgreSQL RLS enforces tenant isolation
- Permission checks via DocumentAccessControlService

## Acceptance Criteria Validation

- [x] `openapi.yaml` created in `/opt/spaceos/spaceos-modules-dms/docs/`
- [x] 36 endpoints defined (exceeds 20-30 requirement)
- [x] All DTOs mapped from Domain Model (DocumentDto, DocumentVersionDto, EntityLinkDto, PermissionDto)
- [x] Enums defined (DocumentStatus, EntityType, PermissionType, MimeTypeCategory)
- [x] Command DTOs for all operations (UploadDocument, AddVersion, LinkToEntity, GrantPermission)
- [x] Blob storage upload/download endpoints documented (multipart/form-data, presigned URLs)
- [x] Full-text search endpoint documented (GET /api/dms/documents/search)
- [x] Entity linking query endpoint documented (GET /api/dms/entity-documents/{type}/{id})
- [x] Security (JWT BearerAuth) documented
- [x] RLS enforcement documented (tenant isolation)
- [x] Validation rules documented
- [x] **Redocly lint PASS (zero errors, zero warnings)**
- [x] Orval code-gen ready (TypeScript client generation)

## Files Created

| File | Lines | Description |
|------|-------|-------------|
| `/opt/spaceos/spaceos-modules-dms/docs/openapi.yaml` | ~1100 | DMS OpenAPI 3.1 Contract |

## Next Steps

1. **Week 1:** Domain Layer implementation (Document aggregate, DocumentVersion value object)
2. **Week 2:** Application Layer + API Controllers with Mediator/CQRS
3. **Week 3:** E2E integration tests (upload, download, search)
4. **Orval code-gen:** Generate TypeScript client for Portal: `spaceos generate api-client dms portal`

---

*Architect Terminal - MSG-ARCHITECT-066*

---

Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
