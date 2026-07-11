---
id: MSG-BACKEND-153
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-DMS
checkpoint_id: CP-DMS-APPLICATION
estimated_nwt: 120
ref: MSG-CONDUCTOR-089
created: 2026-07-06
content_hash: 4ed58321b2ac37e46cca24694cec52f7ad9815ebc54b7c56a5d230330160f726
---

# DMS Week 2 — Application Layer Implementation (CQRS + API)

## Context

DMS Week 1 Domain Layer complete (84 tests, CP-DMS-BACKEND ✅).

**Week 2 Goal:** Implement Application Layer following CQRS pattern established in Kontrolling module.

**Why DMS First:**
- Simplest domain (Document, Folder, Version aggregates)
- No cross-module dependencies
- Establishes CQRS + FluentValidation + API pattern for other modules (HR, Maintenance, QA)

**Current State:**
- ✅ Domain Layer: Document, Folder, Version aggregates with FSM
- ✅ 84 unit tests PASS (100% coverage)
- ✅ Repository contracts: IDocumentRepository, IFolderRepository

---

## Task

Implement Application Layer with CQRS, FluentValidation, DTOs, and API endpoints:

### 1. CQRS Commands/Queries (MediatR)

**Commands (Write Operations):**
```csharp
// Document Commands
CreateDocumentCommand
  - TenantId, FolderId, Title, Description, Tags[], ContentType, FileSizeBytes
  - Handler: CreateDocumentCommandHandler
  - Validation: FluentValidation rules

UploadVersionCommand
  - DocumentId, VersionNumber, Comment, FileSizeBytes, ContentHash
  - Handler: UploadVersionCommandHandler
  - Business rule: Version number must increment

UpdateMetadataCommand
  - DocumentId, Title, Description, Tags[]
  - Handler: UpdateMetadataCommandHandler

DeleteDocumentCommand
  - DocumentId
  - Handler: DeleteDocumentCommandHandler
  - Business rule: Cannot delete if active versions exist
```

**Queries (Read Operations):**
```csharp
// Document Queries
GetDocumentQuery
  - DocumentId
  - Returns: DocumentDto (with latest version info)

SearchDocumentsQuery
  - TenantId, FolderId?, SearchTerm?, Tags[], PageNumber, PageSize
  - Returns: PagedResult<DocumentListDto>
  - Filtering: title/description contains, tags match

GetDocumentHistoryQuery
  - DocumentId
  - Returns: List<DocumentVersionDto> (all versions, newest first)

// Folder Queries
GetFolderTreeQuery
  - TenantId
  - Returns: FolderTreeDto (hierarchical structure)
```

**Total:** 4 commands + 4 queries = 8 handlers

---

### 2. FluentValidation Rules

**CreateDocumentValidator:**
```csharp
public class CreateDocumentValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(5, 200)
            .WithMessage("Title must be between 5 and 200 characters");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(BeValidContentType)
            .WithMessage("Invalid content type");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100 MB
            .WithMessage("File size must be between 1 byte and 100 MB");

        RuleFor(x => x.Tags)
            .Must(t => t == null || t.Length <= 10)
            .WithMessage("Maximum 10 tags allowed");
    }

    private bool BeValidContentType(string contentType)
    {
        var allowed = new[] { "application/pdf", "image/jpeg", "image/png", "text/plain" };
        return allowed.Contains(contentType);
    }
}
```

**UploadVersionValidator:**
```csharp
RuleFor(x => x.Comment)
    .MaximumLength(500)
    .WithMessage("Version comment cannot exceed 500 characters");

RuleFor(x => x.ContentHash)
    .NotEmpty()
    .Matches(@"^[a-fA-F0-9]{64}$") // SHA-256 hash format
    .WithMessage("Invalid content hash format (must be SHA-256)");
```

**Total:** 4 validators (CreateDocument, UploadVersion, UpdateMetadata, DeleteDocument)

---

### 3. DTOs (Data Transfer Objects)

**Request DTOs:**
```csharp
public record CreateDocumentDto(
    Guid FolderId,
    string Title,
    string? Description,
    string[] Tags,
    string ContentType,
    long FileSizeBytes
);

public record UploadVersionDto(
    int VersionNumber,
    string Comment,
    long FileSizeBytes,
    string ContentHash
);
```

**Response DTOs:**
```csharp
public record DocumentDto(
    Guid Id,
    Guid TenantId,
    Guid FolderId,
    string Title,
    string? Description,
    string[] Tags,
    int CurrentVersion,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record DocumentListDto(
    Guid Id,
    string Title,
    string[] Tags,
    int CurrentVersion,
    DateTime CreatedAt
);

public record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    string Comment,
    long FileSizeBytes,
    string ContentHash,
    DateTime UploadedAt
);

public record FolderTreeDto(
    Guid Id,
    string Name,
    List<FolderTreeDto> Subfolders
);
```

**Total:** 6 DTOs (3 request + 5 response)

---

### 4. API Endpoints (Minimal API)

**Document Endpoints:**
```csharp
// POST /api/dms/documents
app.MapPost("/api/dms/documents", async (
    CreateDocumentDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CreateDocumentCommand(
        request.FolderId,
        request.Title,
        request.Description,
        request.Tags,
        request.ContentType,
        request.FileSizeBytes
    );

    var documentId = await mediator.Send(command, ct);

    return Results.Created($"/api/dms/documents/{documentId}", new { documentId });
})
.RequireAuthorization("dms.create");

// GET /api/dms/documents/{id}
app.MapGet("/api/dms/documents/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetDocumentQuery(id);
    var document = await mediator.Send(query, ct);

    return document is not null
        ? Results.Ok(document)
        : Results.NotFound();
})
.RequireAuthorization("dms.read");

// POST /api/dms/documents/{id}/versions
app.MapPost("/api/dms/documents/{id:guid}/versions", async (
    Guid id,
    UploadVersionDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new UploadVersionCommand(
        id,
        request.VersionNumber,
        request.Comment,
        request.FileSizeBytes,
        request.ContentHash
    );

    var versionId = await mediator.Send(command, ct);

    return Results.Created($"/api/dms/documents/{id}/versions/{request.VersionNumber}", new { versionId });
})
.RequireAuthorization("dms.upload");

// GET /api/dms/documents/{id}/history
app.MapGet("/api/dms/documents/{id:guid}/history", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetDocumentHistoryQuery(id);
    var history = await mediator.Send(query, ct);

    return Results.Ok(history);
})
.RequireAuthorization("dms.read");

// PUT /api/dms/documents/{id}/metadata
app.MapPut("/api/dms/documents/{id:guid}/metadata", async (
    Guid id,
    UpdateMetadataDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new UpdateMetadataCommand(id, request.Title, request.Description, request.Tags);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("dms.update");

// DELETE /api/dms/documents/{id}
app.MapDelete("/api/dms/documents/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new DeleteDocumentCommand(id);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("dms.delete");

// GET /api/dms/folders/tree
app.MapGet("/api/dms/folders/tree", async (
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetFolderTreeQuery();
    var tree = await mediator.Send(query, ct);

    return Results.Ok(tree);
})
.RequireAuthorization("dms.read");
```

**Total:** 7 endpoints

---

### 5. Integration Tests

**Test Scope:**
```csharp
// Command Handler Tests (30+ tests)
CreateDocumentCommandHandlerTests
  - Valid document creation
  - Invalid title (too short/long)
  - Invalid file size (0 bytes, >100MB)
  - Invalid content type
  - Duplicate title in same folder (allowed)

UploadVersionCommandHandlerTests
  - Valid version upload
  - Version number validation (must increment)
  - Invalid content hash format
  - Comment max length validation

// Query Handler Tests
GetDocumentQueryHandlerTests
  - Document exists → returns DTO
  - Document not found → returns null
  - RLS enforcement (tenant isolation)

SearchDocumentsQueryHandlerTests
  - Search by title
  - Filter by tags
  - Pagination (page 1, page 2, empty page)
  - RLS enforcement

// API Endpoint Tests (E2E with Testcontainers)
DmsApiTests
  - POST /api/dms/documents → 201 Created
  - GET /api/dms/documents/{id} → 200 OK
  - POST /api/dms/documents/{id}/versions → 201 Created
  - GET /api/dms/documents/{id}/history → 200 OK
  - DELETE /api/dms/documents/{id} → 204 No Content (if no versions)
  - DELETE /api/dms/documents/{id} → 400 Bad Request (if versions exist)
  - GET /api/dms/folders/tree → 200 OK
```

**Expected Test Count:** 30+ integration tests

---

## Acceptance Criteria

- ✅ **8 CQRS handlers** (4 commands + 4 queries) all implemented
- ✅ **4 FluentValidation validators** with comprehensive rules
- ✅ **6 DTOs** (request + response) properly mapped
- ✅ **7 API endpoints** with correct HTTP verbs and status codes
- ✅ **30+ integration tests** PASS (command handlers + query handlers + E2E API)
- ✅ **Build: 0 errors, 0 warnings**
- ✅ **OpenAPI spec generated** (Swagger UI documentation)
- ✅ **RLS policy enforced** (tenant isolation validated in tests)

---

## Files to Create/Modify

**Application Layer (new directory):**
```
spaceos-modules-dms/src/Application/
├── Commands/
│   ├── CreateDocumentCommand.cs
│   ├── CreateDocumentCommandHandler.cs
│   ├── UploadVersionCommand.cs
│   ├── UploadVersionCommandHandler.cs
│   ├── UpdateMetadataCommand.cs
│   ├── UpdateMetadataCommandHandler.cs
│   ├── DeleteDocumentCommand.cs
│   └── DeleteDocumentCommandHandler.cs
├── Queries/
│   ├── GetDocumentQuery.cs
│   ├── GetDocumentQueryHandler.cs
│   ├── SearchDocumentsQuery.cs
│   ├── SearchDocumentsQueryHandler.cs
│   ├── GetDocumentHistoryQuery.cs
│   ├── GetDocumentHistoryQueryHandler.cs
│   ├── GetFolderTreeQuery.cs
│   └── GetFolderTreeQueryHandler.cs
├── Validators/
│   ├── CreateDocumentValidator.cs
│   ├── UploadVersionValidator.cs
│   ├── UpdateMetadataValidator.cs
│   └── DeleteDocumentValidator.cs
└── DTOs/
    ├── CreateDocumentDto.cs
    ├── UploadVersionDto.cs
    ├── UpdateMetadataDto.cs
    ├── DocumentDto.cs
    ├── DocumentListDto.cs
    ├── DocumentVersionDto.cs
    └── FolderTreeDto.cs
```

**API Layer (Minimal API):**
```
spaceos-modules-dms/src/Api/
└── Endpoints/
    └── DmsEndpoints.cs
```

**Integration Tests:**
```
spaceos-modules-dms/tests/Application/
├── Commands/
│   ├── CreateDocumentCommandHandlerTests.cs
│   ├── UploadVersionCommandHandlerTests.cs
│   └── DeleteDocumentCommandHandlerTests.cs
├── Queries/
│   ├── GetDocumentQueryHandlerTests.cs
│   └── SearchDocumentsQueryHandlerTests.cs
└── API/
    └── DmsApiTests.cs
```

**Total:** ~40 new files (~3,000 LOC)

---

## References

**Kontrolling Application Layer (Pattern Reference):**
- `/opt/spaceos/backend/spaceos-modules/spaceos-modules-kontrolling/src/Application/`
- CQRS pattern with MediatR
- FluentValidation integration
- DTO mapping with AutoMapper (optional)

**DMS Domain Layer (Week 1):**
- Document, Folder, Version aggregates
- 84 unit tests PASS
- Repository contracts

**Similar Modules for Reference:**
- CRM Application Layer (if complete)
- QA Application Layer (future, will follow same pattern)

---

## Estimated Effort

**Total: 120 NWT (~4 hours)**

| Phase | Task | NWT | Time |
|-------|------|-----|------|
| **Phase 1** | CQRS Commands (4 commands + handlers) | 30 | ~1 hour |
| **Phase 2** | CQRS Queries (4 queries + handlers) | 25 | ~50 min |
| **Phase 3** | FluentValidation (4 validators) | 15 | ~30 min |
| **Phase 4** | DTOs (6 DTOs) | 10 | ~20 min |
| **Phase 5** | API Endpoints (7 endpoints) | 20 | ~40 min |
| **Phase 6** | Integration Tests (30+ tests) | 15 | ~30 min |
| **Phase 7** | Build + OpenAPI verification | 5 | ~10 min |

**ETA Completion:** ~4 hours from dispatch

---

## Next Steps (After DONE)

**Conductor will dispatch:**
1. HR Week 2 Application Layer → Backend (MSG-BACKEND-154, 150 NWT ~5 hours)
2. Maintenance Week 2 Application Layer → Backend (MSG-BACKEND-155, 150 NWT ~5 hours)
3. QA Week 2 Application Layer → Backend (MSG-BACKEND-156, 150 NWT ~5 hours)

**Frontend readiness:**
- After DMS Week 2 DONE → Frontend can start DMS UI implementation
- Orval codegen + TanStack Query hooks + React components

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
