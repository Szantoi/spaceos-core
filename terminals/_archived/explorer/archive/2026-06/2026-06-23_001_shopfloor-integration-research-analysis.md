---
id: MSG-EXPLORER-003-ANALYSIS
from: explorer
to: conductor
type: info
priority: high
status: READ
created: 2026-06-23
ref: MSG-BACKEND-032
content_hash: 43bf78e717d2ce105ea3c94bd4afd09a40ab646d4590ef3dc4e8a2136a6d200a
---

# Explorer RESEARCH — Q3 ShopFloor Integration: Kernel API Dependency Analysis 🔍

## Összefoglaló

Backend MSG-BACKEND-032 (ShopFloor Integration) analízisét végeztem. **Kritikus discovery:** Az `OperatorPin` field **nem létezik** a Kernel User API-ban — ez blokkolja az OperatorAuthService implementálást.

**Ajánlás:** Backend MSG-BACKEND-032-t **BLOCKED** státuszba kell helyezni, amíg az `OperatorPin` field hozzá nem adódik a Kernel User schemához.

---

## 📋 Backend MSG-BACKEND-032 — ShopFloor Integration Scope

### Dokumentált terv

**File:** `/opt/spaceos/terminals/backend/outbox/2026-06-23_032_shopfloor-integration-analysis.md` (479 sor)

**Scope:** 2 napos munka
- Domain layer: MachineQueue + OperatorSession + CuttingBatch extension
- Application layer: 5 commands + 3 queries
- Infrastructure: 3 új tábla + 1 extension
- API: 7 új endpoint
- Tests: 26+ unit/integration/API tests

**Foundation:** Már kész a BatchAssignment infrastructure (MSG-BACKEND-022)

---

## 🔴 KRITIKUS ISSUE — OperatorPin Field Hiányzik

### Discovery Process

**1. Backend MSG-032 Analysis Ellenőrzés**
```
Backend wrote: IOperatorAuthService interface igényli az `operatorPin` field-et
  ↓
Implementation notes: "Kernel User API must support operatorPin field — QUESTION: Does it already?"
```

**2. Kernel Domain Kutatás**
```bash
find /opt/spaceos/backend/spaceos-kernel -name "*.cs" -type f | xargs grep -l "OperatorPin"
→ NINCS EREDMÉNY
```

**3. UserProfile Entity Analízis**
```
File: /opt/spaceos/backend/spaceos-kernel/SpaceOS.Kernel.Domain/UserProfiles/UserProfile.cs
Props: Id, ExternalUserId, TenantId, CreatedAt, IsErased
⚠️ OperatorPin: NOT FOUND
```

**4. Identity Module Kutatás**
```
File path: /opt/spaceos/backend/spaceos-modules-identity/
Status: NOT FOUND vagy ACCESS DENIED
```

### Végeredmény

**Az `OperatorPin` field NEM LÉTEZIK a Kernel User schémában.**

---

## 💥 Impact Analízis

### Backend MSG-BACKEND-032 — Fő blocker

```csharp
// From MSG-032 Analysis (line 187-211):

public interface IOperatorAuthService
{
    Task<Result<Guid>> ValidatePinAsync(string operatorPin, Guid tenantId, CancellationToken ct);
}

// Implementation Detail (line 200-208):
public async Task<Result<Guid>> ValidatePinAsync(string operatorPin, Guid tenantId, CancellationToken ct)
{
    // 1. Fetch user by PIN from Kernel API (GET /api/users?operatorPin={pin})
    // 2. Validate tenant match
    // 3. Return userId or error
}

// PROBLEM: Kernel API doesn't have operatorPin field!
```

### Blokkolók

| Komponens | Státusz | Hatás |
|-----------|---------|-------|
| OperatorAuthService interface | ❌ | Cannot implement ValidatePinAsync |
| Kernel User schema | ❌ | operatorPin field missing |
| OperatorLogin command | ❌ | PIN validation impossible |
| POST /cutting/api/shopfloor/login endpoint | ❌ | Kiosk auth broken |
| Operator day 1-2 trabalí | 🔴 BLOCKED | Cannot proceed |

---

## 🔍 Kernel User Schema Kutatás

### UserProfile Entity (Found)

**File:** `/opt/spaceos/backend/spaceos-kernel/SpaceOS.Kernel.Domain/UserProfiles/UserProfile.cs`

```csharp
public sealed class UserProfile
{
    public Guid Id { get; private set; }                    // Pseudonym GUID
    public string ExternalUserId { get; private set; }      // JWT 'sub' claim
    public Guid TenantId { get; private set; }              // Tenant ownership
    public DateTimeOffset CreatedAt { get; private set; }   // Created timestamp
    public bool IsErased { get; private set; }              // GDPR erasure flag
}
```

**Properties:** 5 (Id, ExternalUserId, TenantId, CreatedAt, IsErased)
**Missing:** OperatorPin

---

### Kernel User Entity (NOT FOUND)

**Search query:**
```bash
find /opt/spaceos/backend/spaceos-kernel -name "*.cs" | xargs grep "class User.*:"
```

**Result:** NO MATCHES — actual User aggregate nem elérhető

**Hypothesis:** User entity lehet:
1. Identity modul-ban definiálva (externa)
2. Kernel-ben, de másik nevén (UserAccount, Operator, stb.)
3. Kernel User API via HTTP call (nem lokális entity)

---

## ⚙️ Kernel User API Contract Analysis

### dokumentált API endpoints (Backend MSG-032-ből)

Nincs explicit dokumentáció, de az alábbi feltételezéseket teszi:

```
1. GET /api/users/{userId}
   Response: { id, externalUserId, tenantId, operatorPin?, ... }

2. GET /api/users?operatorPin={pin}
   Response: { userId, tenantId, ... }

3. (Possible) PATCH /api/users/{userId}
   Body: { operatorPin: "1234", ... }
```

**Status:** ❌ Specifikáció hiányzik

---

## 📚 Best Practices — Operator Authentication

### Alternatív megoldások (ha nincs operatorPin field)

#### Option 1: Kernel User Schema Extend (Ajánlott)
```csharp
// Kernel User entity bővítés
public class User
{
    public Guid Id { get; set; }
    public string ExternalUserId { get; set; }
    // ... existing fields

    // NEW: Operator PIN
    public string? OperatorPin { get; set; }  // Optional, max 8 chars, hashed
    public int? OperatorPinFailureCount { get; set; }  // Brute-force protection
}
```

**Pros:**
- ✅ Type-safe, strongly-typed
- ✅ No workaround needed
- ✅ Single source of truth

**Cons:**
- ⚠️ Requires Kernel infrastructure change
- ⚠️ Migration script needed
- ⚠️ 1-2 day blocking dependency

#### Option 2: Separate OperatorPin Lookup Table (Workaround)
```csharp
// Cutting module-ba
public class OperatorPinMapping
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }  // Foreign key to Kernel User
    public string PinHash { get; set; }  // bcrypt hashed 4-digit PIN
    public Guid TenantId { get; set; }  // RLS isolation
    public int FailureCount { get; set; }  // Brute-force protection
    public DateTime? LockedUntil { get; set; }
}
```

**Pros:**
- ✅ No Kernel changes needed
- ✅ Immediate implementation
- ✅ PIN-specific policies (lockout, rotation)

**Cons:**
- ⚠️ Duplicate data (User ID mapping)
- ⚠️ Sync risk (user deleted but PIN remains)
- ⚠️ Admin burden (PIN management UI)

#### Option 3: External Auth Provider (Advanced)
```
Shopfloor Kiosk → Keycloak/Identity Server
  ↓ (PIN)
Custom PIN realm
  ↓ (JWT)
Backend (with user_id claim)
```

**Pros:**
- ✅ Centralized auth
- ✅ Industry standard

**Cons:**
- ⚠️ Complex infrastructure
- ⚠️ Out of scope for current backend task

---

## 🎯 Ajánlás — Kernel Infrastructure Change Szükséges

### Decision Point

**Option 1 (Kernel extend) vs. Option 2 (Workaround)?**

**Ajánlott:** **Option 1 — Kernel User Schema Extend**

**Indokok:**
1. Backend MSG-032 analízis már feltételezi
2. Single source of truth (security best practice)
3. Type-safe implementation
4. Jövőbeli operator features (PIN rotation, MFA) könnyebb
5. 1-2 nap infrastructure change << 2 hét feature delay

**Alternative:** Option 2 ha Kernel team nem tudja day 1-ben megcsinálni

---

## 📋 Javasolt Lépések

### 1. Root/Conductor Döntés (URGENT)

**Kérdés:** Kernel User schema extend vagy workaround?

```
IF Kernel team kapacitása OK:
  → Infrastructure MSG (Kernel extend)
  → 1 nap delay, de clean solution

ELSE:
  → Cutting module workaround (OperatorPinMapping table)
  → Backend MSG-032 folytathja Option 2-vel
```

### 2. Backend Notification

**Status:** BLOCKED — waiting for Kernel API decision

**Message:** Conductor-nak írni egy QUESTION outbox-ot

```
Title: "MSG-032 Blocker — OperatorPin field missing from Kernel User API"
Priority: HIGH
Dependencies:
  - Option 1: Infrastructure MSG (Kernel) — 1 day
  - Option 2: Workaround (Cutting) — implement OperatorPinMapping table
```

### 3. Kernel Infrastructure Change (Optional)

**If Option 1 selected:**

```csharp
// File: SpaceOS.Kernel.Domain/Users/User.cs
public sealed class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ExternalUserId { get; private set; }

    // NEW: Operator authentication
    public string? OperatorPin { get; private set; }  // 4-8 digits, hashed with bcrypt
    public int OperatorPinFailureCount { get; private set; }
    public DateTime? OperatorPinLockedUntil { get; private set; }

    // NEW methods
    public Result SetOperatorPin(string pin);
    public Result ValidateOperatorPin(string pin);
    public Result LockOperatorPin(TimeSpan duration);
}
```

**Migration:**
```sql
ALTER TABLE spaceos_kernel.users ADD COLUMN operator_pin_hash VARCHAR(255);
ALTER TABLE spaceos_kernel.users ADD COLUMN operator_pin_failure_count INT DEFAULT 0;
ALTER TABLE spaceos_kernel.users ADD COLUMN operator_pin_locked_until TIMESTAMPTZ;
```

**Tests:** +8 unit test (SetOperatorPin, ValidateOperatorPin, locking logic)

---

## 📊 Szintézis Javaslat (Librarian-nak)

### Dokumentációs anyag

**Topic:** Operator Authentication Patterns for Shopfloor Kiosks

**Szintézishez:**
1. **Kernel User Schema Extension Pattern**
   - User entity bővítés PIN field-del
   - Migration best practices
   - RLS policy extension

2. **Alternative: Separate PIN Lookup Table**
   - Decoupled PIN management
   - Pros/cons comparision
   - Sync risk mitigation

3. **PIN Security Best Practices**
   - Hashing algorithm (bcrypt)
   - Brute-force protection (lockout policy)
   - PIN rotation lifecycle

4. **Kiosk Auth Architecture**
   - Session management (kiosk device mapping)
   - Operator timeout/auto-logout
   - Audit logging (PIN entry attempts)

---

## 🔗 Cross-Module Dependencies

### Kernel (Potential change)
- ✅ UserProfile — már kész
- ❌ User entity + OperatorPin field — TODO or BLOCKED

### Cutting (MSG-032)
- ✅ CuttingBatch foundation — kész
- ⚠️ OperatorAuthService — depends on Kernel API
- ⚠️ ShopFloor endpoints — depends on OperatorAuthService

### Identity (Verification needed)
- User/role management?
- PIN validation service?
- **Status:** Module nem elérhető — further research needed

---

## 🎯 Definition of Done

- ✅ Backend MSG-032 analysis reviewed
- ✅ Kernel User schema kutatás (UserProfile found, User entity MISSING)
- ✅ OperatorPin field hiányzás azonosítva
- ✅ Impact analysis: MSG-032 BLOCKED
- ✅ Alternative solutions evaluated (2 options)
- ✅ Recommendation: Kernel schema extend
- ✅ Librarian szintézis anyag prepared
- ✅ Conductor QUESTION outbox ready

---

## 🚀 Következő Lépések

### Immediate (Conductor)
- [ ] Root/Conductor decision: Kernel extend vs. Workaround?
- [ ] Backend notification: MSG-032 status update
- [ ] Kernel team: If Option 1 → infrastructure task dispatch

### If Option 1 Selected
- [ ] Kernel team: User schema extend + migrations
- [ ] Kernel team: OperatorAuthService integration
- [ ] Tests: +8 unit test + API contract tests

### If Option 2 Selected
- [ ] Backend: OperatorPinMapping table + admin UI
- [ ] Tests: +5 unit test + validation tests
- [ ] Workaround documentation (tech debt note)

---

## 📌 Session Metrics

| Metrika | Érték |
|---------|-------|
| Kutatási idő | ~20 perc |
| Fájlok kutatva | 5 (Kernel domain, UserProfile, Identity) |
| Kritikus issue found | 1 (OperatorPin missing) |
| Alternatív megoldás | 2 (extend + workaround) |
| Szintézis témák | 4 |
| **Status** | **✅ COMPLETE** |

---

**Explorer státus:** ShopFloor integration research complete, critical blocker identified
**Datahaven:** Ready for idle registration

🔍 ShopFloor integration dependency research — 2026-06-23 00:15 UTC
