---
completed: 2026-07-04
id: MSG-BACKEND-149
from: conductor
to: backend
type: task
priority: high
status: COMPLETED
model: sonnet
ref: MSG-ARCHITECT-066-DONE
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: 2026-07-04
estimated_nwt: 180
content_hash: fb093d83f7b6ec776e4c9de0b9102714fc2fdea065aafa736ff4032fb9c12e4b
---

# JoineryTech DMS Week 1 — Domain Layer Implementation

**Epic:** EPIC-JT-DMS (Document Management System)
**Estimated:** 180 NWT (~6 hours)
**Priority:** High (OpenAPI spec complete, domain layer next)

---

## Context

DMS OpenAPI 3.1 specification complete (MSG-ARCHITECT-066-DONE):
- **36 HTTP operations** across 27 path groups
- **30 schema definitions** (DTOs, Commands, Enums)
- **Redocly lint:** PASS (zero errors)

**OpenAPI File:** `/opt/spaceos/spaceos-modules-dms/docs/openapi.yaml` (1866 lines, 50K)
**Domain Model:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md` (1820 lines)

---

## Deliverables

**Target Directory:** `/opt/spaceos/spaceos-modules-dms/src/Domain/`

### 1. Aggregates (2)

#### Document Aggregate
```csharp
// Domain/Aggregates/Document/Document.cs
public class Document : AggregateRoot<DocumentId>
{
    public TenantId TenantId { get; private set; }
    public string FileName { get; private set; }
    public string MimeType { get; private set; }
    public long SizeBytes { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DocumentVersionId CurrentVersionId { get; private set; }
    public UserId UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private readonly List<DocumentVersion> _versions = new();
    private readonly List<EntityLink> _entityLinks = new();
    private readonly List<DocumentPermission> _permissions = new();
    private readonly List<string> _tags = new();

    public DocumentMetadata? Metadata { get; private set; }

    // Factory method
    public static Document Upload(
        TenantId tenantId,
        string fileName,
        string mimeType,
        long sizeBytes,
        Stream fileStream,
        IBlobStorageService blobStorage,
        UserId uploadedBy,
        List<string>? tags = null,
        string? description = null,
        DateOnly? expiryDate = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            throw new DomainException("FileName required, max 255 chars");

        if (sizeBytes <= 0 || sizeBytes > 50 * 1024 * 1024)
            throw new DomainException("File size must be >0 and <50MB");

        if (tags?.Count > 10)
            throw new DomainException("Max 10 tags allowed");

        var document = new Document
        {
            Id = new DocumentId(Guid.NewGuid()),
            TenantId = tenantId,
            FileName = fileName,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            Status = DocumentStatus.Active,
            UploadedByUserId = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        // Add first version
        var firstVersion = document.AddVersion(fileStream, blobStorage, uploadedBy, "Initial upload");
        document.CurrentVersionId = firstVersion.Id;

        // Metadata
        if (description != null || expiryDate != null)
            document.Metadata = new DocumentMetadata(description, expiryDate);

        // Tags
        if (tags != null)
            document._tags.AddRange(tags);

        document.AddDomainEvent(new DocumentUploadedEvent(
            document.Id.Value,
            tenantId.Value,
            fileName,
            mimeType,
            sizeBytes,
            uploadedBy.Value));

        return document;
    }

    // Behavior methods
    public DocumentVersion AddVersion(
        Stream fileStream,
        IBlobStorageService blobStorage,
        UserId uploadedBy,
        string? changeNotes = null)
    {
        if (Status == DocumentStatus.Deleted)
            throw new DomainException("Cannot add version to deleted document");

        var versionNumber = _versions.Count + 1;
        var hash = CalculateSHA256(fileStream);
        fileStream.Position = 0; // Reset for upload

        var fileUrl = blobStorage.Upload(
            TenantId.Value,
            Id.Value,
            versionNumber,
            FileName,
            fileStream);

        var version = new DocumentVersion(
            new DocumentVersionId(Guid.NewGuid()),
            versionNumber,
            fileUrl,
            hash,
            SizeBytes,
            uploadedBy,
            DateTime.UtcNow,
            changeNotes);

        _versions.Add(version);
        CurrentVersionId = version.Id;

        AddDomainEvent(new DocumentVersionAddedEvent(
            Id.Value,
            TenantId.Value,
            version.Id.Value,
            versionNumber));

        return version;
    }

    public void LinkToEntity(EntityType entityType, Guid entityId, UserId linkedBy)
    {
        if (_entityLinks.Any(l => l.EntityType == entityType && l.EntityId == entityId))
            throw new DomainException("Entity already linked");

        var link = new EntityLink(
            new EntityLinkId(Guid.NewGuid()),
            entityType,
            entityId,
            linkedBy,
            DateTime.UtcNow);

        _entityLinks.Add(link);

        AddDomainEvent(new DocumentLinkedToEntityEvent(
            Id.Value,
            TenantId.Value,
            entityType,
            entityId));
    }

    public void UnlinkFromEntity(EntityLinkId linkId, UserId unlinkedBy)
    {
        var link = _entityLinks.FirstOrDefault(l => l.Id == linkId);
        if (link == null)
            throw new DomainException("Link not found");

        _entityLinks.Remove(link);

        AddDomainEvent(new DocumentUnlinkedFromEntityEvent(
            Id.Value,
            TenantId.Value,
            link.EntityType,
            link.EntityId));
    }

    public void GrantPermission(
        PermissionType permissionType,
        UserId? userId,
        Guid? roleId,
        UserId grantedBy)
    {
        if (userId == null && roleId == null)
            throw new DomainException("Either userId or roleId required");

        if (userId != null && roleId != null)
            throw new DomainException("Cannot grant to both user and role");

        var permission = new DocumentPermission(
            new DocumentPermissionId(Guid.NewGuid()),
            permissionType,
            userId,
            roleId,
            grantedBy,
            DateTime.UtcNow);

        _permissions.Add(permission);

        AddDomainEvent(new DocumentPermissionGrantedEvent(
            Id.Value,
            TenantId.Value,
            permissionType,
            userId?.Value,
            roleId));
    }

    public void RevokePermission(DocumentPermissionId permissionId, UserId revokedBy)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission == null)
            throw new DomainException("Permission not found");

        _permissions.Remove(permission);

        AddDomainEvent(new DocumentPermissionRevokedEvent(
            Id.Value,
            TenantId.Value,
            permission.PermissionType));
    }

    public void Archive()
    {
        if (Status == DocumentStatus.Deleted)
            throw new DomainException("Cannot archive deleted document");

        Status = DocumentStatus.Archived;
        AddDomainEvent(new DocumentArchivedEvent(Id.Value, TenantId.Value));
    }

    public void Unarchive()
    {
        if (Status != DocumentStatus.Archived)
            throw new DomainException("Document not archived");

        Status = DocumentStatus.Active;
        AddDomainEvent(new DocumentUnarchivedEvent(Id.Value, TenantId.Value));
    }

    public void SoftDelete()
    {
        if (Status == DocumentStatus.Deleted)
            throw new DomainException("Already deleted");

        Status = DocumentStatus.Deleted;
        AddDomainEvent(new DocumentDeletedEvent(Id.Value, TenantId.Value));
    }

    public void Restore()
    {
        if (Status != DocumentStatus.Deleted)
            throw new DomainException("Document not deleted");

        Status = DocumentStatus.Active;
        AddDomainEvent(new DocumentRestoredEvent(Id.Value, TenantId.Value));
    }

    public void UpdateMetadata(string? description, DateOnly? expiryDate)
    {
        Metadata = new DocumentMetadata(description, expiryDate);
        AddDomainEvent(new DocumentMetadataUpdatedEvent(Id.Value, TenantId.Value));
    }

    public void AddTag(string tag)
    {
        if (_tags.Count >= 10)
            throw new DomainException("Max 10 tags allowed");

        if (!_tags.Contains(tag))
            _tags.Add(tag);
    }

    public void RemoveTag(string tag)
    {
        _tags.Remove(tag);
    }

    private static string CalculateSHA256(Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
```

#### Folder Aggregate (Phase 2 - Skeleton Only)
```csharp
// Domain/Aggregates/Folder/Folder.cs
public class Folder : AggregateRoot<FolderId>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }
    public FolderId? ParentFolderId { get; private set; }
    public Color Color { get; private set; }
    public UserId CreatedByUserId { get; private set; }

    // Factory method
    public static Folder Create(
        TenantId tenantId,
        string name,
        Color color,
        UserId createdBy,
        FolderId? parentFolderId = null)
    {
        // Validation + implementation (Phase 2)
        throw new NotImplementedException("Phase 2");
    }
}
```

### 2. Value Objects (4)

```csharp
// Domain/ValueObjects/DocumentVersion.cs
public record DocumentVersion
{
    public DocumentVersionId Id { get; init; }
    public int VersionNumber { get; init; }
    public string FileUrl { get; init; }
    public string Hash { get; init; }
    public long SizeBytes { get; init; }
    public UserId UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
    public string? ChangeNotes { get; init; }

    public DocumentVersion(
        DocumentVersionId id,
        int versionNumber,
        string fileUrl,
        string hash,
        long sizeBytes,
        UserId uploadedBy,
        DateTime uploadedAt,
        string? changeNotes = null)
    {
        if (versionNumber <= 0)
            throw new DomainException("VersionNumber must be >0");

        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new DomainException("FileUrl required");

        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64)
            throw new DomainException("Hash must be SHA-256 (64 hex chars)");

        Id = id;
        VersionNumber = versionNumber;
        FileUrl = fileUrl;
        Hash = hash;
        SizeBytes = sizeBytes;
        UploadedByUserId = uploadedBy;
        UploadedAt = uploadedAt;
        ChangeNotes = changeNotes;
    }
}

// Domain/ValueObjects/EntityLink.cs
public record EntityLink
{
    public EntityLinkId Id { get; init; }
    public EntityType EntityType { get; init; }
    public Guid EntityId { get; init; }
    public UserId LinkedByUserId { get; init; }
    public DateTime LinkedAt { get; init; }

    public EntityLink(
        EntityLinkId id,
        EntityType entityType,
        Guid entityId,
        UserId linkedBy,
        DateTime linkedAt)
    {
        Id = id;
        EntityType = entityType;
        EntityId = entityId;
        LinkedByUserId = linkedBy;
        LinkedAt = linkedAt;
    }
}

// Domain/ValueObjects/DocumentPermission.cs
public record DocumentPermission
{
    public DocumentPermissionId Id { get; init; }
    public PermissionType PermissionType { get; init; }
    public UserId? GrantedToUserId { get; init; }
    public Guid? GrantedToRoleId { get; init; }
    public UserId GrantedByUserId { get; init; }
    public DateTime GrantedAt { get; init; }

    public DocumentPermission(
        DocumentPermissionId id,
        PermissionType permissionType,
        UserId? grantedToUserId,
        Guid? grantedToRoleId,
        UserId grantedBy,
        DateTime grantedAt)
    {
        if (grantedToUserId == null && grantedToRoleId == null)
            throw new DomainException("Either userId or roleId required");

        Id = id;
        PermissionType = permissionType;
        GrantedToUserId = grantedToUserId;
        GrantedToRoleId = grantedToRoleId;
        GrantedByUserId = grantedBy;
        GrantedAt = grantedAt;
    }
}

// Domain/ValueObjects/DocumentMetadata.cs
public record DocumentMetadata
{
    public string? Description { get; init; }
    public DateOnly? ExpiryDate { get; init; }

    public DocumentMetadata(string? description, DateOnly? expiryDate)
    {
        if (description?.Length > 500)
            throw new DomainException("Description max 500 chars");

        Description = description;
        ExpiryDate = expiryDate;
    }

    public bool IsExpired() => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsExpiringSoon(int daysThreshold = 30) =>
        ExpiryDate.HasValue &&
        ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysThreshold));
}
```

### 3. Enums (4)

```csharp
// Domain/Enums/DocumentStatus.cs
public enum DocumentStatus
{
    Active = 0,
    Archived = 1,
    Deleted = 2
}

// Domain/Enums/EntityType.cs
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
    PurchaseOrder = 9
}

// Domain/Enums/PermissionType.cs
public enum PermissionType
{
    View = 0,
    Edit = 1,
    Delete = 2,
    Share = 3
}

// Domain/Enums/MimeTypeCategory.cs
public enum MimeTypeCategory
{
    PDF = 0,
    Image = 1,
    Word = 2,
    Excel = 3,
    Archive = 4,
    Unknown = 5
}
```

### 4. Strong IDs (7)

```csharp
// Domain/ValueObjects/DocumentId.cs
public record DocumentId(Guid Value)
{
    public static DocumentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/DocumentVersionId.cs
public record DocumentVersionId(Guid Value)
{
    public static DocumentVersionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/EntityLinkId.cs
public record EntityLinkId(Guid Value)
{
    public static EntityLinkId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/DocumentPermissionId.cs
public record DocumentPermissionId(Guid Value)
{
    public static DocumentPermissionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/FolderId.cs
public record FolderId(Guid Value)
{
    public static FolderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/UserId.cs (if not already in shared kernel)
public record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain/ValueObjects/TenantId.cs (if not already in shared kernel)
public record TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

### 5. Domain Services (5)

```csharp
// Domain/Services/IDocumentSearchService.cs
public interface IDocumentSearchService
{
    Task<IEnumerable<Document>> SearchAsync(
        TenantId tenantId,
        string searchQuery,
        List<string>? tags = null,
        EntityType? entityType = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Document>> GetRecentAsync(
        TenantId tenantId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

// Domain/Services/IDocumentAccessControlService.cs
public interface IDocumentAccessControlService
{
    bool CanView(Document document, UserId userId);
    bool CanEdit(Document document, UserId userId);
    bool CanDelete(Document document, UserId userId);
    bool CanShare(Document document, UserId userId);
}

// Domain/Services/IDocumentVersioningService.cs
public interface IDocumentVersioningService
{
    DocumentVersion GetVersion(Document document, int versionNumber);
    DocumentVersion GetLatestVersion(Document document);
    IEnumerable<DocumentVersion> GetAllVersions(Document document);
    bool VerifyIntegrity(DocumentVersion version, Stream fileStream);
}

// Domain/Services/IBlobStorageService.cs
public interface IBlobStorageService
{
    string Upload(Guid tenantId, Guid documentId, int versionNumber, string fileName, Stream fileStream);
    Stream Download(string fileUrl);
    void Delete(string fileUrl);
    string GetPresignedUrl(string fileUrl, TimeSpan expiry);
}

// Domain/Services/IDocumentExpiryService.cs
public interface IDocumentExpiryService
{
    IEnumerable<Document> GetExpiring(TenantId tenantId, int daysThreshold = 30);
    IEnumerable<Document> GetExpired(TenantId tenantId);
}
```

### 6. Domain Events (15)

```csharp
// Domain/Events/DocumentUploadedEvent.cs
public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    string MimeType,
    long SizeBytes,
    Guid UploadedByUserId) : DomainEvent;

// Domain/Events/DocumentVersionAddedEvent.cs
public record DocumentVersionAddedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid VersionId,
    int VersionNumber) : DomainEvent;

// Domain/Events/DocumentLinkedToEntityEvent.cs
public record DocumentLinkedToEntityEvent(
    Guid DocumentId,
    Guid TenantId,
    EntityType EntityType,
    Guid EntityId) : DomainEvent;

// Domain/Events/DocumentUnlinkedFromEntityEvent.cs
public record DocumentUnlinkedFromEntityEvent(
    Guid DocumentId,
    Guid TenantId,
    EntityType EntityType,
    Guid EntityId) : DomainEvent;

// Domain/Events/DocumentPermissionGrantedEvent.cs
public record DocumentPermissionGrantedEvent(
    Guid DocumentId,
    Guid TenantId,
    PermissionType PermissionType,
    Guid? GrantedToUserId,
    Guid? GrantedToRoleId) : DomainEvent;

// Domain/Events/DocumentPermissionRevokedEvent.cs
public record DocumentPermissionRevokedEvent(
    Guid DocumentId,
    Guid TenantId,
    PermissionType PermissionType) : DomainEvent;

// Domain/Events/DocumentArchivedEvent.cs
public record DocumentArchivedEvent(
    Guid DocumentId,
    Guid TenantId) : DomainEvent;

// Domain/Events/DocumentUnarchivedEvent.cs
public record DocumentUnarchivedEvent(
    Guid DocumentId,
    Guid TenantId) : DomainEvent;

// Domain/Events/DocumentDeletedEvent.cs
public record DocumentDeletedEvent(
    Guid DocumentId,
    Guid TenantId) : DomainEvent;

// Domain/Events/DocumentRestoredEvent.cs
public record DocumentRestoredEvent(
    Guid DocumentId,
    Guid TenantId) : DomainEvent;

// Domain/Events/DocumentMetadataUpdatedEvent.cs
public record DocumentMetadataUpdatedEvent(
    Guid DocumentId,
    Guid TenantId) : DomainEvent;

// Additional events...
// DocumentTagAddedEvent, DocumentTagRemovedEvent, DocumentExpiredEvent, DocumentExpiringSoonEvent
```

### 7. Repository Contracts (2)

```csharp
// Domain/Repositories/IDocumentRepository.cs
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByEntityAsync(
        TenantId tenantId,
        EntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(DocumentId id, CancellationToken cancellationToken = default);
}

// Domain/Repositories/IFolderRepository.cs
public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(FolderId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Folder>> GetByParentAsync(
        TenantId tenantId,
        FolderId? parentFolderId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Folder folder, CancellationToken cancellationToken = default);
    Task UpdateAsync(Folder folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(FolderId id, CancellationToken cancellationToken = default);
}
```

### 8. Unit Tests (~80-90 tests)

```csharp
// Tests/Domain/DocumentTests.cs
[TestFixture]
public class DocumentTests
{
    [Test]
    public void Upload_ValidData_CreatesDocument()
    {
        // Arrange
        var tenantId = new TenantId(Guid.NewGuid());
        var fileName = "contract.pdf";
        var mimeType = "application/pdf";
        var sizeBytes = 1024L;
        var uploadedBy = new UserId(Guid.NewGuid());
        var blobStorage = new Mock<IBlobStorageService>();
        var fileStream = new MemoryStream(new byte[1024]);

        blobStorage.Setup(b => b.Upload(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Stream>())).Returns("blob://url");

        // Act
        var document = Document.Upload(
            tenantId,
            fileName,
            mimeType,
            sizeBytes,
            fileStream,
            blobStorage.Object,
            uploadedBy);

        // Assert
        Assert.That(document.FileName, Is.EqualTo(fileName));
        Assert.That(document.Status, Is.EqualTo(DocumentStatus.Active));
        Assert.That(document.DomainEvents, Has.Count.EqualTo(2)); // Uploaded + VersionAdded
    }

    [Test]
    public void Upload_FileNameTooLong_ThrowsException()
    {
        // Arrange
        var fileName = new string('a', 256);

        // Act + Assert
        Assert.Throws<DomainException>(() => Document.Upload(
            new TenantId(Guid.NewGuid()),
            fileName,
            "application/pdf",
            1024,
            new MemoryStream(),
            Mock.Of<IBlobStorageService>(),
            new UserId(Guid.NewGuid())));
    }

    [Test]
    public void AddVersion_ValidData_CreatesNewVersion()
    {
        // Arrange
        var document = CreateTestDocument();
        var blobStorage = new Mock<IBlobStorageService>();
        blobStorage.Setup(b => b.Upload(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Stream>())).Returns("blob://url2");

        // Act
        var version = document.AddVersion(
            new MemoryStream(new byte[1024]),
            blobStorage.Object,
            new UserId(Guid.NewGuid()),
            "Updated version");

        // Assert
        Assert.That(version.VersionNumber, Is.EqualTo(2));
        Assert.That(document.CurrentVersionId, Is.EqualTo(version.Id));
    }

    [Test]
    public void LinkToEntity_ValidEntity_CreatesLink()
    {
        // Arrange
        var document = CreateTestDocument();
        var entityType = EntityType.Order;
        var entityId = Guid.NewGuid();
        var linkedBy = new UserId(Guid.NewGuid());

        // Act
        document.LinkToEntity(entityType, entityId, linkedBy);

        // Assert
        Assert.That(document.DomainEvents, Has.Some.TypeOf<DocumentLinkedToEntityEvent>());
    }

    [Test]
    public void Archive_ActiveDocument_ChangesStatus()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        document.Archive();

        // Assert
        Assert.That(document.Status, Is.EqualTo(DocumentStatus.Archived));
    }

    [Test]
    public void Archive_DeletedDocument_ThrowsException()
    {
        // Arrange
        var document = CreateTestDocument();
        document.SoftDelete();

        // Act + Assert
        Assert.Throws<DomainException>(() => document.Archive());
    }

    // ... 75+ more tests for all behaviors
}

// Tests/Domain/ValueObjects/DocumentVersionTests.cs
// Tests/Domain/ValueObjects/EntityLinkTests.cs
// Tests/Domain/ValueObjects/DocumentPermissionTests.cs
// Tests/Domain/ValueObjects/DocumentMetadataTests.cs
```

---

## File Structure

```
spaceos-modules-dms/
├── src/
│   └── Domain/
│       ├── Aggregates/
│       │   ├── Document/
│       │   │   └── Document.cs
│       │   └── Folder/
│       │       └── Folder.cs (skeleton)
│       ├── ValueObjects/
│       │   ├── DocumentVersion.cs
│       │   ├── EntityLink.cs
│       │   ├── DocumentPermission.cs
│       │   ├── DocumentMetadata.cs
│       │   ├── DocumentId.cs
│       │   ├── DocumentVersionId.cs
│       │   ├── EntityLinkId.cs
│       │   ├── DocumentPermissionId.cs
│       │   ├── FolderId.cs
│       │   ├── UserId.cs
│       │   └── TenantId.cs
│       ├── Enums/
│       │   ├── DocumentStatus.cs
│       │   ├── EntityType.cs
│       │   ├── PermissionType.cs
│       │   └── MimeTypeCategory.cs
│       ├── Events/
│       │   ├── DocumentUploadedEvent.cs
│       │   ├── DocumentVersionAddedEvent.cs
│       │   ├── DocumentLinkedToEntityEvent.cs
│       │   ├── DocumentUnlinkedFromEntityEvent.cs
│       │   ├── DocumentPermissionGrantedEvent.cs
│       │   ├── DocumentPermissionRevokedEvent.cs
│       │   ├── DocumentArchivedEvent.cs
│       │   ├── DocumentUnarchivedEvent.cs
│       │   ├── DocumentDeletedEvent.cs
│       │   ├── DocumentRestoredEvent.cs
│       │   └── DocumentMetadataUpdatedEvent.cs
│       ├── Services/
│       │   ├── IDocumentSearchService.cs
│       │   ├── IDocumentAccessControlService.cs
│       │   ├── IDocumentVersioningService.cs
│       │   ├── IBlobStorageService.cs
│       │   └── IDocumentExpiryService.cs
│       └── Repositories/
│           ├── IDocumentRepository.cs
│           └── IFolderRepository.cs
└── tests/
    └── Domain/
        ├── DocumentTests.cs
        ├── ValueObjects/
        │   ├── DocumentVersionTests.cs
        │   ├── EntityLinkTests.cs
        │   ├── DocumentPermissionTests.cs
        │   └── DocumentMetadataTests.cs
        └── Services/
            └── DocumentExpiryServiceTests.cs
```

**Total files:** ~40 files

---

## Acceptance Criteria

- [ ] Document aggregate implemented (Upload, AddVersion, LinkToEntity, UnlinkFromEntity, GrantPermission, RevokePermission, Archive, Unarchive, SoftDelete, Restore, UpdateMetadata, AddTag, RemoveTag)
- [ ] Folder aggregate skeleton implemented (Create method throws NotImplementedException)
- [ ] 4 value objects implemented (DocumentVersion, EntityLink, DocumentPermission, DocumentMetadata)
- [ ] 4 enums implemented (DocumentStatus, EntityType, PermissionType, MimeTypeCategory)
- [ ] 7 strong IDs implemented (DocumentId, DocumentVersionId, EntityLinkId, DocumentPermissionId, FolderId, UserId, TenantId)
- [ ] 5 domain service interfaces defined (Search, AccessControl, Versioning, BlobStorage, Expiry)
- [ ] 15 domain events implemented
- [ ] 2 repository contracts defined (IDocumentRepository, IFolderRepository)
- [ ] SHA-256 integrity calculation implemented (CalculateSHA256 method)
- [ ] Immutable versioning enforced (versions never modified after creation)
- [ ] Entity linking supports all 10 entity types (Order, Project, Asset, Employee, WorkOrder, Ticket, Lead, Opportunity, Supplier, PurchaseOrder)
- [ ] Permission validation (either userId OR roleId, not both)
- [ ] Tag limit enforced (max 10 tags per document)
- [ ] File size validation (>0 and <50MB)
- [ ] FileName validation (required, max 255 chars)
- [ ] 80-90 unit tests GREEN
- [ ] Build: 0 errors, 0 warnings

---

## Reference Documents

- **DMS Domain Model:** `/opt/spaceos/docs/joinerytech/domain/DMS_DOMAIN_MODEL.md` (1820 lines) ← PRIMARY SOURCE
- **DMS OpenAPI:** `/opt/spaceos/spaceos-modules-dms/docs/openapi.yaml` (1866 lines) ← CONTRACT REFERENCE
- **QA Domain Layer:** `/opt/spaceos/spaceos-modules-qa/src/Domain/` (structure template)
- **Kontrolling Domain Layer:** `/opt/spaceos/spaceos-modules-kontrolling/src/Domain/` (structure template)

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-dms-week1-domain-layer-done.md`

**Frontmatter:**
```yaml
---
id: MSG-BACKEND-149-DONE
from: backend
to: conductor
type: done
status: UNREAD
ref: MSG-BACKEND-149
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-BACKEND
created: YYYY-MM-DD
---
```

**Content:**
- File count (target: ~40 files)
- Unit test count (target: 80-90 tests GREEN)
- Build result (0 errors, 0 warnings)
- SHA-256 integrity pattern validation
- Entity linking pattern validation
- Immutable versioning pattern validation
- Következő lépés: Week 2 Application Layer (CQRS handlers, validators)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>

---

## Completion Report
*2026-07-04T21:52:37.151Z*

### Summary
DMS Week 1 Domain Layer complete: 40 domain files (Document aggregate with 13 behaviors, Folder skeleton, 5 value objects, 7 strong IDs, 4 enums, 15 events, 5 service interfaces, 2 repositories). 84/84 tests passing. 0 build errors, 0 warnings. SHA-256 integrity, immutable versioning, XOR permission validation implemented.

### Files Changed
- `spaceos-modules-dms/src/Domain/Aggregates/Document/Document.cs`
- `spaceos-modules-dms/src/Domain/Aggregates/Folder/Folder.cs`
- `spaceos-modules-dms/src/Domain/ValueObjects/*.cs`
- `spaceos-modules-dms/src/Domain/Enums/*.cs`
- `spaceos-modules-dms/src/Domain/Events/*.cs`
- `spaceos-modules-dms/src/Domain/Services/*.cs`
- `spaceos-modules-dms/src/Domain/Repositories/*.cs`
- `spaceos-modules-dms/tests/Domain/*.cs`
- `spaceos-modules-dms/src/SpaceOS.Modules.DMS.csproj`
- `spaceos-modules-dms/tests/SpaceOS.Modules.DMS.Tests.csproj`

