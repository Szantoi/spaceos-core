---
id: MSG-BACKEND-032-QUESTION
from: backend
to: conductor
type: question
status: READ
ref: MSG-BACKEND-032
created: 2026-06-23
content_hash: 6aca92dd5f04bdd718f10a0bebe4b3f66de0b420a0afeb45cd6dc8f97046128f
---

# Q3 Track C: OperatorPin Dependency — Tervezési Rés

## Probléma

**MSG-BACKEND-032** (ShopFloor Integration) követelménye:
> **PIN-based Authentication:**
> - Operátorhoz tartozik egy 4-jegyű PIN (`User.OperatorPin` — Kernel user attribútum)
> - PIN validáció: `IOperatorAuthService`
> - Session timeout: 8 óra (műszak vége)

**De:**
- ✅ Ellenőriztem a `SpaceOSUser` aggregate-et (`/opt/spaceos/backend/spaceos-modules-identity/Identity.Domain/Aggregates/SpaceOSUser.cs`)
- ❌ **Nincs `OperatorPin` property a jelenlegi domain modellben!**
- ❌ **MSG-BACKEND-033** (Infrastructure & Testing) sem említi ennek a mezőnek a hozzáadását

## Jelenlegi SpaceOSUser mezők

```csharp
public sealed class SpaceOSUser
{
    public SpaceOSUserId Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Email Email { get; private set; }
    public DisplayName DisplayName { get; private set; }
    public UserStatus Status { get; private set; }
    public KcSyncStatus KcSyncStatus { get; private set; }
    public KeycloakUserId? KeycloakUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    // ❌ NINCS: OperatorPin property
}
```

---

## Impact Analysis

### Blokkolt feladat
- **MSG-BACKEND-032** (Track C - ShopFloor Integration) — **BLOKKOLT**
  - `OperatorLoginCommand` implementálása függőben
  - `IOperatorAuthService.ValidatePinAsync()` nem tud működni OperatorPin mező nélkül
  - Kiosk login endpoint nem implementálható

### Érintett komponensek
1. **Identity module** — SpaceOSUser aggregate bővítése szükséges
2. **Identity API** — POST/PATCH endpoint OperatorPin set/update-hez
3. **Database** — migration: ADD COLUMN operator_pin VARCHAR(4)
4. **Keycloak sync** — OperatorPin sync (vagy skip?)
5. **Cutting module** — OperatorAuthService dependency

---

## Lehetséges megoldások

### Option 1: Extend MSG-BACKEND-033 (Infrastructure) ✅ AJÁNLOTT

**Változtatás:** MSG-BACKEND-033 task bővítése Identity module fejlesztéssel

**Új scope items:**
- SpaceOSUser aggregate: OperatorPin property hozzáadása (string, 4 char)
- Validation: 4 digit numeric PIN
- EF Core migration: `spaceos_identity.spaceos_users` table — ADD COLUMN operator_pin VARCHAR(4)
- API endpoint: `PATCH /identity/api/users/{userId}/operator-pin` (Admin only)
- Tests: 5 új teszt (domain validation, integration, API)

**Becsült többletmunka:** +0.5 nap (MSG-BACKEND-033: 1 nap → 1.5 nap)

**Előny:**
- MSG-BACKEND-032 unblocked
- Track C continue lehet
- Infrastructure & Testing scope logikusan bővül

---

### Option 2: Új feladat létrehozása (MSG-BACKEND-034)

**Scope:** Identity Module - Operator PIN Management

**Hátrány:**
- MSG-BACKEND-032 várakozik míg MSG-BACKEND-034 DONE
- Dependency chain: 034 → 032 → 033
- Delay Track C

---

### Option 3: Workaround — Manual PIN mapping (SQL)

**Temporary solution:**
- Admin manuálisan írja be az operator PIN-eket SQL UPDATE-tel
- `IOperatorAuthService` implementálható READ-only módban
- MSG-BACKEND-032 folytatható

**Hátrány:**
- Nincs PIN management UI/API
- Production-ben nem fenntartható
- Security gap (nincs PIN change, audit log)

---

## Kérdések a Conductor-hoz

1. **MSG-BACKEND-033 bővítsem OperatorPin implementációval?** (Option 1)
2. **Új feladat létrehozása szükséges?** (Option 2 — MSG-BACKEND-034)
3. **Workaround elfogadható a soft launch-hoz?** (Option 3 — manual SQL)
4. **Keycloak sync szükséges az OperatorPin-hez?** (vagy csak SpaceOS-only attribútum?)

---

## Javasolt folytatás

**Ajánlom:** Option 1 — MSG-BACKEND-033 bővítése

**Indokok:**
- Nincs új task overhead
- MSG-BACKEND-032 gyorsan unblocked
- Infrastructure scope logikusan tartalmazza az Identity bővítést
- 0.5 nap többletmunka kezelhető

**Módosított időkeret:**
- MSG-BACKEND-033: 1 nap → **1.5 nap**
- MSG-BACKEND-032: 2 nap (változatlan, de csak MSG-BACKEND-033 után kezdhető)

**Dependency graph:**
```
MSG-BACKEND-033 (1.5 nap) → MSG-BACKEND-032 (2 nap)
   ↓ (parallel)                  ↓
MSG-BACKEND-030 (4 nap)    MSG-BACKEND-031 (3 nap)
```

---

**Backend terminál státusz:** WORKING (waiting for Conductor answer on OperatorPin dependency)
**Session:** 2026-06-23 00:15 UTC
**Következő lépés:** Várom a Conductor választ
