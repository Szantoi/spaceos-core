---
id: MSG-ARCHITECT-064
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: 2026-07-04
estimated_nwt: 150
content_hash: 8d3a7b4059fb8c09ac59292c39b2cf19ea5b084ab53636b47bd58e797ec6e5d9
---

# JoineryTech DMS Domain Model — DDD Design Specification

**Epic:** EPIC-JT-DMS (Dokumentumtár / Document Management System)
**Estimated:** 150 NWT (~5 hours)
**Priority:** High (unblocked epic, cross-module document storage)

---

## Context

Az EPIC-JT-DMS (Document Management System) aktiválása folyamatban. A Contract-First development workflow szerint **minden epic aktiválás ELŐTT** szükséges a domain model tervezése.

**Prototípus:** JoineryTech prototípus volt dokumentumtár (migrálandó)
**Integration:** All modules (Orders, Projects, Employees, Assets can link documents)

**Most a Domain Model DDD specifikációt kell elkészítened.**

---

## Deliverables

**File:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md`

**Minimum tartalom (referencia: HR_DOMAIN_MODEL.md, MAINTENANCE_DOMAIN_MODEL.md):**

### 1. Executive Summary
- DMS domain felelősség
- Document lifecycle (Upload → Active → Archived/Deleted)
- Version control (immutable versions, latest pointer)
- Entity linking (documents attached to Orders, Projects, Assets, etc.)
- Search & indexing
- Permission model (who can view/edit/delete)
- Key design principles (immutability, audit trail, need-to-know access)

### 2. Aggregate Roots

**Document Aggregate:**
- Responsibility: Represents a file with metadata, versions, entity links, and permissions
- Properties:
  - FileName, MimeType, SizeBytes
  - UploadedByUserId, UploadedAt
  - CurrentVersionId (pointer to latest version)
  - EntityLinks (list of linked entities: OrderId, ProjectId, AssetId, etc.)
  - Tags (for search/categorization)
  - IsArchived, IsDeleted (soft delete)
- Methods:
  - Create
  - AddVersion (upload new version, immutable)
  - LinkToEntity (Order, Project, Asset, Employee, etc.)
  - UnlinkFromEntity
  - Archive, Unarchive
  - SoftDelete (move to trash)
  - AddTag, RemoveTag
- Invariants:
  - FileName must not be empty
  - CurrentVersionId must always point to a valid version
  - Cannot delete document with active entity links (or require confirmation)
  - Versions are immutable (cannot edit uploaded file, only add new version)

**Folder Aggregate (optional - can be flat structure initially):**
- Responsibility: Organize documents into hierarchical folders
- Properties: FolderName, ParentFolderId (self-referencing), TenantId
- Methods: Create, Rename, Move, Delete
- Invariants: FolderName unique within parent, cannot delete non-empty folder

### 3. Value Objects

**DocumentVersion:**
- VersionNumber (int, auto-increment)
- FileUrl (blob storage URL)
- Hash (SHA-256, for integrity check)
- SizeBytes
- UploadedByUserId
- UploadedAt
- ChangeNotes (optional, what changed in this version)

**EntityLink:**
- EntityType (Order, Project, Asset, Employee, WorkOrder, Ticket, etc.)
- EntityId (Guid, reference to entity)
- LinkedByUserId (who created the link)
- LinkedAt (DateTime)

**DocumentMetadata:**
- Tags (list of strings for categorization)
- Description (optional)
- ExpiryDate (optional, for certificates/compliance docs)

**Permission:**
- PermissionType (View, Edit, Delete)
- GrantedTo (UserId or RoleId)
- GrantedByUserId
- GrantedAt

### 4. Enums

**DocumentStatus:**
- Active
- Archived
- Deleted (soft delete, trash)

**EntityType:**
- Order
- Project
- Asset
- Employee
- WorkOrder
- Ticket
- Lead
- Opportunity
- Other

**PermissionType:**
- View
- Edit
- Delete
- Share (can grant permissions to others)

**MimeType (common types, extensible):**
- PDF (`application/pdf`)
- Image (`image/jpeg`, `image/png`)
- Word (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`)
- Excel (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)
- Unknown (`application/octet-stream`)

### 5. Domain Services

**DocumentSearchService:**
- Responsibility: Full-text search across documents (metadata + content extraction)
- Method: `IEnumerable<Document> Search(string query, SearchFilters filters)`
- SearchFilters: Tags, EntityType, UploadedBy, DateRange, MimeType
- Logic:
  - Search in FileName, Description, Tags
  - Optionally search in file content (PDF text extraction, OCR for images)
  - Rank by relevance (TF-IDF or simple keyword match)

**DocumentAccessControlService:**
- Responsibility: Check if user has permission to view/edit/delete document
- Method: `bool HasPermission(Document document, UserId userId, PermissionType permissionType)`
- Logic:
  - Check explicit permissions on document
  - Check role-based permissions (admin can view all)
  - Check tenant isolation (RLS)

**DocumentVersioningService:**
- Responsibility: Handle version comparison, rollback
- Method: `DocumentVersion GetVersion(Document document, int versionNumber)`
- Method: `void RollbackToVersion(Document document, int versionNumber)` (creates new version with old content)

**BlobStorageService (infrastructure):**
- Responsibility: Upload/download files to blob storage (Azure Blob, S3, MinIO, etc.)
- Method: `Task<string> UploadAsync(Stream fileStream, string fileName, string mimeType)`
- Method: `Task<Stream> DownloadAsync(string fileUrl)`
- Method: `Task DeleteAsync(string fileUrl)`

### 6. Domain Events

**Document Events:**
- DocumentUploadedEvent (first version)
- DocumentVersionAddedEvent (new version uploaded)
- DocumentLinkedToEntityEvent (document linked to Order, Project, etc.)
- DocumentUnlinkedFromEntityEvent
- DocumentArchivedEvent
- DocumentDeletedEvent (soft delete)
- DocumentRestoredEvent (undelete from trash)

**Permission Events:**
- DocumentPermissionGrantedEvent
- DocumentPermissionRevokedEvent

**Search Events:**
- DocumentSearchedEvent (audit search queries for analytics)

### 7. Repository Contracts

**IDocumentRepository:**
- GetByIdAsync
- GetByEntityLinkAsync (all documents linked to a specific Order/Project/etc.)
- GetByUploaderAsync (all documents uploaded by a user)
- SearchAsync (full-text search with filters)
- GetRecentAsync (recently uploaded, date range)
- GetByTagAsync
- AddAsync, UpdateAsync, DeleteAsync (soft delete)

**IDocumentVersionRepository:**
- GetVersionsAsync (all versions of a document)
- GetVersionByNumberAsync (specific version)
- AddVersionAsync

**IFolderRepository (optional):**
- GetByIdAsync
- GetByParentIdAsync (children of a folder)
- GetRootFoldersAsync
- AddAsync, UpdateAsync, DeleteAsync

### 8. FSM State Machines

**Document Lifecycle (simple 3-state):**
- Active → Archived (Archive)
- Archived → Active (Unarchive)
- Active → Deleted (SoftDelete)
- Deleted → Active (Restore)

**No explicit FSM needed** (status is just an enum, not complex workflow)

### 9. Integration Boundaries

**All Modules Can Link Documents:**
- **Order** → contract PDF, customer drawings
- **Project** → progress photos, final delivery photos
- **Asset** → purchase invoice, maintenance manual PDF
- **Employee** → ID scan, contract, certificates
- **WorkOrder** → before/after photos, invoice
- **Ticket** → complaint photos, resolution documents
- **Lead/Opportunity** → quote PDF, proposal

**Storage Integration:**
- Blob storage service (Azure Blob, S3, MinIO)
- FileUrl pattern: `blob://{tenantId}/{documentId}/{versionNumber}/{fileName}`
- File content is NEVER in SQL database (only metadata)

**Search Integration:**
- ElasticSearch or PostgreSQL full-text search (tsvector)
- Document text extraction for PDF (pdf.js, Apache Tika)
- OCR for images (optional, Tesseract)

### 10. Validation Rules

**Document:**
- FileName must not be empty
- MimeType must be valid
- SizeBytes > 0 and < MaxFileSize (e.g., 50 MB)
- CurrentVersionId must exist in versions list
- Cannot delete document with active entity links (or require cascade delete)

**DocumentVersion:**
- VersionNumber auto-incremented (no gaps)
- Hash (SHA-256) required for integrity
- FileUrl must be valid blob storage URL

**EntityLink:**
- EntityType must be valid enum
- EntityId must exist in target module (validation at application layer)

**Permission:**
- GrantedTo (UserId or RoleId) must exist
- PermissionType must be valid enum

### 11. Security & Privacy

**Tenant Isolation (RLS):**
- All documents belong to a TenantId
- PostgreSQL RLS policies enforce tenant isolation
- User can ONLY access documents from their tenant

**Need-to-Know Access:**
- Explicit permissions required for sensitive documents
- Default: Document visible to uploader + admin
- EntityLink visibility: If user can view Order, can view linked documents

**Audit Trail:**
- All document events logged (upload, version, link, delete, permission grant)
- Search queries logged for compliance

**File Integrity:**
- SHA-256 hash for each version
- Detect tampering if hash mismatch on download

---

## Implementation Patterns

### Version Control (Immutability)

```csharp
public class Document : AggregateRoot<DocumentId>
{
    private readonly List<DocumentVersion> _versions = new();
    public IReadOnlyList<DocumentVersion> Versions => _versions.AsReadOnly();
    public Guid CurrentVersionId { get; private set; }

    public void AddVersion(Stream fileStream, string fileName, UserId uploadedBy)
    {
        var versionNumber = _versions.Count + 1;
        var hash = CalculateSHA256(fileStream);
        var fileUrl = _blobStorageService.Upload(fileStream, fileName, MimeType);

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = versionNumber,
            FileUrl = fileUrl,
            Hash = hash,
            SizeBytes = fileStream.Length,
            UploadedByUserId = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        _versions.Add(version);
        CurrentVersionId = version.Id;

        AddDomainEvent(new DocumentVersionAddedEvent(Id, TenantId, versionNumber, fileName));
    }
}
```

### Entity Linking Pattern

```csharp
public void LinkToEntity(EntityType entityType, Guid entityId, UserId linkedBy)
{
    var link = new EntityLink
    {
        EntityType = entityType,
        EntityId = entityId,
        LinkedByUserId = linkedBy,
        LinkedAt = DateTime.UtcNow
    };

    _entityLinks.Add(link);
    AddDomainEvent(new DocumentLinkedToEntityEvent(Id, TenantId, entityType, entityId));
}

public void UnlinkFromEntity(EntityType entityType, Guid entityId)
{
    var link = _entityLinks.FirstOrDefault(l => l.EntityType == entityType && l.EntityId == entityId);
    if (link == null)
        throw new DomainException($"Entity link not found: {entityType} {entityId}");

    _entityLinks.Remove(link);
    AddDomainEvent(new DocumentUnlinkedFromEntityEvent(Id, TenantId, entityType, entityId));
}
```

### Search Pattern (Application Layer)

```csharp
public class DocumentSearchService : IDocumentSearchService
{
    public async Task<IEnumerable<Document>> SearchAsync(string query, SearchFilters filters)
    {
        // PostgreSQL tsvector full-text search
        var results = await _repository.SearchAsync(query, filters);

        // Rank by relevance (TF-IDF or simple keyword match)
        var ranked = results.OrderByDescending(d =>
            CalculateRelevance(d, query));

        return ranked;
    }

    private decimal CalculateRelevance(Document document, string query)
    {
        var score = 0m;
        if (document.FileName.Contains(query, StringComparison.OrdinalIgnoreCase))
            score += 10;
        if (document.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            score += 5;
        if (document.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            score += 3;
        return score;
    }
}
```

---

## Acceptance Criteria

- [ ] DMS_DOMAIN_MODEL.md created in `/opt/spaceos/docs/joinerytech/domain/`
- [ ] 1-2 aggregates defined (Document, optional Folder)
- [ ] 4+ value objects defined (DocumentVersion, EntityLink, DocumentMetadata, Permission)
- [ ] 4 enums defined (DocumentStatus, EntityType, PermissionType, MimeType)
- [ ] 4 domain services defined (DocumentSearchService, DocumentAccessControlService, DocumentVersioningService, BlobStorageService)
- [ ] 8+ domain events defined
- [ ] 2-3 repository contracts defined (IDocumentRepository, IDocumentVersionRepository, optional IFolderRepository)
- [ ] Document lifecycle state transitions defined (Active → Archived → Deleted)
- [ ] Entity linking pattern documented (all modules can link documents)
- [ ] Blob storage integration pattern documented (FileUrl, hash integrity)
- [ ] Search & indexing strategy documented (full-text search)
- [ ] Security & privacy rules documented (RLS, need-to-know, audit trail)
- [ ] Version control immutability pattern documented
- [ ] Table of Contents with navigation links
- [ ] Implementation Guide section (coding patterns, examples)

---

## Reference Documents

- **HR Domain Model:** `/opt/spaceos/docs/joinerytech/domain/HR_DOMAIN_MODEL.md` (structure template)
- **Maintenance Domain Model:** `/opt/spaceos/docs/joinerytech/domain/MAINTENANCE_DOMAIN_MODEL.md` (integration patterns)
- **DDD Patterns:** `/opt/spaceos/docs/knowledge/patterns/BACKEND_PATTERNS.md`
- **Immutability Principle:** ADR-003 (immutability & trust)

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-dms-domain-model-done.md`

**Frontmatter:**
```yaml
---
id: MSG-ARCHITECT-064-DONE
from: architect
to: conductor
type: done
status: READ
ref: MSG-ARCHITECT-064
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- Domain model summary (aggregates, value objects, domain services)
- Blob storage integration pattern validation
- Entity linking pattern (cross-module document attachment)
- Search strategy (full-text search, PostgreSQL tsvector or ElasticSearch)
- Version control immutability validation
- Files created: DMS_DOMAIN_MODEL.md location
- Következő lépés: Week 0 OpenAPI specification

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
