---
id: MSG-FREETIER-FE-001
from: root
to: freetier-fe
type: task
priority: high
status: UNREAD
ref: SpaceOS_FreeTier_Portal_Architecture_v1.md
created: 2026-04-23
---

# FREETIER-FE-001 — Scaffold + Landing + Nesting kalkulátor (Phase 1, Nap 1–4)

> **Tervdok:** `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **API:** `https://freetier.joinerytech.hu` (LIVE)
> **Domain:** `eszkozok.joinerytech.hu` (majd)
> **Használhatsz sub-agent-eket** ha szükséges

**FONTOS:** A tervdok 1026 soros, részletes specifikáció. Olvasd el a session elején — minden döntés, wireframe, komponens struktúra, és security constraint ott van dokumentálva. Ez az üzenet csak összefoglaló.

---

## Scope — Nap 1–4 (a tervdok Phase 1 ütemterv alapján)

### Nap 1: Scaffold
- Vite + React 18 + TypeScript + Tailwind CSS + React Router v7 + React Query v5 + axios + zod
- Projekt struktúra a tervdok Section 14 szerint
- `.env` és `.env.production` (VITE_API_BASE_URL)
- Vitest + Testing Library + MSW setup
- **Tervdok referencia:** Section 14 (Projekt struktúra)

### Nap 2: API client + types + auth
- `src/api/client.ts` — axios instance, `withCredentials: true` (cookie-based!)
- `src/api/types.ts` — zod schema + TypeScript types
- `AuthProvider` + `useAuth` hook (tervdok Section 4.1)
- **NEM Keycloak!** Cookie-based session (`ft_sess`)
- **Tervdok referencia:** Section 4 (Auth flow)

### Nap 3–4: Landing + Nesting kalkulátor
- Layout: Header + Footer (tervdok Section 7)
- HomePage: hero + nesting form + result
- NestingCalculator component (tervdok Section 3)
- SVG vizualizáció (tervdok Section 3.4)
- Responsive: mobile-first (tervdok Section 6)
- Routing: min 3 route (/, /auth/verify, /share/:prefix/:token)
- **Tervdok referencia:** Section 1, 2, 3, 6, 7

---

## Blokkoló megjegyzés

A backend delta (FREETIER-009: session endpoint, CORS, cookie domain) még folyamatban van. A frontend fejlesztés **párhuzamosan indulhat** — MSW mock-okkal dolgozz, és a valós API integráció a FREETIER-009 DONE után tesztelhető.

---

## Definition of Done

- [ ] Vite + React 18 + TS + Tailwind + React Router + React Query scaffold
- [ ] API client (axios, `withCredentials: true`)
- [ ] AuthProvider + useAuth hook
- [ ] Landing page: hero + nesting kalkulátor form
- [ ] Nesting result: SVG vizualizáció + statisztikák
- [ ] Responsive: mobile 375px + desktop
- [ ] Routing: min 3 route
- [ ] Vitest + MSW setup
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 20 pass
- [ ] Outbox DONE üzenet küldve
