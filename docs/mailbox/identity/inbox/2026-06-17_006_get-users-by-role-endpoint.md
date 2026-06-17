---
id: MSG-IDENTITY-006
from: root
to: identity
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-001
created: 2026-06-17
completed: 2026-06-17
---

# Identity — GET /users?role={role} endpoint (TOP 3 dependency)

## Összefoglaló

A Planning Pipeline consensus TOP 3 (Machine & Operator Scheduling UI) implementációjához szükséges a **role-alapú user query endpoint**.

**Scope:** `GET /identity/users?role={role}` endpoint + RBAC + tesztek

**Becs. munka:** 0.5 nap BE

**Priority:** HIGH — TOP 3 FE track 1 napot vár erre

---

## Spec (consensus tervből)

### Backend endpoint

**Endpoint:** `GET /identity/users?role={role}`

**Query params:**
- `role` (string, required) — pl. `machine_operator`, `production_manager`

**Response:**
```json
[
  {
    "id": "uuid",
    "name": "John Doe",
    "email": "john@example.com",
    "role": "machine_operator"
  }
]
```

**Response codes:**
- 200 OK — users found
- 400 Bad Request — invalid role
- 401 Unauthorized — JWT missing
- 403 Forbidden — insufficient permission

### Implementation guide

1. **Endpoint location:** `Identity.Api/Endpoints/UsersEndpoint.cs` (create if needed)

2. **Handler:**
   - `GetUsersByRole(string role)` method
   - Query Keycloak atau Kernel users table (role attribute check)
   - Filter by tenant (RLS policy apply)
   - Return DTO: `UserDto { Id, Name, Email, Role }`

3. **RBAC:**
   - Requires JWT + `[Authorize(Roles = "admin,production_manager")]`
   - Users can query only within their tenant

4. **Validation:**
   - Role whitelist (csak valid role-okat query-zni): `machine_operator`, `production_manager`, `admin`
   - Empty result → 200 OK with `[]`

5. **Tests:**
   - `GetUsersByRole_ValidRole_ReturnsUserList`
   - `GetUsersByRole_InvalidRole_ReturnsBadRequest`
   - `GetUsersByRole_NoAuth_Returns401`
   - `GetUsersByRole_WrongTenant_ReturnsEmpty` (RLS check)

### Tesztek

- +4 be test pass
- Teljes Identity teszt coverage: ~931 → 935 teszt

---

## Precedencia (TOP 3 FE)

TOP 3 Frontend (`BatchAssignmentBoard.tsx`) ez az endpoint-ot hívja az operator autocomplete-hez:

```javascript
// BatchCard.tsx: operator autocomplete
const { data: operators } = useApi(`${API_BASE.identity}/users?role=machine_operator`);
// options: operators.map(op => ({ label: op.name, value: op.id }))
```

**Kritikus:** Ezt az endpoint-ot TOP 1-2 előtt **NEM szükséges**, csak TOP 3 FE-hez.

---

## DoD (Definition of Done)

- [ ] `GET /identity/users?role={role}` endpoint deployed
- [ ] Keycloak/Kernel user query implementálva
- [ ] RLS policy applied (tenant filtering)
- [ ] +4 BE teszt pass
- [ ] API dokumentáció (Swagger tag: `Users`)
- [ ] 0 dotnet build error
- [ ] Smoke test: `curl -H "Authorization: Bearer <token>" https://api.joinerytech.hu/identity/users?role=machine_operator`

---

## Relationship (TOP 3 FE-vel)

**Blokk:** TOP 3 FE-nek kell ez az endpoint azonnali indításhez
**Ajánlott sorrend:** PÁRHUZAMOS (Identity ezt a napot, FE TOP 1-2 párhuzamosan)

---

## Siguiente lépés

1. Identity terminál: `GetUsersByRole` endpoint implementáció (0.5 nap)
2. Deploy + smoke test
3. FE terminál: TOP 3 implementáció (4-5 nap, backend után)

🚀 Indítás: **AZONNAL** (párhuzamosan TOP 1 FE-vel)
