---
id: MSG-ARCHITECT-066
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
ref: MSG-ARCHITECT-064-DONE
created: 2026-07-04
estimated_nwt: 150
content_hash: 746d1291a6b2d2f7d3750de8a58d6cff543cbfd810e3cdd10be3551d6a3ec361
---

# JoineryTech DMS Week 0 — OpenAPI Contract Specification

**Epic:** EPIC-JT-DMS (Dokumentumtár / Document Management System)
**Estimated:** 150 NWT (~5 hours)
**Priority:** High (domain model elkészült, OpenAPI spec következik)

---

## Context

A DMS Domain Model elkészült (MSG-ARCHITECT-064-DONE). Most a Contract-First development workflow Week 0 fázisa következik: **OpenAPI 3.1 Contract specifikáció**.

**Domain Model File:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md` (1820 lines, 2 aggregates, 5 domain services)

**Prototípus:** JoineryTech prototípus volt dokumentumtár (migrálandó)
**Critical Features:** Immutable versioning, entity linking, blob storage

---

## Deliverables

**File:** `/opt/spaceos/spaceos-modules-dms/docs/openapi.yaml`

**Minimum tartalom (referencia: QA, CRM, Kontrolling OpenAPI specs):**

### 1. API Metadata
```yaml
openapi: 3.1.0
info:
  title: SpaceOS JoineryTech DMS API
  version: 1.0.0
  description: |
    Document Management System API — Versioned document storage, entity linking,
    permission management, full-text search.
servers:
  - url: https://dms.joinerytech.hu
    description: Production (multi-tenant)
  - url: http://localhost:5012
    description: Local development
```

### 2. Endpoint Groups (20-30 endpoints expected)

#### Document Endpoints (CRUD + Lifecycle)
```yaml
GET    /api/documents                    # List documents (filter by status, entity, tags)
GET    /api/documents/{id}               # Get document by ID
POST   /api/documents                    # Upload document (creates first version)
PUT    /api/documents/{id}               # Update document metadata
DELETE /api/documents/{id}               # Soft delete document
POST   /api/documents/{id}/archive       # Archive document
POST   /api/documents/{id}/unarchive     # Unarchive document
POST   /api/documents/{id}/restore       # Restore from trash
```

#### Document Version Endpoints
```yaml
GET    /api/documents/{id}/versions                # List all versions
GET    /api/documents/{id}/versions/{versionNumber} # Get specific version
POST   /api/documents/{id}/versions                # Add new version (immutable)
GET    /api/documents/{id}/versions/{versionNumber}/download # Download file
GET    /api/documents/{id}/versions/latest/download          # Download latest
```

#### Entity Linking Endpoints
```yaml
POST   /api/documents/{id}/links          # Link document to entity (Order, Project, Asset, etc.)
DELETE /api/documents/{id}/links/{linkId} # Unlink document from entity
GET    /api/documents/by-entity/{entityType}/{entityId} # List documents for entity
```

#### Permission Endpoints
```yaml
POST   /api/documents/{id}/permissions          # Grant permission (View, Edit, Delete)
DELETE /api/documents/{id}/permissions/{permissionId} # Revoke permission
GET    /api/documents/{id}/permissions          # List permissions
```

#### Search Endpoints
```yaml
GET    /api/documents/search                # Full-text search (PostgreSQL tsvector)
GET    /api/documents/by-tags               # Search by tags
GET    /api/documents/expiring              # Expiring certificates/compliance docs
```

#### Folder Endpoints (Phase 2, optional)
```yaml
GET    /api/folders                    # List folders
POST   /api/folders                    # Create folder
PUT    /api/folders/{id}               # Rename folder
DELETE /api/folders/{id}               # Delete folder
POST   /api/folders/{id}/move          # Move folder
```

### 3. Schema Definitions

**From Domain Model → OpenAPI DTOs:**

#### DocumentDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  tenantId: { type: string, format: uuid }
  fileName: { type: string, maxLength: 255 }
  mimeType: { type: string }
  sizeBytes: { type: integer, format: int64 }
  status: { $ref: '#/components/schemas/DocumentStatus' }
  currentVersionId: { type: string, format: uuid }
  uploadedByUserId: { type: string, format: uuid }
  uploadedAt: { type: string, format: date-time }
  tags: { type: array, items: { type: string } }
  description: { type: string, maxLength: 500, nullable: true }
  expiryDate: { type: string, format: date, nullable: true }
  createdAt: { type: string, format: date-time }
  updatedAt: { type: string, format: date-time }
required: [id, tenantId, fileName, mimeType, sizeBytes, status, currentVersionId]
```

#### DocumentVersionDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  versionNumber: { type: integer }
  fileUrl: { type: string }
  hash: { type: string, description: "SHA-256 hash for integrity" }
  sizeBytes: { type: integer, format: int64 }
  uploadedByUserId: { type: string, format: uuid }
  uploadedAt: { type: string, format: date-time }
  changeNotes: { type: string, maxLength: 500, nullable: true }
required: [id, versionNumber, fileUrl, hash, sizeBytes, uploadedByUserId, uploadedAt]
```

#### EntityLinkDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  entityType: { $ref: '#/components/schemas/EntityType' }
  entityId: { type: string, format: uuid }
  linkedByUserId: { type: string, format: uuid }
  linkedAt: { type: string, format: date-time }
required: [id, entityType, entityId, linkedByUserId, linkedAt]
```

#### DocumentPermissionDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  permissionType: { $ref: '#/components/schemas/PermissionType' }
  grantedToUserId: { type: string, format: uuid, nullable: true }
  grantedToRoleId: { type: string, format: uuid, nullable: true }
  grantedByUserId: { type: string, format: uuid }
  grantedAt: { type: string, format: date-time }
required: [id, permissionType, grantedByUserId, grantedAt]
```

#### Enums
```yaml
DocumentStatus:
  type: string
  enum: [Active, Archived, Deleted]

EntityType:
  type: string
  enum: [Order, Project, Asset, Employee, WorkOrder, Ticket, Lead, Opportunity, Supplier, PurchaseOrder]

PermissionType:
  type: string
  enum: [View, Edit, Delete, Share]

MimeTypeCategory:
  type: string
  enum: [PDF, Image, Word, Excel, Unknown]
```

### 4. Command DTOs (Create/Update)

```yaml
UploadDocumentCommand:
  type: object
  properties:
    fileName: { type: string, maxLength: 255 }
    mimeType: { type: string }
    file: { type: string, format: binary }
    tags: { type: array, items: { type: string } }
    description: { type: string, maxLength: 500, nullable: true }
    expiryDate: { type: string, format: date, nullable: true }
  required: [fileName, mimeType, file]

AddVersionCommand:
  type: object
  properties:
    file: { type: string, format: binary }
    changeNotes: { type: string, maxLength: 500, nullable: true }
  required: [file]

LinkToEntityCommand:
  type: object
  properties:
    entityType: { $ref: '#/components/schemas/EntityType' }
    entityId: { type: string, format: uuid }
  required: [entityType, entityId]

GrantPermissionCommand:
  type: object
  properties:
    permissionType: { $ref: '#/components/schemas/PermissionType' }
    grantedToUserId: { type: string, format: uuid, nullable: true }
    grantedToRoleId: { type: string, format: uuid, nullable: true }
  required: [permissionType]

SearchDocumentsQuery:
  type: object
  properties:
    query: { type: string, maxLength: 200 }
    tags: { type: array, items: { type: string } }
    entityType: { $ref: '#/components/schemas/EntityType', nullable: true }
    uploadedBy: { type: string, format: uuid, nullable: true }
    startDate: { type: string, format: date, nullable: true }
    endDate: { type: string, format: date, nullable: true }
    mimeTypeCategory: { $ref: '#/components/schemas/MimeTypeCategory', nullable: true }
```

### 5. Blob Storage Integration

**File Upload Pattern:**
```yaml
POST /api/documents
Content-Type: multipart/form-data
  - file: [binary]
  - fileName: "contract.pdf"
  - mimeType: "application/pdf"
  - tags: ["contract", "2026"]

Response:
{
  "id": "uuid",
  "fileName": "contract.pdf",
  "currentVersionId": "uuid",
  "fileUrl": "blob://{tenantId}/{docId}/1/contract.pdf"
}
```

**File Download Pattern:**
```yaml
GET /api/documents/{id}/versions/{versionNumber}/download
Response:
  - Presigned URL (Azure Blob SAS, S3 presigned URL, or direct stream)
  - Content-Disposition: attachment; filename="contract.pdf"
  - SHA-256 hash in response header for integrity verification
```

### 6. Full-Text Search (PostgreSQL tsvector)

```yaml
GET /api/documents/search?query=contract&tags=2026&entityType=Order
Response:
{
  "totalCount": 15,
  "items": [
    {
      "id": "uuid",
      "fileName": "contract-2026-001.pdf",
      "relevance": 0.95,
      "matchedFields": ["fileName", "tags"],
      ...
    }
  ]
}
```

**Search Implementation:**
- PostgreSQL `tsvector` column for FileName, Description, Tags
- Query: `SELECT * FROM documents WHERE search_vector @@ to_tsquery('contract & 2026')`
- Ranking: `ts_rank(search_vector, query)`

### 7. Security & RLS

```yaml
securitySchemes:
  BearerAuth:
    type: http
    scheme: bearer
    bearerFormat: JWT

security:
  - BearerAuth: []
```

**RLS Enforcement:**
- All endpoints filter by `TenantId` (extracted from JWT token)
- Permission checks via DocumentAccessControlService
- PostgreSQL RLS policies enforce tenant isolation

**Permission Model:**
- **Default:** Uploader + Admin can view/edit
- **Explicit grants:** Grant permission to specific users/roles
- **Entity-based access:** If user can view Order, can view linked documents

### 8. Validation Rules

**Document:**
- FileName required, max 255 chars
- MimeType required
- SizeBytes > 0 and < MaxFileSize (e.g., 50 MB)
- Tags max 10 tags per document

**Version:**
- File required
- VersionNumber auto-incremented (no gaps)
- Hash (SHA-256) required for integrity

**EntityLink:**
- EntityType must be valid enum
- EntityId must exist in target module (validation at application layer)

**Permission:**
- Either grantedToUserId OR grantedToRoleId required (not both)
- PermissionType must be valid enum

---

## Acceptance Criteria

- [ ] `openapi.yaml` created in `/opt/spaceos/spaceos-modules-dms/docs/`
- [ ] 20-30 endpoints defined (Document CRUD, Versions, Entity Links, Permissions, Search)
- [ ] All DTOs mapped from Domain Model (DocumentDto, DocumentVersionDto, EntityLinkDto, PermissionDto)
- [ ] Enums defined (DocumentStatus, EntityType, PermissionType, MimeTypeCategory)
- [ ] Command DTOs for all operations (UploadDocument, AddVersion, LinkToEntity, GrantPermission)
- [ ] Blob storage upload/download endpoints documented (multipart/form-data, presigned URLs)
- [ ] Full-text search endpoint documented (GET /api/documents/search)
- [ ] Entity linking query endpoint documented (GET /api/documents/by-entity/{type}/{id})
- [ ] Security (JWT BearerAuth) documented
- [ ] RLS enforcement documented (tenant isolation)
- [ ] Validation rules documented
- [ ] Redocly lint PASS (zero errors)
- [ ] Orval code-gen ready (TypeScript client generation)

---

## Reference Documents

- **DMS Domain Model:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md` ← PRIMARY SOURCE
- **QA OpenAPI:** `/opt/spaceos/spaceos-modules-qa/docs/openapi.yaml` (structure template, 28 endpoints)
- **CRM OpenAPI:** `/opt/spaceos/spaceos-modules-crm/docs/openapi.yaml` (entity linking pattern)
- **Contract-First Workflow:** ADR-050 (Orval code-gen, MSW mock, domain-first)

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-dms-week0-openapi-done.md`

**Frontmatter:**
```yaml
---
id: MSG-ARCHITECT-066-DONE
from: architect
to: conductor
type: done
status: READ
ref: MSG-ARCHITECT-066
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- OpenAPI spec summary (endpoint count, schema count)
- Redocly lint result
- Blob storage pattern validation (upload/download)
- Entity linking pattern validation
- Full-text search endpoint validation
- Files created: openapi.yaml location
- Következő lépés: Backend Week 1 Domain Layer implementation

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
