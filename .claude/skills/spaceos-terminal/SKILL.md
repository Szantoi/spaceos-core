---
name: spaceos-terminal
description: >
  SpaceOS terminál kommunikációs skill. Használd amikor üzeneteket kell olvasni
  ("olvasd le az üzeneteidet", "olvasd el az inbox-odat", "van új feladatod?"),
  vagy amikor egy feladatot befejeztél és outbox üzenetet kell írni
  ("kész vagyok", "befejeztem", "írj DONE üzenetet").
  A skill az összes SpaceOS terminálra vonatkozik — Kernel, Orchestrator, Infra,
  E2E, Portal, Joinery, Abstractions. Lefedi az inbox olvasás rituálját,
  a kötelező build+test gate-et, és a DONE/BLOCKED outbox sablonokat.
---

# SpaceOS Terminál — Kommunikációs Protokoll

## 1. Inbox olvasás — "olvasd le az üzeneteidet"

```bash
# UNREAD üzenetek keresése
grep -rl "status: UNREAD" ./mailbox/inbox/ 2>/dev/null

# Ha üres: legfrissebb fájl
ls -lt ./mailbox/inbox/ | grep "^-" | head -3
```

A legfrissebb UNREAD fájlt olvasd el, majd módosítsd:
```
status: UNREAD  →  status: READ
```

Ha több UNREAD van: a legalacsonyabb sorszámút dolgozd fel először.

## 2. Feladat végrehajtás — kötelező gate

**Minden feladatnál, kivétel nélkül:**

```
INBOX READ → IMPLEMENTÁLÁS → BUILD → TEST → OUTBOX
```

### Build + test gate

A terminál típusától függ — a saját CLAUDE.md-ben van definiálva:

| Terminál | Build | Test |
|---|---|---|
| Kernel | `dotnet build` → 0 error | `dotnet test` → minden zöld |
| Orchestrator | `npm run build` → 0 TS error | `npm test` → minden zöld |
| E2E | — | `npm test` → meglévők zöldek maradnak |
| Infra | — | health check: `curl /bff/health` vagy `/healthz` |
| Joinery / Abstractions | `dotnet build` → 0 error | `dotnet test` → minden zöld |

**Ha a build vagy test nem zöld: NE írj DONE-t.** Javítsd, aztán futtasd újra.

## 3. DONE üzenet írása

Amikor a feladat kész és a build+test gate átment:

**Fájlnév:** `YYYY-MM-DD_NNN_<slug>-done.md` → `./mailbox/outbox/`

NNN = az inbox üzenet sorszáma (pl. MSG-KERNEL-067 → 067).

```yaml
---
id: MSG-<TERMINAL>-<NNN>-DONE
from: <terminál>
to: conductor
type: done
priority: <az inbox üzenet prioritása>
status: UNREAD
ref: MSG-<TERMINAL>-<NNN>
created: YYYY-MM-DD
---
```

⚠️ **`type` mező szabályai — kötelező betartani:**

| Állapot | `type` értéke |
|---|---|
| Feladat sikeresen teljesítve | `done` |
| Feladat nem teljesíthető, segítség kell | `blocked` |
| Döntést kér a root-tól | `question` |

**`type: response` TILOS** — nem hordoz státusz információt, a root nem tudja kezelni.

**Kötelező szekciók** — részletes sablonok: `references/outbox-templates.md`

```markdown
## Összefoglaló
Mit implementáltál, mely fájlok változtak, commit hash.

## Tesztek
Hány teszt futott, mind zöld? Új tesztek száma?
(Utolsó sor a test output-ból másolva)

## Security review
Ellenőrzött pontok: input validation, auth, RLS/RBAC, nincs secret a logban.

## Kockázatok / kérdések
Ha van → status: BLOCKED és leírás. Ha nincs → "Nincsenek."
```

## 4. BLOCKED üzenet — ha elakadsz

Ha a feladatot nem tudod befejezni önállóan (hiányzó info, függőség, infrastruktúra):

**Fájlnév:** `YYYY-MM-DD_NNN_<slug>-blocked.md` → `./mailbox/outbox/`

```yaml
---
id: MSG-<TERMINAL>-<NNN>-BLOCKED
from: <terminál>
to: conductor
type: blocked
priority: high
status: UNREAD
ref: MSG-<TERMINAL>-<NNN>
created: YYYY-MM-DD
---
```

```markdown
## Mi blokkol
Konkrét technikai leírás — mi hiányzik, mi nem működik.

## Mit próbáltam
Legalább egy diagnózis kísérlet.

## Kérés a root-tól
Mit kell dönteni / kiadni ahhoz, hogy folytatni tudjam.
```

**Soha ne folytasd találgatással.** Ha 2 próbálkozás után sem megy: BLOCKED.

## 5. Mailbox elérési utak

| Terminál | Inbox | Outbox |
|---|---|---|
| Kernel | `/opt/spaceos/docs/mailbox/kernel/inbox/` | `.../kernel/outbox/` |
| Orchestrator | `/opt/spaceos/docs/mailbox/orchestrator/inbox/` | `.../orchestrator/outbox/` |
| E2E | `/opt/spaceos/docs/mailbox/e2e/inbox/` | `.../e2e/outbox/` |
| Infra | `/opt/spaceos/docs/mailbox/infra/inbox/` | `.../infra/outbox/` |
| Joinery | `/opt/spaceos/docs/mailbox/joinery/inbox/` | `.../joinery/outbox/` |
| Abstractions | `/opt/spaceos/docs/mailbox/abstractions/inbox/` | `.../abstractions/outbox/` |
| Portal | `/opt/spaceos/docs/mailbox/portal/inbox/` | `.../portal/outbox/` |
| Sales | `/opt/spaceos/docs/mailbox/sales/inbox/` | `.../sales/outbox/` |

## 6. EF Core migration — kötelező manuális SQL eljárás

> ⚠️ `dotnet ef database update` **NEM megbízható** ezen a VPS-en.
> A `~/.dotnet/tools/dotnet-ef` v10-es, de a projektek .NET 8 targetűek — inkompatibilis.
> Minden migrációt manuális SQL-lel kell alkalmazni.

**Mikor kell:** Új migration fájl van a `Migrations/` könyvtárban, amit a DB még nem tartalmaz.

**Diagnózis — pending migration van-e:**
```bash
# Ellenőrizd mi van a DB-ben
sudo -u postgres psql -p 5433 -d <adatbázis> \
  -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;'

# Ellenőrizd mi van a kódban
ls src/*/Persistence/Migrations/*.cs | grep -v Snapshot | sort
```

**Migration futtatás:**
```bash
# 1. Olvasd el a .cs fájlt — a migrationBuilder.Sql(@"...") blokkok tartalmazzák az SQL-t
cat src/*/Persistence/Migrations/YYYYMMDD_MigrationName.cs

# 2. Futtasd psql-ben (port: 5433)
sudo -u postgres psql -p 5433 -d <adatbázis> <<'SQL'
-- ide kerül a migration SQL tartalma
SELECT 'migration-name OK' AS result;
SQL

# 3. Ha az EF migrations history táblát is frissíteni kell:
sudo -u postgres psql -p 5433 -d <adatbázis> <<SQL
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YYYYMMDDHHMMSS_MigrationName', '8.0.x')
ON CONFLICT DO NOTHING;
SQL
```

**Stale model snapshot** — ha az EF `No migrations were applied. Already up to date.`-t mond,
de a tábla nem létezik: a snapshot üres. Ellenőrzés:
```bash
wc -l src/*/Persistence/Migrations/*ModelSnapshot.cs
# < 50 sor → stale → manuális SQL szükséges
```

> Teljes gotcha dokumentáció: `docs/knowledge/deployment/KNOWN_GOTCHAS.md`

---

## 7. Amit soha nem szabad

- DONE outbox írása build/test failure mellett
- Találgatással folytatni, ha elakadtál (→ BLOCKED)
- Kódot módosítani a DONE után, outbox nélkül
- Más terminál kódjába nyúlni (csak a saját repo-d)
- `TODO` / `FIXME` kommentet commitolni
