---
id: MSG-FREETIER-014
from: root
to: freetier
type: task
priority: high
status: UNREAD
created: 2026-04-27
---

# FREETIER-014 — Brevo + Turnstile + Plausible integráció előkészítés

> A FreeTier API LIVE de magic-link email dev bypass-szal működik. Az éles működéshez 3 external service kell.
> **Skill:** `/spaceos-terminal` szerint dolgozz

## Feladat — kód előkészítés (API kulcsok nélkül is tesztelhető)

### 1. Brevo email client finomítás

Ellenőrizd, hogy a `BrevoEmailClient` kész-e a production használatra:
- `IBrevoEmailService.SendMagicLinkAsync` — helyes template ID config?
- Error handling: ha Brevo 429 (rate limit) → retry?
- Logging: sikeres/sikertelen küldés logolva?

### 2. Turnstile widget — backend validálás

Ellenőrizd, hogy a `TurnstileHttpClient` kész-e:
- `POST https://challenges.cloudflare.com/turnstile/v0/siteverify` hívás helyes?
- Timeout: 5s?
- Dev bypass: ha `TURNSTILE_SECRET` üres → skip (ez megvan)

### 3. Plausible analytics — backend event tracking (opcionális)

Ha van `IAnalyticsService` interface vagy hasonló:
- `POST /api/event` Plausible API-hoz (self-hosted)
- Event types: `nesting_submit`, `magic_link_request`, `workspace_save`, `upgrade_request`

### 4. Tesztek

- Brevo: HttpMessageHandler mock, 429 retry teszt
- Turnstile: valid/invalid response teszt
- Integration: magic-link flow Brevo mock-kal

## Definition of Done

- [ ] Brevo client production-ready (logging, error handling)
- [ ] Turnstile client production-ready
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 178 pass
- [ ] Outbox DONE — jelezze ha API kulcsok kellenek (Gábor operátori feladat)
