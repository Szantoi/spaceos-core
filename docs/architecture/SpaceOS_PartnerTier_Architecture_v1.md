# SpaceOS — PartnerTier MVP Architecture
## B2B2C Channel Partner Network — iframe embed + API key auth + Commission tracking

> **Verzió:** v1.0 — 2026-04-27
> **Státusz:** TERVDOK — implementáció előtt review és döntés szükséges
> **Forrás:** Growth Strategy §6 (Session E) + FreeTier codebase audit
> **Prerekvizit:** FreeTier LIVE (`freetier.joinerytech.hu`) · Doorstar Soft Launch STABIL (Q3 2026)
> **Tervezett élő:** Q4 2026

---

## 1. Kontextus és scope

### 1.1 Hol tartunk

| Pillér | Státusz |
|--------|---------|
| Manufacturer tenant (Doorstar) | ✅ LIVE — paid SaaS |
| FreeTier anonymous workspace | ✅ LIVE — 176 API teszt, `freetier.joinerytech.hu` |
| **PartnerTier B2B2C embed** | 🔴 Tervezett — ez a spec |

### 1.2 A PartnerTier mit ad hozzá

A FreeTier a SpaceOS brand alatt konvertál prospekteket. A PartnerTier **partner brand alatt** végzi ugyanezt — de a lead (és az adat) a partneré, nem a SpaceOS-é. A SpaceOS branded footer + commission-t kap ha az end customer tenantté upgrade-el.

```
FreeTier flow (ma):
  Prospect → eszkozok.joinerytech.hu → kalkulátor → email → SpaceOS lead

PartnerTier flow (Q4):
  Kuncsaft → [szabászat.hu/kalkulator] → kalkulátor (partner brand) → email → Partner lead
                                                                              → SpaceOS commission (ha upgrade)
```

### 1.3 Mit NEM fed ez a tervdok

| Nem-scope | Miért |
|-----------|-------|
| FreeTier kód módosítása | FreeTier LIVE és stabil — PartnerTier külön service |
| Tier-es billing rendszer (Pro/Enterprise partner díjak) | Phase 2 — manuális számlázás v1-ben |
| Commission kifizetési mechanizmus (stripe payout) | Phase 2 — v1 csak tracking |
| DACH expansion legal | v3+ |
| Penetration test végrehajtása | Koordinációs feladat (hackerlab.hu), nem impl |

---

## 2. Architektúra döntések

### D1: Külön service vs. FreeTier bővítés

**Döntés: KÜLÖN service (`spaceos-partner-api`)**

| Szempont | FreeTier bővítés | Külön service |
|---------|------------------|---------------|
| FreeTier stabilitás | ❌ Kockáztatja a LIVE service-t | ✅ Izolált |
| Partner auth (API key) | ❌ Idegen a FreeTier auth modelljétől (magic link) | ✅ Natív |
| Rate limiting | ❌ FreeTier limit-ek összekeverednek | ✅ Per-partner Redis bucket |
| Deploy | ❌ FreeTier rollback PartnerTier-t is viszi | ✅ Független |
| DB séma | ❌ FreeTier séma szennyeződne partner táblákkal | ✅ Saját séma |

**Közös:** `SpaceOS.Nesting.Algorithms` NuGet v1.1.0 mindkét service-ben. Nincsen code sharing egyéb — csak a NuGet.

### D2: iframe vs. web-component vs. subdomain

**Döntés: iframe primary + {partner}.joinerytech.hu subdomain secondary**

| Integráció | Előny | Hátrány | Döntés |
|------------|-------|---------|--------|
| iframe | Erős izolálás, XSS barrier, egyszerű embed | postMessage protocol kell | ✅ PRIMARY |
| Web component (`<spaceos-calc>`) | Natív DOM, könnyebb styling | XSS kockázat, partner CSS szivárog | ❌ Kerülendő (Growth Strategy M6) |
| Subdomain `{partner}.joinerytech.hu` | Full white-label, SEO-friendly | DNS wildcard, cert management | ✅ SECONDARY (opt-in, Pro+ partnereknek) |

**Embed snippet (partner oldalán):**
```html
<iframe
  src="https://partner.joinerytech.hu/embed/{partnerToken}"
  width="100%" height="700"
  sandbox="allow-scripts allow-same-origin allow-forms"
  allow="clipboard-write"
  loading="lazy">
</iframe>
```

`{partnerToken}` = signed JWT (HS256, `partner_id` + `exp` + `allowed_origin`) — nem az API key!

### D3: API key tárolása

**Döntés: SHA-256 hash a DB-ben, plaintext csak létrehozáskor (one-time display)**

```
Létrehozás:
  1. random 32-byte → hex string (64 char) = raw key
  2. `SHAKE256(raw_key)` → 64-byte hash → `sk_live_<base64url(hash)[:32]>` prefix-szel
  3. DB: `PartnerApiKeys.KeyHash` = full hash (nincs prefix a DB-ben)
  4. UI: "Másold le, nem jelenik meg újra!" one-time display

Ellenőrzés:
  1. `X-Partner-Api-Key: sk_live_<...>` header-ből strip prefix → hash → compare DB
  2. `FixedTimeEquals` (timing-safe)
```

### D4: End customer adat-modell

**Döntés: Partner-scoped, nem Kernel tenant**

Az end customer NEM SpaceOS felhasználó. Csak email (PII) kerül tárolásra, `pgcrypto` AES-256-gel titkosítva. Az adatok a Partner tulajdona — GDPR törlési kérésre 30 napos soft-delete + titkosítási kulcs eldobása.

### D5: Commission tracking — Phase 1 scope

**Döntés: Simple attribution log, kifizetés manuálisan**

V1-ban a commission tracking csak egy `PartnerUpgradeAttributions` tábla:
- Ha egy end customer email megegyezik egy Kernel tenant `Email`-jével → automatikus attribution
- Vagy: ha a FreeTier upgrade flow-ban `partner_id` token jelen volt → attribution
- Kifizetés: havi manuális review (CSV export), Phase 2-ben Stripe Connect

---

## 3. Partner data model

### 3.1 DB séma — `spaceos_partner`

```sql
-- Új séma, saját schema owner
CREATE SCHEMA IF NOT EXISTS spaceos_partner;
ALTER SCHEMA spaceos_partner OWNER TO spaceos_schema_owner;

-- RLS minden táblán
-- spaceos_partner_app role: SELECT/INSERT/UPDATE a partner saját adataira

CREATE TABLE spaceos_partner."Partners" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "Slug"            varchar(64) UNIQUE NOT NULL,             -- URL-safe, pl. "lapszabasz-bt"
    "DisplayName"     varchar(200) NOT NULL,
    "ContactEmail"    varchar(320) NOT NULL,
    "Status"          smallint    NOT NULL DEFAULT 1,          -- 1=PendingReview, 2=Active, 3=Suspended, 4=Terminated
    "Tier"            smallint    NOT NULL DEFAULT 1,          -- 1=Free, 2=Pro, 3=Enterprise
    "AllowedOrigins"  text[]      NOT NULL DEFAULT '{}',       -- CORS allowlist
    "AllowedSubdomain" varchar(64) NULL,                       -- {slug}.joinerytech.hu ha Pro+
    "BrandingJson"    jsonb       NULL,                        -- {logo_url, primary_color, custom_footer}
    "CommissionRate"  decimal(5,4) NOT NULL DEFAULT 0.10,      -- 10% default (§6.3 M9)
    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"       timestamptz NOT NULL DEFAULT now(),
    "TerminatedAt"    timestamptz NULL
);

CREATE TABLE spaceos_partner."PartnerApiKeys" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "PartnerId"       uuid        NOT NULL REFERENCES spaceos_partner."Partners"("Id"),
    "KeyHash"         bytea       NOT NULL UNIQUE,             -- SHA-256 of raw key (no prefix stored)
    "DisplayHint"     varchar(16) NOT NULL,                   -- last 4 chars of raw key, for UI
    "Label"           varchar(100) NULL,                       -- "Production key", "Test key"
    "Status"          smallint    NOT NULL DEFAULT 1,          -- 1=Active, 2=Revoked
    "RateLimitTier"   smallint    NOT NULL DEFAULT 1,          -- mirrors Partner.Tier: 1=5/min, 2=50/min, 3=custom
    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
    "RevokedAt"       timestamptz NULL,
    "LastUsedAt"      timestamptz NULL
);

CREATE TABLE spaceos_partner."PartnerEmbedSessions" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "PartnerId"       uuid        NOT NULL REFERENCES spaceos_partner."Partners"("Id"),
    "SessionToken"    varchar(512) NOT NULL UNIQUE,           -- signed JWT (embed token)
    "AllowedOrigin"   varchar(320) NOT NULL,
    "NestingCount"    integer     NOT NULL DEFAULT 0,
    "ExpiresAt"       timestamptz NOT NULL,
    "CreatedAt"       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE spaceos_partner."PartnerLeads" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "PartnerId"       uuid        NOT NULL REFERENCES spaceos_partner."Partners"("Id"),
    "EmailEncrypted"  bytea       NOT NULL,                   -- pgcrypto AES-256, key=Partner.EncryptionKeyRef
    "EncryptionKeyRef" varchar(64) NOT NULL,                  -- key rotation ref
    "NestingResultJson" jsonb     NOT NULL,                   -- snapshot (no PII)
    "SourceChannel"   smallint    NOT NULL DEFAULT 2,         -- 2=Partner embed
    "ConsentTimestamp" timestamptz NOT NULL,
    "ConsentText"     varchar(1000) NOT NULL,
    "DeletedAt"       timestamptz NULL,                       -- soft delete (GDPR Art.17, 30-day grace)
    "CreatedAt"       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE spaceos_partner."PartnerUpgradeAttributions" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "PartnerId"       uuid        NOT NULL REFERENCES spaceos_partner."Partners"("Id"),
    "LeadId"          uuid        NULL REFERENCES spaceos_partner."PartnerLeads"("Id"),
    "KernelTenantId"  uuid        NOT NULL,                   -- SpaceOS tenant aki upgrade-elt
    "AttributedAt"    timestamptz NOT NULL DEFAULT now(),
    "CommissionRate"  decimal(5,4) NOT NULL,
    "CommissionWindowEnd" timestamptz NOT NULL,               -- AttributedAt + 12 months (§6.3 M9)
    "PaidOut"         boolean     NOT NULL DEFAULT false
);

CREATE TABLE spaceos_partner."PartnerAuditLog" (
    "Id"              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    "PartnerId"       uuid        NOT NULL REFERENCES spaceos_partner."Partners"("Id"),
    "Action"          varchar(100) NOT NULL,                  -- "KEY_CREATED", "LEAD_ACCESSED", etc.
    "ActorId"         varchar(200) NOT NULL,                  -- partner API key hint OR SpaceOS admin ID
    "TargetId"        varchar(200) NULL,
    "IpAddress"       inet        NOT NULL,
    "OccurredAt"      timestamptz NOT NULL DEFAULT now()
);
-- Immutable: no UPDATE/DELETE trigger (PartnerAuditLog is append-only)
```

### 3.2 RLS policy séma

```sql
-- Per-partner isolation: API key middleware sets GUC
ALTER TABLE spaceos_partner."PartnerLeads" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_partner."PartnerLeads" FORCE ROW LEVEL SECURITY;

CREATE POLICY partner_leads_isolation ON spaceos_partner."PartnerLeads"
  FOR ALL TO spaceos_partner_app
  USING (
    "PartnerId" = current_setting('app.current_partner_id', true)::uuid
    AND "DeletedAt" IS NULL
  );
```

**GUC beállítás** (middleware-ből, minden request elején):
```csharp
await conn.ExecuteAsync(
    "SELECT set_config('app.current_partner_id', @id, true)",
    new { id = partnerId.ToString() });
```

### 3.3 ERD

```
Partners ||--o{ PartnerApiKeys : has
Partners ||--o{ PartnerEmbedSessions : issues
Partners ||--o{ PartnerLeads : owns
Partners ||--o{ PartnerUpgradeAttributions : earns
Partners ||--o{ PartnerAuditLog : tracked_by
PartnerLeads }o--|| PartnerUpgradeAttributions : converts_to
```

---

## 4. API key autentikáció

### 4.1 Middleware stack (kérés-feldolgozás sorrendje)

```
Request
  │
  ▼ [1] TLS termination (Nginx)
  ▼ [2] Nginx rate limit (ip-szintű DOS protection, 200 req/min hard cap)
  ▼ [3] PartnerApiKeyMiddleware
          ├─ X-Partner-Api-Key header parse
          ├─ SHA-256 hash → DB lookup (PartnerApiKeys + Partner join)
          ├─ Status == Active check
          ├─ FixedTimeEquals comparison
          ├─ PartnerContext inject (HttpContext.Items)
          ├─ GUC set_config('app.current_partner_id', ...)
          └─ LastUsedAt async update (fire-and-forget, audit)
  ▼ [4] PartnerRateLimitMiddleware
          ├─ scope="nest:partner:{partnerId}"
          ├─ Redis bucket (5-min window)
          └─ Tier-alapú: Free=5/min, Pro=50/min, Enterprise=custom
  ▼ [5] Endpoint handler
```

### 4.2 API kulcs rotáció

```
1. Partner: POST /api/partner/keys → új kulcs (max 3 aktív / partner)
2. Régi kulcs: továbbra is Active
3. Partner leváltja az alkalmazásban (30 napos migration window)
4. Partner: DELETE /api/partner/keys/{id} → Revoked + RevokedAt = now()
5. Audit log: KEY_ROTATED event

Automatikus revokáció: ha LastUsedAt > 365 nap → auto-revoke + email értesítés
```

---

## 5. iframe embed + postMessage protokoll

### 5.1 Embed token flow

```
Partner backend → POST /api/partner/embed/token
  Request: { allowedOrigin: "https://lapszabasz.hu", ttlMinutes: 120 }
  Auth: X-Partner-Api-Key
  Response: { embedToken: "eyJ...", embedUrl: "https://partner.joinerytech.hu/embed/eyJ..." }

Partner frontend → <iframe src="https://partner.joinerytech.hu/embed/{embedToken}">

PartnerTier API:
  - JWT decode: partner_id + allowed_origin + exp
  - CSP header: frame-ancestors 'self' https://lapszabasz.hu
  - Session létrehozás: PartnerEmbedSessions INSERT
```

**Embed token (HS256 signed, 1-2 óra TTL):**
```json
{
  "sub": "partner:{partnerId}",
  "origin": "https://lapszabasz.hu",
  "tier": 2,
  "iat": 1745000000,
  "exp": 1745007200
}
```

### 5.2 postMessage protokoll (iframe ↔ parent)

**Schema-validált, origin-ellenőrzött minden oldalon:**

```typescript
// Partner oldalán (embed parent)
window.addEventListener('message', (event) => {
  if (event.origin !== 'https://partner.joinerytech.hu') return;
  // event.data typed: PartnerEmbedMessage
});

// PartnerTier iframe-ből kiküldött events:
type PartnerEmbedMessage =
  | { type: 'READY'; version: '1.0' }
  | { type: 'NESTING_COMPLETE'; result: NestingResultSummary; sessionId: string }
  | { type: 'LEAD_CAPTURED'; sessionId: string }  // email elküldve, NEM tartalmaz PII-t!
  | { type: 'UPGRADE_INTENT'; sessionId: string } // "Akarok fiókot nyitni" kattintás
  | { type: 'ERROR'; code: string; message: string }

// Partner → iframe (opcionális parancsok)
iframe.contentWindow.postMessage(
  { type: 'SET_CONFIG'; locale: 'hu', theme: 'dark' },
  'https://partner.joinerytech.hu'
);
```

**Biztonsági szabályok:**
- Az iframe SOHA nem küld PII-t postMessage-ben (`LEAD_CAPTURED` csak `sessionId`-t)
- Partner oldal SOHA nem küldhet nesting adatot iframe-be (only config)
- Minden message-ben `type` validálva JSON Schema-val (Ajv, iframe oldalán)

---

## 6. Nesting integráció

### 6.1 SpaceOS.Nesting.Algorithms v1.1.0 re-use

A PartnerTier a **FreeTier-rel azonos NuGet-et** használja — nincs fork, nincs duplication.

```csharp
// SpaceOS.PartnerTier.Infrastructure/Nesting/PartnerNestingService.cs
public sealed class PartnerNestingService(
    NestingStrategyFactory factory,
    IPartnerRateLimitService rateLimit,
    IPartnerAuditLogger audit)
{
    public async Task<NestingResultDto> ComputeAsync(
        PartnerContext partner,
        NestingInput input,
        CancellationToken ct)
    {
        // Tier-alapú algoritmus: Free=FFDH, Pro=Guillotine, Enterprise=MaxRects(placeholder)
        var algo = partner.Tier switch {
            PartnerTier.Free       => "FFDH",
            PartnerTier.Pro        => "Guillotine",
            PartnerTier.Enterprise => "Guillotine", // MaxRects Phase 2
            _                      => "FFDH"
        };

        var strategy = factory.GetStrategy(algo);
        // ... same mapping as FreeTier NestingEngineService ...
    }
}
```

### 6.2 Rate limit különbségek a FreeTier-hez képest

| Dimenzió | FreeTier | PartnerTier |
|----------|----------|-------------|
| Scope | IP-alapú | Per-partner (Redis key: `rl:nest:partner:{partnerId}`) |
| Limit Free | 3 nesting/nap | 5 nesting/min (tier: Free) |
| Limit Pro | — | 50 nesting/min |
| Limit Enterprise | — | Custom (DB-ből olvasva) |
| Fail-closed | ✅ | ✅ |

---

## 7. Commission tracking

### 7.1 Attribution flow

```
End customer email capture (PartnerLeads INSERT)
        │
        │ aszinkron (1-5 perc)
        ▼
AttributionWorker (BackgroundService, 5 perc polling)
        │
        ├─ Kernel API: GET /internal/tenants?email={hash(email)}
        │  (email hash-sel kérdez, PII nem kerül át)
        │
        ├─ Ha match → PartnerUpgradeAttributions INSERT
        │  (commission window = AttributedAt + 12 hónap)
        │
        └─ Domain event: PartnerUpgradeAttributed (outbox)
```

**Megjegyzés:** Kernel email-hash lookup-hoz a Kernel-ben egy új internal endpoint kell (egyszerű, ≤0.5 nap):
```
GET /internal/tenants/by-email-hash?hash={sha256(lower(email))}
→ 200: { tenantId }  |  404
```

### 7.2 Havi report (Phase 1)

```
GET /api/admin/partners/{id}/attribution-report?year=2026&month=11
→ CSV: partner_id, attributed_at, commission_window_end, paid_out
```

---

## 8. Partner portal UI

**Stack:** React 18, Vite, Tailwind — önálló deploy (nem a design-portal Turborepo-ban, önálló Vercel/nginx)

### 8.1 Partner self-service portal

| Funkció | Route | Leírás |
|---------|-------|--------|
| Dashboard | `/dashboard` | Leads/hó chart, conversion rate, commission előnézet |
| API kulcsok | `/keys` | Lista, create, revoke, last-used |
| Branding | `/branding` | Logo upload, primary color picker, custom footer |
| Leads | `/leads` | Lead lista (email masked: `l***@domain.hu`), export CSV |
| Embed kód | `/embed` | Copy-paste snippet, tesztelő iframe |
| Subdomain | `/subdomain` | Kérelmezés (Pro+), DNS guide |

**Auth:** Magic link (partner `ContactEmail`-re) — NEM API key! Az API key gépi hozzáférés, a portal ember által használt.

### 8.2 SpaceOS admin portal (belső)

| Funkció | Route | Leírás |
|---------|-------|--------|
| Alkalmazások | `/admin/applications` | PendingReview lista, review workflow |
| Partner lista | `/admin/partners` | All partners, tier, status, usage |
| Attribution | `/admin/attributions` | Commission report, pay-out mark |
| Suspension | `/admin/partners/{id}/suspend` | Abuse response |

---

## 9. Security hardening

### 9.1 API key security

| Réteg | Implementáció |
|-------|--------------|
| Tárolás | SHA-256 hash, `bytea` — nem varchar (timing side-channel minimalizálás) |
| Összehasonlítás | `CryptographicOperations.FixedTimeEquals` |
| Transport | HTTPS only · `Strict-Transport-Security: max-age=31536000` |
| Rotation | Max 3 aktív / partner · auto-revoke 365 nap inaktivitás után |
| Audit | Minden key usage `PartnerAuditLog`-ba (async, fire-and-forget) |

### 9.2 iframe security

| Védelmi réteg | Implementáció |
|----------------|--------------|
| `Content-Security-Policy` | `frame-ancestors 'self' {allowedOrigin}` — per-request dinamikus generálás |
| `X-Frame-Options` | `ALLOW-FROM {allowedOrigin}` (régebbi böngészőkhöz) |
| `sandbox` attribútum | `allow-scripts allow-same-origin allow-forms` — sem `allow-top-navigation` |
| postMessage origin check | `event.origin === 'https://partner.joinerytech.hu'` — iframe oldalon |
| Embed token | Signed JWT, rövid TTL (max 2 óra), `allowed_origin` claim-mel |

### 9.3 End customer PII

| Réteg | Implementáció |
|-------|--------------|
| Email tárolása | `pgcrypto.pgp_sym_encrypt(email, key)` — per-partner encryption key |
| Key rotation | `EncryptionKeyRef` pointer, annually rotated |
| GDPR törlés | Soft delete + 30 nap grace → encryption key törlése (crypto-shredding) |
| Exportálás | Csak a partner exportálhatja saját lead-jeit (RLS enforced) |
| Lead adat átadás | SOHA nem kerül át SpaceOS más moduljába (need-to-know RBAC) |

### 9.4 Rate limiting rétegek

```
L1: Nginx → 200 req/min per IP (DOS protection)
L2: PartnerRateLimitMiddleware → tier-alapú per-partner
L3: Embed token TTL → session-szintű nesting limit
L4: SemaphoreSlim(20) → compute concurrency guard (FreeTier-rel azonos minta)
L5: API key auto-revoke → 365 nap inaktivitás
```

### 9.5 Fraud detection (alapszint)

```
Rule 1: >100 lead ugyanabból az IP-ből 1 óra alatt → alert + auto-rate-limit
Rule 2: Partner suspicion score (leads/nesting ratio < 0.1) → review flag
Rule 3: Ugyanaz az email 5 különböző partnernél → cross-partner abuse alert
```

**Implementáció:** Redis Sorted Set alapú sliding window counter, 5-perces granularitás.

---

## 10. Deployment architektúra

### 10.1 Új service: `spaceos-partner-api`

```
VPS: /opt/spaceos/spaceos-partner-api/
  src/
    SpaceOS.PartnerTier.Domain/
    SpaceOS.PartnerTier.Application/
    SpaceOS.PartnerTier.Infrastructure/
    SpaceOS.PartnerTier.Api/          ← Minimal API, :5006
  tests/
    SpaceOS.PartnerTier.Tests/
```

**Port:** `5006` (FreeTier: 5003, Orchestrator: 3000, Kernel: 5000)

**Systemd service:**
```ini
[Unit]
Description=SpaceOS PartnerTier API
After=network.target postgresql.service redis.service

[Service]
WorkingDirectory=/opt/spaceos/spaceos-partner-api/publish
ExecStart=/opt/spaceos/spaceos-partner-api/publish/SpaceOS.PartnerTier.Api
User=spaceos
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=always
```

**Nginx route:**
```nginx
# API: api.joinerytech.hu/api/partner → :5006
# Embed: partner.joinerytech.hu/embed → :5006
# Partner portal: partnerportal.joinerytech.hu → Vercel/nginx static
```

### 10.2 DB kapcsolódás

- **Külön schema** (`spaceos_partner`) a meglévő PostgreSQL instance-on
- Saját role (`spaceos_partner_app`, `spaceos_schema_owner`)
- A Kernel DB-vel **nincs shared transaction** (email hash lookup = HTTP call az internal Kernel endpoint-ra)

### 10.3 Redis

- Meglévő Redis instance, új prefix: `partner:` (FreeTier prefix: `rl:`)
- Redis ACL: `partner-service` user, `~partner:*` key pattern

---

## 11. Fázisolás

### Phase 1 (MVP — ez a spec, ~22–25 nap impl)

**Scope:** Alap partner embed + lead capture + API kulcs auth + SpaceOS admin review

| Képesség | Status |
|---------|--------|
| Partner application + manuális review | ✅ |
| API key létrehozás, rotáció, revokáció | ✅ |
| iframe embed token + embed endpoint | ✅ |
| Nesting engine (FFDH/Guillotine) partner-branded | ✅ |
| End customer email capture + PII encryption | ✅ |
| Upgrade attribution (email hash match) | ✅ |
| Partner self-service portal (dashboard, keys, branding) | ✅ |
| Redis rate limiting per-partner | ✅ |
| CORS allowlist, dinamikus CSP | ✅ |
| Penetration test (external) | ✅ koordináció |
| Commission tracking (manuális report) | ✅ |
| Subdomain support | ❌ Phase 2 |
| Tier-es billing (Stripe) | ❌ Phase 2 |
| Fraud detection (alapszint) | ✅ |

### Phase 2 (~10–12 nap, Q1 2027)

- `{partner}.joinerytech.hu` wildcard subdomain + cert automation (Let's Encrypt)
- Stripe Connect partner billing (Free/Pro/Enterprise havi díj)
- Automatikus commission kifizetés
- MaxRects nesting (Enterprise tier)
- Partner analytics dashboard (Metabase/saját)

### Phase 3 (~8 nap, Q2 2027)

- DACH expansion: DE/AT legal review, adatvédelmi DPA template DE-re
- Multi-language embed (HU/EN/DE)
- Partner referral program (partner → partner recruitment)

---

## 12. Implementációs sorrend (track-okra bontva)

A Phase 1 ~22–25 napra van skálázva, **3 párhuzamos track**.

| Nap | Track A: Domain + Application | Track B: Infrastructure + DB | Track C: API + Embed + Portal |
|-----|-------------------------------|------------------------------|-------------------------------|
| 1 | Partner, PartnerApiKey aggregates (FSM: PendingReview→Active→Suspended) | DB séma + 3 migration (M-0001: schema, M-0002: tables, M-0003: RLS) | Nginx + systemd scaffold + port 5006 |
| 2 | PartnerLead aggregate (PII-less domain model, EncryptionKeyRef) | EF Core configurations, DbContext, RLS GUC middleware | Embed token endpoint (JWT sign/verify) |
| 3 | API key domain logic (SHA-256 VO, rotation, max-3 rule) | `PartnerApiKeyMiddleware` (hash lookup, FixedTimeEquals) | `GET /embed/{token}` → branded iframe shell |
| 4 | Application: CreatePartner, ActivatePartner, SuspendPartner commands | `PartnerRateLimitMiddleware` (Redis tier-alapú) | postMessage protocol (READY, NESTING_COMPLETE, LEAD_CAPTURED) |
| 5 | Application: CreateApiKey, RevokeApiKey commands | `PartnerNestingService` (NestingStrategyFactory re-use) | Nesting endpoint (`POST /embed/nest`) |
| 6 | Application: CaptureLeadCommand + PII encryption service | `pgcrypto` AES-256 email encryption infrastructure | Lead capture form (email + GDPR consent) |
| 7 | Application: GenerateEmbedToken, RequestSubdomain stubs | `PartnerAuditLogger` (append-only, immutable) | Partner portal: login (magic link) |
| 8 | Attribution: AttributionWorker BackgroundService | Kernel internal email-hash lookup client | Partner portal: Dashboard (leads chart) |
| 9 | Commission report query handler | Redis fraud detection sliding window | Partner portal: API key management |
| 10 | GDPR: soft-delete + crypto-shredding (encryption key törlés) | `pgcrypto` key rotation infrastructure | Partner portal: Branding config |
| 11 | Partner self-service: upgrade intent flow stub | Dynamic CSP header generation | Partner portal: Embed code snippet + tesztelő |
| 12 | SpaceOS admin: ApplicationReviewCommand | Admin portal: application review UI | Partner portal: Leads lista (masked email) |
| 13 | Fraud detection rules (Redis Sorted Set) | Fraud detection Redis implementation | Admin portal: partner management |
| 14 | Unit tesztek: domain + application (≥80) | Integration tesztek: DB + RLS + rate limit (≥40) | API tesztek: embed flow + auth (≥30) |
| 15–17 | Penetration test koordináció + finding fixek | Security hardening tesztek (API key timing, XSS CSP) | E2E: partner registration → embed → lead capture |
| 18–19 | OpenAPI snapshot (3 fájl: partner-api, embed-api, admin-api) | Load test: 100 concurrent embed sessions | Performance: nesting <500ms P99 |
| 20–22 | DoD checklist verifikáció | Deployment runbook írás | Smoke test (staging → production) |

### Inbox bontás (4 db a PartnerTier terminálnak)

**Inbox #1: Alapinfra + Domain + Auth (Track A+B Nap 1–3)**
- `spaceos-partner-api` repo scaffold (sln, 4 csproj, CLAUDE.md, CI)
- DB séma + 3 migration (RLS, pgcrypto, GUC)
- Partner + PartnerApiKey aggregate (FSM, SHA-256 VO)
- `PartnerApiKeyMiddleware` + `PartnerRateLimitMiddleware`
- DoD: `dotnet build` 0 warning, middleware integration teszt pass

**Inbox #2: Nesting embed + Lead capture (Track A+B+C Nap 4–10)**
- Embed token flow (JWT sign/verify, iframe shell)
- postMessage protokoll implementáció
- `PartnerNestingService` (NuGet re-use)
- Lead aggregate + PII encryption (`pgcrypto`)
- `PartnerAuditLogger` (append-only)
- DoD: E2E embed flow (partner site → iframe → nesting → lead capture) pass

**Inbox #3: Commission + Partner portal + Admin (Track A+B+C Nap 8–13)**
- `AttributionWorker` BackgroundService + Kernel email-hash lookup
- Commission report query
- GDPR soft-delete + crypto-shredding
- Partner self-service portal (React, magic link auth)
- SpaceOS admin portal (application review workflow)
- DoD: Attribution integration teszt pass, portal deployható

**Inbox #4: Security hardening + Tests + Release (Nap 14–22)**
- Fraud detection rules (Redis Sorted Set)
- Pen-test finding fixek
- ≥80 unit + ≥40 integration + ≥30 API teszt
- 3 OpenAPI snapshot committed
- DoD checklist §9 verifikáció
- Production deploy + smoke test

---

## 13. Effort validálás

**Verdikt: 27 nap (Growth Strategy §8.2) REÁLIS, de szoros ✅**

| Komponens | Growth Strategy becslés | Architect validálás |
|-----------|------------------------|---------------------|
| Domain + Application | ~7 | ~7.0 |
| Infrastructure + DB | ~5 | ~5.0 |
| API + Embed + Portal | ~8 | ~8.0 |
| Security hardening | ~5 | ~5.0 |
| Pen-test koordináció | ~2 | ~2.0 |
| **Total** | **~27** | **~27.0** |

**Kockázati tényezők:**
| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|--------------|-------|-----------|
| Pen-test finding-ek > vártnál | Közepes | +3–5 nap | Belső pre-review (OWASP checklist) a külső audit előtt |
| Kernel internal email-hash endpoint halasztás | Közepes | +1 nap | Inbox a Kernel terminálnak párhuzamosan |
| Partner portal React komplexitás | Alacsony | +2 nap | Minimal UI elvű design (no framework components, plain Tailwind) |
| GDPR DPA legal review | Magas | Külső blokkol | Ügyvéd bevonás 4 héttel korábban |

---

## 14. Cross-module dependency

| Dependency | Típus | Blokkoló? |
|-----------|-------|-----------|
| **Kernel: internal email-hash lookup endpoint** | Új endpoint, ≤0.5 nap | **IGEN** — AttributionWorker ezt hívja |
| `SpaceOS.Nesting.Algorithms` v1.1.0 | NuGet, LIVE | **NEM** |
| PostgreSQL meglévő instance | Új schema | **NEM** |
| Redis meglévő instance | Új prefix | **NEM** |
| FreeTier kód | **NEM érintett** | **NEM** |

---

*SpaceOS · PartnerTier Architecture v1.0 · 2026-04-27*
*Státusz: TERVDOK — pending Gábor döntés az iframe/subdomain D2 döntésnél, commission Phase 1 scope-ján*
