---
id: MSG-FREETIER-004
from: root
to: freetier
type: task
priority: critical
status: READ
ref: MSG-FREETIER-003
created: 2026-04-20
---

# FREETIER-004 — KRITIKUS v4.5 spec addendum (F_0001_InitialSchema)

> **FIGYELEM:** Ez az üzenet a FREETIER-003 kiegészítése.
> Az Architect jogosultsági auditja (MSG-ARCH-006-RESPONSE) 3 kritikus hibát talált  
> a spec-ben amelyek **nem szerepelnek a FREETIER-003 inbox üzenetben**.
> Ha a migration még nem fut: add hozzá. Ha már elkészült: külön patchmigration kell.

A spec frissítve: `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` → **v4.5, APPROVED**

---

## 1. 🔴 KRITIKUS — FORCE RLS bypass javítás (OWNER fix)

Az RLS `FORCE ROW LEVEL SECURITY` **hatástalan**, ha a tábla tulajdonosa (OWNER) és a
futtatási role ugyanaz — az owner bypass-olja az RLS-t (PostgreSQL dokumentáció, ADR-SEC-002).

**Hozzáadandó az `F_0001_InitialSchema` migration `Up()` metódusába:**

```csharp
migrationBuilder.Sql(@"
    ALTER TABLE freetier.""Users""           OWNER TO spaceos_schema_owner;
    ALTER TABLE freetier.""Workspaces""      OWNER TO spaceos_schema_owner;
    ALTER TABLE freetier.""WorkspaceRevisions"" OWNER TO spaceos_schema_owner;
    ALTER TABLE freetier.""ShareTokens""     OWNER TO spaceos_schema_owner;
    ALTER TABLE freetier.""MagicLinkTokens"" OWNER TO spaceos_schema_owner;
    ALTER TABLE freetier.""UpgradeRequests"" OWNER TO spaceos_schema_owner;
");
```

**Miért kritikus:** deployment után production adatokkal lett volna észlelhető —
az RLS "be volt kapcsolva" de semmit nem védett.

---

## 2. 🔴 KRITIKUS — GUC regisztráció (app.user_id)

Az `app.user_id` GUC nincs regisztrálva a freetier adatbázisban → runtime `42704` hiba
az első authenticated DB requestnél.

**Hozzáadandó az `F_0001_InitialSchema` migration `Up()` metódusába:**

```csharp
migrationBuilder.Sql(@"
    ALTER DATABASE spaceos_freetier SET ""app.user_id"" TO '';
    ALTER DATABASE spaceos_freetier SET ""app.session_id"" TO '';
");
```

**Megjegyzés:** `app.session_id` is regisztrálni kell — anonymous workspace-eknél ez a GUC
azonosítja a session-t (nem `app.user_id`).

---

## 3. 🟡 HIGH — UpgradeRequests.ContactEmail nullable (GDPR)

A `ContactEmail NOT NULL` constraint megakadályozta volna a User GDPR delete cascade-et.

**EF konfig javítás** (`UpgradeRequestConfiguration.cs`):
```csharp
builder.Property(e => e.ContactEmail)
    .HasMaxLength(320)
    .IsRequired(false);  // nullable — GDPR SEC-07
```

Ha az EF már generálta a migration-t `NOT NULL`-lal, a raw SQL override:
```csharp
migrationBuilder.Sql(@"
    ALTER TABLE freetier.""UpgradeRequests""
    ALTER COLUMN ""ContactEmail"" DROP NOT NULL;
");
```

---

## Definition of Done (addendum)

- [ ] Mind a 6 freetier tábla OWNER TO spaceos_schema_owner a migration-ban
- [ ] `app.user_id` és `app.session_id` GUC regisztrálva `ALTER DATABASE`-vel
- [ ] `UpgradeRequests.ContactEmail` nullable az EF config-ban ÉS a migration-ban
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ FREETIER-003 DoD + ezen fixek legalább 2 tesztje:
  - GUC hiba nem dobódik autentikált DB hívásnál
  - GDPR delete nem dob FK constraint error-t (ContactEmail null-ra frissítve cascade előtt)
- [ ] FREETIER-003 DONE üzenetben legyen jelezve, hogy v4.5 fixek implementálva

> **Sorrend:** Ha a FREETIER-003 migration még nem kész, integráld ezeket oda.
> Ha már `dotnet ef database update` lefutott, új migration kell: `F_0002_RlsOwnerAndGucFix`.
