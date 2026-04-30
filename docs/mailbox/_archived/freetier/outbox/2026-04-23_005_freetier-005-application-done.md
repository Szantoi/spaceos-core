---
id: MSG-FREETIER-005-DONE
from: freetier
to: root
type: done
priority: high
status: READ
ref: MSG-FREETIER-005
created: 2026-04-23
---

# FREETIER-005 DONE — Application réteg (Nap 5.5–8.0)

## Összefoglaló

Teljes Application réteg implementálva: 6 command handler, 3 query handler, FluentValidation, ValidationBehavior MediatR pipeline, interfészek, DTO-k.

### Implementált komponensek

**Service interfészek (5):**
- `IRateLimitService` — D-18 fail-closed
- `IMagicLinkService`
- `IBrevoEmailService`
- `ITurnstileValidator`
- `ILabelStrategyFactory`

**Repository interfészek (5):**
- `IFreeTierUserRepository`
- `IWorkspaceRepository` (CountActiveAsync D-23)
- `IMagicLinkTokenRepository` (InvalidateAllForUserAsync SEC-01)
- `IUpgradeRequestRepository`
- `IShareTokenRepository`

**Command handlers (6):**
| Handler | Biztonsági elem |
|---|---|
| `RequestMagicLinkCommandHandler` | Turnstile, rate limit, SEC-01 invalidate all |
| `VerifyMagicLinkCommandHandler` | `CryptographicOperations.FixedTimeEquals` (SEC-01) |
| `SaveWorkspaceCommandHandler` | D-23 max 20 workspace limit |
| `GenerateShareTokenCommandHandler` | D-13-REV: RawToken transient capture |
| `RevokeShareTokenCommandHandler` | BE-16 |
| `SubmitUpgradeRequestCommandHandler` | FSM Result<T> |

**Query handlers (3):**
| Handler | Megjegyzés |
|---|---|
| `GetWorkspaceQueryHandler` | RLS-scoped |
| `GetWorkspaceRevisionsQueryHandler` | Descending order |
| `GetSharedWorkspaceQueryHandler` | Constant-time hash verify, expiry + revoke check |

**Validátorok:** FluentValidation minden command-on (5 validator)
**ValidationBehavior:** MediatR IPipelineBehavior — Result.Invalid visszaadás
**ConfigureAwait(false):** minden `await` után a handler-ekben
**TokenHashHelper:** `CryptographicOperations.FixedTimeEquals` SHA-256 constant-time compare

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 36, Skipped: 0, Total: 36 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 104 teszt, mind zöld.** (Cél: ≥103 ✅)

Új tesztek (36 Application):
- RequestMagicLinkHandlerTests: 7 (happy path new/existing user, turnstile fail, rate limit, validators, email verification)
- VerifyMagicLinkHandlerTests: 5 (happy path, token not found, expired, wrong token, user not found)
- SaveWorkspaceHandlerTests: 5 (create, update, not found, workspace limit, validator)
- GenerateShareTokenHandlerTests: 4 (happy path, not found, draft error, raw token not null)
- RevokeShareTokenHandlerTests: 3 (happy path, workspace not found, token not found)
- SubmitUpgradeRequestHandlerTests: 3 (happy path, validators)
- GetWorkspaceHandlerTests: 2 (happy path, not found)
- GetWorkspaceRevisionsHandlerTests: 2 (happy path, not found)
- GetSharedWorkspaceHandlerTests: 3 (happy path, wrong token, not found)
- ValidationBehaviorTests: 2 (valid calls next, invalid returns Invalid)

## Security review

- **SEC-01:** `CryptographicOperations.FixedTimeEquals` tokenhash compare-ban (VerifyMagicLink, GetSharedWorkspace)
- **SEC-01:** InvalidateAllForUserAsync hívás a RequestMagicLink handler-ben
- **D-13-REV:** RawToken transient — GenerateShareToken handler capture-öli mentés előtt
- **D-18:** `RateLimitUnavailableException` propagálódik (fail-closed)
- **D-23:** WorkspaceLimit=20 ellenőrzés SaveWorkspaceHandler-ben
- **RawToken soha nem logba:** RequestMagicLink handler nem adja vissza a raw tokent a Result-ban

## Kockázatok / kérdések

Nincsenek. Az Infrastructure implementációk (repository-k, Redis, Brevo) a következő task-ban kerülnek implementálásra.
