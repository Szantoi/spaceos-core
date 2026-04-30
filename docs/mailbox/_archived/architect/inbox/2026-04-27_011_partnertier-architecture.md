---
id: MSG-ARCH-011
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-27
---

# ARCH-011 — PartnerTier MVP Architecture (Growth Strategy Session E)

> A Growth Strategy v1 Session A-D **mind KÉSZ**. Az utolsó nagy item: **Session E — PartnerTier MVP**.
> **Forrás:** `docs/tasks/active/SpaceOS_Growth_Strategy_v1.md` — Section 6 (PartnerTier B2B2C Channel Network)
> **Output:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md`

## Kontextus

A PartnerTier a SpaceOS growth engine harmadik pillére:
1. ✅ **Manufacturer tenant** (Doorstar) — paid SaaS, LIVE
2. ✅ **FreeTier** (anonymous prospect) — lead gen + upgrade funnel, LIVE
3. 🔴 **PartnerTier** — B2B2C channel partner network, white-label embed

**Growth Strategy §6 összefoglaló:**
- 3 persona: Szabászat Partner (B2B), End Customer (B2C), Platform Admin
- Partner auth: API key middleware, partner-scoped tenant
- White-label embed: iframe/web-component a partner oldalán
- Nesting engine: ugyanaz mint FreeTier, de partner-branded + commission tracking
- Commission model: per-nesting fee vagy flat monthly
- Security: partner isolation, rate limiting, API key rotation

**Becsült effort (Growth Strategy):** ~27 nap (2 design + 25 impl)

## Amit a tervdoknak tartalmaznia KELL

1. **Partner data model** — Partners, PartnerApiKeys, PartnerConfigs, PartnerBranding
2. **API key authentication** — middleware, rotation, rate limiting
3. **White-label embed** — iframe vs web-component döntés, CORS, CSP, branding injection
4. **Nesting integráció** — FreeTier nesting engine re-use, partner-scoped rate limit
5. **Commission tracking** — per-nesting fee, monthly report, payout model
6. **Partner portal** — admin UI: API key management, usage stats, branding config
7. **Security** — partner isolation, API key hash (nem plaintext!), rate limiting, abuse prevention
8. **Database** — új séma? Meglévő FreeTier bővítés?
9. **Deployment** — új service? FreeTier API bővítés? Külön domain?
10. **Fázisolás** — Phase 1 (MVP) vs Phase 2 (commission, analytics)
11. **Effort validálás** — 27 nap reális?

## Referenciák

- Growth Strategy: `docs/tasks/active/SpaceOS_Growth_Strategy_v1.md` §6
- FreeTier API: 176 teszt, LIVE (`freetier.joinerytech.hu`)
- FreeTier Portal: 75 teszt, LIVE (`eszkozok.joinerytech.hu`)
- Nesting.Algorithms: v1.1.0 NuGet (FFDH + Guillotine)

## Definition of Done

- [ ] `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` létrehozva
- [ ] Fenti 11 pont lefedve
- [ ] Implementációs sorrend (track-okra bontva)
- [ ] Outbox response
