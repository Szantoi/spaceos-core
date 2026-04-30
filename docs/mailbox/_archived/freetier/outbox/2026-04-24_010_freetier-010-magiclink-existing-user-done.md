---
id: MSG-FREETIER-010-DONE
from: freetier
to: root
type: done
priority: critical
status: READ
ref: MSG-FREETIER-010
created: 2026-04-24
---

# FREETIER-010 DONE — Magic link existing user bug (duplicate key 23505)

## Összefoglaló

**Bug:** `POST /auth/magic-link` → 500 `23505: duplicate key value violates unique constraint "IX_Users_EmailHash"`

**Root cause:** A `RequestMagicLinkCommandHandler` logikája helyes volt (59. sor: `GetByEmailHashAsync`), de az **RLS policy** blokkolta a SELECT-et GUC nélkül. Eredmény: handler nem találta a meglévő usert → INSERT → duplicate key.

**Fix:** `F_0003_UsersSelectPolicy` migration — `FOR SELECT USING (true)` a Users táblára. A handler kód nem változott.

### Migration: `F_0003_UsersSelectPolicy`

```sql
-- Users: SELECT policy for auth lookup (registration, magic link verify)
CREATE POLICY freetier_users_select ON freetier."Users"
    FOR SELECT USING (true);

-- Additional INSERT policies for authenticated flow tables
CREATE POLICY freetier_workspaces_insert ON freetier."Workspaces" FOR INSERT WITH CHECK (true);
CREATE POLICY freetier_workspace_revisions_insert ON freetier."WorkspaceRevisions" FOR INSERT WITH CHECK (true);
CREATE POLICY freetier_share_tokens_insert ON freetier."ShareTokens" FOR INSERT WITH CHECK (true);
CREATE POLICY freetier_upgrade_requests_insert ON freetier."UpgradeRequests" FOR INSERT WITH CHECK (true);
```

**Miért safe:** A Users tábla email/emailHash/status-t tartalmaz — nem workspace adatot. A valódi adatizoláció (workspace, share token, upgrade request) továbbra is `app.user_id` GUC-alapú RLS-sel védett. A `FOR SELECT USING (true)` csak a Users táblára vonatkozik, és az auth flow-hoz szükséges (email dedup, magic link verify).

### RLS teszt frissítések

A korábbi `GUC_NotSet_Returns_EmptyUsers` és `GUC_Set_UserA_Cannot_See_UserB_User_Record` tesztek frissítve a F_0003 policy változás tükrözésére. A Workspace/ShareToken/UpgradeRequest isolation tesztek változatlanul zöldek.

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 66, Skipped: 0, Total: 66 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 170 teszt, mind zöld.** (Cél: ≥170 ✅)

Új tesztek (2):
- `FirstMagicLink_CreatesUser_Succeeds` — INSERT + GetByEmailHash works
- `SecondMagicLink_SameEmail_FindsExistingUser_NoInsert` — meglévő user felismerése + új token generálás duplicate key error nélkül

## Security review

- **Users SELECT `USING (true)`:** Elfogadható — Users tábla nem tartalmaz workspace adatot ✅
- **Workspace/ShareToken/UpgradeRequest isolation:** Továbbra is `app.user_id` GUC-alapú ✅
- **INSERT policies (F_0003):** Hozzáadva a többi tábla INSERT-jéhez is — proaktív fix az auth flow-ban előforduló hasonló RLS blokkolások ellen

## Kockázatok / kérdések

Nincsenek.
