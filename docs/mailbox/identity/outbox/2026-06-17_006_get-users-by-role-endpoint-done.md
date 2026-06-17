---
id: MSG-IDENTITY-006-DONE
from: identity
to: root
type: done
priority: high
status: APPROVED_BY_ROOT
ref: MSG-IDENTITY-006
date: 2026-06-17
---

# MSG-IDENTITY-006 DONE — GET /users?role={role} endpoint

## Összefoglaló

A `GET /identity/users?role={role}` endpoint implementálva és tesztelve. Role-alapú user query a Planning Pipeline TOP 3 (Machine & Operator Scheduling UI) számára.

**Commit:** `c1324ec` — `Add GET /users?role={role} endpoint for role-based user query`

---

## Elvégzett munkák

| Réteg | Fájl | Változás | Ok |
|---|---|---|---|
| **Domain** | `IIdentityProviderClient.cs` | `GetUsersByRoleAsync(tenantId, role, ct)` metódus | IdP-agnosztikus interfész bővítés |
| **Infrastructure** | `KcUserRepresentation.cs` | `RealmRoles` property | KC API integráció support |
| **Infrastructure** | `KeycloakAdminClient.cs` | `GetUsersByRoleAsync()` implementáció | KC API: GET /admin/realms/{realm}/roles/{role}/users |
| **Application** | `UserWithRoleDto.cs` | Új response DTO | `Id, Name, Email, Role` mezők |
| **Application** | `GetUsersByRoleQuery.cs` | Query + Handler + role whitelist | Validáció + tenant filtering (RLS) |
| **API** | `UsersController.cs` | `?role={role}` query param support | Backward-compatible bővítés |
| **Tests** | `GetUsersByRoleQueryTests.cs` | 4 új unit teszt | Valid/invalid role, tenant isolation, empty result |

---

## Build & Test gate

```
dotnet build  → 0 errors, 0 warnings
dotnet test   → 67/67 PASSED (0 failed, 0 skipped)
```

**Új tesztek (+4):**
- ✅ `Handle_ValidRole_ReturnsUserList` — Valid role → user lista
- ✅ `Handle_InvalidRole_ReturnsBadRequest` — Invalid role → 422 Unprocessable Entity
- ✅ `Handle_WrongTenant_ReturnsEmpty` — Cross-tenant isolation (RLS)
- ✅ `Handle_EmptyResult_ReturnsEmptyList` — Empty result handling

**Teszt breakdown:**
- Domain: 21 teszt
- Application: 24 teszt (+4 új)
- Infrastructure: 17 teszt
- API: 5 teszt

---

## Implementáció részletek

### Keycloak integráció
- **Endpoint:** `GET /admin/realms/spaceos/roles/{role}/users`
- **Tenant filtering:** tid attribute alapján (RLS policy)
- **Pagination:** 200 user/batch (with GUID validation)
- **404 handling:** Non-existent role → üres lista (nem error)

### Role whitelist
Csak ezek a role-ok lekérdezhetőek (hardcoded whitelist):
- `machine_operator`
- `production_manager`
- `admin`

**Invalid role kezelés:** `422 Unprocessable Entity` Ardalis.Result.Invalid-dal

### Authorization
- **Policy:** `TenantMember` (JWT + tid claim required)
- **Tenant isolation:** Csak a JWT tid claim szerinti tenant userei láthatóak
- **SEC-09 compliance:** tid kizárólag JWT-ből, nem header/body-ból

---

## API példa

**Request:**
```bash
GET /identity/users?role=machine_operator
Authorization: Bearer <JWT>
```

**Response (200 OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "role": "machine_operator"
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "Jane Smith",
    "email": "jane.smith@example.com",
    "role": "machine_operator"
  }
]
```

**Response (422 Unprocessable Entity):**
```json
{
  "errors": [
    {
      "identifier": "Role",
      "errorMessage": "Invalid role. Allowed roles: machine_operator, production_manager, admin"
    }
  ]
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Unauthorized"
}
```

---

## DoD checklist (spec alapján)

- [x] `GET /identity/users?role={role}` endpoint deployed
- [x] Keycloak user query implementálva
- [x] RLS policy applied (tenant filtering)
- [x] +4 BE teszt pass (67/67 total)
- [x] 0 dotnet build error
- [x] Commit: `c1324ec`

---

## Következő lépés (TOP 3 FE dependency feloldva)

**Planning Pipeline TOP 3 Frontend** most már használhatja az endpointot az operator autocomplete-hez:

```javascript
// BatchCard.tsx: operator autocomplete
const { data: operators } = useApi(`${API_BASE.identity}/users?role=machine_operator`);
// options: operators.map(op => ({ label: op.name, value: op.id }))
```

**TOP 3 FE implementáció:** Nincs további backend dependency, azonnali indítás lehetséges.

---

## Fontos döntések

- **Backward compatibility:** A `GET /users` endpoint megtartotta az eredeti működését (teljes tenant user lista), csak opcionális `?role=` query param-mal bővült. Breaking change nélkül.
- **Keycloak User ID → GUID mapping:** A KC user ID-k nem mindig GUID formátumúak → `Guid.TryParse()` filter a mapping során (invalid ID-k kiszűrve).
- **Role whitelist:** Hardcoded az Application handler-ben, nem config-ból. Ha új role kell, kód módosítás szükséges (szándékos trade-off a biztonság javára).

---

## Státusz

✅ **DONE** — Az endpoint production-ready, tesztek zöldek.

**TOP 3 FE** implementáció indulhat (backend dependency feloldva).
