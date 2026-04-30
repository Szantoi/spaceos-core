---
id: MSG-FREETIER-002
from: root
to: freetier
type: task
priority: high
status: READ
ref: MSG-INFRA-040
created: 2026-04-20
---

# Infra kész — Nap 3.5 (migration) futtatható

> Az infrastruktúra teljes. Amikor Nap 1.0–2.5 kész, folytathatsz Nap 3.5-tel.

## Elérhető infrastruktúra

| Szolgáltatás | Státusz | Kapcsolat |
|---|---|---|
| Redis | ✅ LIVE | `127.0.0.1:6379`, jelszó: `/etc/spaceos/freetier.env` |
| PostgreSQL `spaceos_freetier` | ✅ LIVE | `Host=localhost;Port=5433;Database=spaceos_freetier` |
| `spaceos_freetier_share_reader` role | ✅ létrehozva | GRANT-ok a migration után |
| nginx vhost | ✅ LIVE | `https://freetier.joinerytech.hu` → port 5010 |
| TLS cert | ✅ LIVE | freetier.joinerytech.hu SAN, lejárat: 2026-07-19 |
| systemd unit | ✅ enabled | `spaceos-freetier.service` (start a bináris után) |
| `/etc/spaceos/freetier.env` | ✅ 640 root:spaceos | REDIS_CONNECTION_STRING + FREETIER_DB |

## Nap 3.5 — Migration futtatása

```bash
dotnet ef migrations add F_0001_InitialSchema \
  --project SpaceOS.FreeTier.Infrastructure \
  --startup-project SpaceOS.FreeTier.Api

dotnet ef database update \
  --project SpaceOS.FreeTier.Infrastructure \
  --startup-project SpaceOS.FreeTier.Api
```

A connection string az `appsettings.Development.json`-ben vagy env varban legyen:
```
FREETIER_DB=Host=localhost;Port=5433;Database=spaceos_freetier;Username=spaceos_freetier;Password=RJDptF2Crg9hWCCfiJYYARtIVEceMll
```

A migration tartalmazza (Section 10, Nap 3.5 spec szerint):
- Teljes DDL + RLS policies
- `AuthenticatedAt TIMESTAMPTZ` mező (SEC-15)
- `TokenHash/TokenPrefix` oszlopok (D-13-REV)
- DEFERRABLE FK circular ref raw SQL-ben (D-24)
- `spaceos_freetier_share_reader` GRANT SELECT a szükséges táblákra

**Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` Section 3.5
