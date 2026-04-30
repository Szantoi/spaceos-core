---
id: MSG-ARCH-004
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-23
---

# ARCH-004 — FreeTier Portal Frontend Architecture v1

> A FreeTier API LIVE (162 teszt, freetier.joinerytech.hu). Most a frontend tervdokumentumot kell elkészíteni.
> **Output:** `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md`

---

## Kontextus

**Cél:** Nyilvános nesting kalkulátor webalkalmazás — SEO landing + lead generation + upgrade funnel
**API:** `freetier.joinerytech.hu` (LIVE) — 12 endpoint, cookie-based auth (ft_sess), nem Keycloak
**Domain:** `eszkozok.joinerytech.hu`
**Repo:** `spaceos-freetier-portal` (külön React app, nem a Turborepo monorepo)
**Stack választás:** React 18 + Vite + TypeScript + Tailwind (konzisztens a többi portállal)

## API spec összefoglaló (már LIVE)

| Endpoint | Auth | Funkció |
|---|---|---|
| POST /api/freetier/nest | anonymous | Nesting számítás (SemaphoreSlim 10, rate limit 3/nap/IP) |
| POST /api/freetier/auth/magic-link | anonymous | Magic link kérés → 202 |
| POST /api/freetier/auth/verify | anonymous | Token verify → Set-Cookie ft_sess |
| GET /api/freetier/workspaces | session | Workspace lista |
| POST /api/freetier/workspaces | session | Workspace mentés |
| GET /api/freetier/workspaces/{id} | session | Workspace részletek |
| GET /api/freetier/workspaces/{id}/revisions | session | Revision history |
| POST /api/freetier/workspaces/{id}/share | session | Share token generálás |
| DELETE /api/freetier/workspaces/{id}/share/{shareId} | session | Share visszavonás |
| GET /api/freetier/share/{prefix}/{token} | anonymous | Public share view |
| POST /api/freetier/upgrade | session | Upgrade request |

## Amit a tervdoknak tartalmaznia KELL

### 1. Oldalstruktúra és routing
- Minden oldal wireframe / leírás
- Anonymous vs authenticated route-ok
- ProtectedRoute pattern

### 2. UX / Conversion Funnel
- Anonymous → nesting → "mentsd el" → magic-link regisztráció → workspace → upgrade
- CTA elhelyezés stratégia
- Funnel lépések és mérési pontok

### 3. Nesting kalkulátor UX
- Input form design (sheet méret, alkatrészek, quantity)
- Eredmény megjelenítés (SVG vizualizáció, statisztikák)
- Validáció és hibaüzenetek (SEC-08 limitek: 500 part, 10000mm)

### 4. Auth flow (cookie-based, NEM Keycloak)
- Magic link kérés UI
- Email ellenőrzés képernyő
- Verify callback oldal
- Session kezelés (ft_sess cookie, 401 handling)
- Logout (cookie törlés)

### 5. SEO stratégia
- Meta tags, Open Graph
- SSR szükséges-e vagy CSR elég? (Vite = CSR default)
- robots.txt, sitemap.xml
- Structured data (JSON-LD)

### 6. Responsive design
- Mobile-first breakpoints
- Touch-friendly input kezelés
- Mobile nesting form layout

### 7. Brand és design
- Színpaletta, tipográfia (Tailwind theme)
- Logo / header design
- Doorstar referencia vs saját brand

### 8. Analytics és tracking
- Plausible integráció (self-hosted: analytics.joinerytech.hu)
- Conversion tracking events
- Funnel metrikák

### 9. External integráció
- Cloudflare Turnstile (captcha) elhelyezés — melyik form-okon?
- Plausible analytics script

### 10. Security
- CSP policy (nginx)
- XSS megelőzés
- CORS (freetier API → eszkozok domain)
- Cookie SameSite viselkedés cross-domain

### 11. Testing stratégia
- Unit test (Vitest + Testing Library)
- E2E (Playwright?) — scope és prioritás
- Test target szám

### 12. Deploy topológia
- nginx vhost config
- Static build → `/opt/spaceos/spaceos-freetier-portal/dist/`
- API CORS engedélyezés

### 13. Fejlesztési fázisolás
- Phase 1 (MVP): mi kell a launch-höz?
- Phase 2: mi jöhet később?
- Effort becslés napokban

## Referenciák

- **FreeTier API spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` (v4.5)
- **Growth Strategy:** `docs/tasks/new/SpaceOS_Growth_Strategy_v1.md`
- **Doorstar Portal (referencia UI):** `spaceos-doorstar-portal/` — hasonló stack, de Keycloak OIDC auth
- **Design Portal (referencia UI):** `design-portal/` — Turborepo, más struktúra

## Definition of Done

- [ ] `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` létrehozva
- [ ] Minden fenti 13 pont lefedve
- [ ] Outbox response küldve
