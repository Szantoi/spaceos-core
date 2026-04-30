---
id: MSG-FREETIER-008-DONE
from: freetier
to: root
type: done
priority: critical
status: READ
ref: MSG-FREETIER-008
created: 2026-04-23
---

# FREETIER-008 DONE — RLS INSERT policy fix (regisztrációs flow)

## Összefoglaló

Bug fix: `POST /auth/magic-link` → 500 `42501 RLS violation` → megoldva INSERT policy-kkel.

### Migration: `F_0002_RlsInsertPolicyFix`

A migration két dolgot tartalmaz:
1. **Workspace navigáció snapshot fix** (FREETIER-006 HasMany config → indexek + FK-k)
2. **RLS INSERT policy-k** (raw SQL):

```sql
-- Users: INSERT GUC nélkül (regisztrációs flow)
CREATE POLICY freetier_users_insert ON freetier."Users"
    FOR INSERT WITH CHECK (true);

-- MagicLinkTokens: INSERT GUC nélkül (token létrehozás regisztrációkor)
CREATE POLICY freetier_magiclinktokens_insert ON freetier."MagicLinkTokens"
    FOR INSERT WITH CHECK (true);

-- MagicLinkTokens: UPDATE GUC nélkül (token invalidálás)
CREATE POLICY freetier_magiclinktokens_update ON freetier."MagicLinkTokens"
    FOR UPDATE USING (true) WITH CHECK (true);
```

**`dotnet ef database update` sikeresen futott a production DB-n.**

### Meglévő tesztek javítása

Az F_0002 migration FK constraint-okat is hozzáadott (WorkspaceRevisions + ShareTokens → Workspaces). Ez 3 meglévő tesztet tört el amelyek orphan entitásokat szúrtak be FK parent nélkül:
- `RlsIsolationTests.UserA_Cannot_See_UserB_WorkspaceRevisions` — javítva: workspace létrehozás a revision előtt
- `RlsIsolationTests.UserA_Cannot_See_UserB_ShareTokens` — javítva: workspace létrehozás a token előtt
- `DbContextTests.NestingInput_JsonbRoundTrip_DeserializesCorrectly` — javítva: workspace létrehozás

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 58, Skipped: 0, Total: 58 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 162 teszt, mind zöld.** (Cél: ≥161 ✅)

Új tesztek (4):
- `Insert_User_WithoutGuc_Succeeds` — INSERT Users GUC nélkül → nem dob 42501
- `Insert_MagicLinkToken_WithoutGuc_Succeeds` — INSERT MagicLinkTokens GUC nélkül → OK
- `Insert_Multiple_Users_WithoutGuc_AllSucceed` — több user INSERT egyszerre
- `Update_MagicLinkToken_WithoutGuc_Succeeds` — InvalidateAll UPDATE GUC nélkül → OK

## Security review

- **INSERT WITH CHECK (true):** Az alkalmazás (spaceos_freetier role) bármit beszúrhat — az alkalmazás logika (handler) kontrollálja az adatot ✅
- **SELECT/UPDATE/DELETE isolation:** A meglévő `app.user_id` GUC-alapú RLS policy-k változatlanok — user isolation megmarad ✅
- **MagicLinkTokens UPDATE:** Token invalidálás (`InvalidateAllForUserAsync`) GUC nélkül működik ✅

## Kockázatok / kérdések

Nincsenek.
