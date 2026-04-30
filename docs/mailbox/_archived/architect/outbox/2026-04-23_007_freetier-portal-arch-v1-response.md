---
id: MSG-ARCH-007-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-004
created: 2026-04-23
---

## Összefoglalás

Elkészült a `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` — a FreeTier frontend portál teljes architekturális specifikációja. Mind a 13 kért pont lefedve + 3 extra szekció (döntési tábla, projekt struktúra, dependency elemzés).

## Spec kulcspontjai

**Stack:** React 18 + Vite + TypeScript + Tailwind CSS + React Query v5 + react-router-dom v7 + axios + zod (Doorstar mintával konzisztens)

**Auth:** Cookie-based (`ft_sess`), NEM Keycloak — custom `AuthProvider` + session check endpoint

**Routing:** 13 route (6 anonymous, 5 session-protected, 2 share)

**UX funnel:** Landing → Calculator → Result → Magic Link → Workspace → Upgrade (5% konverziós target)

**Testing:** ~85 teszt (50 unit + 25 integration + 10 E2E)

**Effort:** Phase 1 (MVP): **16 nap**, Phase 2: ~10.5 nap

## Backend dependency-k (BLOKKOLÓ)

Az audit során 4 hiányzó backend endpointot/config-ot azonosítottam, amelyek a portál működéséhez szükségesek:

| # | Hiány | Miért kritikus | Effort |
|---|-------|----------------|--------|
| FP-14 | `GET /api/freetier/auth/session` | Page reload után a frontend nem tudja ellenőrizni a session érvényességét | 0.25 nap |
| FP-15 | CORS middleware config | Cross-origin cookie küldés `eszkozok` → `freetier` subdomain-re | 0.25 nap |
| FP-16 | Cookie `Domain=.joinerytech.hu` | `ft_sess` cookie cross-subdomain sharing | 0.1 nap |
| FP-17 | `DELETE /api/freetier/auth/session` | Explicit logout (Redis session cleanup) | 0.25 nap |

**Total backend delta:** ~0.85 nap — ez a FreeTier API backend implementációjába beépíthető.

## Kockázatok

1. **FP-14 nélkül a portál nem tud session-t ellenőrizni page reload-ra** — ez a legfontosabb backend dependency
2. **FP-16 nélkül a cookie egyáltalán nem megy cross-subdomain-re** — a teljes auth flow eltörik
3. SEO: CSR-t választottam (v1), Google JS rendering-re bízva — ha az organikus forgalom nem indul be, Phase 2-ben SSG bevezethető
