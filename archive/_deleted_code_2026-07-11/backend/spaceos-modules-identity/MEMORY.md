# IDENTITY Memory

Utolsó frissítés: 2026-06-20

## Aktuális állapot

- **Modul:** spaceos-modules-identity (Port: 5008)
- **Tesztek:** 67/67 PASS
- **Build:** 0 error, 0 warning
- **Git:** `c1324ec` (GET /users?role={role} endpoint)

## Fontos kontextus

- **Track A-E:** COMPLETED — teljes Identity modul implementálva
- **MSG-IDENTITY-006:** APPROVED — GET /users?role={role} endpoint
- **MSG-IDENTITY-007:** READ — ROOT APPROVE döntés feldolgozva
- **TOP 3 PHASE 2:** Backend dependency RESOLVED
- **VPS Deploy:** Ready, várjuk Cutting DONE → együtt deploy

## Implementált architektúra (MSG-006)

- Clean abstraction: `IIdentityProviderClient` interface bővítés
- Proper DTO pattern: `UserWithRoleDto` dedicated response object
- Query CQRS: `GetUsersByRoleQuery` + Handler separation
- KC API: `GET /admin/realms/{realm}/roles/{role}/users`
- Pagination: 200 user/batch
- 404 handling: Non-existent role → empty list

## Security compliance

- Role whitelist: `machine_operator`, `production_manager`, `admin`
- Tenant isolation: JWT tid claim only (SEC-09)
- Invalid role → 422 Unprocessable Entity

## API Endpoints (teljes)

| Method | Path | Status |
|--------|------|--------|
| GET | `/identity/users` | ✅ |
| GET | `/identity/users?role={role}` | ✅ NEW |
| GET | `/identity/users/{id}` | ✅ |
| POST | `/identity/users` | ✅ |
| PUT | `/identity/users/{id}` | ✅ |
| POST | `/identity/users/{id}/disable` | ✅ |
| POST | `/identity/users/{id}/enable` | ✅ |
| POST | `/identity/users/{id}/reset-password` | ✅ |
| POST | `/identity/admin/tenants/{id}/sync-from-keycloak` | ✅ |

## Következő lépések

- Nincs aktív inbox feladat
- Várunk VPS deploy-ra (Cutting-gel együtt)
- Készen állunk további feladatokra

## Megoldott problémák

- Keycloak.AuthServices.Sdk net10.0 inkompatibilitás → plain HttpClient
- EF Core materialization issue → fixed (commit 616a89f)
- AllowedHosts TestServer 400 → `UseSetting("AllowedHosts", "*")`

## Session tapasztalatok

- Role whitelist hardcoded: security over flexibility
- Keycloak User ID → GUID mapping: `Guid.TryParse()` filter
- SEC-09 compliance: tid kizárólag JWT-ből
