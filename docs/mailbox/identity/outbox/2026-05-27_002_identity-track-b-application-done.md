---
id: MSG-IDENTITY-002-DONE
from: identity
to: root
type: done
priority: high
status: READ
ref: MSG-IDENTITY-002
created: 2026-05-27
---

# MSG-IDENTITY-002-DONE — Track B: Identity.Application implementáció kész

## Összefoglaló

Track B teljes egészében implementálva. Commit: `c6ad6f8`

**Létrehozott fájlok:**

| Fájl | Leírás |
|---|---|
| `Common/ICurrentUserContext.cs` | TenantId, UserId, IsAdmin, IsSuperAdmin |
| `Common/IKcSyncOutboxRepository.cs` | Outbox interfész + KcSyncOutboxEntry + KcSyncOperation enum |
| `Common/IRateLimitService.cs` | Rate limit interfész (Redis sliding window — impl: Track C) |
| `Common/UserMapper.cs` | SpaceOSUser → UserDto mapping |
| `Common/DTOs/UserDto.cs` | Response DTO |
| `Common/DTOs/CreateUserDto.cs` | |
| `Common/DTOs/UpdateUserProfileDto.cs` | |
| `Users/Queries/ListTenantUsersQuery.cs` | Handler: JWT tid alapú tenant izoláció |
| `Users/Queries/GetUserByIdQuery.cs` | Handler: explicit BOLA guard (SEC-02) |
| `Users/Commands/CreateUserCommand.cs` | Handler + Validator: duplikált email → Conflict, invalid → Invalid, outbox INSERT |
| `Users/Commands/UpdateUserProfileCommand.cs` | Handler + Validator |
| `Users/Commands/DisableUserCommand.cs` | Handler: already_disabled → Result.Error |
| `Users/Commands/EnableUserCommand.cs` | Handler: already_active → Result.Error |
| `Users/Commands/ResetPasswordCommand.cs` | Handler: 5/user/hour rate limit (SEC-04) |
| `Users/Commands/SyncTenantUsersFromKeycloakCommand.cs` | Handler: SuperAdmin only, tid assert |
| `Users/Specifications/TenantUsersByStatusSpec.cs` | Ardalis.Specification |
| `Users/Specifications/TenantUserByIdSpec.cs` | |
| `Users/Specifications/TenantUserByEmailSpec.cs` | |

## Tesztek

```
Passed!  - Failed: 0, Passed: 41, Skipped: 0, Total: 41, Duration: 323 ms
```

20 új Application teszt (+ 21 meglévő Domain teszt):
- `CreateUserCommandTests` — 4 teszt (siker, duplikált email, invalid email, outbox payload)
- `DisableUserCommandTests` — 4 teszt (active→disabled, already_disabled error, not found, cross-tenant)
- `UpdateUserProfileCommandTests` — 3 teszt
- `ListTenantUsersQueryTests` — 3 teszt (cross-tenant izoláció, status filter)
- `GetUserByIdQueryTests` — 3 teszt (BOLA, not found, siker)
- `ResetPasswordCommandTests` — 3 teszt (siker, 6. kísérlet rate limit, cross-tenant)

## Definition of Done checklist

- [x] `GET /identity/users` — csak JWT `tid` tenant usereit adja vissza
- [x] `GET /identity/users/{id}` — explicit handler-szintű BOLA guard
- [x] `POST /identity/users` — duplikált email → `Result.Conflict()`
- [x] `POST /identity/users` — invalid email → `Result.Invalid()`
- [x] `POST /identity/users/{id}/disable` — már disabled → `Result.Error("already_disabled")`
- [x] `POST /identity/users/{id}/reset-password` — 6. kísérlet → `Result.Error("rate_limit_exceeded")`
- [x] `dotnet build` → 0 error, 0 warning
- [x] `dotnet test` → 41/41 zöld (20 Application + 21 Domain)

## Security review

- SEC-02: BOLA guard minden handler-ben, ahol userId path param van
- SEC-04: ResetPasswordCommand rate limit interfészen át (Redis impl: Track C)
- SEC-06: SyncFromKeycloak — SuperAdmin double-check handler-ben is
- Nincs secret/PII logban
- `tid` kizárólag `ICurrentUserContext.TenantId`-ból (JWT-ből jön az infra rétegen)

## Kockázatok / kérdések

Nincsenek. Track C (Infrastructure) és Track D (Keycloak/Workers) párhuzamosan futhat.
