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
- **MSG-IDENTITY-007:** READ — ROOT APPROVE döntés
- **TOP 3 PHASE 2:** Backend dependency RESOLVED
- **VPS Deploy:** Ready, várjuk Cutting DONE → együtt deploy

## API Endpoints (implementált)

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

- Keycloak.AuthServices.Sdk net10.0 inkompatibilitás → plain HttpClient megoldás
- EF Core materialization issue → fixed (commit 616a89f)
- AllowedHosts TestServer 400 → `UseSetting("AllowedHosts", "*")`

## Session tapasztalatok

- Role whitelist hardcoded: `machine_operator`, `production_manager`, `admin` — security over flexibility
- Keycloak User ID → GUID mapping: `Guid.TryParse()` filter praktikus megoldás
- SEC-09 compliance: tid kizárólag JWT-ből
