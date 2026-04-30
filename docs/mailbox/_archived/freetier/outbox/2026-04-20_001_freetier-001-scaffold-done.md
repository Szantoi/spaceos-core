---
id: MSG-FREETIER-001-DONE
from: freetier
to: root
type: done
priority: high
status: READ
ref: MSG-FREETIER-001
created: 2026-04-20
---

# FREETIER-001 DONE — Repo scaffold + domain alapréteg

## Összefoglaló

**Repo:** `/opt/spaceos/spaceos-freetier-api/`

Elkészült a teljes scaffold és domain réteg (Nap 1.0–2.5 spec szerint).

### Létrehozott fájlok

```
SpaceOS.FreeTier.sln
Directory.Build.props          ← net8.0, Nullable=enable, TreatWarningsAsErrors=true
nuget.config                   ← nuget.org + local-spaceos (Contracts artifacts)
src/
  SpaceOS.FreeTier.Domain/     ← aggregates, entities, VOs, events, enums, abstractions
  SpaceOS.FreeTier.Application/ ← üres skeleton (tesztek FREETIER-003-ban)
  SpaceOS.FreeTier.Infrastructure/ ← üres skeleton (FREETIER-003-ban)
  SpaceOS.FreeTier.Api/        ← Program.cs + /healthz + appsettings.json
tests/
  SpaceOS.FreeTier.Domain.Tests/      ← 51 teszt
  SpaceOS.FreeTier.Application.Tests/ ← üres (FREETIER-003-ban töltjük)
  SpaceOS.FreeTier.Integration.Tests/ ← 2 teszt (healthcheck)
```

### Domain réteg implementált komponensek

| Komponens | Megjegyzés |
|---|---|
| `AggregateRoot<TId>` + `IClock` + `IDomainEventContainer` | Alapabstrakciók |
| `FreeTierUser` | Register (EmailHash SHA-256), Activate (AuthenticatedAt SEC-15), ExtendSession (180d limit), RequestDelete (GDPR) |
| `Workspace` | Create, Save (revision chain), GenerateShareToken (D-13-REV hash), RevokeShare, Archive |
| `UpgradeRequest` | Submit + FSM: Pending→Approved→Provisioned→Completed (Result<T>) |
| `WorkspaceRevision` | Immutable snapshot, denormalized UserId (RLS) |
| `ShareToken` | 32-byte CSPRNG → base64url → SHA-256 hex; TokenPrefix = first 8 chars; RawToken transient |
| `MagicLinkToken` | D-11-REV: 32-byte CSPRNG, SHA-256 hash, 15min expiry |
| `NestingInput` | SEC-08: max 500 parts, 1–10000mm, ≤100 char names; ToJson/FromJson |
| `SessionNonce` | D-14-REV: 16-byte CSPRNG, ToRedisKey() = "sess:"+SHA256(value) |
| 7 domain event | sealed record: UserRegistered, MagicLinkRequested, FreeTierUserActivated, WorkspaceSaved, ShareTokenGenerated, ShareTokenRevoked, UpgradeRequested |
| 4 enum | FreeTierUserStatus, WorkspaceStatus, UpgradeRequestStatus, LabelStrategy |

**API skeleton:**
- `Program.cs`: Serilog, Swagger, `/healthz` → `{"status":"healthy"}` 200 OK
- `appsettings.json`: Kestrel `http://127.0.0.1:5010`
- `public partial class Program` (WebApplicationFactory kompatibilitás)

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51, Duration: 151 ms - SpaceOS.FreeTier.Domain.Tests.dll (net8.0)
Passed!  - Failed: 0, Passed:  2, Skipped: 0, Total:  2, Duration:  12 ms - SpaceOS.FreeTier.Integration.Tests.dll (net8.0)
```

**Összesen: 53 teszt, mind zöld.** (Application.Tests: üres — FREETIER-003-ban kerülnek bele.)

DoD tesztek:
- ✅ `/healthz` 200 + body `{"status":"healthy"}` (2 integration teszt)
- ✅ FreeTierUser aggregate: 8+ teszt (Register, Activate, ExtendSession, 180d limit, RequestDelete, domain events)
- ✅ Workspace: 8+ teszt (Create, Save, GenerateShareToken, hash verify, RevokeShare)
- ✅ UpgradeRequest: 6+ teszt (Submit, FSM transitions, invalid state → Result.Error)
- ✅ ShareToken: 3+ teszt (hash, prefix, revoke)
- ✅ NestingInput: 2+ teszt (valid, 501 parts → error)

## Security review

- **D-11-REV / D-13-REV:** TokenHash mindig SHA-256 hex (64 char), plaintext token soha nem kerül tárolásra; RawToken csak a Generate() transient visszatéréseként elérhető
- **D-14-REV:** SessionNonce 16-byte CSPRNG, Redis key = "sess:"+SHA256(nonce)
- **SEC-15:** AuthenticatedAt mező implementálva; ExtendSession 180 napos abszolút limit érvényesítve domain szinten
- **BE-02:** UpgradeRequest state machine Result<T> visszatérési értékkel, private setterek
- **BE-07:** IClock injektálva minden domain factory-ba, nincs `DateTime.UtcNow` közvetlen hívás
- **SEC-08:** NestingInput.Create() validálja: max 500 parts, 1–10000mm dim, ≤100 char name
- **EmailHash:** SHA-256(lowercase(email)), nem tárolt plaintext az EmailHash-hez

## Kockázatok / kérdések

A `SpaceOS.Modules.Contracts` 1.3.0 NuGet hivatkozás az Infrastructure projektben van előkészítve, de a domain réteg még nem használja (szükségtelen volt a FREETIER-001 scope-ban).

Nincs blokkoló kockázat. FREETIER-003 (persistence réteg) blokkolójától az infra megérkezett (MSG-FREETIER-002 ✅).
