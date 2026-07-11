---
id: MSG-BACKEND-023
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md
created: 2026-06-22
content_hash: 5b1e4c482fec495a5733a117b36de726f1a10efffd8521259cd6da2bb71de7ea
---

# Identity: GET /users?role={role} Endpoint Implementation

## Kontextus

A **TOP 3 Machine & Operator Scheduling UI** frontend implementációjához szükséges egy role-alapú user query endpoint az Identity modulban.

**Doorstar use-case:** Amikor művezetők vágási tervet rendelnek géphez, ki kell választani az operátort egy dropdown-ból. Ez a lista csak a `machine_operator` role-lal rendelkező user-eket tartalmazhatja.

## Feladat

Implementáld a `GET /identity/users?role={role}` endpointot.

### Endpoint specifikáció

**Request:**
```
GET /identity/users?role=machine_operator
Authorization: Bearer {jwt}
```

**Response:**
```json
{
  "users": [
    {
      "id": "uuid",
      "username": "janos.kovacs",
      "firstName": "János",
      "lastName": "Kovács",
      "email": "janos.kovacs@doorstar.hu",
      "roles": ["machine_operator"]
    }
  ]
}
```

### Implementációs scope

1. **Application:** Query handler
   - Input: `role` string paraméter
   - Keycloak client query: `GET /admin/realms/{realm}/users?role={role}`
   - Response mapping: User DTO lista

2. **API:** Presentation layer
   - Új endpoint: `src/SpaceOS.Modules.Identity.Api/Endpoints/UsersEndpoints.cs`
   - RBAC: csak authenticated user hívhatja (JWT required)
   - Query parameter validation: role nem lehet üres

3. **Tesztek (minimum 2):**
   - Integration test: `GetUsersByRole_ValidRole_ReturnsUsersList`
   - Integration test: `GetUsersByRole_EmptyRole_Returns400`

### DoD

- [ ] Endpoint működik: `GET /identity/users?role={role}`
- [ ] +2 teszt pass
- [ ] `dotnet test` pass (63+ tests)
- [ ] `dotnet publish` sikeres

### Implementációs irányelvek

**Követendő minták:**
- Identity modul már használ Keycloak HttpClient-et
- Meglévő endpoint minta: `src/.../Endpoints/UsersEndpoints.cs` (ha létezik)
- Response DTO: `UserDto` (firstName, lastName, email, roles)

**Kockázatok:**
- Keycloak admin API jogosultság: a service client-nek `view-users` role kell
- Role paraméter validálás: csak megengedett role-ok (whitelist?)

### Kapcsolódó fájlok

- `backend/spaceos-modules-identity/src/SpaceOS.Modules.Identity.Application/Queries/`
- `backend/spaceos-modules-identity/src/SpaceOS.Modules.Identity.Api/Endpoints/`
- `backend/spaceos-modules-identity/tests/`

### Becsült idő

**0.5 nap** (query handler + endpoint + 2 teszt)

---

**Priority:** MEDIUM — Frontend TOP 3 fallback működik mock nélkül is, de UX javítás
**Ref:** `/opt/spaceos/docs/tasks/archive/CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md`
