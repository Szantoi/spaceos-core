# SpaceOS — Portal World Architecture
## Frontend Architecture v4 FINAL (Implementation-Ready)

> **Verzió:** v4-final — 2026-04-29
> **Státusz:** ✅ IMPLEMENTATION-READY — Claude Code session-bontás Section 10.4-ben
> **Companion file:** `SpaceOS_Portal_World_Architecture_v4_README.md` (Claude Code agent context loader)
> **Prereq:** Kernel `EnabledModules` (Migration 0025) DEPLOYED · Orchestrator `/bff/api/*` + `/bff/cutting/*` DEPLOYED · Manufacturing service DEV COMPLETE (port 5007)
> **Design forrás:** `https://api.anthropic.com/v1/design/h/Gf2t9hUM4KFNw5f9Kr8igw` (404 — design-agnostic, mapping → post-launch)
> **Review pipeline:** v1 → v2 (frontend) → v3 (security) → v4 (BFF) → **v4-final (assembly)**
> **Nyitott döntések:** D-01..D-09 LOCKED (lásd Section 0)
> **Kumulált findings:** 25 FE-NN + 17 SEC-FE-NN + 16 BFF-NN = **58 finding** (4🔴 · 10🟠 · 34🟡 · 10🟢)
> **Critical gates:** 4 CRITICAL finding külön validation step Section 10.6-ban

---

## 0. Locked decisions (D-01..D-09)

| ID | Döntés | Lock |
|---|---|---|
| D-01 | Scope — 5 world v1-ben | Home, Sales, Production, Shop Floor, Settings |
| D-02 | Cél portál app | Egy közös moduláris `apps/joinerytech` build, **nincs aktív brand skin layer** v1-ben (default skin passzívan) |
| D-03 | Home pattern | World card grid + auto-redirect single-module/single-role esetén |
| D-04 | WorldShell layout | Egy közös `<WorldShell>`, `chrome: 'standard' \| 'minimal' \| 'none'` prop |
| D-05 | URL topology | `/w/{world}/{screen}` |
| D-06 | World visibility | Statikus `worldCatalog.ts` (FE) szűrve `enabledModules` alapján; **BFF route guard authoritative server-side** |
| D-07 | Shop Floor | v1: `/w/shopfloor` route `chrome='none'`-szal a portálon belül; v2: külön app entry-point |
| D-08 | State izoláció | Globális (auth, brand, tenant) + per-world Zustand slice; React Query keys `[worldId, ...]` namespace |
| D-09 | Code splitting | Per-world chunk + nested Suspense heavy screen-ekre |

---

## 1. Kumulált Finding Összesítő (v1 → v4)

**v4 BFF review — 16 finding** (1🔴 CRITICAL · 4🟠 HIGH · 9🟡 MEDIUM · 2🟢 LOW)
**v3 security review — 17 finding** (3🔴 + 4🟠 + 9🟡 + 1🟢)
**v2 frontend review — 25 finding** (2🟠 + 16🟡 + 7🟢 · 0🔴)
**Kumulált:** 58 finding (4🔴 · 10🟠 · 34🟡 · 10🟢)

### v4 BFF findings (BFF-NN)

#### Route Design

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| **BFF-01** | 🔴 CRITICAL | Abstractions BFF antipattern | A v3 Section 5.2 `/bff/abstractions/*` route-ot tartalmaz a `Modules.Abstractions` Graph engine eléréséhez. **De** a sub-senior-backend-bff port-térkép szerint az Abstractions **NuGet only**, nincs HTTP endpoint. A graph engine egy library, amit a Kernel és más service-ek konzumálnak, NEM egy önálló service. Egy `/bff/abstractions/*` route nem proxy-zhat sehova (nincs `:5003` port). Ez vagy (a) téves architektúra-feltevés, vagy (b) implicit Kernel-route, ami félrevezetően van címkézve. | **A configurator endpoint-ok a Kernel BFF route-on át mennek**: `/bff/api/configurator/{productTemplateId}/configure` (vagy `/bff/api/products/{id}/graph`). A Kernel a Abstractions NuGet-et hívja in-process, és HTTP-n szolgálja ki a frontendet. Section 5.2 javítva: `/bff/abstractions/*` törölve, configurator endpoint-ok a `/bff/api/*` alá kerültek. **Anti-pattern lista bővítve:** "BFF route NuGet-only library-hoz TILOS." |
| BFF-02 | 🟠 HIGH | OpenAPI snapshot drift | A v3 Section 5.4 4 service-re említ snapshot-ot (kernel + cutting + manufacturing + abstractions). BFF-01 alapján abstractions snapshot nem létezhet (NuGet, nincs OpenAPI). **De** a tényleges drift kockázat: a Manufacturing snapshot új kell ("ÚJ snapshot" jelölés), de a service még csak DEV COMPLETE — produkción még nem áll. Ha a snapshot a service véglegesítése előtt commit-olódik, később breaking change a kódgenerált klienst eltöri. | (a) Abstractions snapshot törlése; (b) Manufacturing snapshot **csak akkor commit-olható, ha a service production candidate** (legalább staging deployolt) — **contract freeze** prereq Track F-en. Section 5.4 + 10.1 update: Track F első napja a Manufacturing OpenAPI véglegesítése a backend csapattal, contract review checkpoint. |
| BFF-03 | 🟠 HIGH | Aggregátor endpoint hiányzó pattern | A v3 Section 5.3 csak `/bff/api/me/session` és `/me/home-state` aggregátorokat említ. A frontend lapok közül **az `OrderDetailScreen` 4+ parallel fetch-et fog kiváltani**: order header, order lines, handshake state, configurator graph state. A sub-skill explicit szabály szerint ez aggregátor jelölt. | Új aggregátor: `GET /bff/api/orders/:id/full` — visszaadja a header + lines + handshake + linked configurator state-et egyetlen response-ban. Hasonlóan: `GET /bff/cutting/plans/:id/full` (header + items + offcuts), `GET /bff/manufacturing/tasks/:id/full` (task + transitions + assignments). Section 5.3 expanded. |
| BFF-04 | 🟡 MEDIUM | Verziózás hiánya | A v3 BFF route-ok nem verziózottak (`/bff/api/*` nem `/bff/v1/api/*`). A sub-skill checklist 4. pont kifejezetten kérdezi. | **Indoklás v1-ben:** a BFF a SpaceOS Orchestrator része, kliens-szempontból belső; a frontend a snapshot-codegen-ből generál típusos klienst, breaking change → új snapshot + frontend rebuild egyszerre. Verziózás külső konzumens nélkül overhead. **DE** v2 horizon (PartnerTier `/bff/partner/*` jövőbeli külső API-ja): ott `/bff/v1/partner/*` kötelező. Section 5.6 + 11 explicit. |
| BFF-05 | 🟢 LOW | Health check endpoint hiányzik | Nincs `/bff/health` endpoint-ot tervezve, ami az összes downstream service-t pingeli. | `GET /bff/health` — Orchestrator pingeli Kernel (5000), Cutting (5005), Manufacturing (5007); response: `{ status: 'ok'|'degraded'|'down', services: { kernel, cutting, manufacturing } }`. UX: Settings world admin-only "System Status" tab használja; CI/CD smoke teszt használja. Section 5.8 új. |

#### Proxy & Middleware

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| **BFF-06** | 🟠 HIGH | JWT forwarding semantics | A v3 Section 5.5 4-step middleware láncot említ (`auth + tenant + enabledModules + roleGuard`). **De** nem rögzíti, hogy a BFF a downstream Kernel/Cutting/Manufacturing service-eknek **HOGYAN** továbbítja az identitást. Két anti-pattern lehetséges: (a) BFF saját service account JWT-vel → tenant context elveszik; (b) BFF a user JWT-jét továbbítja, de a downstream service nem validálja → trust boundary lyuk. | Explicit pattern Section 5.6.1-ben: BFF **forwardolja a user JWT-t** `Authorization: Bearer <user_jwt>` header-rel. Mellette **explicit `X-Tenant-Id` header** a JWT-ből kiszedve (defense-in-depth — downstream service ezt cross-check-eli a JWT `tenant_id` claim-jével; mismatch → 403 + audit). A downstream service-ek (`Kernel.Api`, `Manufacturing.Api`) a saját `TenantSessionInterceptor`-ja a `X-Tenant-Id`-t használja az RLS-hez. Frozen decision (memóriából): `TenantSessionInterceptor` már létezik a Kernel-ben — ez illeszkedik. |
| BFF-07 | 🟡 MEDIUM | Rate limiting policy | A v3 Section 5.5 timeout policy van, de **rate limit nincs**. Aggregátor endpoint-ok (`/me/session`, `/orders/:id/full`) nehezebbek; az audit log lekérdezés is. | Rate limit policy táblázat (Section 5.6.2): per-user limit; aggregátorok 60 req/min/user; lista endpoint 120 req/min/user; mutáció 30 req/min/user; PIN login (Shop Floor) 5 req/5min/machine (már SEC-FE-ben szerepelt). Express middleware: `express-rate-limit` Redis store-ral (Orchestrator már Redis-zel beszél). 429 response Retry-After header-rel. |
| BFF-08 | 🟡 MEDIUM | Request body validation policy | A v3 nem rögzíti, hogy a BFF valid-ál-e request body-t mielőtt továbbítja. Két opció: pass-through (kis BFF, downstream validál) vagy validate-at-gate. | **Pass-through alapelv** Section 5.6.3-ban: a BFF **NEM duplikálja** a backend validation-t (két validation source = drift kockázat). KIVÉTEL: csak akkor validál a BFF, ha (a) request shape transform szükséges (aggregátor case), vagy (b) PII/security: PIN login → BFF validate `length === 6` és `/^\d{6}$/` pattern, hogy ne juttassuk a SQL-near rétegig. Backend mindig revalidates. |
| BFF-09 | 🟢 LOW | Logging granularity | Kérés/válasz logging policy nincs explicit. SEC-FE-16-ban szerepelt no-console-prod, de a BFF struct logging külön rétege. | Pino structured logger (Orchestrator already uses); per-request log line: `{ requestId, userId, tenantId, route, method, status, duration_ms }`; **NEM log-olunk:** request body (PII), response body (volume), tokent, headers (kivéve `X-Tenant-Id`, `X-Request-Id`). Korrelációs ID minden request-ben (trace-elhetőség). |

#### Error Handling

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| BFF-10 | 🟡 MEDIUM | Error transformation completeness | A v3 Section 5.5 unified error format `{ error: { code, message, details? } }`. **De** a downstream services (Kernel ASP.NET, Manufacturing) saját error format-ot adnak. Ha a BFF csak proxy-zik, a frontend két formátumot kap (Kernel ProblemDetails + Manufacturing custom). | Section 5.6.4: **error response normalizer middleware** az Orchestrator-on. Minden downstream response-ot transzformál: ASP.NET ProblemDetails (`type`, `title`, `status`, `detail`) → `{ error: { code, message } }`, ahol `code` a `type` URI utolsó szegmense (pl. `https://kernel/errors/order-not-found` → `code: "ORDER_NOT_FOUND"`). Stack trace **soha** nem továbbítódik (SEC-FE-15 mirror). |
| BFF-11 | 🟠 HIGH | Circuit breaker policy | A v3 retry/timeout van, de **circuit breaker nincs**. Ha a Manufacturing service down → minden `/bff/manufacturing/*` request 10 sec-ig vár (timeout) → user UX terrible, BFF event loop telítődik. | Pattern: `opossum` (Node.js circuit breaker lib). Per-downstream-service breaker: 5 consecutive failure → 30 sec OPEN state → 503 immediate response (no wait); HALF_OPEN state után 1 próba; siker → CLOSED. Frontend response: `{ error: { code: 'SERVICE_UNAVAILABLE', service: 'manufacturing' } }` → toast `t('error.serviceDown.manufacturing')`. Section 5.6.5 új. |
| BFF-12 | 🟡 MEDIUM | Timeout per endpoint type granularity | A v3 timeout 4 kategória — viszont **file upload** és **aggregátor különböző timeout-ja** nem szerepel. | Bővített tábla Section 5.6.6: GET egyszerű 10s; aggregátor 30s; mutáció 15s; file upload 120s; Shop Floor task fetch 10s; PIN login 10s; **opcionális v2:** SSE/WS connection idle timeout 5min. |

#### Data & Caching

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| BFF-13 | 🟡 MEDIUM | Response shape consistency | A v3 nem rögzíti a `{ data: T, meta?: {...} }` szabványt. Egyes endpoint-ok lista-választ adhatnak (`User[]`), mások wrapped-et (`{ data: User[] }`). | **Standard:** lista endpoint `{ data: T[], meta: { page, pageSize, total, hasNext } }`; detail endpoint `{ data: T }`; mutáció `{ data: T }`. **Kivétel:** `/bff/api/me/session` (root object — historikus, nem törjük). Section 5.6.7 + Zod schema standard a frontend codegen-ben. |
| BFF-14 | 🟡 MEDIUM | Pagination defaults | A v3 audit log endpoint kötelező `?from=&to=&limit=50` (R-07 risk). De a többi lista endpoint pagination default-ja nincs egységesítve. | Egységes default: `?page=1&pageSize=25` (max 100). Backend kötelezően honorálja. Frontend `<PagedTable>` ezzel a default-tal indul. Audit log külön: max 200 (HashChain page-elhetőség kedvéért). |
| BFF-15 | 🟡 MEDIUM | Cache-Control referencia adatokon | A v3 explicit `Cache-Control: public` SEMELYIK `/bff/*` válaszra (security). **De** referencia adatok (worldCatalog metadata, machine list, error code list) lassan változnak. A teljes tilalom túlzás bizonyos esetekre. | Differenciált policy Section 5.6.8: **érzékeny endpoint** (`/bff/api/me/*`, `/bff/api/orders/*`, `/bff/manufacturing/*`) `Cache-Control: no-store` (rögzítve); **lassan változó referencia** (`/bff/api/reference/machines`, `/bff/api/reference/error-codes`) `Cache-Control: private, max-age=300, must-revalidate`; ETag minden GET válaszon → 304 Not Modified `If-None-Match` esetén. |
| BFF-16 | 🟢 LOW | Codegen automation gap | A v3 manuális OpenAPI snapshot regen-t feltételez. Risk: fejlesztő elfelejti commit-olni a snapshot-ot, frontend codegen elavul. | CI job: `pnpm codegen:check` (snapshot diff against last commit) — pull request blocker, ha a Kernel/Cutting/Manufacturing OpenAPI változott, de a commit-olt snapshot nem. Új DoD checkbox Section 9.2-ben.

### v3 Security findings (SEC-FE-NN) — érvényben marad

#### Authentication & Token Handling

| ID | Súly | Terület | Probléma | v3 javítás |
|---|---|---|---|---|
| **SEC-FE-01** | 🔴 CRITICAL | Token storage trust boundary | A v2-ben az `authStore` Zustand-ban tárolja a KC tokent és nem-persist; a frozen decision rögzíti, hogy auth state nem kerül localStorage-be. Viszont **a KC SDK alapértelmezésben localStorage-be ír** (`oidc-client-ts` `WebStorageStateStore`) — ha ez nincs explicit override-olva, az access + refresh + id_token mind localStorage-ben landol. A frozen rule csak akkor érvényes, ha a userManager `userStore` mező explicit `InMemoryWebStorage`-re van állítva. | Explicit konfig: `userManager.settings.userStore = new WebStorageStateStore({ store: new InMemoryWebStorage() })`. **Refresh token átállítása BFF-managed httpOnly cookie pattern-re v2 horizon, v1 interim: `refresh_token` is csak in-memory** (oldal-újratöltés = re-login, ez DoD-ben dokumentálva). Section 4.10 + 5.6 + 9.1 explicit checkbox. |
| **SEC-FE-02** | 🔴 CRITICAL | JWT payload trust | A v2 Section 4.1 `tenantStore.brandSkin` mezője és az `enabledModules` a `/bff/api/me/session` válaszból tölt — ez OK. **De** a frozen rule (memóriából) szerint a `brand_skin` JWT-ből decode-olva tilos; a v2 nem rögzíti explicit, hogy semmilyen kliens-szintű döntés (route guard, role check, module visibility) nem épülhet a JWT payload közvetlen olvasására. Ha bármely hely (pl. KC SDK `user.profile.roles`) közvetlenül használt → trust boundary sérül. | **Anti-pattern lista** Section 5.6-ban: TILOS `jwtDecode(accessToken)` használat business döntéshez; minden auth-derived state (`user`, `roles`, `enabledModules`, `brandSkin`, `tenantId`) az `/me/session` BFF-verified response-ból. Vitest teszt: ESLint custom rule (`no-restricted-imports: jwt-decode`) + grep CI gate. |
| **SEC-FE-03** | 🔴 CRITICAL | `enabledModules` URL bypass | A v2 Section 3.5 `WorldGuard` kliens-szintű redirect — explicit "csak UX". A BFF authorization middleware (Section 5.5) létezik. **De** a tervben hiányzik: (a) a `/bff/api/me/session` válaszába bekódolt `enabledModules` listának **server-side, ugyanabból a forrásból kell jönnie**, mint a route guard middleware-nek (single source of truth, nem két különálló config). (b) Cross-tenant URL spoofelés — ha user a `/w/sales/orders/{otherTenantOrderId}`-t hívja, a BFF tenantId-jét HOL ellenőrzi? | **Single source of truth:** Orchestrator `enabledModulesService.getForTenant(tenantId)` — ez egy hívás, MIND `/me/session` MIND a route guard middleware ezt használja. **Cross-tenant védelem:** minden `:resourceId` paraméteres BFF endpoint Kernel oldalon `WHERE tenant_id = :sessionTenantId` kötelező (RLS aktív, de explicit query-szintű enforcement is); 404 ha mismatch (NEM 403 — információt nem szivárogtatunk a létezésről). Section 5.6 új. |
| SEC-FE-04 | 🟠 HIGH | Logout cleanup completeness | A v2 logout flow nem specifikus. A frozen rule szerint logout-nál minden Zustand store reset + `queryClient.clear()` kötelező. v2-ben ez csak implicit. Ha bármi marad memóriában (pl. `useSalesOrderDraftStore` draft adat egy másik usertől, ha shared computer) → cross-user data leak. | Explicit `logout()` implementáció Section 4.11-ben: 1) `userManager.signoutRedirect()`, 2) callback-ben `queryClient.clear()`, 3) MINDEN Zustand store `.getState().reset()`, 4) sessionStorage tisztítás (`sessionStorage.clear()`), 5) `caches.delete(...)` ha service worker (v1-ben N/A), 6) `/login` redirect. Vitest teszt: 3 store reset verify post-logout. |
| SEC-FE-05 | 🟠 HIGH | Session timeout (idle) | A v2 nem említ inaktivitás-alapú auto-logout-ot. KC token TTL ~15 min, refresh ~30 min — de ha user otthagyja a gépet, a refresh tovább pumpál. Ipari környezet (Doorstar iroda + shop floor) → shared/exposed gépek reális. | Idle timeout: 15 perc no-input → toast warning (60 sec countdown) → auto-logout. `useIdleTimer` hook (új): mouse + key + touch event listener; `setTimeout` reset-eli; lockout után `logout()`. Shop Floor: szigorúbb — 5 perc no-touch → PIN re-entry (NEM full logout, machine session marad). Section 4.12 új. |
| SEC-FE-06 | 🟡 MEDIUM | PKCE code_verifier exposure | A v2 nem rögzíti, hogy a KC SDK PKCE flow `code_verifier`-jét hova logolja. Default oidc-client-ts debug-mode-ban console-ra ír. Production build-ben kockázat: ha bárki dev console-t nyit (pl. customer support session), a verifier látható lehet. | KC SDK config: `userManager.events.addAccessTokenExpiring(silent_renew)` log-ok kikapcsolva; `Log.setLevel(Log.NONE)` production build-ben (Vite `import.meta.env.PROD` flag). DoD: production build console-grep — semmi `code_verifier` / `access_token` / `refresh_token` substring. |

#### Authorization & Route Protection

| ID | Súly | Terület | Probléma | v3 javítás |
|---|---|---|---|---|
| **SEC-FE-07** | 🟠 HIGH | Flash of unauthorized content | A v2 `<LazyWorldRoute>` HOC `<WorldGuard>`-ja `<Navigate>`-et ad vissza ha nincs jog — ez OK chunk pre-load ellen (FE-01). **De** a Home grid (`<WorldHome>`) **render-eli a kártyákat** az `enabledModules` alapján. Ha a session response 200ms-ot késik, az `enabledModules: []` üres, és a grid Empty state-et villant fel — vagy fordítva, ha a default state `'all worlds'`, akkor minden kártya megjelenik egy pillanatra, mielőtt a guard szűr. | `useEnabledWorlds` hook: amíg `tenantStore.loaded === false`, a Home renderel **`<WorldLoadingScreen>`-t** (ne grid skeleton, hanem teljes loading), NEM jelenít meg világ-kártyát. Ugyanez a sidebar nav: a `WorldSidebar` várja a `loaded` flag-et; csak utána renderel nav itemeket. DoD: Playwright teszt — slow 3G simulation, nincs FOUC. |
| **SEC-FE-08** | 🟠 HIGH | Shop Floor session scope creep | A v2 D-07 LOCK: Shop Floor `/w/shopfloor` route-ban van, közös `apps/joinerytech` build-en belül. **A kockázat:** ha a Shop Floor PIN-validated session ugyanabban a böngészőben létezik, mint a KC user session, akkor egy compromised tablet-ről a portál többi világa is elérhető (`/w/sales` pl.). A v2 `useShopFloorSessionStore` lokális state, de a BFF authorizációja (`/bff/shopfloor/*`) machine token, **a többi `/bff/*` viszont KC token-t vár** — ha mindkettő egyszerre van böngészőben, a portal többi világa attacker számára elérhető. | (a) Shop Floor terminálon **NINCS KC user login flow** — a tablet eleve nem engedi a `/w/home`-ra navigálást; `<ShopFloorPinGate>` hard-redirect-eli minden non-shopfloor route-ot a `/w/shopfloor/pin`-re. (b) v1 interim: `apps/joinerytech` build detect-eli a `?mode=shopfloor` URL paramétert vagy a `localStorage.shopfloor_kiosk = "true"` flag-et (admin-set), ekkor a teljes router csak a shopfloor route-okat regisztrálja. (c) v2 horizon (D-07 roadmap): külön app entry-point. Section 5.7 új. |
| SEC-FE-09 | 🟡 MEDIUM | Role-based UI hide ≠ security | A v2 Section 9.1 csekkbox: "admin-only Settings/Users". A `<WorldGuard>` role check redirect-el. **De** a `<WorldNavItem>` szintjén ha admin-only nav item-et hide-olunk, a server-side enforcement külön kell. Ha bárki az URL-t direct megpróbálja (`/w/settings/users`), a guard redirect — OK. **De** a `/bff/api/users` endpoint-on a `roleGuard` middleware nélkül a frontend hide érték nélküli. | Section 5.5 expansion: minden `roleGuard`-os endpoint-ra explicit teszt-eset (Playwright + Vitest BFF mock): nem-admin user direct fetch → 403 + audit log. Frontend hide pure UX, NEM security. Tracking: BFF route minden role-gated path felsorolva Section 5.6-ban. |
| SEC-FE-10 | 🟡 MEDIUM | PIN session scope minimization | A v2 Shop Floor session a `useShopFloorSessionStore`-ban tárolja a `machineId`-t és a `pinValidated`-t. **De** a tervben nincs explicit, hogy a machine token MILYEN scope-ot ad: minden machine, minden task, minden tenant? A DoD szerint a Shop Floor specifikus biztonsági elvárás: **session = csak assigned machines, csak task view**. | Machine token claims (BFF issued JWT vagy opaque token): `{ tenantId, machineId, scopes: ['shopfloor:task:read', 'shopfloor:task:status:write'] }`. NEM tartalmaz: order edit, configurator, user data, audit log. Backend-en (`/bff/shopfloor/*` middleware) scope check minden endpoint-on. Section 5.7 explicit. |

#### XSS & Injection

| ID | Súly | Terület | Probléma | v3 javítás |
|---|---|---|---|---|
| **SEC-FE-11** | 🔴 CRITICAL | `dangerouslySetInnerHTML` audit | A v2 nem említi explicit, hogy `dangerouslySetInnerHTML` TILOS. A configurator (Sales world) Graph engine view potenciálisan szerver által generált HTML-t / SVG-t renderelhet — ez egy **rejtett XSS vektor**. Ha a backend bármikor felhasználói inputból (pl. termék-megjegyzés mező) generál DOM-ot, és a frontend rumourly renderel-i, a teljes session compromiseolható. | Section 5.6: **TILOS `dangerouslySetInnerHTML` v1-ben**. Egyetlen kivétel: `<HashDisplay>` (statikus hash) — ott string render, nincs DOM injection. SVG kivétel: csak `lucide-react` (statikus, build-time). ESLint rule: `react/no-danger: error`. CI grep gate: `grep -r "dangerouslySetInnerHTML" src/` → 0 match required. |
| SEC-FE-12 | 🟠 HIGH | URL/route param sanitization | A v2 Section 2.5 deep linking — minden lista screen `useSearchParams`. **De** a `parseOrderFilters(searchParams)` parser nincs spec — ha a status enum-ot nem validálja és csak passthrough-olja a BFF-nek, egy maliciózus URL `?status=DROP TABLE` jellegű inputot küldhet. A BFF védi a SQL-t (parametrized), de a kliens-oldali Zod validáció megakadályozza a felesleges 400-akat és a hibaüzenet-szivárgást. | Minden URL paraméter Zod schema-val parse-olódik mount-kor; invalid → reset to default + replace history; Zod error console-ra log-olódik (development), production-ban silent. Pattern: `parseFiltersFromUrl(searchParams, OrderFiltersSchema)` — egy közös util. Section 2.5 expansion. |
| SEC-FE-13 | 🟡 MEDIUM | Configurator formula evaluation | A v2 Sales world `ProductConfiguratorScreen` a `Modules.Abstractions` Graph engine view-jét renderelni. A graph engine `resolveFormula` server-side fut (Kernel/Abstractions). **De** ha bárhol a frontend kap egy formula stringet és kliens-oldalon evalueálja (pl. élő preview), `new Function()` vagy `eval()` használat → kódvégrehajtás. v1-ben ezek nem terveződnek, de ESLint nincs még tiltva. | ESLint: `no-eval: error`, `no-new-func: error`. CI gate. Ha v2/v3 horizon-ban élő preview kell → Web Worker sandbox + AST-based safe expression evaluator (külön spec). Frozen v3: client-side formula eval TILOS. |
| SEC-FE-14 | 🟢 LOW | CSP `unsafe-inline` audit | A v2 Section 9.2 CSP header említve: `default-src 'self'; script-src 'self'; ...`. Vite dev mode HMR-rel `unsafe-inline` kell — production build viszont nem. Ha a CSP nem szigorodik prod-ban, XSS védelem gyenge. | DoD checkbox: production CSP `unsafe-inline` 0 előfordulás (sem `script-src` sem `style-src` alatt); Vite production build inline style → `style.css` external; CSP report-only mode v1-ben (csak log), v2-ben enforcing. |

#### Data Exposure

| ID | Súly | Terület | Probléma | v3 javítás |
|---|---|---|---|---|
| SEC-FE-15 | 🟡 MEDIUM | Error message raw display | A v2 Section 5.5 unified error format `{ error: { code, message, details? } }`. **De** a Toaster (FE-11) renderelhet-i a server-side `message`-t direkt — ez tartalmazhat path/stack info-t (pl. `npgsql: relation "users" does not exist at line 42`). | Toast pattern: `toast.error(t(`error.${code}`) || t('common.error.generic'))` — fordítási kulcs primary, generic fallback. A `message` mező CSAK development-ben jelenik meg (`import.meta.env.DEV`). Production-ban error code → user-friendly i18n string. Section 5.6 + 6.1 új error i18n kulcsok. |
| SEC-FE-16 | 🟡 MEDIUM | Console logging in production | A v2 Section 9.1-ben nincs explicit "no console.log in prod" gate. Dev során logging ok, de release-en érzékeny adatok (token expiry timing, response body) szivároghatnak. | Vite plugin: `vite-plugin-remove-console` production build-ben (kivéve `console.error` — Sentry-szerű collector v2 horizon). DoD: prod bundle grep `console\.\(log\|debug\|info\|warn\)` → 0 match (csak `console.error` allowed). |
| SEC-FE-17 | 🟡 MEDIUM | Cross-tenant data leak veszély | A v2 Section 5.5 RLS (Row-Level Security) említve a Kernel-ben. **De** a frontend tervben nincs explicit assertion, hogy minden lista/detail response `tenantId`-je ellenőrzött a renderelés ELŐTT. Ha BFF bármikor accidentally egy joinolt query miatt cross-tenant adatot ad vissza, a frontend gyanútlanul renderel. | Defense-in-depth: minden BFF response Zod parser ellenőrzi a `tenantId` mezőt egyezést a `tenantStore.tenantId`-vel; mismatch → throw + audit log + redirect Home + toast "Adathiba" (NEM részletes — nem szivárogtatunk). Pattern: `assertSameTenant(response, expectedTenantId)`. Section 4.13 új. |

### v2 Frontend findings (FE-NN) — érvényben marad

#### Route & Navigation

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| **FE-01** | 🟠 HIGH | Route guard ordering | A v1 Section 2.4 példájában a `<WorldGuard>` a `<Suspense>` BELSEJÉBEN van — emiatt a nem-engedélyezett world chunk-ja letöltődik még a redirect előtt. Bandwidth-pazarlás + chunk URL-ből kikövetkeztethető a world létezése (info leak) | Wrapper sorrend: `<ErrorBoundary> > <WorldGuard> > <Suspense> > <LazyWorld>`. Ha a guard `<Navigate>`-et ad vissza, a `<Suspense>` soha nem trigger-el, a lazy import nem indul. Section 2.4 + új `<LazyWorldRoute>` HOC. |
| **FE-02** | 🟠 HIGH | Layout pattern | A v1 minden world komponens belül saját `<WorldShell>`-t renderelt → world-váltáskor (pl. `/w/sales` → `/w/production`) a sidebar+topbar **unmount + remount**, query-k újraindulnak, sidebar collapse state elvész, perceptible flicker. | React Router v6 layout route: `<WorldShell>` mint **parent route element**, `<Outlet>` a content. Csak a content-area lazy chunk cserélődik. Sidebar/topbar stabil marad. Section 2.1 + 3.1 átszerkesztve. |
| FE-03 | 🟡 MEDIUM | Deep linking | Filter/sort state (orders list, plans list, audit log) URL-ben NINCS reflektálva — bookmark, share, browser back nem hozza vissza a szűrt nézetet | `useSearchParams` minden filter/sort komponenshez; URL → store init at mount; standardizált query string konvenció (`?status=...&from=...&to=...&page=...`) |
| FE-04 | 🟡 MEDIUM | Browser history policy | World switch / modal open / guard redirect — push vagy replace? Nem specifikálva → Back gomb viselkedés inkonzisztens | Policy: world-switch = push; guard-redirect = replace; modal-open = NO history change (state in sub-route opcionális v3-ra); auth-redirect = replace |
| FE-05 | 🟡 MEDIUM | 404 strategy granularity | A v1 csak top-level `*` route 404-et kezel. "World létezik, de erőforrás nem" (pl. `/w/sales/orders/9999`) eset nem specifikus → globális 404 oldalra dob, kontextus-vesztés | World-level `<EntityNotFound>` komponens; query 404 → `<EntityNotFound>` a world content-areán belül (sidebar+topbar megmarad); csak ismeretlen world-id → globális 404 |
| FE-06 | 🟡 MEDIUM | Lazy wrapper enforcement | A v1 csak példában mutatja a wrapper-szettet — nincs reusable HOC, ami minden lazy world-re kötelezi a sorrendet | Bevezetni `<LazyWorldRoute worldId chunkLoader>` HOC-ot; `worldRoutes.tsx`-ben central registry; minden új world ezzel kerül be |

### Component Architecture

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-07 | 🟡 MEDIUM | Form stratégia | Configurator, PIN entry, settings forms — controlled vs uncontrolled, validáció lib nem specifikálva | Standard: react-hook-form + Zod schema; controlled forms; minden form ugyanazt a pattern-t használja; common error rendering Section 3.5-ben |
| FE-08 | 🟡 MEDIUM | Heavy screen ErrorBoundary | NestingVisualizer/FsmBoard összeomlás → world-szintű ErrorBoundary kapja el → user az egész world-ből kiesik | Heavy screen saját ErrorBoundary + saját Suspense pár (nested). World-szintű boundary csak a fő world-shell hibát kapja |
| FE-13 | 🟡 MEDIUM | Memoization stratégia | Heavy lista (PagedTable rows N>100), board cells (FsmBoard) memoization nincs specifikálva | Konvenció: `<PagedTable>` row child komponens `React.memo`-val; FsmBoard cell komponens `React.memo` + stabil callback (`useCallback`); selectors Zustand-on shallow compare |
| FE-18 | 🟡 MEDIUM | Modal focus trap | Dialog/modal focus trap a v1-ben nem specifikus → kbd nav kiszökhet | `<Dialog>` standard komponens (radix-ui adoption javasolt) focus trap + Esc + ARIA dialog role + initial focus + return focus on close |
| FE-19 | 🟢 LOW | Component placement rationale | App-local vs `@spaceos/ui` indoklás csak implicit | 1 mondatos rationale új komponensenként (Section 3.2 oszlop) |
| FE-20 | 🟢 LOW | List key konvenció | Lista key stratégia nem dokumentált | Konvenció: entity ID használata, NEVER index; ha nincs ID (in-memory draft items), `crypto.randomUUID()` mount-kor |

### State Management

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-09 | 🟡 MEDIUM | Live data refresh | Shop Floor task feed `staleTime: 5s` van, de `refetchInterval` nincs → nem auto-update, csak manuális/route-revisit refetch | Explicit `refetchInterval: 10_000` Shop Floor task list + `refetchIntervalInBackground: false` (battery). v3 backlog: SSE/WS upgrade |
| FE-10 | 🟡 MEDIUM | Mutation feedback policy | Optimistic update vs invalidation policy nem specifikálva mutáció-típusonként | Policy táblázat (Section 4.5): status update (Shop Floor) = optimistic + rollback; order save = invalidation; configurator = optimistic + rollback; admin user op = invalidation |
| FE-11 | 🟡 MEDIUM | Toast / notification | Mutáció eredmény visszacsatolás nincs rendszerezve | `<Toaster>` komponens (`@spaceos/ui` candidate); `useToast` hook; ARIA live region; success/error/warning/info variants; auto-dismiss kivéve kritikus error |
| FE-12 | 🟡 MEDIUM | Loading state taxonomy | Csak chunk-load loading specifikálva; query loading + mutation pending nem rendszerezve | Taxonomy (Section 4.6): world-load = full skeleton screen; query-load = component-level skeleton/spinner; mutation-pending = button disabled + inline spinner |
| FE-16 | 🟡 MEDIUM | Configurator draft persistence | `useSalesOrderDraftStore` world-unmount → reset; user accidentally navigál → unsaved changes elveszik | `useBlocker` (React Router v6) confirm dialog ha draft dirty; `beforeunload` browser-szinten; opcionális auto-save sessionStorage-be (NEM localStorage — security) |
| FE-17 | 🟡 MEDIUM | Auth refresh edge case | KC silent renew failure mid-world → mit lát a user? Nincs spec → blank screen / loop kockázat | `onSilentRenewError` handler: graceful redirect `/login?returnTo=<currentPath>`; post-login restore `returnTo` path; toast "Munkamenet lejárt, jelentkezz be újra" |

### Performance

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-14 | 🟡 MEDIUM | Virtualization | PagedTable virtualizál-e? Audit log + Orders list 1000+ row potenciálisan; v1-ben nem specifikus | DoD: PagedTable internal virtualization audit a Track A elején; ha nem virtualizál + N>200 → `react-virtuoso` integráció Audit Log + Orders list-re (ezekből a többi `PagedTable` használat <100 row jellemzően) |
| FE-15 | 🟡 MEDIUM | Bundle budget granularity | Production world "override OK if nested heavy" homályos | Granuláris budget: world-shell-content < 50KB gzip; minden heavy screen chunk < 100KB gzip; CI-kapu PR-szinten (`size-limit` lib); regression alert |

### Accessibility & Responsive

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-21 | 🟢 LOW | Asset optimization | SVG inline vs sprite, font preconnect/swap nem specifikálva | SVG icons: `lucide-react` (tree-shakeable); brand SVG inline; raszter képek `loading="lazy"`; `font-display: swap`; preconnect Google Fonts/CDN; subset Inter/Manrope |
| FE-22 | 🟢 LOW | Semantic landmarks + ARIA | `<nav>`, `<main>`, `<header>`, `<aside>` használat + icon-only aria-label nem explicit | WorldShell: `<header>` topbar, `<nav aria-label="Sidebar">` sidebar, `<main>` content; minden world `<h1>`; minden icon-only `aria-label` (sidebar collapse, user menu); icon next-to-text `aria-hidden="true"` |
| FE-23 | 🟢 LOW | Empty state taxonomy | Mikor `<EmptyState>` (zero result, error, forbidden) — nem specifikus | 3 variant az `<EmptyState>`-ben: `zero` (info icon + "Nincs még…" + opcionális CTA); `error` (warning icon + retry button); `forbidden` (lock icon + "Hozzáférés megtagadva" + admin contact CTA) |
| FE-24 | 🟢 LOW | Locale hot-swap | Mid-session locale switch nem specifikus (page reload?) | `i18nStore.setLocale()` hot-swap (no page reload); React-friendly bundle split namespace per locale; preferre user-saved locale `/me/session`-ből, fallback hostname-based |
| FE-25 | 🟢 LOW | i18n date/number format | Lokalizáció csak fordítási kulcs szinten — szám/dátum formázás nem specifikus | `Intl.DateTimeFormat` + `Intl.NumberFormat` minden render-en; `useLocaleFormat` hook (`@spaceos/i18n` candidate) |

### Cumulative — érvényben marad v1-ből

A v1 11 finding-je nem volt — first review pass.

---

## 2. Route architektúra

### 2.1 Route map (React Router v6 layout routes)

**Δ FE-02:** A `<WorldShell>` mostantól **layout route element** — egyszer mountolódik, world-váltáskor csak a content cserélődik. Sidebar/topbar stabil, query refetch nincs (a tenant/session query-k cache-elve maradnak).

```
/                                        → ProtectedRoute → /w/home (auth check)
/login                                   → KeycloakLoginRedirect
/callback                                → KeycloakCallback
/silent-callback                         → KeycloakSilentRenew
/logout                                  → KeycloakLogout

# ─── Layout route: <WorldShell> renderelődik egyszer, <Outlet> a content ───

/w                                       → <WorldShell chrome="standard">
   ''                                      → redirect /w/home
   home                                    → <WorldHome /> (card grid OR auto-redirect)
   sales/*                                 → <LazyWorldRoute worldId="sales" /> [content-only chunk]
     /                                       → SalesIndex (default: orders)
     orders                                  → OrdersListScreen
     orders/:orderId                         → OrderDetailScreen
     orders/:orderId/items/:lineId/configure → ProductConfiguratorScreen [nested Suspense + EB]
     handshakes                              → B2bHandshakesScreen
   production/*                            → <LazyWorldRoute worldId="production" /> [content-only chunk]
     /                                       → ProductionIndex (default: cutting-plan)
     cutting-plan                            → CuttingPlanListScreen
     cutting-plan/:planId                    → CuttingPlanDetailScreen
     cutting-plan/:planId/nesting            → NestingVisualizerScreen [nested Suspense + EB]
     manufacturing                           → ManufacturingFsmBoardScreen [nested Suspense + EB]
     manufacturing/:taskId                   → ManufacturingTaskDetailScreen
   settings/*                              → <LazyWorldRoute worldId="settings" /> [content-only chunk]
     /                                       → SettingsIndex (default: tenant)
     tenant                                  → TenantInfoScreen
     users                                   → UserListScreen [role: admin]
     audit                                   → AuditLogScreen (HashDisplay reuse)

# ─── Külön layout route: <WorldShell chrome="none"> Shop Floor-nak ───

/w/shopfloor                             → <WorldShell chrome="none"> + <ShopFloorPinGate>
   /                                       → ShopFloorIndex
   pin                                     → PinEntryScreen
   tasks                                   → ShopFloorTaskListScreen
   task/:taskId                            → ShopFloorTaskDetailScreen

# ─── Globális 404 + entity-szintű 404 (FE-05) ───

*                                        → NotFound404 (globális — ismeretlen world-id)
                                          (entity 404 → <EntityNotFound> a world content-ben)
```

**Δ FE-01 (wrapper order):** `<LazyWorldRoute>` HOC bevezetése — biztosítja a helyes wrapper sorrendet:

```tsx
// apps/joinerytech/src/worlds/LazyWorldRoute.tsx
function LazyWorldRoute({ worldId, chunkLoader }: LazyWorldRouteProps) {
  const Lazy = useMemo(() => lazy(chunkLoader), [chunkLoader]);
  return (
    <ErrorBoundary fallback={<WorldErrorScreen worldId={worldId} />}>
      <WorldGuard worldId={worldId}>
        {/* Guard returns <Navigate> ha jogosulatlan → Suspense soha nem trigger */}
        <Suspense fallback={<WorldLoadingScreen worldId={worldId} />}>
          <Lazy />
        </Suspense>
      </WorldGuard>
    </ErrorBoundary>
  );
}
```

**Központi registry (`apps/joinerytech/src/worlds/registry.ts`):**

```ts
export const worldRoutes = [
  { worldId: 'sales',      chunkLoader: () => import('./sales/SalesWorld') },
  { worldId: 'production', chunkLoader: () => import('./production/ProductionWorld') },
  { worldId: 'settings',   chunkLoader: () => import('./settings/SettingsWorld') },
];
// shopfloor saját chrome="none" layout route-on, regiszten kívül
```

### 2.2 Code splitting stratégia (D-09 LOCKED)

**Per-world boundary:** minden `/w/{world}/*` szegmens önálló `React.lazy()` import.

**Nested Suspense — heavy screen kandidátusok:**

| Screen | Heavy lib (becsült gzip) | Boundary |
|---|---|---|
| `ProductConfiguratorScreen` | Abstractions Graph engine view (~30-50KB) | Nested Suspense `/w/sales` chunk-on belül |
| `NestingVisualizerScreen` | Custom canvas renderer (~40KB) | Nested Suspense `/w/production` chunk-on belül |
| `ManufacturingFsmBoardScreen` | recharts (~80KB gzip, FsmBadge animations) | Nested Suspense `/w/production` chunk-on belül |
| `AuditLogScreen` | (HashDisplay már létezik, könnyű) | Nincs — fő production chunk |

**Bundle budget gate (DoD):**
- App entry < 200KB gzip
- Per-world chunk < 80KB gzip (kivéve: Production, ha `recharts` nem nested → akkor downgrade kötelező)
- v2 frontend review automatikus FE-finding ha bármelyik világ chunk > 80KB és nincs nested boundary

### 2.3 enabledModules → route visibility

**Mapping (`@spaceos/domain/worldCatalog.ts`):**

| World ID | `requiredModules` | `requiredRoles` | `chrome` | `requiresPin` |
|---|---|---|---|---|
| `home` | `[]` (mindig látható) | `[]` | `standard` | false |
| `sales` | `['door']` v1-re (vagy `['cabinet']` ha bekapcsolt) | `['sales-rep', 'admin']` | `standard` | false |
| `production` | `['cutting']` (manufacturing implicit) | `['production-lead', 'admin']` | `standard` | false |
| `shopfloor` | `['cutting']` | `['shopfloor-worker']` | `none` | true |
| `settings` | `[]` | `['admin']` (csak részlegesen, audit lehet teljes role-nak) | `standard` | false |

**Kliens-oldali szűrés (UX) ≠ Server-oldali authorization (security).** A részleteket Section 3.4 és Section 5.5 tárgyalja.

### 2.4 Suspense + ErrorBoundary kötelező — `<LazyWorldRoute>` HOC

**Δ FE-01 + FE-06:** Minden lazy world a `<LazyWorldRoute>` HOC-on át regisztrálódik (Section 2.1). A wrapper sorrend kötelezve:

```
<ErrorBoundary>
  └── <WorldGuard>          ← guard FIRST: nem-engedélyezett → <Navigate>, lazy nem indul
        └── <Suspense>      ← csak ha guard PASS-olt
              └── <Lazy />  ← chunk download + render
```

**Δ FE-08:** Heavy screen-ek saját nested `<ErrorBoundary>` + `<Suspense>` párral, hogy egy Three.js/recharts crash ne kapja el a world-szintű boundary, ami az egész world-ből kidobná a usert:

```tsx
// Production world-ön belül, NestingVisualizerScreen használat:
<Route path="cutting-plan/:planId/nesting" element={
  <ErrorBoundary fallback={<HeavyScreenError onRetry={...} />}>
    <Suspense fallback={<HeavyScreenLoading />}>
      <NestingVisualizerScreen />
    </Suspense>
  </ErrorBoundary>
} />
```

**Frozen decision (memóriából, érvényes):** *"Lazy-loaded module routes require `<Suspense>` and `<ErrorBoundary>` wrappers"* — `<LazyWorldRoute>` ezt strukturálisan kényszeríti.

### 2.5 URL state, history és 404 stratégia (FE-03/04/05)

#### Deep linking — URL ↔ filter state szinkron (FE-03)

Minden lista/szűrhető screen `useSearchParams`-szal szinkronizálja a filter/sort/page state-et:

| Screen | URL paraméterek |
|---|---|
| `OrdersListScreen` | `?status=draft,submitted&from=2026-04-01&to=2026-04-30&page=1&sort=-updatedAt` |
| `CuttingPlanListScreen` | `?status=ready,executing&page=1` |
| `ManufacturingFsmBoardScreen` | `?stage=cnc,edge&assignee=me` |
| `AuditLogScreen` | `?from=...&to=...&actor=...&action=...&page=1` |
| `UserListScreen` | `?role=admin,sales-rep&page=1` |
| `B2bHandshakesScreen` | `?state=pending,accepted&page=1` |

**Pattern (sample):**
```ts
const [searchParams, setSearchParams] = useSearchParams();
const filters = parseOrderFilters(searchParams);
const { data } = useQuery({
  queryKey: ['sales', 'orders', filters],
  queryFn: () => api.orders.list(filters),
});
const updateFilter = (next: OrderFilters) =>
  setSearchParams(serializeOrderFilters(next), { replace: true });
```

#### History policy (FE-04)

| Akció | Push / Replace | Indok |
|---|---|---|
| World switch (`/w/sales` → `/w/production`) | Push | User vissza akar tudni menni |
| Filter változás (`?status=...`) | **Replace** | Back gomb ne lépdeljen filter-történetben |
| Pagination (`?page=2`) | Push | Back = előző oldal — lista UX standard |
| Modal open | Nincs history change v1-ben | (state in route v3-ra) |
| Guard redirect (no module/role) | **Replace** | History ne tartalmazza a tiltott URL-t |
| Auth redirect (`/login` → world) | **Replace** | Login flow ne legyen Back-gombbal visszanavigálható |

#### 404 granularity (FE-05)

| Eset | Komponens | Helyszín |
|---|---|---|
| Ismeretlen route (`/foo`) | `<NotFound404>` (globális) | App root, `*` route |
| Ismeretlen world (`/w/unknown`) | `<NotFound404>` (globális) | Globális, `<WorldShell>`-en kívül |
| Tiltott world (modul/role miss) | `<Navigate to="/w/home" replace>` | `<WorldGuard>`-on belül |
| Ismeretlen entity (`/w/sales/orders/9999`) | `<EntityNotFound resource="order" id="9999" />` | World content-area, `<WorldShell>` benne marad — kontextus megtartva, sidebar+topbar látható |
| Forbidden entity (cross-tenant kísérlet) | `<EntityForbidden />` + audit log entry | World content-area; admin contact CTA |

---

## 3. Komponens architektúra

### 3.1 Komponens fa — layout route-os shared shell (Δ FE-02)

```
<App>
└── <BrowserRouter>
    └── <AuthProvider> (Keycloak)
        └── <QueryClientProvider> (TanStack Query)
            └── <Toaster />                          (FE-11 — globális toast/notification)
                └── <Routes>
                    ├── /login, /callback, /logout (system routes — shell-mentes)
                    ├── /w → <WorldShell chrome="standard">  ← LAYOUT ROUTE (egyszer mountol)
                    │   ├── <header><WorldTopBar /></header>
                    │   │   ├── <BrandLogo>          (Δ stabil — nem újra-render world switch-en)
                    │   │   ├── <TenantBreadcrumb>
                    │   │   ├── <UserMenu>           (icon-only → aria-label, FE-22)
                    │   │   └── <LocaleSwitcher>     (FE-24)
                    │   ├── <nav aria-label="Sidebar"><WorldSidebar /></nav>  (FE-22)
                    │   │   ├── <WorldNavGroup>
                    │   │   │   └── <WorldNavItem>
                    │   │   └── <SidebarFooter>
                    │   └── <main>
                    │       └── <Outlet />           ← csak EZ cseréltődik world-váltáskor
                    │           └── <LazyWorldRoute>
                    │               └── <ErrorBoundary>
                    │                   └── <WorldGuard>
                    │                       └── <Suspense>
                    │                           └── <LazyWorldChunk />
                    │                               └── world-specific routes
                    │                                   └── [nested ErrorBoundary + Suspense
                    │                                       heavy screens-re — FE-08]
                    │
                    ├── /w/shopfloor → <WorldShell chrome="none"> ← KÜLÖN layout route
                    │   └── <ShopFloorPinGate>
                    │       └── <Outlet />
                    │
                    └── * → <NotFound404 />
```

**Stabilitási garanciák (Δ FE-02):**
- World-váltáskor a `<WorldShell>` NEM mountolódik újra
- A `<UserMenu>`, `<TenantBreadcrumb>`, `<LocaleSwitcher>` query-jei (`['session']`, `['tenant']`) cache-ből szolgálnak ki — nincs refetch
- `<WorldSidebar>` aktuális világ-jelölést `useParams()` és `useLocation()` alapján számolja, NEM unmountol

### 3.2 Új komponensek

**Δ v2:** + `LazyWorldRoute`, `EntityNotFound`, `EntityForbidden`, `HeavyScreenError`, `HeavyScreenLoading`, `Dialog` (radix-ui based). Új oszlop: rationale (FE-19).

| Komponens | Csomag | Típus | Props | Leírás | Rationale (FE-19) |
|---|---|---|---|---|---|
| `WorldShell` | app | Layout | `chrome, children` | Fő shell, layout route element | App-local v1 — single portal (D-02) |
| `WorldTopBar` | app | Layout | `showTenant?` | Felső sáv: brand, tenant, user, locale | App-local — portál-specifikus brand |
| `WorldSidebar` | app | Layout | — | Bal oldali nav, `useParams` alapján aktív world | App-local — `worldCatalog`-ra kötött |
| `WorldNavGroup` | app | Presentational | `label, items, collapsible?` | Sidebar csoport | App-local |
| `WorldNavItem` | app | Presentational | `label, icon, path, badge?` | Egy nav link | App-local |
| `WorldGuard` | app | Logic | `worldId, children` | Modul + role + PIN check, redirect | App-local — runtime config-driven |
| **`LazyWorldRoute`** *(ÚJ FE-06)* | app | HOC | `worldId, chunkLoader` | Wrapper sorrend kényszerítése | App-local — registry-driven |
| `WorldHome` | app | Page | — | Home logic (grid VS redirect) | App-local |
| `WorldCard` | app | Presentational | `world, accent?, badge?, onClick` | Home grid kártya | App-local |
| `WorldLoadingScreen` | app | Presentational | `worldId` | Suspense fallback (skeleton) | App-local |
| `WorldErrorScreen` | app | Presentational | `worldId, error, onRetry` | World ErrorBoundary fallback | App-local |
| **`EntityNotFound`** *(ÚJ FE-05)* | app | Presentational | `resource, id?, suggestUrl?` | Entity 404 a world content-areán belül | App-local |
| **`EntityForbidden`** *(ÚJ FE-05)* | app | Presentational | `resource, contactAdmin?` | Cross-tenant / forbidden entity UX | App-local |
| **`HeavyScreenError`** *(ÚJ FE-08)* | app | Presentational | `screenName, error, onRetry` | Nested ErrorBoundary fallback heavy screenen | App-local |
| **`HeavyScreenLoading`** *(ÚJ FE-08)* | app | Presentational | `screenName` | Nested Suspense fallback | App-local |
| `ShopFloorPinGate` | app | Logic | `children` | PIN gate, machine binding | App-local — interim v1 (D-07) |
| `PinEntryScreen` | app | Page | — | 6-digit PIN keypad UI | App-local |
| `OrdersListScreen` | app | Page | — | `PagedTable` + URL filters (FE-03) | App-local |
| `OrderDetailScreen` | app | Page | — | Order header + lines + handshake state | App-local |
| `ProductConfiguratorScreen` | app | Page | — | Graph engine view (Abstractions BFF) | App-local — heavy, nested boundary |
| `B2bHandshakesScreen` | app | Page | — | Handshake list + URL filters | App-local |
| `CuttingPlanListScreen` | app | Page | — | `PagedTable` + URL filters | App-local |
| `CuttingPlanDetailScreen` | app | Page | — | Plan header + line items + offcuts | App-local |
| `NestingVisualizerScreen` | app | Page | — | Canvas renderer (heavy, nested) | App-local — heavy |
| `ManufacturingFsmBoardScreen` | app | Page | — | FSM board, recharts (heavy, nested) | App-local — heavy |
| `ManufacturingTaskDetailScreen` | app | Page | — | Task header + state transitions | App-local |
| `ShopFloorTaskListScreen` | app | Page | — | Touch-optimized list, large rows | App-local |
| `ShopFloorTaskDetailScreen` | app | Page | — | Touch-optimized detail + action buttons | App-local |
| `TenantInfoScreen` | app | Page | — | Read-only tenant info v1 | App-local |
| `UserListScreen` | app | Page | — | `PagedTable`, admin-only | App-local |
| `AuditLogScreen` | app | Page | — | `PagedTable` + `HashDisplay` reuse | App-local |

### 3.3 Új @spaceos/ui komponensek (shared, vN candidates)

**Δ v2:** + `Toaster`/`useToast` (FE-11), `Dialog` (FE-18 — radix-ui adapter), `EmptyState` 3 variant (FE-23), `useLocaleFormat` hook (FE-25).

| Komponens | Csomag | Indok |
|---|---|---|
| `KpiCard` | `@spaceos/ui` | Home grid + Settings dashboard használja |
| `EmptyState` | `@spaceos/ui` | Több world ugyanazt a "nincs adat" pattern-t mutatja — **3 variant: `zero` / `error` / `forbidden`** (FE-23) |
| `LargeTouchButton` | `@spaceos/ui` | Shop Floor + jövőbeli mobile worker UX |
| `PinKeypad` | `@spaceos/ui` | Shop Floor PIN + jövőbeli kiosk-flow-k |
| **`Toaster` + `useToast`** *(ÚJ FE-11)* | `@spaceos/ui` | Globális toast/notification + ARIA live region (success/error/warning/info, auto-dismiss kivéve kritikus) |
| **`Dialog`** *(ÚJ FE-18)* | `@spaceos/ui` | Radix-ui adapter: focus trap + Esc + ARIA dialog role + initial focus + return focus |
| **`useLocaleFormat`** *(ÚJ FE-25)* | `@spaceos/i18n` | `Intl.DateTimeFormat` + `Intl.NumberFormat` lokál-alapú szám/dátum/pénznem formázás |

### 3.4 Meglévő `@spaceos/ui` újrahasználata

| Komponens | Hol használjuk | Módosítás |
|---|---|---|
| `FsmBadge` | Manufacturing FSM board, Order detail (handshake state) | Nincs |
| `HashDisplay` | Audit log, Order receipt | Nincs |
| `PagedTable` | Orders, Cutting plans, Users, Audit log | Nincs |
| `TradeTypeBadge` | Order list (door/cabinet trade jelzés) | Nincs |
| `JsonIntentEditor` | **NEM v1** — csak ha admin product config szerk. (roadmap) | — |

### 3.5 WorldGuard logika

**Δ SEC-FE-07:** session-loaded gate — amíg `tenantStore.loaded === false`, NEM jelenít meg world content-et / kártyát. Megakadályozza a flash of unauthorized content-et slow connection esetén.

```tsx
function WorldGuard({ worldId, children }: WorldGuardProps) {
  const session = useSession();
  const { loaded: tenantLoaded } = useTenantStore();
  const world = worldCatalog[worldId];

  // SEC-FE-07: amíg nem jött vissza a session response, loading screen
  if (!tenantLoaded) {
    return <WorldLoadingScreen worldId={worldId} />;
  }

  // 1. Module check
  if (world.requiredModules.length > 0) {
    const hasModule = world.requiredModules.some(m =>
      session.enabledModules.includes(m)
    );
    if (!hasModule) return <Navigate to="/w/home" replace />;
  }

  // 2. Role check
  if (world.requiredRoles.length > 0) {
    const hasRole = world.requiredRoles.some(r =>
      session.user.roles.includes(r)
    );
    if (!hasRole) return <Navigate to="/w/home" replace />;
  }

  // 3. PIN check (ShopFloor)
  if (world.requiresPin && !useShopFloorSession.getState().pinValidated) {
    return <Navigate to="/w/shopfloor/pin" replace />;
  }

  return <>{children}</>;
}
```

**Ugyanez a `<WorldHome>` és `<WorldSidebar>` komponensekre is vonatkozik** — `tenantLoaded` flag-re várva renderelnek tartalmat. DoD Playwright teszt: slow 3G simulation, network panel ellenőrzés (no FOUC).

**KRITIKUS:** ez **csak UX redirect**. A security boundary a BFF-en van (Section 5.6 + 5.7).

---

## 4. State management

### 4.1 Globális store-ok (D-08 hibrid)

| Store | Csomag | Státusz | Tartalom |
|---|---|---|---|
| `authStore` | app `stores/` | DEPLOYED (változatlan) | KC token, user, refresh, logout |
| `brandStore` | app `stores/` | DEPLOYED, **passzív v1** | Default skin betöltés; per-tenant override roadmap |
| `tenantStore` | app `stores/` | **ÚJ v1** | Tenant info, `enabledModules`, `tenantType` |
| `i18nStore` | `@spaceos/i18n` | DEPLOYED (változatlan) | Locale, hostname-based detection |

**`tenantStore` interface:**
```ts
interface TenantState {
  tenantId: string | null;
  tenantName: string;
  tenantType: 'Manufacturer' | 'PanelCutter' | 'Trader' | 'Logistics' | 'Installer' | 'EndCustomer';
  enabledModules: string[];
  brandSkin: string; // v1: always 'default'
  loaded: boolean;
  loadFromSession: () => Promise<void>;
}
```

**Persist policy:** `tenantStore` NEM persist — minden session start friss BFF lekéréssel (`/bff/api/me/session`). Frontend frozen decision: *"Zustand `persist` middleware MUST NOT persist sensitive auth state to localStorage."*

### 4.2 Per-world Zustand slice-ok

| Slice | World | Tartalom | Reset trigger |
|---|---|---|---|
| `useSalesOrderDraftStore` | sales | Order draft (in-progress configurator state, line item edits) | World unmount, order submit |
| `useProductionPlanStore` | production | Active plan filter, board column collapse state | World unmount |
| `useShopFloorSessionStore` | shopfloor | PIN validated, machine ID, current task ID, shift start time | PIN logout, end-of-shift, world unmount |
| `useSettingsUiStore` | settings | Aktív settings tab, audit filter | World unmount |

**Slice cleanup (world unmount):**
```tsx
useEffect(() => {
  return () => {
    // World unmount → reset domain slice
    useSalesOrderDraftStore.getState().reset();
  };
}, []);
```

### 4.3 TanStack Query — query key stratégia

**Konvenció:** `[worldId, resource, ...identifiers]`

**Δ FE-09:** Shop Floor live feed `refetchInterval` explicit (auto-update); `refetchIntervalInBackground: false` (battery).

| Key | Endpoint | Stale time | gcTime | Refetch interval |
|---|---|---|---|---|
| `['session']` | `/bff/api/me/session` | 5 min | 10 min | — |
| `['tenant']` | `/bff/api/tenant` | 10 min | 30 min | — |
| `['enabled-modules']` | (a session részeként) | 10 min | 30 min | — |
| `['sales', 'orders', filters]` | `/bff/api/orders?...` | 30 sec | 5 min | — |
| `['sales', 'order', orderId]` | `/bff/api/orders/:id` | 30 sec | 5 min | — |
| `['sales', 'configurator', productTemplateId, params]` | `/bff/api/configurator/:productTemplateId/configure` | 5 min | 10 min | — |
| `['sales', 'handshakes']` | `/bff/api/handshakes` | 30 sec | 5 min | — |
| `['production', 'cutting-plans', filters]` | `/bff/cutting/plans?...` | 30 sec | 5 min | — |
| `['production', 'plan', planId]` | `/bff/cutting/plans/:id` | 30 sec | 5 min | — |
| `['production', 'manufacturing', filters]` | `/bff/manufacturing/tasks?...` | 15 sec | 5 min | **30 sec** (FsmBoard) |
| `['production', 'task', taskId]` | `/bff/manufacturing/tasks/:id` | 15 sec | 5 min | — |
| `['shopfloor', 'tasks', machineId]` | `/bff/shopfloor/tasks?machineId=...` | 5 sec (live) | 1 min | **10 sec** (FE-09, csak foreground) |
| `['shopfloor', 'task', taskId]` | `/bff/shopfloor/task/:id` | 5 sec | 1 min | **10 sec** (csak foreground) |
| `['settings', 'users']` | `/bff/api/users` | 1 min | 10 min | — |
| `['settings', 'audit', filters]` | `/bff/api/audit?...` | 30 sec | 5 min | — |

**v3 backlog:** SSE/WebSocket upgrade Shop Floor + Manufacturing FSM board-ra; jelenlegi polling interim.

### 4.4 World unmount cache cleanup

```ts
// apps/joinerytech/src/worlds/useWorldCleanup.ts
function useWorldCleanup(worldId: string) {
  const queryClient = useQueryClient();
  useEffect(() => {
    return () => {
      // World unmount → kifutó request cancel + cache eviction
      queryClient.cancelQueries({ queryKey: [worldId] });
      // gcTime majd elsöpri, ezért removeQueries opcionális; UX-szempontból
      // ha gyors re-entry várható (pl. settings tab váltás), removeQueries TILOS.
    };
  }, [worldId, queryClient]);
}
```

### 4.5 Server vs. client state szétválasztás

| Típus | Tárolás | Példa |
|---|---|---|
| **Server state** | TanStack Query | Orders, plans, tasks, users, audit log |
| **Client UI state** | Zustand slice | Filter selections, sidebar collapsed?, active tab |
| **Form draft state** | Zustand slice + form library (react-hook-form) | Order draft, configurator inputs |
| **Auth/session state** | Zustand globális (`authStore`, `tenantStore`) | KC token, user, tenant |

**Anti-pattern (v1-ben TILOS):** server state Zustand-be tükrözése. Minden BFF-eredmény közvetlenül a Query cache-ben.

### 4.6 Mutation policy — optimistic vs invalidation (Δ FE-10)

| Mutáció | Stratégia | Indok |
|---|---|---|
| Shop Floor task status update | **Optimistic + rollback** | LIVE feel; ha BFF reject → rollback + toast error |
| Order header save (draft → submitted) | **Invalidation** | Server-side state machine; legitim hibák gyakoribbak |
| Order line item edit (configurator) | **Optimistic + rollback** | Folyamatos szerkesztés UX |
| Configurator: Graph engine recompute | **Invalidation** + spinner | Server-side számítás authoritative; nem érdemes optimistic |
| B2B Handshake accept/decline | **Invalidation** | Audit-relevant — nem optimistic |
| Audit log filter | **N/A** (read-only) | — |
| Admin user create/disable | **Invalidation** | Audit-relevant |
| Tenant info update | **Invalidation** | Confirmation pattern (toast + reload) |

**Pattern (sample, optimistic):**
```ts
useMutation({
  mutationFn: api.shopfloor.updateTaskStatus,
  onMutate: async ({ taskId, nextStatus }) => {
    await queryClient.cancelQueries({ queryKey: ['shopfloor', 'task', taskId] });
    const prev = queryClient.getQueryData(['shopfloor', 'task', taskId]);
    queryClient.setQueryData(['shopfloor', 'task', taskId], old => ({ ...old, status: nextStatus }));
    return { prev };
  },
  onError: (_err, { taskId }, ctx) => {
    queryClient.setQueryData(['shopfloor', 'task', taskId], ctx?.prev);
    toast.error('Státuszváltás sikertelen, visszaállítva.');
  },
  onSettled: ({ taskId }) => {
    queryClient.invalidateQueries({ queryKey: ['shopfloor', 'task', taskId] });
  },
});
```

### 4.7 Loading state taxonomy (Δ FE-12)

| Állapot | Megjelenés | Hol |
|---|---|---|
| **World chunk load** | Full skeleton screen (header+sidebar+content placeholder) | `<WorldLoadingScreen>`, Suspense fallback |
| **Heavy screen chunk load** | Partial skeleton (csak content area) | `<HeavyScreenLoading>`, nested Suspense |
| **Query initial load** | Component-level skeleton: list rows = `PagedTable` skeleton; detail = card skeleton; chart = placeholder | Az adott screen render-jén |
| **Query background refetch** | Subtle indikátor (top-bar 2px progress, vagy nav-item dot) | TanStack `isFetching` state |
| **Mutation pending** | Action button disabled + inline spinner; screen lock CSAK ha kritikus (PIN login, payment) | Lokális komponens-szint |
| **Polling refresh** | Nincs visible UI (Shop Floor task feed) — frissül in-place | Background |

### 4.8 Memoization stratégia (Δ FE-13)

| Komponens / use-case | Pattern |
|---|---|
| `<PagedTable>` row child komponens | `React.memo` + stabil row prop reference |
| `<ManufacturingFsmBoardScreen>` cell | Cell komponens `React.memo`; status-mapper `useMemo` |
| `<NestingVisualizerScreen>` canvas redraw | `useMemo` viewport calc; `useCallback` event handlers |
| Zustand selectors | Shallow compare (`useShallow` from zustand/react/shallow) |
| Heavy form (configurator) | `react-hook-form` controlled mode + per-field subscription (avoid re-render whole form) |
| `<WorldSidebar>` items | `useMemo` filtered worldCatalog (changes ritkán: enabledModules + roles) |

### 4.9 Draft persistence + leave confirmation (Δ FE-16)

`useSalesOrderDraftStore` és `useShopFloorSessionStore` kritikus: accidental nav → munkavesztés.

| Mechanizmus | Trigger | Tárolás |
|---|---|---|
| `useBlocker` (React Router v6) | World/route change, ha `isDirty` | In-memory; confirm dialog |
| `beforeunload` event listener | Tab close / browser back | Native browser confirm |
| Auto-save sessionStorage | 30 sec debounce ha `isDirty` | sessionStorage (NEM localStorage — security) |
| Manual "Save draft" button | Order detail toolbar | BFF (server-side draft entity, opcionális v3) |

**KRITIKUS (memóriából frozen):** persist middleware NEM tárolhat sensitive auth state-et. Draft-ban személyes adat (név, cím) lehet → ezért sessionStorage és NEM localStorage; tab-bezáráskor elveszik.

### 4.10 Auth refresh edge case + token storage hardening (Δ FE-17, SEC-FE-01, SEC-FE-06)

**Token storage policy (SEC-FE-01):**

```ts
// apps/joinerytech/src/auth/userManager.ts
import { UserManager, WebStorageStateStore, InMemoryWebStorage, Log } from 'oidc-client-ts';

export const userManager = new UserManager({
  authority: import.meta.env.VITE_KC_ISSUER,
  client_id: 'portal-app',
  redirect_uri: `${window.location.origin}/callback`,
  silent_redirect_uri: `${window.location.origin}/silent-callback`,
  post_logout_redirect_uri: `${window.location.origin}/logout`,
  response_type: 'code',
  scope: 'openid profile email',
  // SEC-FE-01: tokens in-memory ONLY — page reload = re-login (interim v1)
  userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
  // PKCE state ugyanabba a storage-be — nem szivárog localStorage-be
  stateStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
  automaticSilentRenew: true,
  monitorSession: false, // session-state iframe tilos (clickjacking + perf)
});

// SEC-FE-06: production console silence
if (import.meta.env.PROD) {
  Log.setLevel(Log.NONE);
}

// FE-17 + SEC-FE-04: silent renew failure → graceful redirect
userManager.events.addSilentRenewError((error) => {
  const currentPath = window.location.pathname + window.location.search;
  toast.warning(t('auth.session.expired'));
  navigate(`/login?returnTo=${encodeURIComponent(currentPath)}`, { replace: true });
});

// post-login callback handler (SEC-FE-12 — open redirect védelem)
const returnTo = searchParams.get('returnTo') ?? '/w/home';
const isSafeReturnTo = returnTo.startsWith('/')
  && !returnTo.startsWith('//')
  && !returnTo.includes('@')
  && returnTo.length < 200;
navigate(isSafeReturnTo ? returnTo : '/w/home', { replace: true });
```

**v2 horizon migration:** refresh_token httpOnly cookie pattern (BFF-managed session) — full page reload survives, attacker JS-ből nem hozzáférhető. Jelenleg v1 interim: in-memory only, page reload = re-login (DoD-ben dokumentálva).

### 4.11 Logout cleanup (Δ SEC-FE-04)

```ts
// apps/joinerytech/src/auth/logout.ts
export async function logout() {
  try {
    // 1. KC end-session (revoke refresh token server-side)
    await userManager.signoutRedirect({
      post_logout_redirect_uri: `${window.location.origin}/logout`,
    });
  } catch (e) {
    // server unreachable — folytatódik a kliens-oldali cleanup
  }
}

// `/logout` callback: kliens-oldali cleanup MINDEN store-ra
export function logoutCallback() {
  // 2. TanStack Query — minden cache törlés
  queryClient.clear();
  queryClient.removeQueries(); // explicit (clear() in-flight nem cancellál mindig)

  // 3. Zustand store reset — explicit list (NEM auto-discovery, hogy ne maradjon ki)
  useAuthStore.getState().reset();
  useTenantStore.getState().reset();
  useBrandStore.getState().reset();
  useSalesOrderDraftStore.getState().reset();
  useProductionPlanStore.getState().reset();
  useShopFloorSessionStore.getState().reset();
  useSettingsUiStore.getState().reset();

  // 4. sessionStorage purge (draft auto-save FE-16, sidebar collapse state)
  sessionStorage.clear();

  // 5. KC userManager local signout (dupla biztosítás)
  userManager.removeUser();

  // 6. v2 horizon: caches.delete(...) ha service worker
  // if ('caches' in window) await caches.keys().then(keys => keys.forEach(k => caches.delete(k)));

  // 7. Redirect login
  window.location.replace('/login');
}
```

**Vitest teszt (`logout.test.ts`):** post-logout assertion 7 store mind reset state-ben; queryClient cache 0 entry; sessionStorage üres.

### 4.12 Idle timeout (Δ SEC-FE-05)

**Standard portál (Sales/Production/Settings):**

```ts
// apps/joinerytech/src/auth/useIdleTimer.ts
const IDLE_LIMIT_MS = 15 * 60 * 1000;       // 15 min no input
const WARNING_BEFORE_MS = 60 * 1000;        // 60 sec countdown warning

export function useIdleTimer() {
  useEffect(() => {
    let timeoutId: number;
    let warningId: number;

    const reset = () => {
      clearTimeout(timeoutId);
      clearTimeout(warningId);
      warningId = window.setTimeout(() => {
        toast.warning(t('auth.idle.warning'), { duration: WARNING_BEFORE_MS });
      }, IDLE_LIMIT_MS - WARNING_BEFORE_MS);
      timeoutId = window.setTimeout(() => logout(), IDLE_LIMIT_MS);
    };

    const events = ['mousedown', 'keydown', 'touchstart', 'scroll'];
    events.forEach(e => window.addEventListener(e, reset, { passive: true }));
    reset();

    return () => {
      clearTimeout(timeoutId);
      clearTimeout(warningId);
      events.forEach(e => window.removeEventListener(e, reset));
    };
  }, []);
}
```

**Shop Floor (külön policy):**
- 5 perc no-touch → PIN re-entry screen (NEM full logout, machine session marad)
- Machine session TTL: shift-end vagy 12 óra (whichever first); admin-revocable
- Idle warning hangos pulzus (audio cue, ipari környezet) opcionális v2

### 4.13 Cross-tenant defense-in-depth (Δ SEC-FE-17)

```ts
// apps/joinerytech/src/api/assertSameTenant.ts
export function assertSameTenant<T extends { tenantId?: string }>(
  response: T,
  expectedTenantId: string,
): T {
  if (response.tenantId && response.tenantId !== expectedTenantId) {
    // SEC-FE-17: defense-in-depth — RLS már megvéd, de ez extra réteg
    console.error('Cross-tenant data detected', { expected: expectedTenantId });
    // Audit endpoint: BFF-en log-olni, NEM ide
    fetch('/bff/api/audit/client-anomaly', {
      method: 'POST',
      body: JSON.stringify({ type: 'cross_tenant_response', expected: expectedTenantId }),
      keepalive: true,
    }).catch(() => {});
    throw new CrossTenantError();
  }
  return response;
}

// Használat (TanStack Query queryFn-ben):
const { data } = useQuery({
  queryKey: ['sales', 'order', orderId],
  queryFn: async () => {
    const res = await api.orders.get(orderId);
    return assertSameTenant(res, useTenantStore.getState().tenantId!);
  },
});
```

**ErrorBoundary `CrossTenantError` catch:** redirect `/w/home` + toast `t('error.dataMismatch')` (NEM részletes); audit log automatikusan a fenti POST-ban.

---

## 5. BFF API surface

### 5.1 Meglévő BFF route-ok (Codebase Status szerint)

| Route prefix | Backend | Port | Státusz |
|---|---|---|---|
| `/bff/api/*` | Kernel | 5000 | DEPLOYED |
| `/bff/joinery/*` | Joinery (deprecated → Door modulra vándorol) | 5002 | DEPLOYED |
| `/bff/cutting/*` | Cutting | 5005 | DEPLOYED |

### 5.2 Új BFF route-ok v1-hez

| Route | Backend | Port | Cél | Auth |
|---|---|---|---|---|
| `GET /bff/api/me/session` | Kernel | 5000 | User + tenant + enabledModules + roles + brand_skin (mindig 'default' v1) egy hívásban | KC user |
| `GET /bff/api/me/home-state` | Kernel | 5000 | Home auto-redirect logikához (primary world derive) | KC user |
| `GET /bff/api/tenant` | Kernel | 5000 | Tenant detail + enabled modules | KC user |
| `GET /bff/api/audit` | Kernel | 5000 | Audit log (Hash chain) | KC user, role: admin |
| `GET /bff/api/users` | Kernel | 5000 | User list | KC user, role: admin |
| **`GET /bff/api/configurator/:productTemplateId/configure`** *(Δ BFF-01 fix)* | Kernel | 5000 | Graph engine view (Kernel hívja in-process Abstractions NuGet-et) | KC user |
| **`GET /bff/api/orders/:id/full`** *(Δ BFF-03 aggregátor)* | Kernel | 5000 | Order header + lines + handshake state + linked configurator state egy hívásban | KC user |
| `/bff/manufacturing/*` | Manufacturing | 5007 | Phase 1 endpoints (EdgeBanding, CNC, Order saga) | KC user |
| **`GET /bff/manufacturing/tasks/:id/full`** *(Δ BFF-03 aggregátor)* | Manufacturing | 5007 | Task + transitions + assignments egy hívásban | KC user |
| **`GET /bff/cutting/plans/:id/full`** *(Δ BFF-03 aggregátor)* | Cutting | 5005 | Plan header + items + offcuts egy hívásban | KC user |
| `POST /bff/shopfloor/pin/login` | Manufacturing | 5007 | PIN auth → machine token | Anonymous + machine fingerprint |
| `POST /bff/shopfloor/pin/logout` | Manufacturing | 5007 | Machine session revoke | Machine token |
| `GET /bff/shopfloor/tasks` | Manufacturing | 5007 | Machine-scoped task feed | Machine token |
| `PATCH /bff/shopfloor/task/:id/status` | Manufacturing | 5007 | Task FSM transition | Machine token |
| **`GET /bff/health`** *(Δ BFF-05)* | Orchestrator | (önmaga) | Downstream service health aggregate | KC user, role: admin |

**Δ BFF-01:** `/bff/abstractions/*` **eltávolítva** — Abstractions NuGet-only library, nincs HTTP service. A configurator a Kernel BFF route-on át megy.

### 5.3 Aggregáló endpoint-ok (v1 minimum)

**Δ BFF-03:** Detail screen-ek N+1 fetch redukciójára `*/full` aggregátorok.

| Endpoint | Aggregáció | Cél |
|---|---|---|
| `/bff/api/me/session` | Kernel: user + tenant + enabledModules + roles | Avoid 4 round-trip a kezdő render-en |
| `/bff/api/me/home-state` | enabledModules + roles + recentActivity (opcionális v2) | Auto-redirect logika |
| **`/bff/api/orders/:id/full`** | Kernel: order header + lines + handshake state + configurator graph state | OrderDetailScreen 4+ fetch egy hívásban |
| **`/bff/manufacturing/tasks/:id/full`** | Manufacturing: task + transitions + assignments + machine info | ManufacturingTaskDetailScreen |
| **`/bff/cutting/plans/:id/full`** | Cutting: plan header + line items + offcuts + nesting result (ha kész) | CuttingPlanDetailScreen |

**Roadmap (v2-v3):**
- `/bff/dashboard/kpi` — cross-world KPI aggregátor (Cabinet 0.3 Federation pattern alapján)

### 5.4 OpenAPI codegen bővítés

**Δ BFF-02:** Abstractions snapshot törölve (NuGet-only); Manufacturing snapshot contract-freeze prereq.

| Service | Snapshot fájl | v1 hatás |
|---|---|---|
| Kernel | `@spaceos/api-client/snapshots/kernel.json` | Kibővítendő: `/me/session`, `/me/home-state`, `/audit`, `/users`, `/configurator/:id/configure`, `/orders/:id/full`, `/health` |
| Cutting | `@spaceos/api-client/snapshots/cutting.json` | Kibővítendő: `/plans/:id/full` |
| Manufacturing | `@spaceos/api-client/snapshots/manufacturing.json` | **ÚJ snapshot — contract-freeze prereq Track F-en** (csak akkor commit-olható, ha a service production-candidate; staging deploy szükséges) |
| ~~Abstractions~~ | ~~snapshot~~ | ❌ NuGet-only library, nincs HTTP endpoint — törölve |

**Frozen decision (memóriából):** *"OpenAPI codegen from committed snapshot — never runtime schema discovery."* Minden új endpoint snapshot regen + commit.

**Δ BFF-16:** CI job `pnpm codegen:check` — pull request blocker, ha a Kernel/Cutting/Manufacturing OpenAPI változott, de a commit-olt snapshot nem; lokális regen szükséges.

### 5.5 Authorization & error handling

#### Server-side authorization (KRITIKUS — Section 2.3 mirror)

Minden új BFF route Express middleware láncot implementál:

```
1. authMiddleware       — KC JWT verify (vagy machine token Shop Floor-on)
2. tenantMiddleware     — tenantId injection a request-be
3. enabledModulesGuard  — route prefix → required module mapping
4. roleGuard (opcionális) — role check ha route admin-only
```

**Mapping tábla (Orchestrator config):**

```ts
const routeModuleMap = {
  '/bff/cutting/*':                          { requiredModules: ['cutting'] },
  '/bff/manufacturing/*':                    { requiredModules: ['cutting'] },
  '/bff/api/configurator/*':                 { requiredModules: ['door', 'cabinet'] }, // bármelyik elég
  '/bff/api/orders/*':                       { requiredModules: ['door', 'cabinet'] }, // bármelyik elég
  '/bff/shopfloor/*':                        { requiredModules: ['cutting'], authMode: 'machine' },
};
```

Ha a tenant `enabledModules` nem tartalmazza a route módulát → **403 Forbidden** + audit log entry.

#### Error response format

Egységes formátum (Orchestrator already deployed):
```json
{
  "error": {
    "code": "FORBIDDEN_MODULE",
    "message": "Tenant does not have 'cutting' module enabled",
    "details": { "module": "cutting", "tenantId": "..." }
  }
}
```

#### Retry / timeout policy

| Kategória | Timeout | Retry | Megjegyzés |
|---|---|---|---|
| GET (egyszerű) | 10 sec | 3 (exponential backoff) | TanStack Query default |
| GET (aggregátor) | 30 sec | 1 | `/me/session`, `/me/home-state` |
| POST/PATCH/DELETE | 15 sec | **0** | Mutáció — soha nem retry-olunk silently |
| Shop Floor task fetch | 10 sec | 3 | Eli rosszul tűrt hálózat → 5 sec stale time önmagában is jó |
| PIN login | 10 sec | 0 | Ne retry-oljunk auth-ot (rate limit védi a backendet) |

#### Cache stratégia

- **TanStack Query:** in-memory (Section 4.3 stale/gc time)
- **Service Worker cache:** **NEM v1** — service worker → roadmap (offline Shop Floor scenario)
- **HTTP cache:** Orchestrator nem küld `Cache-Control: public` semelyik `/bff/*` válaszra (security)

### 5.6 Security trust boundary + anti-pattern lista (Δ SEC-FE-02, SEC-FE-03, SEC-FE-09, SEC-FE-11, SEC-FE-13, SEC-FE-15)

#### Single source of truth — `enabledModules` + role (SEC-FE-03)

```
                         ┌──────────────────────────────────┐
                         │  Orchestrator                    │
                         │  enabledModulesService           │
                         │  .getForTenant(tenantId)         │  ◄── SINGLE SOURCE
                         └──────────────────────────────────┘
                              │                          │
              ┌───────────────┘                          └──────────────┐
              ▼                                                          ▼
   ┌──────────────────────┐                              ┌──────────────────────────┐
   │ /bff/api/me/session  │                              │ Route guard middleware   │
   │ (response body)      │                              │ /bff/{module}/*          │
   │ → tenantStore (FE)   │                              │ (server-side enforce)    │
   └──────────────────────┘                              └──────────────────────────┘
              │                                                          │
              ▼                                                          ▼
   FE: <WorldGuard> redirect (UX only)              BFF: 403 Forbidden ha mismatch
   FE: <WorldSidebar> nav rendering                 BFF: audit log entry
```

**Garancia:** a két hely (FE display + BFF enforce) fizikailag ugyanazt az `enabledModulesService` hívást használja. Nincs külön JSON config két helyen → nincs drift.

#### Anti-pattern lista — TILOS v1-ben (DoD CI gate)

| Anti-pattern | Miért tilos | Detection |
|---|---|---|
| `jwtDecode(accessToken)` business döntéshez | SEC-FE-02 — JWT trust boundary; minden auth-derived state `/me/session`-ből | ESLint `no-restricted-imports: ['jwt-decode', 'jose']`; CI grep |
| `dangerouslySetInnerHTML={{...}}` | SEC-FE-11 — XSS vektor | ESLint `react/no-danger: error`; CI grep `dangerouslySetInnerHTML` → 0 match |
| `eval()` / `new Function()` | SEC-FE-13 — kódvégrehajtás | ESLint `no-eval: error`, `no-new-func: error` |
| `localStorage.setItem(token...)` / refresh token in storage | SEC-FE-01 — XSS-exfiltrálható | ESLint custom rule + Zustand `persist` deny-list |
| Server `error.message` direct toast/UI render | SEC-FE-15 — info leak | Code review + Vitest pattern test (toast.error mindig i18n key-vel) |
| `console.log()` production build | SEC-FE-16 — token/data leak | `vite-plugin-remove-console`; CI prod bundle grep |
| Inline `<script>` / inline `<style>` | SEC-FE-14 — CSP `unsafe-inline` szükségletet támaszt | ESLint + Vite production build inline → external |
| URL paraméter passthrough validáció nélkül | SEC-FE-12 — spoof, hibaüzenet leak | Zod schema parse minden `useSearchParams`-on |
| Cross-tenant response render assertion nélkül | SEC-FE-17 — defense-in-depth | `assertSameTenant` minden BFF response queryFn-ben |
| KC SDK debug log production-ben | SEC-FE-06 — code_verifier exposure | `Log.setLevel(Log.NONE)` ha `import.meta.env.PROD` |

#### Role-gated BFF endpoints — explicit lista (SEC-FE-09)

| BFF endpoint | Required role | Audit log on access |
|---|---|---|
| `GET /bff/api/users` | `admin` | Yes |
| `POST /bff/api/users/:id/disable` | `admin` | Yes |
| `GET /bff/api/audit` | `admin` (vagy `auditor` ha létezik) | No (audit log self-read) |
| `GET /bff/api/tenant` | bárki authenticated | No |
| `PATCH /bff/api/tenant` | `admin` | Yes |
| `GET /bff/api/orders` | `sales-rep`, `admin` | No |
| `POST /bff/api/orders` | `sales-rep`, `admin` | Yes |
| `GET /bff/manufacturing/tasks` | `production-lead`, `admin` | No |
| `PATCH /bff/manufacturing/tasks/:id` | `production-lead`, `admin` | Yes |
| `POST /bff/shopfloor/pin/login` | (machine fingerprint) | Yes (every attempt — invalid PIN audit) |
| `GET /bff/shopfloor/tasks` | (machine token + scope `shopfloor:task:read`) | No |
| `PATCH /bff/shopfloor/task/:id/status` | (machine token + scope `shopfloor:task:status:write`) | Yes |

**Frontend `<WorldGuard>` UX redirect — NEM helyettesíti.** Minden role-gated endpoint backend-en `roleGuard` middleware-rel; nem-jogosult → 403 + audit (a fenti tábla "Yes" oszlopa).

#### Error i18n kulcsok (SEC-FE-15)

```
common.error.generic          = "Hiba történt"   / "Something went wrong"
common.error.network          = "Hálózati hiba" / "Network error"
common.error.forbidden        = "Hozzáférés megtagadva" / "Access denied"
common.error.notFound         = "Nincs ilyen elem" / "Not found"
common.error.dataMismatch     = "Adathiba — frissítsd az oldalt" / "Data mismatch — please refresh"
common.error.sessionExpired   = "Munkamenet lejárt" / "Session expired"
common.error.code.{ERROR_CODE} = (per-code lokalizáció ahol szükséges)
```

**Toaster pattern:**
```ts
const message = t(`common.error.code.${errorCode}`, t('common.error.generic'));
toast.error(message);
// import.meta.env.DEV-ben opcionálisan: console.debug('Server detail:', error.message);
```

### 5.7 Shop Floor session isolation (Δ SEC-FE-08, SEC-FE-10)

#### Kiosk mode detection (SEC-FE-08)

A v1 interim megoldás: **ugyanaz a build, KÜLÖN router config kiosk mode-ban**.

```ts
// apps/joinerytech/src/main.tsx
const isKiosk = (() => {
  // 1. URL paraméter (admin által beállított boot URL)
  const urlMode = new URLSearchParams(window.location.search).get('mode');
  if (urlMode === 'shopfloor') {
    localStorage.setItem('shopfloor_kiosk', 'true');
    return true;
  }
  // 2. Persisted flag (admin által set-elt tablet)
  return localStorage.getItem('shopfloor_kiosk') === 'true';
})();

// Két különböző router shape — kioszkban a portál többi route-ja NEM regisztrálódik
const router = isKiosk ? createShopFloorRouter() : createPortalRouter();

// Kiosk router KIZÁRÓLAG /w/shopfloor/* + /login(machine)/* route-okat tartalmaz.
// /w/home, /w/sales, /w/production, /w/settings teljesen hiányzik a router-ből
// → URL spoof esetén nincs match → globális 404 (NEM a portál home).
```

**Garancia:** ha kiosk tabletten valaki megpróbál `/w/sales`-re navigálni, a router-ben nincs ilyen route → 404 → nincs information leak a portál többi világáról.

**v2 horizon (D-07 roadmap):** külön Vite app entry-point (`apps/shopfloor`), külön build, külön bundle, külön CSP. Akkor az `apps/joinerytech` build véglegesen NEM tartalmazza a `/w/shopfloor/*` route-okat.

#### Machine token scope minimization (SEC-FE-10)

Machine token claims (BFF issued, opaque vagy JWT):

```json
{
  "tenantId": "doorstar",
  "machineId": "edge-banding-01",
  "scopes": [
    "shopfloor:task:read",
    "shopfloor:task:status:write"
  ],
  "issuedAt": "2026-04-29T07:00:00Z",
  "expiresAt": "2026-04-29T19:00:00Z",
  "boundFingerprint": "sha256:..."
}
```

**Scope check táblázat (Orchestrator middleware):**

| BFF endpoint | Required scope |
|---|---|
| `GET /bff/shopfloor/tasks?machineId=X` | `shopfloor:task:read` + `machineId === token.machineId` |
| `GET /bff/shopfloor/task/:id` | `shopfloor:task:read` + task assigned to `token.machineId` |
| `PATCH /bff/shopfloor/task/:id/status` | `shopfloor:task:status:write` + task assigned to `token.machineId` |
| `/bff/api/*` (pl. orders, users, tenant) | **NEM accessible** machine tokennel — KC user token required |

**Backend enforcement:** ha machine token kísérli a `/bff/api/orders` endpointot → 403 + audit log entry (`unauthorized_scope_access`).

#### PIN brute-force védelem (SEC-FE — meglévő DoD)

| Aspect | Policy |
|---|---|
| Max próbálkozás | 5 invalid PIN |
| Lockout idő | 5 perc per machine fingerprint |
| Audit log | MINDEN invalid PIN (sikeres és sikertelen egyaránt) |
| Lockout után | Toast warning + admin contact CTA; no automatic retry |
| PIN minimum | 6 számjegy (frozen) |
| PIN rotation | Negyedéves; admin által kényszerítve (BFF endpoint) |

#### Tablet capture / device theft scenario

| Védelem | Mechanizmus |
|---|---|
| Machine token TTL | 12 óra max + shift-end revoke |
| Admin revoke endpoint | `POST /bff/api/admin/machines/:id/revoke` (KC admin user) — instant token invalidation BFF-szintű revocation list-en |
| Fingerprint binding | Machine token `boundFingerprint` (UA + screen + canvas) — ha eltér → token reject |
| Offline mode | **NINCS** v1-ben (sub-senior-security explicit elvárás) — capability nélkül = nincs offline data leak |
| Camera/photo | **NINCS** v1-ben — proof-of-completion fotó v2 horizon, csak akkor MIME check + max 5MB |

### 5.8 BFF infrastructure patterns (Δ BFF-06..16)

#### 5.8.1 JWT forwarding + tenant header (BFF-06)

```ts
// orchestrator/src/middleware/forwardAuth.ts
export function buildDownstreamHeaders(req: Request): HeadersInit {
  const userJwt = req.headers.authorization;          // 'Bearer <user_jwt>'
  const tenantId = req.tenantContext.tenantId;        // tenantMiddleware által set-elt
  const requestId = req.requestId;                    // korreláció

  return {
    'Authorization': userJwt!,                        // user JWT forwardolva (NEM service account)
    'X-Tenant-Id': tenantId,                          // defense-in-depth header
    'X-Request-Id': requestId,                        // tracing
    'X-Forwarded-For': req.ip,
  };
}
```

**Downstream service (Kernel/Cutting/Manufacturing) elvárások:**
1. Validálja a JWT-t (signature, exp, issuer = KC).
2. Cross-check: `jwt.tenant_id === X-Tenant-Id`. Mismatch → 403 + audit (`tenant_header_mismatch`).
3. RLS / explicit `WHERE tenant_id = X-Tenant-Id` query-szinten.

#### 5.8.2 Rate limiting (BFF-07)

| Kategória | Limit | Példák |
|---|---|---|
| Aggregátor | 60 req/min/user | `/me/session`, `/orders/:id/full`, `/tasks/:id/full`, `/plans/:id/full` |
| Lista | 120 req/min/user | `/orders`, `/cutting/plans`, `/manufacturing/tasks`, `/audit` |
| Detail GET | 240 req/min/user | `/orders/:id`, `/users`, `/tenant` |
| Mutation | 30 req/min/user | POST/PATCH/DELETE bármi |
| PIN login | 5 req / 5min / machine fingerprint | `/bff/shopfloor/pin/login` (SEC-FE-08 mirror) |
| Health | 60 req/min/IP | `/bff/health` (CI/CD load |

**Implementáció:** `express-rate-limit` Redis store-ral; Orchestrator már kapcsolódik Redis-hez. 429 response: `Retry-After` header (másodperc), body `{ error: { code: 'RATE_LIMITED', retryAfter: number } }`.

#### 5.8.3 Request body validation (BFF-08)

**Pass-through alapelv:** BFF NEM duplikálja a backend validation-t.

**Kivételek (BFF-szintű validáció kötelező):**

| Endpoint | Validáció | Indok |
|---|---|---|
| `POST /bff/shopfloor/pin/login` | `length === 6 && /^\d{6}$/` | PIN-t ne juttassuk SQL-near rétegig invalid formával (DoS védelem + cleaner audit) |
| Minden file upload (v2 horizon) | `Content-Length < 5MB`, `Content-Type` whitelist | Memory-/storage exhaustion előtt |
| Minden body-s endpoint | `Content-Length < 1MB` (alapértelmezetten) | Generic flood védelem |

Backend mindig revalidates. A BFF validation csak gate, nem business logic.

#### 5.8.4 Error response normalizer (BFF-10)

Minden downstream response error normalizáló middleware-en megy át:

```ts
// ASP.NET ProblemDetails → unified format
function normalizeError(downstreamResponse: any, downstreamService: string) {
  // Kernel/Manufacturing (ASP.NET): ProblemDetails shape
  if (downstreamResponse.type && downstreamResponse.title) {
    const code = extractCodeFromTypeUri(downstreamResponse.type)
                 ?? `${downstreamService.toUpperCase()}_ERROR`;
    return {
      error: {
        code,                                          // pl. "ORDER_NOT_FOUND"
        message: downstreamResponse.title,             // user-safe message
        // detail / stack / instance: SOHA NEM továbbítva (SEC-FE-15 mirror)
      }
    };
  }
  // Cutting (Node) — már unified format-ban érkezik
  return downstreamResponse;
}
```

**Production safety:** csak DEV environment-ben adunk vissza `details` mezőt; production-ben strip.

#### 5.8.5 Circuit breaker (BFF-11)

```ts
// orchestrator/src/proxy/circuitBreaker.ts
import CircuitBreaker from 'opossum';

const breakers = {
  manufacturing: new CircuitBreaker(callManufacturing, {
    timeout: 10000,
    errorThresholdPercentage: 50,
    resetTimeout: 30000,                  // 30 sec OPEN állapot után HALF_OPEN
    rollingCountTimeout: 60000,           // 1 perc rolling window
    rollingCountBuckets: 6,
  }),
  cutting: /* ... */,
  kernel: /* ... */,                      // kernel-re is, de tolerancia magasabb (core service)
};

breakers.manufacturing.on('open', () => {
  logger.warn('Circuit OPEN: manufacturing');
  metrics.increment('circuit_open', { service: 'manufacturing' });
});

// fallback response 503 + retry hint
breakers.manufacturing.fallback(() => ({
  status: 503,
  body: { error: { code: 'SERVICE_UNAVAILABLE', service: 'manufacturing', retryAfter: 30 } }
}));
```

**Frontend reakció:** toast `t('error.serviceDown.{service}')`; TanStack Query nem retry-ol 503-ra (BFF-12 mirror); after `retryAfter` sec felhasználó manuális retry-t indíthat.

#### 5.8.6 Timeout per endpoint type (BFF-12 expanded)

| Kategória | Timeout | Retry | Megjegyzés |
|---|---|---|---|
| GET egyszerű | 10 sec | 3 (exponential backoff TanStack-ben) | TanStack Query default |
| GET aggregátor | 30 sec | 1 | `/me/session`, `/orders/:id/full`, etc. |
| POST/PATCH/DELETE | 15 sec | **0** | Mutáció — soha nem retry-olunk silently |
| Shop Floor task fetch | 10 sec | 3 | 5 sec stale time önmagában is jó |
| PIN login | 10 sec | 0 | Ne retry-oljunk auth-ot (rate limit védi a backendet) |
| File upload (v2 horizon) | 120 sec | 0 | Nagy payload, manual retry |
| SSE/WS connection (v2 horizon) | 5 min idle | n/a | Heartbeat ping minden 30 sec |
| Health check | 5 sec | 1 | Gyors válaszidő igény |

#### 5.8.7 Response shape consistency (BFF-13)

```ts
// Lista
{ "data": Order[], "meta": { "page": 1, "pageSize": 25, "total": 142, "hasNext": true } }

// Detail / aggregátor
{ "data": OrderFull }

// Mutation
{ "data": Order }

// Error (any)
{ "error": { "code": "ORDER_NOT_FOUND", "message": "Order not found" } }
```

**Kivétel:** `/bff/api/me/session` (root object, historikus — frontend ezzel számol).

Frontend Zod schema standard a `@spaceos/api-client` codegen-ben — minden endpoint Zod-validated parse, mismatch → throw + audit (defense-in-depth).

#### 5.8.8 Cache-Control policy (BFF-15)

| Endpoint kategória | Header | Megjegyzés |
|---|---|---|
| `/bff/api/me/*` | `Cache-Control: no-store` | Session-érzékeny |
| `/bff/api/orders/*`, `/bff/cutting/*`, `/bff/manufacturing/*` | `Cache-Control: no-store` | Üzleti adatok |
| `/bff/api/audit` | `Cache-Control: no-store` | Audit log szigorú |
| `/bff/api/reference/*` (machines, error-codes) | `Cache-Control: private, max-age=300, must-revalidate` | Lassan változó referencia |
| `/bff/health` | `Cache-Control: no-cache, max-age=10` | Aggregált, gyors válasz, friss |
| Minden GET | + `ETag` | 304 Not Modified `If-None-Match` esetén |

#### 5.8.9 Pagination defaults (BFF-14)

| Lista endpoint | Default page size | Max page size |
|---|---|---|
| `/orders`, `/cutting/plans`, `/manufacturing/tasks`, `/users`, `/handshakes` | 25 | 100 |
| `/audit` | 50 | 200 (HashChain page-elhetőség kedvéért) |

Backend kötelezően honorálja, default fallback ha hiányzik a query string-ből. Frontend `<PagedTable>` `pageSize=25` default-tal indul.

#### 5.8.10 Structured logging (BFF-09)

```ts
// orchestrator pino config
logger.info({
  requestId,
  userId,
  tenantId,
  route: '/bff/api/orders/:id/full',
  method: 'GET',
  status: 200,
  duration_ms: 142,
}, 'request_completed');
```

**SOHA NEM logolt:** request body (PII), response body (volume), `Authorization` header, `Cookie` header, PIN, JWT teljes formában.

**LOGOLT:** `requestId` (X-Request-Id), `userId` (JWT sub), `tenantId`, route, method, status, duration_ms, error code (ha 4xx/5xx).

---

## 6. i18n & Brand

### 6.1 Új fordítási kulcsok (HU/EN)

**Namespace konvenció:** `worlds.{worldId}.{key}` és `nav.{worldId}.{key}` és `common.*`.

| Kulcs | EN | HU |
|---|---|---|
| `worlds.home.title` | Home | Kezdőlap |
| `worlds.home.welcome` | Welcome, {name} | Üdvözöllek, {name} |
| `worlds.home.cards.sales` | Sales | Értékesítés |
| `worlds.home.cards.production` | Production | Gyártás |
| `worlds.home.cards.shopfloor` | Shop Floor | Műhely |
| `worlds.home.cards.settings` | Settings | Beállítások |
| `worlds.sales.title` | Sales | Értékesítés |
| `worlds.sales.nav.orders` | Orders | Rendelések |
| `worlds.sales.nav.handshakes` | B2B Handshakes | B2B kapcsolatok |
| `worlds.production.title` | Production | Gyártás |
| `worlds.production.nav.cuttingPlan` | Cutting Plan | Szabászati terv |
| `worlds.production.nav.manufacturing` | Manufacturing | Gyártásirányítás |
| `worlds.shopfloor.title` | Shop Floor Terminal | Műhely terminál |
| `worlds.shopfloor.pin.title` | Enter PIN | PIN bevitele |
| `worlds.shopfloor.pin.invalid` | Invalid PIN | Hibás PIN |
| `worlds.shopfloor.pin.locked` | Locked. Contact supervisor. | Zárolt. Hívd a műszakvezetőt. |
| `worlds.shopfloor.tasks.empty` | No tasks assigned | Nincs aktív feladat |
| `worlds.settings.title` | Settings | Beállítások |
| `worlds.settings.nav.tenant` | Tenant | Cégadatok |
| `worlds.settings.nav.users` | Users | Felhasználók |
| `worlds.settings.nav.audit` | Audit Log | Naplók |
| `common.loading` | Loading… | Betöltés… |
| `common.error.generic` | Something went wrong | Hiba történt |
| `common.error.network` | Network error | Hálózati hiba |
| `common.error.forbidden` | Access denied | Hozzáférés megtagadva |
| `common.error.notFound` | Not found | Nincs ilyen elem |
| `common.error.dataMismatch` *(SEC-FE-17)* | Data mismatch — please refresh | Adathiba — frissítsd az oldalt |
| `common.error.sessionExpired` *(SEC-FE-05)* | Session expired | Munkamenet lejárt |
| `auth.session.expired` *(FE-17)* | Your session has expired. Please sign in again. | A munkameneted lejárt. Jelentkezz be újra. |
| `auth.idle.warning` *(SEC-FE-05)* | You will be signed out in 60 seconds due to inactivity. | Inaktivitás miatt 60 másodperc múlva kijelentkeztetünk. |
| `common.action.retry` | Retry | Újrapróbál |
| `common.action.cancel` | Cancel | Mégse |
| `common.action.save` | Save | Mentés |
| `nav.user.profile` | Profile | Profil |
| `nav.user.logout` | Sign out | Kijelentkezés |

### 6.2 Brand token bővítés

**v1: NINCS új brand token.** D-02 LOCKED — egy közös moduláris portál, default skin (JoineryTech `@spaceos/brand-tokens` package). A `brandStore` betöltéskor mindig `'default'`-ra esik vissza, a tenant `brand_skin` mezőt nem alkalmazza runtime-ban.

**Roadmap (Section 11):**
- v2: per-tenant brand skin override layer aktiválás (`Doorstar` skin létezik, csak nincs activated)
- v2: hostname-based brand selection ha külön domain-ek brand-eltérnek

### 6.3 Locale hot-swap + format (Δ FE-24, FE-25)

**Hot-swap (no page reload):**
- `i18nStore.setLocale(next)` → React re-render az új namespace-szel
- Translation namespace per locale split (Vite import.meta.glob), lazy load: csak az aktuális + fallback
- User választás → `/bff/api/me/preferences` PATCH (BFF stores), következő login már perzisztált
- v1 fallback: hostname-based detection (`joinerytech.hu` → EN, `asztalostech.hu` → HU); user override előre

**Format (Intl API):**
```ts
// @spaceos/i18n/useLocaleFormat.ts (FE-25)
export function useLocaleFormat() {
  const { locale } = useI18nStore();
  return useMemo(() => ({
    date: (d: Date | string) => new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(d)),
    dateTime: (d: Date | string) => new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(d)),
    number: (n: number, opts?: Intl.NumberFormatOptions) => new Intl.NumberFormat(locale, opts).format(n),
    currency: (n: number, ccy = 'HUF') => new Intl.NumberFormat(locale, { style: 'currency', currency: ccy }).format(n),
  }), [locale]);
}
```

---

## 7. Responsive & Accessibility

### 7.1 Breakpoint stratégia

| Breakpoint | Szélesség | Cél | World viselkedés |
|---|---|---|---|
| Desktop | ≥ 1280px | Primary | Sidebar nyitva, multi-column layout |
| Tablet | 768-1279px | Adapted | Sidebar drawer-ré válik (toggle), single-column tartalom |
| Mobile | < 768px | Limited support v1-ben | Home, Settings olvasható; Sales/Production "use desktop" warning + degradált view; Shop Floor optimalizált touch UI |

**Tailwind config konvenció (változatlan):** `sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px`. World shell max szélesség: `2xl: 1536px` content-area.

### 7.2 a11y követelmények (WCAG 2.1 AA minimum)

**Δ FE-22:** Semantic landmarks explicit; **Δ FE-18:** focus trap modal-okban kötelező.

| Terület | Minimum |
|---|---|
| **Színkontraszt** | 4.5:1 normál szöveg, 3:1 nagy szöveg |
| **Keyboard nav** | Minden interaktív elem Tab-elérhető; sidebar, modal, dropdown |
| **Focus management** | Route change után focus a `<h1>`-re VAGY skip-link-re |
| **Focus trap** *(FE-18)* | Modal/Dialog-ban kötelező: első/utolsó tabbable elem között ciklikus Tab; Esc bezár; return focus az opening trigger-re |
| **Form labels** | Minden input `<label>` + `aria-describedby` hibaüzenetre |
| **Live regions** | Toast (`<Toaster>` FE-11) `aria-live="polite"`; kritikus hiba `aria-live="assertive"` |
| **Skip-to-content** | Layout első elem `<a href="#main">` |
| **Heading hierarchy** | World screen: 1× `<h1>`, max 3 szint |
| **Image alt** | Minden non-decorative kép `alt=`; ikonok `aria-hidden="true"` ha label mellett |
| **Semantic landmarks** *(FE-22)* | `<header>` topbar, `<nav aria-label="Sidebar">` sidebar, `<main>` content area, `<aside>` opcionális segéd-panel |
| **ARIA labels icon-only** *(FE-22)* | Sidebar collapse, user menu, action button → `aria-label`; ikon next-to-text → `aria-hidden="true"` az SVG-n |

**Lighthouse Accessibility gate:** ≥ 95 (DoD).

### 7.3 Touch target minimumok

| Komponens | Min méret | Megjegyzés |
|---|---|---|
| `WorldNavItem` (sidebar) | 44×44px | WCAG 2.1 AA target size |
| `WorldCard` (home) | 200×200px | nagy kattintási felület |
| **Shop Floor minden interaktív** | **64×64px** | ipari környezet, esetleges kesztyű |
| `PinKeypad` gombok | 80×80px | Touch keypad |
| `LargeTouchButton` (új `@spaceos/ui`) | 64×64px default, 80×80px Shop Floor variant | — |
| Standard portál gombok | 40×40px | desktop UX |

---

## 8. Test stratégia

### 8.1 Vitest unit tesztek

**Δ v2:** + LazyWorldRoute wrapper-order tesztek, EntityNotFound/Forbidden, useBlocker, onSilentRenewError, Toaster/useToast, Dialog focus trap, useLocaleFormat, mutation policy patterns.

| Tesztelendő egység | Fájl | Min teszt |
|---|---|---|
| `worldCatalog` integritás | `worldCatalog.test.ts` | 5 (minden world létezik, requiredModules valid, chrome valid enum) |
| `WorldGuard` logika | `WorldGuard.test.tsx` | 8 (modul match, missing, role match/missing, PIN check, redirect, kombinációk) |
| **`LazyWorldRoute` wrapper order** *(FE-01/06)* | `LazyWorldRoute.test.tsx` | 4 (guard rejects → no chunk load; guard passes → suspense; error in chunk → world EB; ARIA correct) |
| `WorldShell` chrome variants + layout-route stability | `WorldShell.test.tsx` | 6 (standard, minimal, none variants; nem unmountol world-switch-en; Outlet renderelődik) |
| `WorldHome` redirect logic | `WorldHome.test.tsx` | 6 (single module → redirect, multi → grid, no module → grid empty) |
| `useEnabledWorlds` hook | `useEnabledWorlds.test.ts` | 4 (filter logic, role intersection, empty case) |
| `useShopFloorSession` PIN flow | `useShopFloorSession.test.ts` | 6 (PIN validate, invalid retry, lockout, machine binding, logout) |
| `tenantStore` | `tenantStore.test.ts` | 4 (load from session, reset, persist OFF) |
| **`EntityNotFound` / `EntityForbidden`** *(FE-05)* | `EntityNotFound.test.tsx` | 4 (resource render, suggest URL, forbidden CTA, audit log fired) |
| **`useBlocker` confirm dialog** *(FE-16)* | `useDraftBlocker.test.tsx` | 4 (dirty + nav → confirm; cancel → stay; confirm → leave; beforeunload registered) |
| **`onSilentRenewError` handler** *(FE-17)* | `onSilentRenewError.test.ts` | 3 (redirect with returnTo, validate returnTo not external, post-login restore) |
| **`Toaster` + `useToast`** *(FE-11)* | `Toaster.test.tsx` | 5 (4 variants, ARIA live, auto-dismiss, error persistent, queue) |
| **`Dialog` focus trap** *(FE-18)* | `Dialog.test.tsx` | 4 (initial focus, trap cycle, Esc, return focus) |
| **`useLocaleFormat`** *(FE-25)* | `useLocaleFormat.test.ts` | 5 (date, number, currency, locale switch, fallback) |
| BFF client mock | `apiClient.test.ts` | 5 per service (5× services = 25 teszt) |
| **`logout` 7-step cleanup** *(SEC-FE-04)* | `logout.test.ts` | 4 (signout call, queryClient.clear, 7 store reset, sessionStorage.clear) |
| **`useIdleTimer`** *(SEC-FE-05)* | `useIdleTimer.test.ts` | 5 (15min trigger, warning at 14min, mouse reset, key reset, cleanup on unmount) |
| **Kiosk mode router** *(SEC-FE-08)* | `routerSelection.test.ts` | 4 (URL param detect, localStorage flag, kiosk has only shopfloor routes, portal has no shopfloor) |
| **`assertSameTenant`** *(SEC-FE-17)* | `assertSameTenant.test.ts` | 4 (match passes, mismatch throws, audit POST fired, ErrorBoundary catches) |
| **URL filter Zod parse** *(SEC-FE-12)* | `parseFiltersFromUrl.test.ts` | 5 (valid pass, invalid reset, default fallback, replace history, no DEV console in PROD) |
| **`<WorldGuard>` flash-of-unauth fix** *(SEC-FE-07)* | `WorldGuardLoading.test.tsx` | 3 (tenantLoaded false → loading; loaded + module match → children; loaded + miss → redirect) |
| **Anti-pattern ESLint config** *(SEC-FE-02/11/13)* | (CI lint job, nem Vitest) | N/A |

**Cél:** ≥ 115 új Vitest unit teszt v1 implementáció után (v2: 90 → v3: 115, +25 a security finding-ek miatt).

### 8.2 Playwright E2E szcenáriók

**Δ v2:** + URL filter persistence, world switch shell stability, draft blocker, silent renew failure, optimistic UI rollback.

| Flow | Teszt | Becsült teszt# |
|---|---|---|
| Auth | KC login → /w/home redirect | 1 |
| Auth | Logout → /login | 1 |
| Auth | Token refresh silent renew | 1 |
| **Auth** *(FE-17)* | Silent renew failure → returnTo restore | 1 |
| Home | Single-module user → auto redirect | 1 |
| Home | Multi-module user → grid render | 1 |
| Home | Card click → world navigation | 1 |
| **Shell** *(FE-02)* | World switch → sidebar/topbar nem flicker, query nem refetch | 1 |
| Sales | Orders list → filter → paging | 2 |
| **Sales** *(FE-03)* | Filter → URL sync → reload → state restore | 1 |
| Sales | Order detail open → handshake state | 1 |
| Sales | Configurator open → Graph engine view → save | 1 (heavy) |
| **Sales** *(FE-16)* | Configurator dirty → world nav → confirm dialog | 1 |
| Production | Cutting plan list → detail | 1 |
| Production | Manufacturing FSM board → status transition | 1 |
| **Production** *(FE-10)* | Optimistic status update → simulate 500 → rollback + toast | 1 |
| **Production** *(FE-08)* | Heavy screen crash → nested EB catches → world stays | 1 |
| Shop Floor | PIN entry → invalid → lockout | 2 |
| Shop Floor | PIN entry → valid → task list | 1 |
| Shop Floor | Task detail → status update | 1 |
| **Shop Floor** *(FE-09)* | Task list polling → background update visible | 1 |
| Settings | Tenant info → audit log → users (admin) | 2 |
| Settings | Non-admin user → users page → 403 redirect | 1 |
| World guard | Disabled module → /w/cutting access → home redirect | 1 |
| **World guard** *(FE-01)* | Forbidden world URL access → no chunk in network log + redirect | 1 |
| **404** *(FE-05)* | `/w/sales/orders/9999` → EntityNotFound, sidebar+topbar megmarad | 1 |
| **a11y** *(FE-18)* | Dialog open → focus trap, Esc → close + return focus | 1 |
| **i18n** *(FE-24)* | Locale switch → no page reload → translations update | 1 |
| **Security** *(SEC-FE-01)* | Token storage → DevTools → Application → localStorage NEM tartalmaz access/refresh token | 1 |
| **Security** *(SEC-FE-04)* | Logout → minden Zustand store reset + queryClient cache 0 entry verify | 1 |
| **Security** *(SEC-FE-05)* | Idle 14:00 → warning toast; idle 15:00 → auto-logout | 1 |
| **Security** *(SEC-FE-07)* | Slow 3G simulation, login → /w/home → no FOUC of world cards | 1 |
| **Security** *(SEC-FE-08)* | Kiosk mode (`?mode=shopfloor`) → /w/sales URL → 404 (NEM portál home) | 1 |
| **Security** *(SEC-FE-09)* | Non-admin user → `/bff/api/users` direct fetch → 403 + audit log | 1 |
| **Security** *(SEC-FE-12)* | URL paraméter spoof (`?status=DROP`) → reset to default + no error toast leak | 1 |
| **Security** *(SEC-FE-15)* | Backend 500 hibával → toast generic message (NEM stack/path leak) | 1 |
| **Security** *(SEC-FE-17)* | Mock cross-tenant response → `<EntityForbidden>` + audit POST verify | 1 |
| **Cél** | | **≥ 39 új E2E flow** (Δ v3: +9 security)|

### 8.3 MSW mock handler-ek

**Δ BFF-01:** `abstractions.ts` handler törölve; **Δ BFF-03/05:** új `/full` aggregátor + `/health` handler-ek a megfelelő fájlokban.

Minden új BFF route-hoz külön handler. Struktúra:

```
apps/joinerytech/src/test/mocks/
├── handlers/
│   ├── kernel.ts           (me/session, me/home-state, tenant, audit, users, configurator, orders/:id/full, health)
│   ├── manufacturing.ts    (tasks, task detail, status update, tasks/:id/full)
│   ├── cutting.ts          (existing + plans/:id/full)
│   └── shopfloor.ts        (pin login/logout, machine tasks)
├── fixtures/
│   ├── tenants.ts
│   ├── orders.ts
│   ├── plans.ts
│   ├── tasks.ts
│   └── users.ts
└── server.ts
```

**Cél:** minden új BFF route-hoz min 1 MSW handler — happy path + 1 error case (403 vagy 500 vagy 503 circuit-breaker).

---

## 9. Definition of Done

### 9.1 Frontend gates

- [ ] Minden lazy-loaded world `<LazyWorldRoute>` HOC-on át (FE-01, FE-06): ErrorBoundary > WorldGuard > Suspense > Lazy
- [ ] `<WorldShell>` layout route-ként mountol egyszer (FE-02); world-váltáskor csak a content cserélődik
- [ ] Heavy screen-ek nested ErrorBoundary + Suspense pár (FE-08): NestingVisualizer, FsmBoard, ProductConfigurator
- [ ] `enabledModules` mismatch → redirect /w/home replace (FE-04); chunk NEM töltődik le (FE-01)
- [ ] List screens URL-szinkronizált filter/sort/page (FE-03); standardizált search param konvenció
- [ ] World-en belüli entity 404 → `<EntityNotFound>` content area-ban (FE-05); WorldShell stabil
- [ ] Cross-tenant kísérlet → `<EntityForbidden>` + audit log entry (Section 5.5)
- [ ] Form: react-hook-form + Zod minden form-ra (FE-07)
- [ ] Modal/Dialog focus trap, Esc, return focus (FE-18)
- [ ] Live region (Toaster) ARIA-polite (success/info), ARIA-assertive (error) (FE-11)
- [ ] Loading state taxonomy alkalmazva (FE-12): world / heavy / query / mutation / polling
- [ ] Mutation policy table alkalmazva (FE-10): optimistic ahol kell, invalidation ahol nem
- [ ] Memoization: `<PagedTable>` rows, FsmBoard cells, configurator fields (FE-13)
- [ ] `useBlocker` confirm dialog ha draft dirty és nav-elsz (FE-16); `beforeunload` is
- [ ] `onSilentRenewError` handler: redirect `/login?returnTo=...`, post-login restore (FE-17)
- [ ] Locale switch hot-swap (no page reload) (FE-24); Intl-alapú format (FE-25)
- [ ] Minden form input `<label>` + `aria-describedby` hibára
- [ ] Responsive 3 breakpoint tesztelve (1280px, 768px, 375px)
- [ ] Touch target minimum 44px standard, 64px Shop Floor
- [ ] Keyboard nav minden world-ben (Tab order, Esc bezár modal, Enter submit)
- [ ] Skip-to-content link `<App>` legelső interaktív elemeként
- [ ] Semantic landmarks: `<header>`, `<nav aria-label>`, `<main>`, opcionális `<aside>` (FE-22)
- [ ] Sidebar collapse state per session (sessionStorage, NEM localStorage)
- [ ] World unmount → per-world Zustand slice reset + Query cancel
- [ ] List rendering: entity ID `key`, NEVER index (FE-20)

### 9.1.1 Security gates (Δ v3)

#### Authentication & Token

- [ ] **SEC-FE-01:** `userManager.userStore = InMemoryWebStorage()`; CI grep `localStorage.setItem.*token` → 0 match a prod bundle-ben
- [ ] **SEC-FE-01:** Refresh token in-memory only v1 (page reload = re-login); v2 horizon → BFF httpOnly cookie session
- [ ] **SEC-FE-02:** ESLint `no-restricted-imports: jwt-decode, jose`; CI grep `jwtDecode\|jose\.decodeJwt` a `src/` alatt → 0 match
- [ ] **SEC-FE-02:** Minden auth-derived state (`user`, `roles`, `enabledModules`, `tenantId`) `/me/session` BFF-verified response-ból
- [ ] **SEC-FE-04:** `logout()` 7-step cleanup (KC signout + queryClient.clear + 7 store reset + sessionStorage.clear + userManager.removeUser); Vitest verify
- [ ] **SEC-FE-05:** `useIdleTimer` 15 min standard portál; Shop Floor 5 min PIN re-entry; warning toast 60 sec előtt
- [ ] **SEC-FE-06:** Production `Log.setLevel(Log.NONE)`; prod bundle grep `code_verifier\|access_token\|refresh_token` console-ban → 0 match

#### Authorization & Routes

- [ ] **SEC-FE-03:** Single source of truth — Orchestrator `enabledModulesService` egyszer hívva, használva mind `/me/session` mind a route guard middleware által
- [ ] **SEC-FE-03:** Cross-tenant URL access → 404 (NEM 403, info-szivárgás védelem); RLS + explicit `WHERE tenant_id = :sessionTenantId` minden Kernel query-ben
- [ ] **SEC-FE-07:** `tenantStore.loaded === false` esetén `<WorldHome>`, `<WorldSidebar>`, `<WorldGuard>` mind `<WorldLoadingScreen>` (no FOUC); Playwright slow-3G teszt
- [ ] **SEC-FE-08:** Kiosk mode detection (`?mode=shopfloor` vagy localStorage flag) → router teljesen szeparált, portál többi route NEM regisztrálódik
- [ ] **SEC-FE-09:** Minden role-gated BFF endpoint (Section 5.6 lista) `roleGuard` middleware; Vitest BFF mock + Playwright nem-admin → 403 verify
- [ ] **SEC-FE-10:** Machine token `scopes` claim minimization (`shopfloor:task:read/write` only); `/bff/api/*` machine tokennel → 403 + audit

#### XSS & Injection

- [ ] **SEC-FE-11:** ESLint `react/no-danger: error`; CI grep `dangerouslySetInnerHTML` → 0 match
- [ ] **SEC-FE-12:** Minden `useSearchParams` Zod schema-val parse-olva; invalid → reset to default + replace history
- [ ] **SEC-FE-13:** ESLint `no-eval: error`, `no-new-func: error`; CI grep `\beval\(\|new Function\(` → 0 match
- [ ] **SEC-FE-14:** Production CSP `unsafe-inline` 0 előfordulás; Vite production build inline → external; CSP report-only v1, enforcing v2

#### Data Exposure

- [ ] **SEC-FE-15:** Toaster mindig `t('common.error.code.{ERROR_CODE}', t('common.error.generic'))` pattern; raw server `error.message` csak DEV
- [ ] **SEC-FE-16:** `vite-plugin-remove-console` production build (kivéve `console.error`); prod bundle grep `console\.(log\|debug\|info\|warn)` → 0 match
- [ ] **SEC-FE-17:** Minden BFF response queryFn-ben `assertSameTenant`; `CrossTenantError` ErrorBoundary catch → /w/home redirect + audit POST

### 9.2 BFF gates

- [ ] Minden új BFF route: `authMiddleware` + `tenantMiddleware` + `enabledModulesGuard`
- [ ] Role-gated endpoint (`/audit`, `/users`) `roleGuard` middleware-rel
- [ ] OpenAPI snapshot frissítve (kernel, cutting, manufacturing) + codegen lefuttatva, commitelve
- [ ] Error response format konzisztens: `{ error: { code, message } }` (Δ BFF-10: production-ben `details` strip)
- [ ] Timeout: 10s default, 30s aggregátor, 15s mutation, 120s file upload (Δ BFF-12)
- [ ] Shop Floor PIN endpoint: rate limit (5 invalid PIN / 5 perc / machine) + audit log entry minden invalid PIN-re
- [ ] CORS: csak `portal.joinerytech.hu` + dev `http://localhost:5173` (frozen)
- [ ] CSP header Orchestrator-ról: `default-src 'self'; script-src 'self'; ...` (no inline)
- [ ] **BFF-01:** `/bff/abstractions/*` route NEM létezik; configurator endpoint `/bff/api/configurator/:productTemplateId/configure` alatt
- [ ] **BFF-02:** Manufacturing OpenAPI snapshot csak production-candidate (staging deploy) után commit-olható; Track F első napja contract-freeze checkpoint
- [ ] **BFF-03:** `*/full` aggregátorok implementálva: `/orders/:id/full`, `/tasks/:id/full`, `/plans/:id/full`
- [ ] **BFF-05:** `/bff/health` endpoint Orchestrator-on, downstream ping aggregate
- [ ] **BFF-06:** JWT forwarding `Authorization: Bearer <user_jwt>` + `X-Tenant-Id` header; downstream cross-check verify-elt
- [ ] **BFF-07:** Rate limit policy táblázat (Section 5.8.2) implementálva `express-rate-limit` Redis store-ral; 429 + Retry-After
- [ ] **BFF-08:** Pass-through validation alapelv; PIN login és file upload BFF-szintű validáció kivétel
- [ ] **BFF-09:** Pino structured log; `requestId` minden request-en; PII / token / body NEM logolva
- [ ] **BFF-10:** Error normalizer middleware (ASP.NET ProblemDetails → unified shape); `details` csak DEV
- [ ] **BFF-11:** `opossum` circuit breaker per downstream service (kernel, cutting, manufacturing); 503 + retryAfter fallback
- [ ] **BFF-13:** Response shape `{ data, meta? }` minden endpoint-on (kivéve `/me/session` historikus)
- [ ] **BFF-14:** Pagination `?page=1&pageSize=25` default, max 100 (audit max 200)
- [ ] **BFF-15:** `Cache-Control` differentiált: érzékeny `no-store`, referencia `private, max-age=300, must-revalidate`; minden GET ETag
- [ ] **BFF-16:** CI job `pnpm codegen:check` — PR blocker ha snapshot drift
- [ ] **BFF-04 v2 horizon:** `/bff/v1/partner/*` versioning ha PartnerTier API megjelenik (v1-ben nincs változás)

### 9.3 Test gates

- [ ] Meglévő FE tesztek zöld (Doorstar Portal: 99 → +115 = ~214; E2E: 277 → +39 = ~316)
- [ ] Új Vitest unit tesztek: ≥ 115 (Δ v3: +25 a security finding-ek miatt; v2 báz: 90)
- [ ] Új Playwright E2E flow-k: ≥ 39 (Δ v3: +9 security)
- [ ] MSW handler minden új BFF route-hoz (kernel +8 [me/session, me/home-state, audit, users, configurator, orders/:id/full, health, error-codes ref], cutting +1 [plans/:id/full], manufacturing +4 [tasks list, task, /full, status], shopfloor +4 = **17 új handler**)
- [ ] Test coverage ≥ 70% statements, ≥ 60% branches a `apps/joinerytech/src/worlds/` mappában
- [ ] **Wrapper-order regression test** (FE-01): Network panel ellenőrzés — forbidden world chunk URL nincs a network log-ban
- [ ] **Layout-route stability test** (FE-02): React DevTools snapshot — `<WorldShell>` instance ID állandó world-switch során
- [ ] **Security CI gates** *(Δ v3)*: ESLint anti-pattern szabályok PASS; prod bundle grep checks (token storage, console, dangerouslySetInnerHTML, eval) all 0 match

### 9.4 Performance gates

**Δ FE-15 (granuláris budget) + FE-14 (virtualization):**

- [ ] Lighthouse Performance ≥ 90 (desktop, prod build, /w/home)
- [ ] Lighthouse Accessibility ≥ 95 (minden world-re)
- [ ] Bundle size: app entry < 200KB gzip
- [ ] **World container chunk < 50KB gzip** (FE-15) — csak a world-shell + routes + screen wrapper, heavy screens nélkül
- [ ] **Heavy screen chunk < 100KB gzip** (FE-15) — NestingVisualizer, FsmBoard, ProductConfigurator
- [ ] CI gate (`size-limit` lib): PR-szinten regression alert, konkrét per-chunk budget enforcement
- [ ] First Contentful Paint < 1.5s desktop prod
- [ ] Cumulative Layout Shift < 0.1
- [ ] Time to Interactive < 3s desktop prod
- [ ] **PagedTable virtualization audit Track A elején** (FE-14): ha PagedTable nem virtualizál, Audit Log + Orders list (potenciálisan 1000+ row) `react-virtuoso` integrációval
- [ ] **Asset opt** (FE-21): `lucide-react` icon set, `font-display: swap`, preconnect, raszter `loading="lazy"`

### 9.5 Összesített

- [ ] Minden Section 9.1-9.4 gate teljesül
- [ ] CONTRACT_ISSUES.md üres VAGY minden issue tracked Manufacturing/Kernel inboxban
- [ ] CLAUDE.md frissítve a v1 conventions-szel (worldCatalog, WorldShell, query keys)
- [ ] README.md `apps/joinerytech/src/worlds/` mappa-szintű, magyarázza a world pattern-t
- [ ] V1 deployolva `portal.joinerytech.hu`-n + smoke test (login → 5 world elérés)

---

## 10. Sprint terv (Claude Code implementáció)

### 10.1 Track-ek és sorrend

```
Track A — Foundation             ───────►  ┐
Track F — BFF route additions    ──────────┼──►  Track C — Sales
                                            │     Track D — Production
Track B — Home + Settings        ───────►   │     Track E — Shop Floor
                                            │
                                            └──►  Final integration + DoD
```

| Track | Tartalom | Becsült effort | Függőség |
|---|---|---|---|
| **A — Foundation** | `worldCatalog.ts`, `WorldShell`, `WorldGuard`, `WorldTopBar`, `WorldSidebar`, `tenantStore`, route shell, lazy boundaries | ~3 nap | — |
| **F — BFF route additions** | Orchestrator: `/me/session`, `/me/home-state`, `/audit`, `/users`, `/api/configurator/:id/configure`, `/api/orders/:id/full`, `/manufacturing/*` proxy + `/tasks/:id/full`, `/cutting/plans/:id/full`, `/shopfloor/*` proxy + middleware láncok (auth, tenant, enabledModulesGuard, roleGuard, error normalizer, circuit breaker, rate limit) + `/bff/health`. **Day 1: contract-freeze checkpoint** Manufacturing OpenAPI véglegesítése a backend csapattal. | ~4 nap | Backend services DEPLOYED (Manufacturing 5007 már DEV COMPLETE), Manufacturing staging deploy a contract-freeze előtt |
| **B — Home + Settings** | `WorldHome` (grid + redirect), `WorldCard`, `TenantInfoScreen`, `AuditLogScreen`, `UserListScreen` | ~2 nap | A, F |
| **C — Sales** | `OrdersListScreen`, `OrderDetailScreen`, `ProductConfiguratorScreen`, `B2bHandshakesScreen`, `useSalesOrderDraftStore` | ~5 nap | A, F |
| **D — Production** | `CuttingPlanListScreen`, `CuttingPlanDetailScreen`, `NestingVisualizerScreen`, `ManufacturingFsmBoardScreen`, `ManufacturingTaskDetailScreen` | ~5 nap | A, F |
| **E — Shop Floor** | `ShopFloorPinGate`, `PinEntryScreen`, `ShopFloorTaskListScreen`, `ShopFloorTaskDetailScreen`, `useShopFloorSessionStore`, `PinKeypad` (`@spaceos/ui`) | ~4 nap | A, F |
| **G — Integration + DoD** | E2E full flow, Lighthouse audit, bundle size analysis, CONTRACT_ISSUES drain | ~2 nap | B, C, D, E |

**Összesen:** ~24 nap fokozatosan, párhuzamosítással ~14-16 nap critical path.

### 10.2 Agent utasítás (Claude Code, `spaceos-fe` session)

```
Te a `spaceos-fe` tmux session vagy. WD: ~/spaceos-doorstar-portal/.

Forrás: SpaceOS_Portal_World_Architecture_v1.md (jelen dokumentum,
mailbox-on át kapod task-bontásban).

Sorrend (parallel-ezhető Track A és Track F):

NAP 1-3 — Track A:
  1. Hozd létre a worldCatalog.ts-t a @spaceos/domain csomagban
     (Section 2.3 tábla alapján).
  2. Implementáld a WorldShell-t a chrome prop-pal (Section 3.1 fa).
  3. Implementáld a WorldGuard-ot (Section 3.5 logika).
  4. Új tenantStore (Section 4.1 interface).
  5. Route shell az App.tsx-ben (Section 2.1 map alapján), minden
     world Suspense + ErrorBoundary-ban.
  6. Vitest tesztek: WorldShell, WorldGuard, worldCatalog, tenantStore.
  7. BUILD + TEST + LINT + TYPECHECK zöld → outbox DONE.

NAP 1-3 — Track F (parallel, külön session/inbox):
  CONTRACT_ISSUES alapján vagy közvetlen utasításra Orchestrator
  módosítás → új BFF route-ok + middleware láncok (Section 5.2, 5.5).

NAP 4-5 — Track B (függ A+F-től):
  WorldHome + WorldCard + Settings világ képernyői.

NAP 6-10 — Track C, D, E parallel:
  Sales / Production / Shop Floor világok.

NAP 11-12 — Track G:
  E2E + Lighthouse + bundle audit.

Constraint:
  - Minden új lazy world → Suspense + ErrorBoundary
  - Per-world cleanup useEffect → Query cancel + Zustand slice reset
  - OpenAPI codegen újrafuttatva minden BFF endpoint változás után
  - PIN flow (Shop Floor): TILOS PIN-t a localStorage-ben tárolni;
    machine token sessionStorage-ben (interim v1, v2-ben device-bound)
  - Bundle size mérés minden track végén: `pnpm build --report`
```

### 10.3 Kockázatok és mitigációk

**Δ v2:** + R-09 (PagedTable virtualization unknown), R-10 (radix-ui adoption), R-11 (size-limit CI integráció).

| ID | Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|---|
| R-01 | Manufacturing service Phase 1 BFF route-jai még nem proxy-zódnak az Orchestrator-on | Magas | Track F blokkol | Track F first day: új Orchestrator middleware + `/bff/manufacturing/*` proxy felépítés; ha a backend endpoint hiányzik, mock fixture-ral kezdjük + CONTRACT_ISSUES |
| R-02 | Shop Floor PIN flow security model még nem véglegesített (machine token vs. KC) | Magas | Track E SEC-FE finding (v3) | v1 interim: `/bff/shopfloor/*` saját auth middleware-rel, machine fingerprint + sessionStorage; v2: kiemelni külön app entry-pointra (D-07 LOCKED) |
| R-03 | Production world Three.js / recharts bundle > 100KB gzip heavy chunk | Közepes | Performance gate fail | Nested Suspense kötelező (FE-08); ha még akkor is sok, ManufacturingFsmBoard-ban recharts → custom CSS bar chart |
| R-04 | Configurator (Sales world) Kernel `/configurator/:id/configure` endpoint nem fed le minden ProductTemplate-et | Közepes | Sales track scope creep | v1: csak deployed Doorstar Door template; Cabinet template → roadmap |
| R-05 | KC `portal-app` redirect URI nem tartalmazza a `localhost:5173`-t | Alacsony | Dev loop blokkol | v4.1 amendment §4 már tartalmazza, de Day 1 verify infra task |
| R-06 | `enabledModules` szerverkor-szerinti lemaradása (cache) → user új modult kap, de UX nem mutatja | Alacsony | UX zavar | `/me/session` stale time 5 min; admin-trigger `queryClient.invalidateQueries(['session'])` settings change után |
| R-07 | Audit log endpoint pagination + filter teljesítmény (Hash chain nagy) | Közepes | Settings world lassú | BFF: kötelező `?from=&to=&limit=50`; szerver oldal cursor-based paging Kernel oldalon |
| R-08 | Több párhuzamos Claude Code session ütközése (Track C+D+E parallel ugyanabban a repo-ban) | Magas | Merge conflict | Track-enkénti git branch; Track A jelez `main`-be merge előtt |
| **R-09** *(FE-14)* | `<PagedTable>` (`@spaceos/ui`) belső implementáció ismeretlen — virtualizál-e? | Közepes | Audit Log + Orders lassú N>500 esetén | Track A első napja: PagedTable forrás-audit; ha nem virtualizál → `react-virtuoso` integráció Audit + Orders specifikusan, vagy PagedTable v2 internal upgrade |
| **R-10** *(FE-18)* | Radix-ui adoption — új dependency a `@spaceos/ui` csomagba | Alacsony | Bundle bloat ha rosszul tree-shake-elt | Csak a használt primitive-ek import-ja (Dialog, DropdownMenu); size-limit CI-ban watchelni |
| **R-11** *(FE-15)* | `size-limit` lib CI integrációja még nincs | Közepes | Bundle regression észrevétlen | Track A: `size-limit` config commit + GitHub Action job; PR-szinten failing if budget exceeded |
| **R-12** *(SEC-FE-01)* | KC SDK alapértelmezésben localStorage-be ír; ha az `InMemoryWebStorage` config-ot eltévesztjük, token leak | Közepes | 🔴 CRITICAL prod-ben | Track A első commit Vitest teszt: `userManager.userStore` instance assertion `InMemoryWebStorage` típus; CI-szinten fail ha `localStorage` oldalt megjelenik a token |
| **R-13** *(SEC-FE-08)* | Kiosk mode router szétválasztás runtime detection — admin tévedés (jelölés nélkül adott tablet) → portál hozzáférés Shop Floor user-rel | Magas | Cross-world data exposure Shop Floor terminálon | Defense-in-depth: KC user login bárhol = portál mode (akkor sem Shop Floor mód aktív, ha localStorage flag van); kiosk mode csak akkor érvényes, ha NINCS KC session aktív; v2 horizon: külön app entry-point teljes elválasztás |
| **R-14** *(SEC-FE-03)* | `enabledModulesService` single source — ha az Orchestrator és a Kernel külön-külön implementálja, drift kockázat | Közepes | Frontend more permissive than backend (UX jó, security ok); fordítva (FE strict, BE permissive) → user nem fér hozzá ahhoz, amihez kellene | Implementáció: a service Kernel-ben él, az Orchestrator csak `/internal/enabled-modules/:tenantId` Kernel call-t cache-eli (60 sec); single source garantálva |
| **R-15** *(SEC-FE-15)* | Error i18n kulcsok hiányosak v1-ben — toast generic fallback gyakori → user nem érti mi történt | Közepes | Support load + UX gyenge | Track A: Top 30 leggyakoribb error code i18n kulcs előre lefordítva; backlog ticket minden új error code-ra; observability alert ha generic fallback >5% toast-on |
| **R-16** *(BFF-01)* | Konfigurátor endpoint Kernel oldalra mozgatása ütközhet a Kernel jelenlegi domain határaival (Abstractions in-process hívás új BFF route-ot igényel) | Közepes | Track F+C scope csúszás | Track F Day 1: Kernel csapattal egyeztetés a `/api/configurator/:productTemplateId/configure` endpoint shape-ről; ha jelentősebb átalakítás, parallel Track-ként ütemezve, Configurator screen mock fixture-ral indul |
| **R-17** *(BFF-02)* | Manufacturing service még csak DEV COMPLETE — staging deploy késhet → contract-freeze blokkolja Track F-et | Magas | Track F BFF route-ok mock fixture-ral, integráció késhet | Day 1 escalation: backend csapat staging deploy ETA; ha >3 nap, Track F mock-first módban indul, késleltetett snapshot commit |
| **R-18** *(BFF-11)* | Circuit breaker `opossum` lib új dependency az Orchestrator-on; konfigurációs hibák production OPEN state-be tehetik a kernel-t | Alacsony | Hamis 503-ak | Conservative initial config (50% error threshold, 60 sec rolling window); staging-en chaos test a release előtt; metrics dashboard `circuit_open` event-re |
| **R-19** *(BFF-15)* | Differenciált `Cache-Control` policy bevezetése — ha tévedésből érzékeny endpoint kap `private, max-age=...`, böngésző cache-elhet üzleti adatot | Közepes | Stale data, esetleg cross-user ha shared computer | Default `no-store` minden új endpoint-on; explicit allowlist a referencia endpointoknak; CI grep `Cache-Control: private` és felülvizsgálat minden új endpoint-on |

### 10.4 Claude Code session-bontás (implementation-ready)

**Cél:** A 7 track napra bontva, mindegyik egy Claude Code session = egy jól-scoped task. A `spaceos-fe` tmux session a `tmux dispatcher` pattern alapján fogadja ezeket a inbox-ban.

**Notation:** `[session-id]` = mailbox címke; `[output]` = elvárt outbox jel; `[gate]` = ha nem teljesül, szakasz nem zárható.

#### Track A — Foundation (3 nap)

**A.1 — `worldCatalog` + types** *(0.5 nap)*

```
[session-id]   fe-a1-worldcatalog
[input]
  - SpaceOS_Portal_World_Architecture_v4_final.md (Section 0, 2.3, 3.5)
  - SpaceOS_Portal_World_Architecture_v4_README.md
[task]
  1. @spaceos/domain csomagban hozd létre: src/world/worldCatalog.ts
  2. WorldDefinition interface a Section 2.3 + 3.5 alapján
     (id, chrome: 'standard'|'minimal'|'none', requiredModules: string[],
      requiredRoles: string[], defaultScreen: string, requiresPin: boolean,
      labelKey: string, iconName: string)
  3. 5 world definition: home, sales, production, shopfloor, settings
  4. Vitest unit teszt: worldCatalog.test.ts (5 teszt — Section 8.1)
[output]
  - Build + test + lint + typecheck zöld
  - PR diff + outbox DONE jelzés
[gate]
  - Type-check 0 error
```

**A.2 — `tenantStore` + session loader** *(0.5 nap)*

```
[session-id]   fe-a2-tenantstore
[task]
  1. apps/joinerytech/src/stores/tenantStore.ts (Section 4.1 interface)
  2. Persist: NEM (Section 4.1 frozen rule)
  3. Loader: useEffect az App-ben, /bff/api/me/session fetch → setTenant()
  4. CSAK ha tenantStore.loaded === true, akkor renderelődnek világ-érzékeny komponensek
     (SEC-FE-07 fix, Section 3.5)
  5. Vitest: tenantStore.test.ts (4 teszt)
[output]
  - tenantStore deployolva, "loaded" flag használatban
[gate]
  - tenantStore.persist === undefined (security)
  - SEC-FE-02: tenantStore SOHA NEM olvas JWT-ből (csak /me/session response)
```

**A.3 — `WorldShell` + `LazyWorldRoute` HOC** *(1 nap)*

```
[session-id]   fe-a3-worldshell
[input]
  - Section 2.1 (route map), 2.4 (LazyWorldRoute), 3.1 (komponens fa)
[task]
  1. apps/joinerytech/src/worlds/WorldShell.tsx (chrome prop, layout-route)
     - <header>, <nav aria-label="Sidebar">, <main> semantic landmarks (FE-22)
     - chrome='none' → csak <Outlet />
  2. apps/joinerytech/src/worlds/WorldGuard.tsx (Section 3.5 logika)
     - tenantLoaded gate (SEC-FE-07)
     - module / role / PIN check → <Navigate replace>
  3. apps/joinerytech/src/worlds/LazyWorldRoute.tsx HOC (Section 2.4)
     - Wrapper sorrend: ErrorBoundary > Guard > Suspense > Lazy (FE-01)
  4. apps/joinerytech/src/worlds/registry.ts — central world registry
  5. App.tsx: createBrowserRouter — layout route /w → WorldShell, child route-ok
     LazyWorldRoute-tal (Section 2.1)
  6. Vitest: WorldShell.test.tsx (6), WorldGuard.test.tsx (8),
     LazyWorldRoute.test.tsx (4), WorldGuardLoading.test.tsx (3)
[output]
  - Skeleton 5 world: csak placeholder content, de a shell + guard működik
  - Network panel teszt: forbidden world chunk URL nincs a logban (FE-01 verify)
[gate]
  - Layout route stability: WorldShell instance ID ugyanaz world-switch során (FE-02 verify)
  - 21 új Vitest teszt zöld
```

**A.4 — Auth flow hardening** *(0.5 nap)*

```
[session-id]   fe-a4-auth
[input]
  - Section 4.10 (token storage InMemoryWebStorage)
  - Section 4.11 (logout 7-step cleanup)
  - Section 4.12 (idle timer)
  - SEC-FE-01, SEC-FE-04, SEC-FE-05, SEC-FE-06
[task]
  1. apps/joinerytech/src/auth/userManager.ts — InMemoryWebStorage konfig (SEC-FE-01)
  2. Production: Log.setLevel(Log.NONE) (SEC-FE-06)
  3. apps/joinerytech/src/auth/logout.ts — 7-step (SEC-FE-04)
  4. apps/joinerytech/src/auth/useIdleTimer.ts — 15min standard (SEC-FE-05)
  5. apps/joinerytech/src/auth/onSilentRenewError.ts — returnTo redirect + open-redirect védelem
  6. ESLint config: no-restricted-imports (jwt-decode, jose) (SEC-FE-02)
  7. ESLint config: react/no-danger, no-eval, no-new-func (SEC-FE-11, SEC-FE-13)
  8. Vitest: logout.test.ts (4), useIdleTimer.test.ts (5), onSilentRenewError.test.ts (3)
[output]
  - prod bundle grep: 'localStorage.setItem.*token' → 0 match
  - prod bundle grep: 'console.log|debug|info|warn' → 0 match (kivéve console.error)
  - prod bundle grep: 'dangerouslySetInnerHTML', 'eval(', 'new Function(' → 0 match
[gate]
  - SEC-FE-01..06, SEC-FE-11, SEC-FE-13 CI gate-ek mind PASS
```

**A.5 — Performance + tooling** *(0.5 nap)*

```
[session-id]   fe-a5-tooling
[task]
  1. size-limit konfig (FE-15) — app entry < 200KB, world chunk < 50KB,
     heavy < 100KB
  2. PagedTable virtualization audit (FE-14, R-09)
     - Forrás megnézése; ha NEM virtualizál, react-virtuoso integráció
  3. vite-plugin-remove-console konfig (SEC-FE-16)
  4. lucide-react adoption (FE-21)
  5. CI workflow: pnpm codegen:check (BFF-16)
  6. CI workflow: pnpm size-limit
[output]
  - GitHub Actions: lint, typecheck, test, codegen-check, size-limit jobs
[gate]
  - PR-szinten failing if budget exceeded
  - codegen drift detected → PR blocker
```

#### Track F — BFF route additions (4 nap, parallel A-val)

**F.1 — Contract-freeze checkpoint (Day 1, KRITIKUS escalation pont)** *(0.25 nap)*

```
[session-id]   bff-f1-contract-freeze
[input]
  - Section 5.2 (route table), 5.4 (snapshot list)
  - BFF-01 (configurator), BFF-02 (Manufacturing OpenAPI), R-16, R-17
[task]
  1. Kernel csapat egyeztetés: /api/configurator/:productTemplateId/configure
     endpoint shape final (BFF-01 fix)
  2. Manufacturing csapat egyeztetés: staging deploy ETA
     - Ha < 3 nap → contract-freeze + snapshot commit Day 2
     - Ha > 3 nap → Track F mock-first módban indul, deferred snapshot
  3. CONTRACT_ISSUES.md update minden gap-pel
[output]
  - Confirmed endpoint contract az inbox-ban
  - R-16, R-17 status update
[gate]
  - Configurator endpoint shape vagy production-ready vagy mock fixture spec
```

**F.2 — Middleware lánc + auth + tenant** *(1 nap)*

```
[session-id]   bff-f2-middleware
[input]
  - Section 5.5 (middleware lánc), 5.8.1 (JWT forwarding)
[task]
  1. orchestrator/src/middleware/auth.ts — KC JWT verify
  2. orchestrator/src/middleware/tenant.ts — JWT-ből tenantId, req.tenantContext
  3. orchestrator/src/middleware/enabledModulesGuard.ts (Section 5.5 mapping)
     - SINGLE SOURCE: Kernel `/internal/enabled-modules/:tenantId` cache 60s
       (SEC-FE-03)
  4. orchestrator/src/middleware/roleGuard.ts — Section 5.6 lista alapján
  5. orchestrator/src/proxy/forwardAuth.ts — buildDownstreamHeaders helper
     (X-Tenant-Id explicit + JWT forward, BFF-06)
  6. Vitest BFF: middleware-chain.test.ts (8 teszt)
[output]
  - Minden új BFF route ezzel a lánccal védve
[gate]
  - Cross-tenant test: user A token + URL B tenantId → 403 (SEC-FE-03 mirror)
  - Machine token + /bff/api/* → 403 (SEC-FE-10)
```

**F.3 — Error normalizer + circuit breaker + rate limit** *(1 nap)*

```
[session-id]   bff-f3-resilience
[input]
  - Section 5.8.2 (rate limit), 5.8.4 (error normalizer), 5.8.5 (circuit breaker)
[task]
  1. orchestrator/src/middleware/errorNormalizer.ts (BFF-10)
     - ASP.NET ProblemDetails → unified format
     - production strip details
  2. orchestrator/src/middleware/rateLimit.ts (BFF-07)
     - express-rate-limit Redis store
     - Section 5.8.2 policy táblázat alapján
  3. orchestrator/src/proxy/circuitBreaker.ts (BFF-11)
     - opossum lib, per-downstream breaker
     - Section 5.8.5 config
  4. Vitest: error-normalizer.test.ts (5), rate-limit.test.ts (4),
     circuit-breaker.test.ts (5)
[output]
  - 503 + retryAfter response ha downstream OPEN
  - 429 + Retry-After header ha rate limit túllépve
[gate]
  - Chaos test staging-en (R-18) — Manufacturing kill → 503 instant
  - Stack trace SOHA nem szerepel response-ban (SEC-FE-15)
```

**F.4 — BFF route-ok implementáció** *(1.5 nap)*

```
[session-id]   bff-f4-routes
[input]
  - Section 5.2 (route tábla, 16 új route)
  - Section 5.6 (role-gated lista)
  - Section 5.8.6 (timeout per type), 5.8.7 (response shape), 5.8.8 (cache-control), 5.8.9 (pagination)
[task]
  1. /bff/api/me/session aggregátor (Kernel call)
  2. /bff/api/me/home-state aggregátor
  3. /bff/api/tenant
  4. /bff/api/audit (admin role)
  5. /bff/api/users (admin role)
  6. /bff/api/configurator/:productTemplateId/configure (BFF-01 fix)
  7. /bff/api/orders/:id/full (BFF-03 aggregátor)
  8. /bff/manufacturing/* proxy (Phase 1 endpoints)
  9. /bff/manufacturing/tasks/:id/full (BFF-03 aggregátor)
 10. /bff/cutting/plans/:id/full (BFF-03 aggregátor)
 11. /bff/shopfloor/pin/login + /logout (PIN BFF-szintű validáció — BFF-08)
 12. /bff/shopfloor/tasks (machine token + scope check)
 13. /bff/shopfloor/task/:id/status (machine token + scope + audit log)
 14. /bff/health (BFF-05 — Kernel/Cutting/Manufacturing aggregate ping)
 15. OpenAPI snapshot regen (kernel.json, cutting.json, manufacturing.json)
     — csak ha contract-frozen (F.1 outbox-ból)
 16. Pino structured log Section 5.8.10 alapján
[output]
  - Minden 16 új route deployolva, MSW handler-ekkel teszteke
[gate]
  - 16 új BFF gate (Section 9.2) mind PASS
  - codegen:check job zöld
```

**F.5 — Logging + structured log + observability** *(0.25 nap)*

```
[session-id]   bff-f5-observability
[task]
  1. Pino logger config (Section 5.8.10) — request_completed log line
  2. Metrics endpoint (Prometheus-style): rate_limit_hits, circuit_open,
     downstream_latency_ms (P50, P99)
  3. CI: prod bundle grep — request body NEM logolva (NoPII gate)
[output]
  - /metrics endpoint, alapszintű observability
```

#### Track B — Home + Settings (2 nap)

**B.1 — `WorldHome` + auto-redirect logic** *(1 nap)*

```
[session-id]   fe-b1-home
[input]
  - Section 3.2 (komponens lista), Section D-03 (single-module redirect)
  - SEC-FE-07 (loading guard)
[task]
  1. apps/joinerytech/src/worlds/home/WorldHome.tsx
     - tenantLoaded gate (SEC-FE-07)
     - useEnabledWorlds hook (filter worldCatalog by enabledModules + roles)
     - Single-module + single-role → <Navigate replace>
     - Egyébként <WorldCardGrid />
  2. apps/joinerytech/src/worlds/home/WorldCard.tsx (presentational)
  3. KpiCard (@spaceos/ui candidate, FE-19 rationale-ben deferred)
  4. Vitest: WorldHome.test.tsx (6), useEnabledWorlds.test.ts (4)
[output]
  - Login → /w/home renderel, role-aware grid VAGY redirect
[gate]
  - Slow 3G simulation: no FOUC of unauthorized cards (SEC-FE-07 verify)
```

**B.2 — Settings világ (Tenant + Audit + Users)** *(1 nap)*

```
[session-id]   fe-b2-settings
[task]
  1. apps/joinerytech/src/worlds/settings/SettingsWorld.tsx (lazy chunk)
  2. TenantInfoScreen (read-only v1)
  3. AuditLogScreen — PagedTable + HashDisplay reuse + URL filter Zod parse (SEC-FE-12)
  4. UserListScreen — admin-only WorldGuard + role check
  5. URL filters: useSearchParams + Zod schema (FE-03 + SEC-FE-12)
  6. Vitest: parseFiltersFromUrl.test.ts (5)
  7. MSW handler: kernel/audit, kernel/users
[output]
  - Settings világ teljes funkcióval, admin-only Users + Audit
[gate]
  - Non-admin → /w/settings/users → /w/home redirect (SEC-FE-09)
  - Cross-tenant audit query → 404 (SEC-FE-03)
```

#### Track C — Sales (5 nap)

**C.1 — Orders list + detail + URL filter** *(2 nap)*

```
[session-id]   fe-c1-orders
[task]
  1. apps/joinerytech/src/worlds/sales/SalesWorld.tsx (lazy chunk)
  2. OrdersListScreen — PagedTable + URL filter (Zod) (FE-03, SEC-FE-12)
  3. OrderDetailScreen — /bff/api/orders/:id/full aggregátor (BFF-03)
  4. assertSameTenant minden Query queryFn-ben (SEC-FE-17)
  5. Memoization: PagedTable rows React.memo (FE-13)
  6. Vitest: assertSameTenant.test.ts (4), useUrlFilters.test.ts (5)
  7. MSW: kernel/orders + orders/:id/full handler
[output]
  - Orders flow működik, paging/filter URL-szinkronban
[gate]
  - URL spoof test: ?status=DROP → reset to default + no error toast leak (SEC-FE-12)
  - Cross-tenant order ID → 404 (SEC-FE-03)
```

**C.2 — Configurator (heavy screen)** *(2 nap)*

```
[session-id]   fe-c2-configurator
[input]
  - Section 5.2 endpoint: /bff/api/configurator/:productTemplateId/configure (BFF-01)
  - F.1 contract-freeze outbox
[task]
  1. ProductConfiguratorScreen — Graph engine view renderer
     - Nested ErrorBoundary + Suspense (FE-08)
     - Heavy chunk < 100KB gzip (FE-15)
  2. useSalesOrderDraftStore (Zustand slice, Section 4.2)
     - Draft persistence sessionStorage (FE-16, NEM localStorage SEC-FE-01)
     - useBlocker confirm dialog (FE-16)
     - beforeunload listener
  3. Optimistic mutation pattern (FE-10) — line item edit
  4. Vitest: useDraftBlocker.test.tsx (4)
  5. MSW: kernel/configurator handler
[output]
  - Configurator működik, draft mentés + leave confirmation
[gate]
  - dangerouslySetInnerHTML grep → 0 (SEC-FE-11)
  - Heavy chunk size-limit gate PASS
  - useSalesOrderDraftStore SOHA NEM persist localStorage-be
```

**C.3 — B2B Handshakes** *(1 nap)*

```
[session-id]   fe-c3-handshakes
[task]
  1. B2bHandshakesScreen — handshake list + URL filter
  2. Mutation: accept/decline (invalidation pattern, FE-10)
  3. MSW: kernel/handshakes
[output]
  - Handshake lista + state transitions
```

#### Track D — Production (5 nap)

**D.1 — Cutting Plans (list + detail)** *(2 nap)*

```
[session-id]   fe-d1-cutting-plans
[task]
  1. apps/joinerytech/src/worlds/production/ProductionWorld.tsx (lazy chunk)
  2. CuttingPlanListScreen — PagedTable + URL filter
  3. CuttingPlanDetailScreen — /bff/cutting/plans/:id/full (BFF-03)
  4. assertSameTenant + URL Zod parse
  5. MSW: cutting/plans + plans/:id/full
[output]
  - Cutting plans flow működik
```

**D.2 — Nesting Visualizer (heavy)** *(1 nap)*

```
[session-id]   fe-d2-nesting
[task]
  1. NestingVisualizerScreen — canvas renderer
     - Nested ErrorBoundary + Suspense (FE-08)
     - Heavy chunk < 100KB gzip (FE-15)
  2. useMemo viewport calc, useCallback event handlers (FE-13)
[output]
  - Nesting view működik plan detail-ből nyitva
[gate]
  - Heavy chunk size-limit PASS
```

**D.3 — Manufacturing FSM Board** *(2 nap)*

```
[session-id]   fe-d3-manufacturing
[task]
  1. ManufacturingFsmBoardScreen — recharts (heavy, nested boundary)
  2. ManufacturingTaskDetailScreen — /bff/manufacturing/tasks/:id/full (BFF-03)
  3. Optimistic status transition (FE-10)
  4. RefetchInterval: 30 sec FsmBoard (Section 4.3)
  5. MSW: manufacturing handlers
  6. E2E: optimistic rollback test (FE-10 + simulate 500)
[output]
  - FSM Board + task detail, status transitions
[gate]
  - Heavy chunk size-limit PASS
  - 503 from manufacturing → toast t('error.serviceDown.manufacturing') (BFF-11)
```

#### Track E — Shop Floor (4 nap)

**E.1 — Kiosk router separation** *(0.5 nap, KRITIKUS SEC-FE-08)*

```
[session-id]   fe-e1-kiosk-mode
[input]
  - Section 5.7.1 (kiosk mode detection)
[task]
  1. apps/joinerytech/src/main.tsx — isKiosk runtime detection
  2. createKioskRouter() — CSAK /w/shopfloor/* + /login/machine
  3. createPortalRouter() — CSAK /w/{home,sales,production,settings}/*
  4. Defense-in-depth: ha KC user session aktív, MINDIG portal mode (R-13)
  5. Vitest: routerSelection.test.ts (4) — NEM merging routes
[output]
  - Kiosk mode tablet → /w/sales URL → 404 (NEM portal home)
[gate]
  - SEC-FE-08 verify: portal router-ben nincs /w/shopfloor route, kiosk-ban nincs /w/sales
```

**E.2 — PIN flow + machine session** *(1.5 nap)*

```
[session-id]   fe-e2-pin
[task]
  1. apps/joinerytech/src/worlds/shopfloor/ShopFloorWorld.tsx (chrome='none')
  2. PinEntryScreen — PinKeypad (@spaceos/ui), large touch targets (64×64px)
  3. ShopFloorPinGate — useShopFloorSessionStore.pinValidated check
  4. useShopFloorSessionStore — sessionStorage (NEM localStorage SEC-FE-01)
  5. /bff/shopfloor/pin/login flow + machine fingerprint (SEC-FE-10)
  6. PIN brute-force lockout UX (5 retry + 5min)
  7. Vitest: useShopFloorSession.test.ts (6)
  8. E2E: invalid PIN x5 → lockout, valid PIN → tasks
[output]
  - PIN flow működik, machine token issued, scope-restricted
[gate]
  - Machine token SOHA nem ad hozzáférést /bff/api/* endpoint-okhoz (SEC-FE-10)
```

**E.3 — Shop Floor task UI** *(2 nap)*

```
[session-id]   fe-e3-shopfloor-tasks
[task]
  1. ShopFloorTaskListScreen — touch-optimized, large rows
  2. ShopFloorTaskDetailScreen — touch buttons (64×64px+)
  3. RefetchInterval: 10 sec (Section 4.3, FE-09)
  4. Optimistic status update + rollback (FE-10)
  5. 5 perc no-touch → PIN re-entry (NEM full logout, SEC-FE-05)
  6. MSW: shopfloor handlers
  7. E2E: task list polling, status update flow
[output]
  - Shop Floor terminál teljes funkcióval
[gate]
  - Touch target audit: minden interaktív elem ≥ 64px
```

#### Track G — Integration + DoD (2 nap)

**G.1 — E2E + Lighthouse + bundle analysis** *(1 nap)*

```
[session-id]   fe-g1-integration
[task]
  1. Full E2E suite futtatás (≥ 39 flow, Section 8.2)
  2. Lighthouse Performance ≥ 90, Accessibility ≥ 95 (minden world)
  3. Bundle audit: app entry < 200KB, world < 50KB, heavy < 100KB
  4. CONTRACT_ISSUES drain
  5. SEC-FE CI gate-ek mind PASS verify
[output]
  - DoD Section 9 minden checkbox tickelve
```

**G.2 — Smoke test + deploy + retro** *(1 nap)*

```
[session-id]   fe-g2-deploy
[task]
  1. Production deploy portal.joinerytech.hu
  2. Smoke test: KC login → 5 world elérés
  3. Doorstar pilot user feedback (3 sales-rep + 2 production-lead + 1 shop-floor)
  4. Retro report: mi ment jól, mi javítandó v2-re
[output]
  - SpaceOS Portal v1 LIVE
  - Retro doc commit-elve
[gate]
  - 5 world mind elérhető és funkcionál
  - Pilot user-ek minimum 1 valós order/plan/task minden world-ben
```

#### Track összegzés

| Track | Sessions | Nap (sequential) | Nap (parallel-friendly) |
|---|---|---|---|
| A — Foundation | A.1–A.5 | 3 | 3 (kritikus út) |
| F — BFF | F.1–F.5 | 4 | 3 (parallel A-val) |
| B — Home + Settings | B.1–B.2 | 2 | 2 (függ A,F-től) |
| C — Sales | C.1–C.3 | 5 | 5 (parallel D,E-vel) |
| D — Production | D.1–D.3 | 5 | 5 (parallel C,E-vel) |
| E — Shop Floor | E.1–E.3 | 4 | 4 (parallel C,D-vel) |
| G — Integration | G.1–G.2 | 2 | 2 |
| **Sequential** | 24 sessions | **25 nap** | — |
| **Parallel critical path** | — | — | **~14 nap** |

### 10.5 Mailbox templates (tmux-dispatcher pattern)

A `spaceos-fe` és `spaceos-bff` tmux session-ök inbox-outbox-CONTRACT_ISSUES mailbox struktúrával dolgoznak. Minden session task egy fájl az inbox-ban.

#### Inbox template (egy session task)

Fájl: `~/spaceos-doorstar-portal/inbox/<session-id>.md`

```markdown
# [SESSION-ID]: <task-cím>

**Trigger:** v4-final / Section 10.4 / [session-id]
**Estimated effort:** <X> nap
**Prereq sessions:** <session-id-lista vagy "none">
**Reference document sections:** <section-list>

## Task

<részletes leírás Section 10.4-ből>

## Acceptance criteria (DoD-tickbox)

- [ ] <gate 1>
- [ ] <gate 2>
- ...

## Constraints

- <frozen pattern>
- <anti-pattern listából idézve>

## Output expectation

- Files changed: <directory list>
- Test count: ≥ <N> új teszt
- Build/lint/typecheck: zöld
- Outbox jelzés formátuma: lásd Outbox template
```

#### Outbox template (session DONE jel)

Fájl: `~/spaceos-doorstar-portal/outbox/<session-id>.done.md`

```markdown
# [SESSION-ID] DONE

**Completed:** YYYY-MM-DD HH:MM
**Duration:** <actual nap>
**Files changed:** <count>
**Tests added:** <count>

## Summary

<1-2 mondat: mi készült el>

## Acceptance criteria

- [x] <gate 1> — <verify-link vagy log>
- [x] <gate 2> — <verify-link vagy log>

## Build status

- pnpm build: ✅
- pnpm test: ✅ (<count> passing)
- pnpm lint: ✅
- pnpm typecheck: ✅
- pnpm size-limit: ✅ (vagy "N/A — Track A.5 előtt")

## Findings addressed

- FE-NN, SEC-FE-NN, BFF-NN: <melyek implementálva>

## CONTRACT_ISSUES update

<új issue, vagy "none">

## Hand-off to

<következő session-id, ami most unblocked>
```

#### CONTRACT_ISSUES template (FE↔BFF gap)

Fájl: `~/spaceos-doorstar-portal/CONTRACT_ISSUES.md` (cumulative)

```markdown
# CONTRACT_ISSUES — open

## CI-001: <rövid cím>

**Reported by session:** <session-id>
**Date:** YYYY-MM-DD
**Status:** OPEN | RESOLVED | DEFERRED-V2
**Severity:** BLOCKER | NON-BLOCKER

**Issue:**
<frontend és backend közti contract gap leírás>

**Expected:**
<mit várt a frontend a v4-final alapján>

**Actual:**
<mit szolgáltat a backend>

**Workaround:**
<mock fixture vagy interim megoldás>

**Resolution path:**
<who-when-what>
```

### 10.6 CRITICAL findings — explicit validation steps

A 4 CRITICAL finding mindegyikére **külön validation lépés** szükséges, mielőtt a v1 production deploy elindul. Ezek nem deployolhatóak gate-ek; G.1 integration session után, G.2 deploy előtt kötelező mind ellenőrizve.

#### CRITICAL #1 — SEC-FE-01: Token storage in-memory only

**Validation:**
```bash
# Production build
pnpm build

# Bundle grep
grep -r "localStorage.setItem" dist/assets/*.js | grep -i "token\|jwt\|access\|refresh"
# Expected: 0 match

# Runtime DevTools verify (manual)
# 1. Login a portálba
# 2. F12 → Application → Storage → localStorage
# 3. Verify: NINCS access_token, refresh_token, id_token kulcs
# 4. Verify: oidc-client-ts state nem persist localStorage-ben
```

**Pass criteria:** Zero match a bundle-ben + DevTools-ban localStorage 0 token kulcs.

#### CRITICAL #2 — SEC-FE-02: JWT payload trust elimination

**Validation:**
```bash
# Source-tree grep
grep -rE "jwtDecode|jose\.decodeJwt|jwt-decode" apps/joinerytech/src/
# Expected: 0 match

# ESLint enforcement
pnpm eslint apps/joinerytech/src/ --rule "no-restricted-imports:[error, paths:['jwt-decode','jose']]"
# Expected: 0 violations

# Manual code review
# Minden auth-derived state (user, roles, enabledModules, tenantId) /me/session-ből jön?
```

**Pass criteria:** Zero `jwt-decode` import + minden auth state /me/session response-ból.

#### CRITICAL #3 — SEC-FE-03: enabledModules single source + cross-tenant URL

**Validation:**
```bash
# Backend test (Orchestrator)
# 1. enabledModulesService használata MIND /me/session-ben MIND a route guard middleware-ben
grep -r "getEnabledModulesForTenant\|enabledModulesService" orchestrator/src/
# Expected: minden használat ugyanazt a service-t hívja

# E2E cross-tenant verify
# Test: user A token + URL B tenant order ID → 404 (NEM 403, NEM 200)
pnpm playwright test cross-tenant.spec.ts
```

**Pass criteria:** Single source verified + cross-tenant URL access → 404.

#### CRITICAL #4 — BFF-01: Abstractions BFF antipattern eliminated

**Validation:**
```bash
# Route grep
grep -r "/bff/abstractions" orchestrator/src/ apps/joinerytech/src/
# Expected: 0 match

# Configurator endpoint check
grep -r "/bff/api/configurator" apps/joinerytech/src/
# Expected: használva a Sales world configurator screen-en

# Snapshot check
ls @spaceos/api-client/snapshots/
# Expected: kernel.json, cutting.json, manufacturing.json (NINCS abstractions.json)
```

**Pass criteria:** Zero `/bff/abstractions` referencia + configurator a Kernel BFF-en + snapshot list helyes.

#### Final gate — pre-deploy CRITICAL checklist

```
[ ] CRITICAL #1 SEC-FE-01 — Token storage in-memory only verified
[ ] CRITICAL #2 SEC-FE-02 — JWT payload trust eliminated
[ ] CRITICAL #3 SEC-FE-03 — enabledModules single source + cross-tenant 404
[ ] CRITICAL #4 BFF-01 — Abstractions BFF antipattern eliminated
```

**Ha bármelyik CRITICAL gate FAIL → deploy STOP, finding újra-implementálva, retest.**

---

## 11. Mi jön utána (roadmap)

### 11.1 v2 horizon (3-6 hónap)

| Feature | Prereq | Blokkoló |
|---|---|---|
| **Spatial world** (Three.js 3D editor) | Modules.Spatial backend | Three.js bundle stratégia (separate route-level chunk + WebGL detect) |
| **Cabinet world** | Modules.Cabinet aggregate | Cabinet 0.4+ |
| **Brand skin layer aktiválás** (per-tenant) | Doorstar skin csomagolva | `@spaceos/brand-tokens` runtime override engedélyezése; D-02 unlock |
| **Shop Floor → external app entry-point** | Új nginx vhost + CI | Security review (v3 finding mitigáció) |
| **Cross-world dashboard** | BFF aggregator (`/bff/dashboard/kpi`) | Cabinet 0.3 Federation pattern adaptation |
| **Service Worker + offline** Shop Floor | TanStack Query persistor + IndexedDB cache | Shop Floor terminál hálózat-megszakadás scenario |

### 11.2 v3 horizon (6-12 hónap)

| Feature | Aktor | Megjegyzés |
|---|---|---|
| **Trader world** | `Trader` aktor-típus | Modules.Trading szükséges |
| **Logistics/Delivery world** | `Logistics` aktor-típus | Modules.Delivery |
| **Installer world** | `Installer` aktor-típus | Modules.Installation |
| **EndCustomer portál** | `EndCustomer` aktor-típus | Külön domain (B2C UX más, mint a B2B portál) |
| **Mobile-first responsive** | — | Sales + Production world mobile-optimalizáció |
| **PWA + push notification** | — | Shop Floor + Production lead értesítések |

### 11.3 Continuous

- a11y audit minden release-en (WCAG 2.1 AA → AAA célzott)
- Lighthouse CI automation a deploy pipeline-ban
- Bundle size regression alert (PR-szintű)
- E2E coverage mátrix (új world / új persona kombinációk)

---

## 12. Design → Code mapping (placeholder)

A design bundle (`https://api.anthropic.com/v1/design/h/Gf2t9hUM4KFNw5f9Kr8igw`) **404** v1 készítésekor. Ha v2-re a bundle elérhetővé válik, a következő tábla töltődik:

| Design fájl | React komponens | Csomag | Megjegyzés |
|---|---|---|---|
| `page-home.jsx` | `WorldHome.tsx` | app | TBD |
| `page-shopfloor.jsx` | `ShopFloorTaskListScreen.tsx` + `PinEntryScreen.tsx` | app | TBD |
| `ui.jsx → StatusPill` | `FsmBadge.tsx` (létezik) | `@spaceos/ui` | Esetleges merge / variant |
| `data-worlds.js` | `worldCatalog.ts` | `@spaceos/domain` | Mapping ellenőrzés |

---

*SpaceOS — Portal World Architecture v4 FINAL (Implementation-Ready) · 2026-04-29*
*5 world (Home, Sales, Production, Shop Floor, Settings) · Design-agnostic*
*Kumulált: 25 FE-NN + 17 SEC-FE-NN + 16 BFF-NN = 58 finding addressed (4🔴 · 10🟠 · 34🟡 · 10🟢)*
*4 CRITICAL gates Section 10.6-ban — pre-deploy mandatory*
*Implementation: 24 Claude Code session, ~14 nap critical path · Companion: SpaceOS_Portal_World_Architecture_v4_README.md*
