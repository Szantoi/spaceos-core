---
id: MSG-IDENTITY-002
from: root
to: identity
type: task
priority: high
status: READ
ref: MSG-IDENTITY-001-DONE
created: 2026-05-27
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-IDENTITY-002 — Track B: Identity.Application implementáció

## Kontextus

Track A (Domain) elfogadva — 21/21 teszt zöld, commit `96e23f1`.
Track B párhuzamosan futhat Track C-vel és D-vel.

Spec: `/opt/spaceos/docs/tasks/active/IDENTITY-V1_modules-identity.md` — §2.3, §4

## Feladat

Implementáld az **`Identity.Application`** projektet teljes egészében.

### NuGet csomagok

```bash
cd Identity.Application
dotnet add package MediatR --version 12.4.0
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Ardalis.Result --version 9.0.0
dotnet add package Ardalis.Specification --version 9.1.0
```

### 1. Common/

- `ICurrentUserContext.cs` — interfész: `TenantId`, `UserId`, `IsAdmin`
- `DTOs/UserDto.cs` — response DTO (id, tenantId, email, firstName, lastName, status, kcSyncStatus)
- `DTOs/CreateUserDto.cs` — email, firstName, lastName
- `DTOs/UpdateUserProfileDto.cs` — firstName, lastName

### 2. Users/Queries/

**`ListTenantUsersQuery`** + Handler + `TenantUsersByStatusSpec`
- Handler: `AsNoTracking()`, RLS `SET LOCAL` az infra rétegen át
- Spec: `TenantUsersByStatusSpec(tenantId, status?)` — Ardalis.Specification
- Result: `Result<IReadOnlyList<UserDto>>`

**`GetUserByIdQuery`** + Handler + `TenantUserByIdSpec`
- Handler: explicit BOLA guard: `if (user.TenantId != _currentUser.TenantId) return Result.Forbidden()`
- Result: `Result<UserDto>`

### 3. Users/Commands/

**`CreateUserCommand`** + Handler + Validator
- Validator: email format (FluentValidation), firstName/lastName nem üres, max 100 char
- Handler:
  1. Duplicate email check (`TenantUserByEmailSpec`, `AsNoTracking`)
  2. `SpaceOSUser.Create()` → `KcSyncStatus = Pending`
  3. DB tranzakció: INSERT `spaceos_users` + INSERT `kc_sync_outbox` (atomiáris!)
  4. `PopDomainEvents()` + dispatch
  5. `Result<UserDto>.Success()` → 201

**`UpdateUserProfileCommand`** + Handler + Validator

**`DisableUserCommand`** + Handler
- `user.Disable()` → ha `Result.IsError("already_disabled")` → 400

**`EnableUserCommand`** + Handler

**`ResetPasswordCommand`** + Handler
- Redis sliding window rate limit: 5/user/hour (a handler csak az Outbox-ot insertálja, a tényleges KC hívás a WorkerService feladata)

**`SyncTenantUsersFromKeycloakCommand`** + Handler
- `SuperAdmin` only (policy check a handlerben is)
- tid assert: `kc_user.tid != tenantId → skip + warn`

### 4. Tesztek — `Identity.Tests/Application/`

- `CreateUserCommandTests.cs` — duplicate email → conflict, invalid email → 422, siker → 201 + Outbox insert
- `DisableUserCommandTests.cs` — Active → Disabled OK; már Disabled → error
- `UpdateUserProfileCommandTests.cs`
- `ListTenantUsersQueryTests.cs` — cross-tenant izoláció (mock repo-val)
- `GetUserByIdQueryTests.cs` — BOLA: idegen tenant → Forbidden
- `ResetPasswordCommandTests.cs` — 6. kísérlet → rate_limit_exceeded

## Definition of Done

- [ ] `GET /identity/users` — csak JWT `tid` tenant usereit adja vissza
- [ ] `GET /identity/users/{id}` — explicit handler-szintű tenant guard: `user.TenantId != currentUser.TenantId → Result.Forbidden()`
- [ ] `POST /identity/users` — duplikált email → `Result.Conflict()`
- [ ] `POST /identity/users` — invalid email → `Result.Invalid()`
- [ ] `POST /identity/users/{id}/disable` — már disabled → `Result.Error("already_disabled")`
- [ ] `POST /identity/users/{id}/reset-password` — 6. kísérlet → `Result.Error("rate_limit_exceeded")`
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden teszt zöld (minimum 15 Application teszt)

## Megjegyzés

- Az Outbox INSERT az Application handler dolga (Unit of Work pattern) — az Infrastructure réteg csak a repository interfészt implementálja
- `IKcSyncOutboxRepository` interfészt definiálj a Domain/Interfaces-ben vagy az Application/Common-ban
- Track C (Infrastructure) párhuzamosan fut — ha kell az Infrastructure implementáció, mock-olj
