---
id: MSG-FREETIER-008
from: root
to: freetier
type: task
priority: critical
status: READ
ref: MSG-INFRA-048-DONE
created: 2026-04-23
---

# FREETIER-008 — RLS INSERT policy fix (regisztrációs flow)

> **BUG:** `POST /auth/magic-link` → 500: `42501: new row violates row-level security policy for table "Users"`
> **Root cause:** Az RLS policy `USING ("Id" = current_setting('app.user_id', true)::uuid)` ALL operations-ra vonatkozik. Új user regisztrációnál nincs `app.user_id` GUC → INSERT blokkolva.

---

## Fix

A Kernel mintájára: **külön INSERT policy** az `Users` és `MagicLinkTokens` táblákra, ami nem igényli a GUC-ot.

### Migration: `F_0002_RlsInsertPolicyFix`

```bash
dotnet ef migrations add F_0002_RlsInsertPolicyFix \
  --project src/SpaceOS.FreeTier.Infrastructure \
  --startup-project src/SpaceOS.FreeTier.Api
```

A generált `Up()` metódusba raw SQL:

```csharp
migrationBuilder.Sql(@"
    -- Users: INSERT engedélyezés GUC nélkül (regisztrációs flow)
    CREATE POLICY freetier_users_insert ON freetier.""Users""
        FOR INSERT
        WITH CHECK (true);

    -- MagicLinkTokens: INSERT engedélyezés (token létrehozás regisztrációkor)
    CREATE POLICY freetier_magiclinktokens_insert ON freetier.""MagicLinkTokens""
        FOR INSERT
        WITH CHECK (true);

    -- MagicLinkTokens: UPDATE engedélyezés (token invalidálás — InvalidateAllForUserAsync)
    CREATE POLICY freetier_magiclinktokens_update ON freetier.""MagicLinkTokens""
        FOR UPDATE
        USING (true)
        WITH CHECK (true);
");
```

**Miért safe:** Az `INSERT WITH CHECK (true)` megengedi az alkalmazásnak (spaceos_freetier user) hogy bármilyen sort beszúrjon. A SELECT/UPDATE/DELETE továbbra is a `app.user_id` GUC-ot ellenőrzi → user isolation megmarad. Az alkalmazás logika (handler) kontrollálja, hogy milyen adatot szúr be.

### Érintett táblák és műveletek regisztrációs flow-ban

| Tábla | Művelet | GUC elérhető? | Policy szükséges |
|---|---|---|---|
| Users | INSERT (Register) | ❌ | `FOR INSERT WITH CHECK (true)` |
| MagicLinkTokens | INSERT (Generate) | ❌ | `FOR INSERT WITH CHECK (true)` |
| MagicLinkTokens | UPDATE (InvalidateAll) | ❌ | `FOR UPDATE USING (true)` |
| Workspaces | INSERT (Create) | ✅ (auth után) | Nem kell — meglévő policy OK |
| ShareTokens | INSERT | ✅ (auth után) | Nem kell |
| UpgradeRequests | INSERT | ✅ (auth után) | Nem kell |

### Tesztek (+3 minimum)

1. **RLS INSERT teszt (Testcontainers):** `RequestMagicLink` → INSERT Users + MagicLinkTokens GUC nélkül → sikeres
2. **RLS isolation teszt:** User A INSERT → User B SELECT → üres (isolation megmarad)
3. **Reteszt:** `POST /auth/magic-link` → 202 (nem 500)

---

## Definition of Done

- [ ] `F_0002_RlsInsertPolicyFix` migration létrehozva (`dotnet ef migrations add`)
- [ ] INSERT + UPDATE policy-k a fenti 2 táblára
- [ ] Meglévő SELECT/UPDATE/DELETE isolation tesztek továbbra is zöldek
- [ ] Új teszt: GUC nélküli INSERT sikeres (Users + MagicLinkTokens)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 161 pass (158 előző + min 3 új)
- [ ] Outbox DONE üzenet küldve
