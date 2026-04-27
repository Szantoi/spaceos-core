---
id: partner-outbox-002
from: partner
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-04-27_002_partner-inbox2-embed-lead-commission.md
date: 2026-04-27
---

# DONE: PartnerTier — Embed Token + Lead Capture + Commission (Inbox #2)

## Eredmény

- `dotnet build`: 0 error, 0 warning
- `dotnet test`: 111 pass, 0 fail, 0 skip (56 meglévő + 55 új)

## Megvalósított

### Domain
- `PartnerLead` entity — AES-256-CBC email titkosítás (IV prepend), SHA-256 emailHash, GDPR soft-delete
- `PartnerUpgradeAttribution` entity — 12 hónapos attribution ablak, `MarkConverted` FSM guard

### Infrastructure
- `EmbedTokenService` — HS256 JWT generálás + validálás (ClockSkew=0), issuer/audience validáció
- `IKernelEmailHashClient` + `KernelEmailHashClientStub` — valódi HTTP client Inbox #4-ben
- `AttributionWorker` — BackgroundService, hourly batch, 100-as limit, scoped DbContext

### API — új endpointok
- `POST /api/partner/embed-token` — API key auth → JWT kiadás (1h TTL, tier + allowedOrigin claim)
- `POST /api/partner/nest` — Bearer embed token auth, tier-alapú algoritmus (Free=FFDH, Pro/Enterprise=Guillotine)
- `POST /api/partner/leads` — AES-256 email encrypt, SHA-256 hash visszaadva

### Tesztek (55 új)

| Fájl | Teszt |
|------|-------|
| Domain/PartnerLeadTests.cs | 13 |
| Domain/PartnerUpgradeAttributionTests.cs | 11 |
| Infrastructure/EmbedTokenServiceTests.cs | 11 |
| Infrastructure/AttributionWorkerTests.cs | 8 |
| Api/EmbedEndpointTests.cs | 12 |

## Technikai megjegyzések

1. PartnerTier namespace konflikt — DomainTier alias minden érintett fájlban
2. JwtPayload.Exp obsolete — TreatWarningsAsErrors=true miatt Expiration property-t használja
3. /api/partner/nest SkipPaths-ban van, Bearer token auth inline a handler-ben
4. Valódi pgcrypto — csak Inbox #4 DB migration-nél kerül be; app layer AES-256-CBC-t szimulál

## Következő lépés (Inbox #3)
- SpaceOS admin portal endpoints (partner review, suspension)
- Partner magic-link auth (self-service portal)
- Commission report query handler
- GDPR crypto-shredding
