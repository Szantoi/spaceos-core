---
id: MSG-IDENTITY-001
from: root
to: identity
type: task
priority: high
status: UNREAD
ref: SpaceOS_Modules_Identity_Architecture_v4
created: 2026-05-27
---

# MSG-IDENTITY-001 — Track A: Identity.Domain implementáció

## Kontextus

Új service: `spaceos-modules-identity` (port 5008). A tervdokumentum:
`/opt/spaceos/docs/tasks/new/SpaceOS_Modules_Identity_Architecture_v4.md`

Ez az első feladat (Track A) — a Domain réteg. Nincs dependency, azonnal indítható.

## Feladat

Hozd létre a solution struktúrát és implementáld a **`Identity.Domain`** projektet teljes egészében.

### 1. Solution létrehozása

```bash
cd /opt/spaceos/backend/spaceos-modules-identity

dotnet new sln -n SpaceOS.Modules.Identity
dotnet new classlib -n Identity.Domain --framework net8.0
dotnet new classlib -n Identity.Application --framework net8.0
dotnet new classlib -n Identity.Infrastructure --framework net8.0
dotnet new webapi -n Identity.Api --framework net8.0
dotnet new xunit -n Identity.Tests --framework net8.0

dotnet sln add Identity.Domain Identity.Application Identity.Infrastructure Identity.Api Identity.Tests

# Project references
dotnet add Identity.Application reference Identity.Domain
dotnet add Identity.Infrastructure reference Identity.Application
dotnet add Identity.Api reference Identity.Infrastructure
dotnet add Identity.Tests reference Identity.Domain Identity.Application
```

### 2. `Identity.Domain` — implementálandó fájlok

A spec **2.2 és 2.3 fejezete** alapján:

**Aggregates/**
- `SpaceOSUser.cs` — Aggregate Root
  - Factory: `SpaceOSUser.Create()` → `KcSyncStatus = Pending`, `KeycloakUserId = null`
  - `Disable()` → csak ha `Status == Active` → `Result.Error("already_disabled")`
  - `Enable()` → csak ha `Status == Disabled` → `Result.Error("already_active")`
  - Nincs public setter — minden mutáció metóduson át
  - Domain event raise minden mutáción

**ValueObjects/**
- `SpaceOSUserId.cs` — UUID wrapper, immutable
- `KeycloakUserId.cs` — string wrapper, nullable, immutable after sync
- `Email.cs` — lowercase normalize, trim, RFC 5322 format validáció
- `DisplayName.cs` — FirstName + LastName, max 100 char each
- `UserStatus.cs` — enum: `Active | Disabled`
- `KcSyncStatus.cs` — enum: `Pending | Synced | Failed`

**DomainEvents/**
- `UserCreatedEvent.cs`
- `UserProfileUpdatedEvent.cs`
- `UserDisabledEvent.cs`
- `UserEnabledEvent.cs`
- `PasswordResetRequestedEvent.cs`
- `UserKcSyncFailedEvent.cs`

**Interfaces/**
- `ISpaceOSUserRepository.cs`
- `IIdentityProviderClient.cs` — IdP-agnosztikus interfész (spec 4.5 fejezet)

### 3. Tesztek — `Identity.Tests/Domain/`

- `SpaceOSUserTests.cs`:
  - `Create_SetsKcSyncStatusToPending`
  - `Create_SetsKeycloakUserIdToNull`
  - `Disable_WhenActive_Succeeds`
  - `Disable_WhenAlreadyDisabled_ReturnsError`
  - `Enable_WhenDisabled_Succeeds`
  - `Enable_WhenAlreadyActive_ReturnsError`
  - `Email_Normalizes_ToLowercase`
  - `Email_InvalidFormat_ThrowsOrReturnsError`
  - `DisplayName_ExceedsMaxLength_ThrowsOrReturnsError`

## Definition of Done

Spec **§7 Domain gates** alapján:
- [ ] `SpaceOSUser.Create()` — public setter nélkül, `KcSyncStatus = Pending`
- [ ] `Disable()` / `Enable()` — invariáns guard, domain event raise
- [ ] `Email` VO — lowercase normalize, RFC 5322 format
- [ ] `DisplayName` VO — max 100 char each field
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden teszt zöld (minimum 9 Domain teszt)

## Fontos megjegyzések

- `Identity.Domain` projektnek **nulla** external NuGet dependency — csak .NET BCL
- `Result<T>` típushoz **Ardalis.Result** csak az `Identity.Application`-ben engedélyezett
- Domain events-hez egyszerű POCO osztályok — nincs MediatR dependency a Domain-ben
- A spec `/keycloak-outbox` skill és `/identity-security` skill hasznos kontextusnak

## Következő track

Track B (Application layer) — csak Track A DONE után kerül kiadásra.
