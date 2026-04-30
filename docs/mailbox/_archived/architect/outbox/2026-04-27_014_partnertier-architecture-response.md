---
id: MSG-ARCH-014-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-011
created: 2026-04-27
---

## Összefoglalás

A PartnerTier MVP Architecture tervdok elkészült: `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md`

A Growth Strategy §6 mind a 11 pontját lefedi. Egy kisebb Kernel dependency azonosítva (internal email-hash lookup endpoint, ≤0.5 nap), minden más a PartnerTier service-en belül marad.

---

## 1. Kulcsdöntések (validálásra vár)

### D1: Külön service (`spaceos-partner-api`) — nem FreeTier bővítés

**Indok:** FreeTier LIVE és stabil — partner auth modellje (API key) idegen a FreeTier magic link modelljétől. A két service **csak a `SpaceOS.Nesting.Algorithms` NuGet-en** osztozik (v1.1.0, LIVE).

### D2: iframe primary + subdomain secondary — NEM web-component

**Indok:** Growth Strategy M6 explicit döntése (widget = XSS kockázat). Iframe erős izolálást ad, postMessage schema-validált protokollal. `{partner}.joinerytech.hu` wildcard subdomain Pro+ partnereknek — Phase 2-ben.

### D3: Commission Phase 1 = tracking-only, kifizetés manuális

**Indok:** Stripe Connect complexity nem indokolt az első 5–20 partnernél. Havi CSV export + manuális review elegendő. Phase 2-ben automatizálható.

**Gábor döntést igénylő pont:** A commission rate (10%, 12 hónap, Growth Strategy M9) bedrótozva a specifikációba — megerősítés kell hogy ez az induló adat az adatbázisban.

---

## 2. DB séma + Security összesítő

**`spaceos_partner` séma — 5 tábla:**
- `Partners` — status FSM (PendingReview→Active→Suspended→Terminated), tier, CORS allowlist, branding JSON
- `PartnerApiKeys` — SHA-256 hash tárolás (`bytea`), max 3 aktív/partner, auto-revoke 365 nap inaktivitás
- `PartnerEmbedSessions` — signed JWT embed token per-session
- `PartnerLeads` — end customer email `pgcrypto` AES-256 titkosítva, soft-delete + crypto-shredding (GDPR)
- `PartnerUpgradeAttributions` — commission tracking, 12 hónapos ablak
- `PartnerAuditLog` — immutable append-only, DB-trigger blokkja UPDATE/DELETE-et

**RLS:** per-partner GUC (`app.current_partner_id`) — azonos minta a Kernel `app.current_tenant_id`-vel.

---

## 3. Nesting re-use

A PartnerTier a `SpaceOS.Nesting.Algorithms` v1.1.0-t re-use-olja, **code duplication nélkül**:
- Free partner → FFDH
- Pro partner → Guillotine
- Enterprise → Guillotine (MaxRects placeholder Phase 2-re)

A FreeTier `RedisRateLimitService` mintáját követi a `PartnerRateLimitService` (per-partner bucket, tier-alapú limit: 5/50/custom per perc).

---

## 4. Blokkoló — Kernel internal endpoint

Az `AttributionWorker` BackgroundService a Kernel-ből kér email-hash lookup-ot:
```
GET /internal/tenants/by-email-hash?hash={sha256(lower(email))}
→ 200: { tenantId }  |  404
```

Ez egy egyszerű Kernel endpoint (≤0.5 nap), de **a PartnerTier attribution flow-ját blokkolja**. A Kernel terminálnak párhuzamosan kell kiadni az PartnerTier Inbox #1-gyel.

---

## 5. Effort validálás

**27 nap REÁLIS, de szoros** — 3 track párhuzamosan futtatva.

4 inbox a PartnerTier terminálnak:
| # | Tartalom | Nap |
|---|----------|-----|
| Inbox #1 | Alapinfra + Domain + Auth (scaffold, DB, API key middleware) | ~3 |
| Inbox #2 | Nesting embed + Lead capture (iframe, postMessage, PII encryption) | ~7 |
| Inbox #3 | Commission + Portal + Admin (attribution worker, React UI) | ~6 |
| Inbox #4 | Security hardening + Tests + Release (pen-test, DoD) | ~8 |

**Kritikus kockázat:** GDPR DPA legal review ügyvédet igényel — legalább 4 héttel a launch előtt el kell indítani.

---

## 6. Tervdok hivatkozás

`docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` — teljes spec:
- §3: Partner data model (5 tábla, RLS, ERD)
- §4: API key auth middleware stack
- §5: iframe embed token flow + postMessage protokoll
- §6: Nesting integráció + rate limit
- §7: Commission tracking
- §8: Partner portal + SpaceOS admin portal
- §9: Security hardening (12 réteg)
- §10: Deployment (új service, port 5006, Nginx, systemd)
- §11: Fázisolás (Phase 1/2/3)
- §12: Implementációs sorrend (22–25 nap, 3 track, 4 inbox)
