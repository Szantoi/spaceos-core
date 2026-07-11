---
processed: 2026-07-06
id: MSG-BACKEND-154
from: root
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-ROOT-001,MSG-MONITOR-101
created: 2026-07-06
estimated_nwt: 100
---

# JoineryTech DMS Week 1 — Domain Layer Implementation

**Epic:** EPIC-JT-DMS
**Phase:** Phase 2
**Priority:** CRITICAL (unblocks MSG-BACKEND-153 DMS Week 2)
**Architect Spec:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md`

---

## Context

Conductor dispatched **DMS Week 2** (MSG-BACKEND-153) but **DMS Week 1 was never created**. The DMS module doesn't exist (`spaceos-modules-dms` directory missing). This task creates the **Domain Layer** foundation.

**Why This Matters:**
- DMS = Single Source of Truth for all uploaded files across modules
- Other modules (CRM, HR, Maintenance, QA) need to link documents to entities
- Version control with SHA-256 integrity verification
- Need-to-know access control with audit trail

---

## Task Objective

Implement the **DMS Domain Layer** with full DDD tactical patterns:
- Document aggregate (core)
- Value objects (DocumentVersion, EntityLink, DocumentPermission)
- Enums (4)
- Domain events (20+)
- Domain services (5)
- Repository contracts (2)
- **84+ unit tests** (100% domain coverage)

---

## Scope (Week 1 Only)

### ✅ In Scope

**1. Document Aggregate**
- Factory method: `Document.Create()`
- Version management: `AddVersion()`, immutable versions
- Entity linking: `LinkToEntity()`, `UnlinkFromEntity()`
- Tagging: `AddTag()`, `RemoveTag()`
- Permissions: `GrantPermission()`, `RevokePermission()`
- Lifecycle: `Archive()`, `Unarchive()`, `SoftDelete()`, `Restore()`
- Metadata: `UpdateMetadata()`
- SHA-256 hash calculation for integrity

**2. Value Objects**
- **DocumentVersion** — Immutable version record (ID, version number, file URL, hash, size)
- **EntityLink** — Link between document and entity (Order, Project, Asset, Employee, etc.)
- **DocumentPermission** — Permission grant (View, Edit, Delete, Share)

**3. Enums**
- **DocumentStatus** — Active, Archived, Deleted
- **EntityType** — Order, Project, Asset, Employee, WorkOrder, Ticket, Lead, Opportunity, Supplier, PurchaseOrder, Inspection, Other
- **PermissionType** — View, Edit, Delete, Share
- **MimeTypeCategory** — PDF, Image, Document, Spreadsheet, Archive, Unknown

**4. Strongly-Typed IDs**
- **DocumentId** — Guid-based ID
- **FolderId** — Guid-based ID (Phase 2, create now for completeness)

**5. Domain Events (20+ events)**
- **Document Lifecycle** (7): DocumentUploadedEvent, DocumentVersionAddedEvent, DocumentMetadataUpdatedEvent, DocumentArchivedEvent, DocumentUnarchivedEvent, DocumentDeletedEvent, DocumentRestoredEvent
- **Entity Links** (2): DocumentLinkedToEntityEvent, DocumentUnlinkedFromEntityEvent
- **Tags** (2): DocumentTagAddedEvent, DocumentTagRemovedEvent
- **Permissions** (2): DocumentPermissionGrantedEvent, DocumentPermissionRevokedEvent
- **Search** (1): DocumentSearchedEvent
- **Folders** (6 — Phase 2): FolderCreatedEvent, FolderRenamedEvent, FolderMovedEvent, FolderDeletedEvent

**6. Domain Services (5 services)**
- **DocumentSearchService** — Full-text search with filters (PostgreSQL tsvector)
- **DocumentAccessControlService** — Permission checking (owner, admin, explicit grants)
- **DocumentVersioningService** — Version rollback, integrity verification
- **DocumentExpiryService** — Expiry tracking for certificates/compliance docs
- **BlobStorageService Interface** — Upload/download/delete files (implementation in Infrastructure layer)

**7. Repository Contracts (2 interfaces)**
- **IDocumentRepository** — 10 methods (GetById, GetByEntityLink, GetByUploader, Search, GetRecent, GetByTag, GetByExpiryDateRange, GetExpiredDocuments, Add, Update)
- **IFolderRepository** — 8 methods (Phase 2 — create interface now)

**8. Unit Tests (84+ tests)**
- **DocumentTests** (25 tests) — Factory method, versioning, entity links, tags, permissions, lifecycle (archive/delete/restore), metadata updates, SHA-256 integrity
- **DocumentSearchServiceTests** (12 tests) — Search query, filters (tags, entity type, date range, status), relevance ranking
- **DocumentAccessControlServiceTests** (15 tests) — Owner access, admin access, explicit permissions, entity-linked access
- **DocumentVersioningServiceTests** (12 tests) — Version rollback, integrity verification, version history
- **DocumentExpiryServiceTests** (10 tests) — Expiring documents (7/14/30 days), expired documents
- **Value Object Tests** (10 tests) — EntityLink, DocumentPermission equality, immutability

---

### ❌ Out of Scope (Week 2)

- Infrastructure Layer (EF Core, PostgreSQL, RLS, blob storage implementation)
- Application Layer (CQRS commands/queries, handlers)
- API endpoints
- Integration with other modules

---

## Implementation Guide

### Step 1: Project Setup (10 min)

```bash
cd /opt/spaceos/backend/spaceos-modules
mkdir -p spaceos-modules-dms/src/Domain/{Aggregates,Enums,StrongIds,ValueObjects,Events,Services,Repositories,FSM}
mkdir -p spaceos-modules-dms/tests/Domain/{Aggregates,Services}

# Create .csproj files
cat > spaceos-modules-dms/src/SpaceOS.Modules.DMS.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>SpaceOS.Modules.DMS</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR.Contracts" Version="2.0.1" />
    <PackageReference Include="Ardalis.Specification" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../spaceos-kernel/SpaceOS.Kernel.Domain/SpaceOS.Kernel.Domain.csproj" />
  </ItemGroup>
</Project>
EOF

cat > spaceos-modules-dms/tests/SpaceOS.Modules.DMS.Tests.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="3.0.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../src/SpaceOS.Modules.DMS.csproj" />
  </ItemGroup>
</Project>
EOF
```

### Step 2: Strongly-Typed IDs (5 min)

```csharp
// src/Domain/StrongIds/DocumentId.cs
public record DocumentId(Guid Value)
{
    public static DocumentId New() => new(Guid.NewGuid());
    public static DocumentId From(Guid value) => new(value);
}

// src/Domain/StrongIds/FolderId.cs (Phase 2)
public record FolderId(Guid Value)
{
    public static FolderId New() => new(Guid.NewGuid());
    public static FolderId From(Guid value) => new(value);
}
```

### Step 3: Enums (10 min)

```csharp
// src/Domain/Enums/DocumentStatus.cs
public enum DocumentStatus
{
    Active = 0,
    Archived = 1,
    Deleted = 2
}

// src/Domain/Enums/EntityType.cs
public enum EntityType
{
    Order = 0,
    Project = 1,
    Asset = 2,
    Employee = 3,
    WorkOrder = 4,
    Ticket = 5,
    Lead = 6,
    Opportunity = 7,
    Supplier = 8,
    PurchaseOrder = 9,
    Inspection = 10,
    Other = 99
}

// src/Domain/Enums/PermissionType.cs
public enum PermissionType
{
    View = 0,
    Edit = 1,
    Delete = 2,
    Share = 3
}

// src/Domain/Enums/MimeTypeCategory.cs
public enum MimeTypeCategory
{
    PDF,
    Image,
    Document,
    Spreadsheet,
    Archive,
    Unknown
}
```

### Step 4: Value Objects (20 min)

**See Architect Spec Lines 420-502** for full implementation.

Key value objects:
- **DocumentVersion** — `{Id, VersionNumber, FileUrl, Hash, SizeBytes, UploadedByUserId, UploadedAt, ChangeNotes}`
- **EntityLink** — `{EntityType, EntityId, LinkedByUserId, LinkedAt}`
- **DocumentPermission** — `{PermissionType, GrantedToUserId, GrantedByUserId, GrantedAt}`

### Step 5: Document Aggregate (60 min)

**See Architect Spec Lines 56-331** for full implementation.

**Factory Method:**
```csharp
public static Document Create(
    TenantId tenantId,
    string fileName,
    string mimeType,
    UserId uploadedByUserId,
    Stream fileStream,
    IBlobStorageService blobStorage,
    string description = null,
    DateOnly? expiryDate = null)
```

**Key Methods:**
- `AddVersion()` — Add new version (immutable versioning)
- `LinkToEntity()` / `UnlinkFromEntity()` — Entity linking
- `AddTag()` / `RemoveTag()` — Tagging
- `GrantPermission()` / `RevokePermission()` — Permission management
- `Archive()` / `Unarchive()` — Archive lifecycle
- `SoftDelete()` / `Restore()` — Delete lifecycle
- `UpdateMetadata()` — Update filename, description, expiry date
- `CalculateSHA256()` — SHA-256 hash calculation

**Invariants:**
- FileName must not be empty
- MimeType must be valid
- CurrentVersionId must always point to valid version
- Versions are immutable (cannot be edited)
- Cannot add versions to non-active documents
- Cannot link deleted documents to entities

### Step 6: Domain Events (30 min)

**See Architect Spec Lines 949-1081** for all 20+ events.

**Document Lifecycle Events:**
- DocumentUploadedEvent
- DocumentVersionAddedEvent
- DocumentMetadataUpdatedEvent
- DocumentArchivedEvent / DocumentUnarchivedEvent
- DocumentDeletedEvent / DocumentRestoredEvent

**Entity Link Events:**
- DocumentLinkedToEntityEvent
- DocumentUnlinkedFromEntityEvent

**Tag Events:**
- DocumentTagAddedEvent
- DocumentTagRemovedEvent

**Permission Events:**
- DocumentPermissionGrantedEvent
- DocumentPermissionRevokedEvent

### Step 7: Domain Services (60 min)

**See Architect Spec Lines 599-943** for full implementations.

**5 Services to Implement:**

1. **DocumentSearchService** (Lines 629-674)
   - `SearchAsync(query, filters, skip, take)` — PostgreSQL tsvector search
   - `CalculateRelevance(doc, query)` — Relevance scoring (filename=10, description=5, tags=3)

2. **DocumentAccessControlService** (Lines 704-758)
   - `HasPermissionAsync(document, userId, permissionType)` — Check permission (owner, admin, explicit grants)
   - `GetUserPermissionsAsync(document, userId)` — Get all permissions for user

3. **DocumentVersioningService** (Lines 794-840)
   - `GetVersion(document, versionNumber)` — Get specific version
   - `RollbackToVersionAsync(document, versionNumber, userId, blobStorage)` — Rollback to previous version (creates new version with old content)
   - `VerifyIntegrityAsync(version, blobStorage)` — Verify SHA-256 hash matches

4. **DocumentExpiryService** (Lines 916-942)
   - `GetExpiringDocumentsAsync(daysUntilExpiry)` — Get documents expiring within N days
   - `GetExpiredDocumentsAsync()` — Get expired documents

5. **IBlobStorageService** (Lines 850-886) — **Interface only** (implementation in Infrastructure layer)
   - `UploadAsync(fileStream, blobPath, mimeType)` → Returns blob URL
   - `DownloadAsync(blobUrl)` → Returns file stream
   - `DeleteAsync(blobUrl)` → Deletes file
   - `GeneratePresignedUrlAsync(blobUrl, expiration)` → Time-limited download URL

### Step 8: Repository Contracts (30 min)

**See Architect Spec Lines 1089-1210** for full interfaces.

**IDocumentRepository (10 methods):**
- Queries: GetByIdAsync, GetByEntityLinkAsync, GetByUploaderAsync, SearchAsync, GetRecentAsync, GetByTagAsync, GetByExpiryDateRangeAsync, GetExpiredDocumentsAsync
- Commands: AddAsync, UpdateAsync

**IFolderRepository (8 methods — Phase 2):**
- Create interface now for completeness (implementation in Phase 2)

### Step 9: Unit Tests (90 min)

**84+ tests total** covering:

**DocumentTests.cs (25 tests):**
- `CanCreateDocument()` — Factory method validation
- `CanAddVersion()` — Version creation
- `VersionsAreImmutable()` — Cannot modify versions
- `CanLinkToEntity()` — Entity linking
- `CannotLinkDeletedDocument()` — Business rule validation
- `CanArchive()` / `CanRestore()` — Lifecycle transitions
- `HashIsCalculatedCorrectly()` — SHA-256 integrity
- `CanGrantPermission()` / `CanRevokePermission()` — Permission management
- `CanAddTag()` / `CanRemoveTag()` — Tagging
- `CanUpdateMetadata()` — Metadata updates
- FSM validation (Active → Archived → Deleted → Restored)

**Domain Service Tests (49 tests total):**
- DocumentSearchServiceTests (12 tests)
- DocumentAccessControlServiceTests (15 tests)
- DocumentVersioningServiceTests (12 tests)
- DocumentExpiryServiceTests (10 tests)

**Value Object Tests (10 tests):**
- EntityLink equality
- DocumentPermission equality
- Immutability verification

---

## Test Strategy

**Pattern:** xUnit + FluentAssertions (same as Maintenance/QA modules)

```csharp
[Fact]
public void CanCreateDocument()
{
    // Arrange
    var tenantId = TenantId.New();
    var userId = UserId.New();
    var fileStream = CreateTestFileStream();
    var blobStorage = new MockBlobStorageService();

    // Act
    var document = Document.Create(
        tenantId,
        "contract.pdf",
        "application/pdf",
        userId,
        fileStream,
        blobStorage,
        "Test contract");

    // Assert
    document.Should().NotBeNull();
    document.FileName.Should().Be("contract.pdf");
    document.Status.Should().Be(DocumentStatus.Active);
    document.Versions.Should().HaveCount(1);
    document.CurrentVersionNumber.Should().Be(1);

    // Domain event verification
    var events = document.GetDomainEvents();
    events.Should().HaveCount(1);
    events.First().Should().BeOfType<DocumentUploadedEvent>();
}
```

---

## Acceptance Criteria

### ✅ Domain Layer Complete

- [x] **Document aggregate** implemented with all methods (Create, AddVersion, LinkToEntity, Archive, SoftDelete, Restore, UpdateMetadata, GrantPermission, RevokePermission, AddTag, RemoveTag)
- [x] **3 value objects** implemented (DocumentVersion, EntityLink, DocumentPermission)
- [x] **4 enums** implemented (DocumentStatus, EntityType, PermissionType, MimeTypeCategory)
- [x] **2 strongly-typed IDs** (DocumentId, FolderId)
- [x] **20+ domain events** implemented (all lifecycle, entity link, tag, permission events)
- [x] **5 domain services** implemented (Search, AccessControl, Versioning, Expiry, BlobStorage interface)
- [x] **2 repository contracts** (IDocumentRepository, IFolderRepository)
- [x] **SHA-256 hash calculation** for file integrity
- [x] **Immutable versioning** enforced (versions cannot be edited)

### ✅ Test Coverage

- [x] **84+ unit tests** (100% domain coverage target)
- [x] **All domain invariants tested** (business rules, FSM transitions)
- [x] **All domain events verified** (event raising on aggregate methods)
- [x] **Domain service logic validated** (search relevance, permission checking, version rollback, expiry tracking)

### ✅ Build & Quality

- [x] **Build SUCCESS** — `dotnet build` completes with 0 errors, 0 warnings
- [x] **All tests PASS** — `dotnet test` shows 84/84 passed
- [x] **Code quality** — Follows DDD patterns from Maintenance/QA modules

---

## Integration Dependencies

**DMS Week 2 (MSG-BACKEND-153) depends on this task:**
- Application Layer (CQRS commands/queries) needs Domain Layer
- Infrastructure Layer (EF Core, blob storage) needs Repository contracts

**Cross-Module Integration:**
- CRM, HR, Maintenance, QA will use `IDmsIntegration` to link documents
- DocumentSearchService will power document search across all modules

---

## Technical Notes

### SHA-256 Hash Calculation

```csharp
private static string CalculateSHA256(Stream stream)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    stream.Position = 0;
    var hashBytes = sha256.ComputeHash(stream);
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

### Blob URL Pattern

```
blob://{tenantId}/{documentId}/{versionNumber}/{fileName}
```

### FSM State Diagram

```
[*] --> Active (Upload)
Active --> Archived (Archive)
Archived --> Active (Unarchive)
Active --> Deleted (SoftDelete)
Deleted --> Active (Restore)
Archived --> Deleted (SoftDelete)
```

### Test Coverage Breakdown

| Component | Tests | Focus |
|---|---|---|
| Document aggregate | 25 | Factory, versioning, links, tags, permissions, lifecycle |
| DocumentSearchService | 12 | Search, filters, relevance ranking |
| DocumentAccessControlService | 15 | Owner, admin, explicit permissions, entity-linked access |
| DocumentVersioningService | 12 | Version rollback, integrity verification |
| DocumentExpiryService | 10 | Expiring/expired document queries |
| Value objects | 10 | Equality, immutability |

---

## Next Steps After Completion

1. **DONE outbox** with:
   - Build SUCCESS confirmation (0 errors, 0 warnings)
   - Test PASS confirmation (84/84 tests)
   - File structure listing
   - Test coverage summary

2. **Week 2 unblock:** MSG-BACKEND-153 (DMS Application Layer) can proceed

---

## References

- **Architect Spec:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md` (1820 lines, full DDD specification)
- **Similar Modules:** Maintenance Week 1 (MSG-BACKEND-145), QA Week 1 (MSG-BACKEND-146)
- **Estimated Time:** 100 NWT (~3.3 hours)

---

🚀 **PRIORITY:** CRITICAL — Unblocks DMS Week 2 and entire Phase 2 cascade

---

*Root Terminal - MSG-ROOT-001 Resolution*
