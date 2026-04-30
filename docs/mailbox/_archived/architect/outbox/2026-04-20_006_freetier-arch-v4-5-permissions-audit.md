---
id: MSG-ARCH-006-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: jogosultsági audit (közvetlen Gábor kérés)
created: 2026-04-20
---

## Összefoglalás

A `SpaceOS_FreeTier_Architecture_v4.md` spec-en jogosultsági auditot végeztem. **5 hibát találtam és javítottam a DDL-ben.** A spec most v4.5 verzión van.

## Talált és javított hibák

| # | Súlyosság | Probléma | Javítás |
|---|---|---|---|
| 1 | 🔴 KRITIKUS | `FORCE ROW LEVEL SECURITY` hatástalan volt — a táblák `spaceos_app` tulajdonában lettek volna, az owner bypass-olja a FORCE RLS-t (ADR-SEC-002 sérülés) | `ALTER TABLE ... OWNER TO spaceos_schema_owner` mind a 6 freetier táblán |
| 2 | 🔴 KRITIKUS | `app.user_id` GUC nem volt regisztrálva → runtime `42704` hiba az első DB requestnél | `ALTER DATABASE spaceos SET "app.user_id" TO ''` hozzáadva |
| 3 | 🟡 HIGH | `ShareTokens` DDL-ben `Token VARCHAR(64)` plaintext volt — ellentmondott a D-13-REV döntésnek (SHA-256 hash kell) | `Token` → `TokenHash` VARCHAR(64) + `TokenPrefix` VARCHAR(8), ERD és index is frissítve |
| 4 | 🟡 HIGH | `spaceos_freetier_share_reader` role D-20 döntésben definiált, de a DDL-ből hiányzott — ShareDbContext deployment-kor elszállt volna | `CREATE ROLE` + `GRANT SELECT` 3 táblán hozzáadva |
| 5 | 🟡 HIGH | `UpgradeRequests.ContactEmail NOT NULL` — GDPR user delete cascade sikertelen lett volna | `VARCHAR(320)` nullable-re javítva (SEC-07 fix átvezetve) |

## Spec állapota

`docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` → **v4.5, 54 finding, APPROVED**

Az implementáló terminálnak csak az `F_0001_InitialSchema.cs` migrációt érinti — domain kód változatlan.

## Technikai blokker

A 🔴 KRITIKUS hibák silent failure-t okoztak volna:
- Az OWNER hiánya miatt az RLS "be van kapcsolva" de semmit nem véd — csak deployment után, production adatokkal lett volna észlelhető
- A GUC hiba az első authenticated DB híváskor jött volna elő
