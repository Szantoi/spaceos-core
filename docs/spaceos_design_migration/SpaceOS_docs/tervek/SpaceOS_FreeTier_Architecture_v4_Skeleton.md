# SpaceOS — FreeTier Anonymous Workspace Architecture
## Anonymous Nesting · Magic-Link Workspace · Upgrade Funnel

> **Verzió:** v4.0-skeleton — 2026-04-20
> **Státusz:** ⏳ DESIGN DRAFT — Session D arch-planner indításra kész
> **Blokkoló feltétel:** FT-1..FT-5 döntések APPROVED (`SpaceOS_Session_D_Kickoff_Decisions_FT1_FT5.md`)
> **Kumulált review:** ⏳ PENDING (v2 → database · v3 → security · v4 → backend)
> **Referencia:** `SpaceOS_Growth_Strategy_v1.md` · `SpaceOS_Session_D_Kickoff_Decisions_FT1_FT5.md` · `SpaceOS_Modules_Contracts_Architecture_v4_2.md` (1.3.0 extension points) · `SpaceOS.Nesting.Algorithms` 1.0.0
> **Repo:** `spaceos-freetier-api` (új polyrepo)
> **DB schema:** `freetier` (shared PostgreSQL 16, nem új DB)
> **Port:** 5007 (systemd, loopback-only)
> **Becsült effort:** ~17.5 fejlesztői nap (FT-1..FT-5 delta után)
> **Test baseline:** 3150+ pass (Soft Launch LIVE)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

⏳ **PENDING** — Feltöltésre kerül az arch-planner review pipeline során.

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | ⏳ | ⏳ | ⏳ |
| v2 → `/senior-security` → v3 | ⏳ | ⏳ | ⏳ |
| v3 → `/senior-backend` → v4 | ⏳ | ⏳ | ⏳ |
| **Összesen** | ⏳ | | ⏳ |

---

## 2. Architekturális döntések

### 2.1 Core döntések (FT-1..FT-5 — approved)

| # | Döntés | Választás | Referencia |
|---|--------|-----------|------------|
| **FT-1** | Izolációs modell | User-scoped FreeTier.Api (nem tenant) | Kickoff Decisions §FT-1 |
| **FT-2** | Session + rate limit store | Redis anonymous + PG post-auth + Redis counter | Kickoff Decisions §FT-2 |
| **FT-3** | URL stratégia | `eszkozok.joinerytech.hu` külön subdomain | Kickoff Decisions §FT-3 |
| **FT-4** | Rate limit PRIMARY | FreeTier.Api middleware + Redis | Kickoff Decisions §FT-4 |
| **FT-5** | Upgrade flow v1 | Manuális (Slack notif + admin provisioning) | Kickoff Decisions §FT-5 |

### 2.2 Inherited döntések (Growth Strategy v1)

| # | Döntés | Választás | GS ref |
|---|--------|-----------|--------|
| D-01 | Input modell | SEO landing + manuális form | PQ1 |
| D-02 | Visualization | L2 interaktív SVG + 4 label strategy + 3D v2 | PQ2 |
| D-03 | Auth mechanizmus | Magic link + 30 nap sliding | FT1 |
| D-04 | Share | Read-only + export · editable v2 | FT3 |
| D-05 | Label default | FullLabelStrategy | FT4 |
| D-06 | PDF elemek | SVG + cut list + yield + branding + CTA + meta (6 elem) | PQ7 |
| D-07 | Virality tags | Mind a 4 (PDF footer, share landing, invite, DXF watermark) | FT7 |
| D-08 | Email vendor | Brevo (magic link + marketing) | GS §5.4 |
| D-09 | Captcha | Cloudflare Turnstile (cookieless) | GS §5.4 |
| D-10 | Analytics | Plausible self-hosted | GS §5.4 |

### 2.3 Új döntések (Session D scope)

⏳ **PENDING** — A review pipeline során kerülnek be.

| # | Döntés | Választás | Indoklás |
|---|--------|-----------|----------|
| D-11 | Magic link token format | ⏳ | ⏳ |
| D-12 | Workspace JSON schema verziózás | ⏳ | ⏳ |
| D-13 | Share token formátum | ⏳ | ⏳ |
| D-14 | Session fingerprint algoritmus | ⏳ | ⏳ |
| D-15 | Rate limit sliding window bucket méret | ⏳ | ⏳ |
| D-16 | Redis eviction policy | ⏳ | ⏳ |
| D-17 | Upgrade export format stability contract | ⏳ | ⏳ |

---

## 3. Scope határ

### Core fázis (ez a dokumentum — v1)

| Fogalom | Tartalom | Státusz kezelés |
|---------|----------|-----------------|
| **Anonymous Session** | 10 min TTL Redis key · fingerprint-alapú · nesting lefuttatható, eredmény olvasható | `ACTIVE` → (timeout) → törölve |
| **FreeTierUser** | Magic-link auth'd user · 30 nap sliding · `user_id` izoláció | `PENDING_VERIFY` → `ACTIVE` → `EXPIRED` |
| **Workspace** | Permanent user-owned nesting history + saved configurations | `DRAFT` → `SAVED` → `SHARED` → `ARCHIVED` |
| **ShareToken** | Read-only + export link · opcionális expiry | `ACTIVE` → `EXPIRED` |
| **UpgradeRequest** | Kernel tenant provisioning kérés | `PENDING` → `APPROVED` → `PROVISIONED` → `COMPLETED` |

### NEM scope (későbbi fázisok)

| Fogalom | Fázis | Prereq |
|---------|-------|--------|
| 3D Three.js visualization | v2 | v1 SVG DEPLOYED |
| Editable share (collaboration) | v2 | v1 read-only share DEPLOYED |
| Stripe checkout + auto-provisioning | v2 | v1 manual upgrade validated |
| DACH subdomain mirror (`werkzeuge.asztalostech.hu`) | v2 | EN mirror (`tools.joinerytech.com`) DEPLOYED |
| PartnerTier embed (iframe) | Session E (v2.5) | FreeTier v1.5 LIVE |
| DXF/STEP export | v1.5 | v1 PDF DEPLOYED |

---

## 4. Domain modell

### 4.1 Solution struktúra

```
spaceos-freetier-api/
├── SpaceOS.FreeTier.Domain/
│   ├── Aggregates/
│   │   ├── FreeTierUser.cs           ⏳ v1
│   │   ├── Workspace.cs              ⏳ v1
│   │   └── UpgradeRequest.cs         ⏳ v1
│   ├── Entities/
│   │   ├── WorkspaceRevision.cs      ⏳ v1 — snapshot per save
│   │   └── ShareToken.cs             ⏳ v1
│   ├── ValueObjects/
│   │   ├── MagicLinkToken.cs         ⏳ v1
│   │   ├── SessionFingerprint.cs     ⏳ v1 — IP hash + UA hash
│   │   └── NestingInput.cs           ⏳ v1 — input panel + lines
│   ├── Enums/
│   │   ├── FreeTierUserStatus.cs
│   │   ├── WorkspaceStatus.cs
│   │   ├── UpgradeRequestStatus.cs
│   │   └── LabelStrategy.cs          ⏳ v1 — 4 érték
│   ├── Events/
│   │   ├── AnonymousNestingRequested.cs
│   │   ├── MagicLinkRequested.cs
│   │   ├── FreeTierUserActivated.cs
│   │   ├── WorkspaceSaved.cs
│   │   ├── ShareTokenGenerated.cs
│   │   ├── FreeTierUpgradeRequested.cs  ← hoz Slack webhook-ot
│   │   └── UpgradeProvisioned.cs
│   └── Repositories/
│       ├── IFreeTierUserRepository.cs
│       ├── IWorkspaceRepository.cs
│       └── IUpgradeRequestRepository.cs
├── SpaceOS.FreeTier.Application/
│   ├── Commands/
│   │   ├── SubmitAnonymousNesting/
│   │   ├── RequestMagicLink/
│   │   ├── VerifyMagicLink/
│   │   ├── SaveWorkspace/
│   │   ├── GenerateShareToken/
│   │   └── RequestUpgrade/
│   ├── Queries/
│   │   ├── GetWorkspace/
│   │   ├── ListUserWorkspaces/
│   │   └── GetSharedWorkspace/
│   ├── DTOs/
│   │   ├── AnonymousNestingRequest.cs
│   │   ├── AnonymousNestingResult.cs
│   │   ├── WorkspaceDto.cs
│   │   └── WorkspaceExportV1.cs      ← stable contract upgrade flow-hoz
│   └── Services/
│       ├── IRateLimitService.cs      ← Redis counter
│       ├── IMagicLinkService.cs
│       ├── IBrevoEmailService.cs
│       ├── ITurnstileValidator.cs
│       └── ILabelStrategyFactory.cs
├── SpaceOS.FreeTier.Infrastructure/
│   ├── Data/
│   │   ├── FreeTierDbContext.cs      ← `freetier` schema
│   │   ├── UserSessionInterceptor.cs ← user_id-scoped RLS
│   │   └── Migrations/
│   │       └── F_0001_InitialSchema.cs
│   ├── Redis/
│   │   ├── RedisSessionStore.cs
│   │   ├── RedisRateLimitCounter.cs
│   │   └── RedisConnectionFactory.cs
│   ├── External/
│   │   ├── BrevoEmailClient.cs
│   │   ├── TurnstileHttpClient.cs
│   │   └── SlackWebhookClient.cs     ← upgrade notif
│   └── Pdf/
│       └── QuestPdfBrandedExporter.cs
├── SpaceOS.FreeTier.Api/
│   ├── Endpoints/
│   │   ├── AnonymousNestingEndpoints.cs
│   │   ├── AuthEndpoints.cs          ← magic link request + verify
│   │   ├── WorkspaceEndpoints.cs
│   │   ├── ShareEndpoints.cs
│   │   └── UpgradeEndpoints.cs
│   ├── Middleware/
│   │   ├── TurnstileMiddleware.cs
│   │   ├── RateLimitMiddleware.cs    ← Redis-backed
│   │   └── UserSessionMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
└── SpaceOS.FreeTier.Tests/
    ├── Domain/
    ├── Application/
    ├── Api/
    └── Security/
```

### 4.2 Aggregate-ek ⏳ PENDING

Részletes domain model (pseudo-code skeleton):

```csharp
// Domain/Aggregates/FreeTierUser.cs
public sealed class FreeTierUser
{
    // ⏳ Properties: Id, Email (hashed?), Status, CreatedAt, LastActivityAt, SlidingExpiryAt
    // ⏳ Factory: Register(email, turnstileToken, clock) → Result<FreeTierUser>
    // ⏳ Commands: ActivateViaMagicLink, ExtendSession, RequestUpgrade, Archive
    // ⏳ Events: FreeTierUserActivated, UpgradeRequested
}

// Domain/Aggregates/Workspace.cs  
public sealed class Workspace
{
    // ⏳ Properties: Id, UserId, Name, CurrentRevisionId, Status, SharedToken?
    // ⏳ Revisions: List<WorkspaceRevision> (snapshot per save)
    // ⏳ Commands: Save, GenerateShare, RevokeShare, Archive
}

// Domain/Aggregates/UpgradeRequest.cs
public sealed class UpgradeRequest
{
    // ⏳ Properties: Id, UserId, CompanyName, VAT, Contact, Expectations, Status
    // ⏳ Commands: Approve (admin), MarkProvisioned (admin), MarkCompleted
    // ⏳ Events: FreeTierUpgradeRequested (Slack trigger)
}
```

⏳ Full domain model — arch-planner session writes v1.

---

## 5. Infrastructure

### 5.1 Deployment topológia ⏳ PENDING részletezés

```
                  ┌──────────────────────────────────────────────────┐
Browser  ─HTTPS─▶ │ Nginx (TLS 1.2/1.3)                               │
                  │   eszkozok.joinerytech.hu → 127.0.0.1:5007        │
                  │   limit_req zone (anti-DoS)                       │
                  │   X-SpaceOS-Brand: freetier (host-injection)      │
                  └────────────┬─────────────────────────────────────┘
                               ▼
                  ┌──────────────────────────────────────────────────┐
                  │ FreeTier.Api :5007 (systemd, loopback-only)       │
                  │   ├── TurnstileMiddleware                         │
                  │   ├── RateLimitMiddleware (Redis counter)         │
                  │   ├── UserSessionMiddleware (magic link scope)    │
                  │   └── Endpoints                                   │
                  └──────┬────────────────────────────────────┬─────┘
                         ▼                                     ▼
                  ┌─────────────┐                      ┌────────────┐
                  │ Redis 7.4   │                      │ PG 16      │
                  │ :6379       │                      │ :5433      │
                  │ sess:{...}  │                      │ `freetier` │
                  │ rl:{...}    │                      │  schema    │
                  └─────────────┘                      └────────────┘
                                                              │
                                                              │ (upgrade)
                                                              ▼
                                                       ┌────────────┐
                                                       │ Kernel     │
                                                       │ (tenant    │
                                                       │  seed via  │
                                                       │  manual    │
                                                       │  script)   │
                                                       └────────────┘

External:
  ├── Cloudflare Turnstile (captcha validate)
  ├── Brevo API (magic link + marketing)
  ├── Slack webhook (#spaceos-sales — upgrade notif)
  └── Plausible (self-hosted, `analytics.joinerytech.hu`)
```

### 5.2 Redis konfiguráció ⏳ PENDING

- Install: `apt install redis-server` (Ubuntu 24.04)
- Config: `/etc/redis/redis.conf` — loopback-only, `maxmemory 512mb`, `maxmemory-policy allkeys-lru`, `save ""` (AOF off), `requirepass` (env var)
- Systemd: `redis-server.service`
- UFW: 6379 NEM nyitva kifelé

### 5.3 Nginx vhost ⏳ PENDING

- `/etc/nginx/sites-available/spaceos-freetier` — új vhost
- TLS cert reuse: `/etc/letsencrypt/live/joinerytech.hu/` (wildcard SAN)
- `limit_req_zone $binary_remote_addr zone=freetier:10m rate=20r/s`
- `proxy_pass 127.0.0.1:5007`

### 5.4 Approved package bővítés

| Package | Verzió | Licensz | Approval |
|---|---|---|---|
| `StackExchange.Redis` | 2.8.x | MIT | ⏳ TO_BE_APPROVED — explicit lista bővítés |
| `QRCoder` | 1.4.x | MIT | ⏳ TO_BE_APPROVED |
| `QuestPDF` | 2024.x | MIT | ✅ APPROVED (Joinery v2 használta) |

---

## 6. Security ⏳ PENDING — v2 review

Placeholder findings kategóriák:

| Kategória | Amire figyelni fog a `/sub-senior-security` |
|---|---|
| **Auth** | Magic link token entropy, one-time use, expiry · Turnstile verify flow |
| **Session hijacking** | SessionFingerprint spoofing resistance · cookie flags |
| **Rate limit bypass** | Multiple IP rotation · headless browser detection |
| **Data exposure** | ShareToken leak in referer · PDF metadata PII |
| **GDPR** | PII collection minimization · retention + right-to-delete endpoint |
| **Email abuse** | Magic link email flooding · bounce handling |
| **Upload validation** | Nesting input size limits · CSV/JSON injection |
| **CSP/CORS** | Subdomain isolation · iframe-blocking (own frame only) |

---

## 7. Database schema ⏳ PENDING — v2 review

Placeholder — full SQL DDL az arch-planner session deliverable.

```sql
-- freetier.users
-- freetier.workspaces  
-- freetier.workspace_revisions
-- freetier.share_tokens
-- freetier.upgrade_requests
-- RLS FORCE + user_id policy
```

---

## 8. Backend review ⏳ PENDING — v4 review

Placeholder — Golden Rules compliance check:
- ConfigureAwait(false) on every async
- AsNoTracking on read queries
- Result<T> return types
- PopDomainEvents + DispatchAsync on mutations
- Ardalis.Specification for list queries
- No public setters on aggregates

---

## 9. Definition of Done

### Solution gates
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] 5 csproj struktúra · CLAUDE.md · README.md · appsettings.json
- [ ] Test baseline ≥ 150 teszt (becsülve)

### Domain gates (⏳ részletezve v4-ben)
- [ ] 3 aggregate: FreeTierUser, Workspace, UpgradeRequest
- [ ] 2+ entity: WorkspaceRevision, ShareToken
- [ ] 3+ VO: MagicLinkToken, SessionFingerprint, NestingInput
- [ ] 4+ enum
- [ ] 7+ domain event
- [ ] Minden mutáció domain event-et vet

### Application gates (⏳ részletezve v4-ben)
- [ ] 6 command handler
- [ ] 3 query handler
- [ ] FluentValidation minden command-on
- [ ] IRateLimitService + IMagicLinkService + IBrevoEmailService + ITurnstileValidator + ILabelStrategyFactory

### Infrastructure gates (⏳ részletezve v4-ben)
- [ ] Redis connection factory + retry policy
- [ ] PG `freetier` schema + Migration F_0001
- [ ] UserSessionInterceptor (RLS on user_id)
- [ ] Brevo HTTP client + rate limit backoff
- [ ] Slack webhook client
- [ ] QuestPDF branded exporter

### API gates
- [ ] 5 endpoint group (AnonymousNesting, Auth, Workspace, Share, Upgrade)
- [ ] 3 middleware (Turnstile, RateLimit, UserSession)
- [ ] Swagger/OpenAPI descriptor
- [ ] Health check endpoint (`/healthz`)

### Security gates (⏳ v3 pipeline töltse)
- [ ] RLS FORCE mind a 4 táblán (users, workspaces, revisions, share_tokens)
- [ ] Rate limit profile 2 tier (anonymous + auth'd)
- [ ] Magic link TTL + one-time-use
- [ ] Turnstile validation minden submission-ön
- [ ] GDPR delete endpoint + 30-day grace

### Infra gates
- [ ] Nginx vhost `eszkozok.joinerytech.hu` LIVE
- [ ] Redis systemd active + loopback-only
- [ ] FreeTier.Api systemd active + loopback-only
- [ ] DNS record
- [ ] Brevo sender domain verify
- [ ] Turnstile site key production

### Monitoring gates
- [ ] Plausible event tracking (landing, submit, save, upgrade_request)
- [ ] Structured logs (Serilog → journalctl)
- [ ] Rate limit hit counter metric

### Végállapot
- [ ] E2E happy path: anonymous landing → nesting → email capture → magic link → workspace save → share generate → export PDF
- [ ] E2E upgrade path: workspace → upgrade form → Slack notif received → manual provisioning → credentials email

---

## 10. Claude Code implementációs csomag ⏳ PENDING — v4 outcome

Végrehajtási sorrend (placeholder):

| Nap | Feladat | Track |
|-----|---------|-------|
| 1 | Repo scaffold + 5 csproj + systemd unit + nginx vhost | Setup |
| 2 | ⏳ | A-Domain |
| ... | ⏳ | ... |

⏳ Full breakdown: arch-planner v4 output.

---

## 11. Implementation Context (Claude Code agent) ⏳ PENDING

### Shell discovery commands (első lépések Claude Code-ban)

```bash
# Repository discovery
cd /home/gabor/dev
git clone git@github.com:gaborszabo/spaceos-freetier-api.git
cd spaceos-freetier-api

# Solution structure check (empty repo → scaffold from docs)
ls -la
cat CLAUDE.md 2>/dev/null || echo "Scaffold from this doc"

# Approved package list
cat /path/to/SpaceOS_Master_Prompt.md | grep -A 3 "Approved Packages"

# Related repos (read-only reference)
ls ~/dev/spaceos-modules-cutting       # Cutting for PanelReservation pattern
ls ~/dev/spaceos-kernel                # Kernel for TenantSessionInterceptor pattern
ls ~/dev/SpaceOS.Nesting.Algorithms    # Nesting NuGet — already DONE

# Check Redis availability on dev machine
redis-cli ping 2>/dev/null || sudo apt install redis-server

# Check Turnstile test keys
echo "Turnstile dummy site key: 1x00000000000000000000AA (always passes)"
echo "Turnstile dummy secret: 1x0000000000000000000000000000000AA"

# Brevo sandbox API
echo "Brevo: use BREVO_API_KEY env var, sender domain verify required before prod"
```

### First implementation step after arch doc v4 APPROVED

```bash
# 1. Solution scaffold
dotnet new sln -n SpaceOS.FreeTier
dotnet new classlib -n SpaceOS.FreeTier.Domain -f net8.0
dotnet new classlib -n SpaceOS.FreeTier.Application -f net8.0
dotnet new classlib -n SpaceOS.FreeTier.Infrastructure -f net8.0
dotnet new webapi -n SpaceOS.FreeTier.Api -f net8.0
dotnet new xunit -n SpaceOS.FreeTier.Tests -f net8.0
dotnet sln add **/*.csproj

# 2. Domain project — zero external deps (enforce by convention)
cd SpaceOS.FreeTier.Domain
dotnet add package Ardalis.Result

# 3. Application project
cd ../SpaceOS.FreeTier.Application
dotnet add reference ../SpaceOS.FreeTier.Domain/SpaceOS.FreeTier.Domain.csproj
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package Ardalis.Specification

# 4. Infrastructure project
cd ../SpaceOS.FreeTier.Infrastructure
dotnet add reference ../SpaceOS.FreeTier.Application/SpaceOS.FreeTier.Application.csproj
dotnet add package Microsoft.EntityFrameworkCore -v 8.0.*
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL -v 8.0.*
dotnet add package StackExchange.Redis                 # FT-2
dotnet add package QRCoder                             # label strategy
dotnet add package QuestPDF
```

---

## 12. Risks & Open Questions

⏳ **PENDING** — populate during review pipeline.

Előzetes kockázati területek (arch-planner explicit átnézi):

| Terület | Kockázat |
|---|---|
| Magic link email deliverability | Gmail/Outlook spam filter, bounce handling |
| Redis single instance | SPOF — session loss acceptable, rate limit bypass if Redis down |
| Turnstile bot false-positives | Legitimate user kizárás |
| GDPR data residency | EU vs. non-EU user data — csak EU (magic link email domain filter?) |
| Upgrade workspace export stability | v1 schema → v2 schema migration |
| DACH expansion localization | EN v1.5 first, DACH v2 |

---

## 13. Review Pipeline Status

| Stage | Status | Assignee | Expected delta |
|---|---|---|---|
| v1 draft skeleton | ✅ READY (ez a dokumentum) | Architect | — |
| FT-1..FT-5 kickoff decisions | ⏳ Gábor approval | Gábor | 0 |
| v2 database review | ⏳ QUEUED | `/sub-database-designer` + `/sub-database-schema-designer` | ~0.5 nap design delta |
| v3 security review | ⏳ QUEUED | `/sub-senior-security` | ~1 nap design delta |
| v4 backend review | ⏳ QUEUED | `/sub-senior-backend` | ~0.5 nap design delta |
| Claude Code implementation | ⏳ QUEUED | Dev team | ~17.5 nap (ld. §2 Delta) |

---

## 14. Appendix — Cross-references

| Forrás | Mit ad ehhez |
|---|---|
| `SpaceOS_Growth_Strategy_v1.md` §5 | FreeTier paradigmaváltás vázolása, PQ1..PQ8 + FT1..FT8 döntések |
| `SpaceOS_Session_D_Kickoff_Decisions_FT1_FT5.md` | FT-1..FT-5 formal decision record (ez a doc 2.1 szekciója idézi) |
| `SpaceOS_Modules_Contracts_Architecture_v4_2.md` (1.3.0) | `SourceChannel.FreeTier`, `AnonymousSheetRequest`, `SubmitAnonymousSheetAsync` DIM |
| `SpaceOS.Nesting.Algorithms 1.0.0` | FFDH + Guillotine + MaxRects placeholder — standalone NuGet, FreeTier fogyasztó |
| `spaceos-modules-cutting/Application/Adapters/IInventoryReservationAdapter.cs` | Reservation pattern minta (CUTTING-038) |
| `spaceos-kernel/Infrastructure/Security/TenantSessionInterceptor.cs` | Interceptor pattern — user-scoped adaptáláshoz |
| `Codebase_Status_20260420.md` | Soft Launch LIVE státusz (infra deploy target) |

---

## 15. Sign-off sor (végállapot)

```
[ ] v1 draft       → Architect (2026-04-20)
[ ] v2 database    → /sub-database-designer
[ ] v3 security    → /sub-senior-security
[ ] v4 backend     → /sub-senior-backend
[ ] Gábor APPROVED → Implementation READY
[ ] Claude Code    → CUTTING-like FREETIER-001..NNN task breakdown
```
