# SpaceOS — Session D Kickoff Decisions · FT-1..FT-5

> **Verzió:** v1.0 — 2026-04-20
> **Státusz:** ✅ APPROVED — 2026-04-20 (Gábor)
> **Hatálya alatt:** `SpaceOS_FreeTier_Architecture_v4.md` (Session D arch-planner)
> **Kapcsolódó:** `SpaceOS_Growth_Strategy_v1.md` · `SpaceOS_Q3_Planning_Brief_v1.md` Téma 1
> **Döntési session:** Architect ↔ Gábor — 2026-04-20 Claude.ai
> **Scope:** 5 architekturális döntés, amelyek nélkül Session D arch doc nem indítható

---

## 0. Purpose

A Growth Strategy v1 Decision Log (PQ1..PQ8 + FT1..FT8) rögzítette a FreeTier termékdöntéseket. Ez a dokumentum a **végrehajtási architektúra** még nyitott 5 kérdését zárja le (FT-1..FT-5), hogy a Session D arch-planner zöld úton induljon.

Minden döntés: Growth Strategy-val **konzisztens** vagy azon **túl lép** (explicit megjelölve).

---

## FT-1 — Synthetic tenant modell

### Status
🟢 PROPOSED · **User-scoped FreeTier.Api**

### Context
A FreeTier külön projekt (`SpaceOS.FreeTier.Api` :5007 — GS PQ5), saját DB schema (`freetier` — GS FT6). Az izolációs modell nyitott: tenant, user, vagy shared guest tenant?

### Decision
A FreeTier.Api **user-scoped** — minden magic-link felhasználó saját `user_id` alapon izolálódik. A Kernel Tenant koncepciót **nem** alkalmazzuk a FreeTier-ben.

### Rationale
- Skálázhatóság: 10k guest user ≠ 10k Kernel tenant
- Egyszerűség: RLS `user_id = current_setting('app.user_id')` elégséges
- Upgrade tisztasága: `FreeTier.user_id` → új Kernel `tenant_id` mapping egy jól definiált esemény

### Consequences
- Saját adatmodell: `FreeTierUser` aggregate (nem `Tenant`)
- Saját `UserSessionInterceptor` (nem `TenantSessionInterceptor`)
- Domain réteg nem osztható meg a Kernel-lel (elfogadott trade-off)
- Upgrade esemény: `FreeTierUpgrade` domain service — Keycloak tenant create + Kernel seed

### Growth Strategy konzisztencia
✅ Konform · GS nem specifikálta a belső izolációt, ez a gap kitöltése

---

## FT-2 — Session store + rate limit store

### Status
🟢 PROPOSED · **Redis anonymous + PG post-auth + Redis rate limit counter**

### Context
GS PQ3: permanent tárolás email auth után · 10 min anonymous. GS FT6: shared PG `freetier` schema a permanent állapotra. Nyitott: hol él a 10 perces anonymous session és a rate limit counter?

### Decision
- **Anonymous session (10 min):** Redis 7.4 TTL key (`sess:{fingerprint}`)
- **Post-auth workspace (permanent):** PostgreSQL `freetier.workspaces` (permanent, auditálható)
- **Rate limit counter:** Redis sliding window (`rl:{scope}:{fingerprint}`)

### Rationale
- Natív TTL támogatás Redis-ben — PG cron cleanup elkerülhető
- Memória-sebességű rate limit — anonymous traffic (potenciálisan magas QPS) nem terheli a PG-t
- Ops egyszerűség: egyetlen Redis szolgál mindkét use case-t
- Single Redis instance v1, cluster csak v2-ben (vagy ha load indokolja)

### Consequences
- Új infra komponens: Redis 7.4 self-hosted VPS-en, `redis://127.0.0.1:6379`
- Új approved NuGet: `StackExchange.Redis` (MIT)
- Redis AOF persistence **off** (10 min TTL nem igényel disk survival)
- Backup/HA: v1 nincs (single point of failure elfogadott — cache-only data)
- Session D infra section: Redis systemd unit + UFW loopback-only

### Growth Strategy konzisztencia
✅ Konform · GS §9 "Redis deployment — infra upgrade Session A körül" ide konkretizálódik

---

## FT-3 — URL stratégia

### Status
🟢 PROPOSED · **`eszkozok.joinerytech.hu` külön subdomain (+ EN mirror)**

### Context
GS PQ8 a **path-ot** adta meg (`/eszkozok/szabaszat-optimalizalo`), de a **host** nyitott: Portal subpath vs. subdomain vs. külön domain?

### Decision
- HU: `eszkozok.joinerytech.hu`
- EN mirror: `tools.joinerytech.com` (v1.5-ben)
- DACH tükör (v2): `werkzeuge.asztalostech.hu`

A landing + workspace + PDF preview **mind** ezen a subdomain-en fut.

### Rationale
- Domain authority: subdomain SEO reputation transfer megbízhatóan működik (Google utolsó 5 év jelzés)
- Clean service separation: `portal.joinerytech.hu` (Kernel tenant) és `eszkozok.joinerytech.hu` (FreeTier) külön CSP, külön cookie scope, külön rate limit profile
- Wildcard cert `*.joinerytech.hu` már megvan (SAN, Let's Encrypt) — nincs +cert
- Portal bundle-ja nem hízik a FreeTier kóddal

### Consequences
- Új nginx vhost: `/etc/nginx/sites-available/spaceos-freetier`
- DNS: `eszkozok.joinerytech.hu` A record (vagy CNAME) a VPS-re
- `X-SpaceOS-Brand: freetier` header injection host-alapon (nginx)
- Portal nginx config **változatlan** — teljes service elszigetelés

### Growth Strategy konzisztencia
✅ Konform · GS PQ8 path-szintű volt, ez a host-szintű kiterjesztés

---

## FT-4 — Rate limit enforcement PRIMARY layer

### Status
🟢 PROPOSED · **FreeTier.Api middleware + Redis counter**

### Context
GS PQ4 "mind az 5 layer" — defense in depth. Nyitott: melyik a PRIMARY (business limit), és melyek a supplementary (anti-DoS)?

### Decision

| Layer | Szerep | Típus |
|---|---|---|
| Cloudflare Turnstile | Bot-szűrés submission előtt | Supplementary |
| Nginx `limit_req` | IP-alapú DoS edge védelem (20 req/s) | Supplementary |
| Orchestrator | **NEM ÉRINTETT** — FreeTier.Api nem proxy-zott Orch-on keresztül | — |
| **FreeTier.Api middleware** | **Business logic rate limit** (scope + Redis counter) | ✅ **PRIMARY** |
| DB unique constraints | Anti-duplikáció last line of defense | Supplementary |

**Limit profile-ok:**

| Scope | Anonymous | Magic-link auth'd |
|---|---|---|
| `nest:submit` | 3/óra | 20/nap |
| `workspace:save` | — (nincs anonymous save) | 100/nap |
| `share:generate` | — | 20/nap |
| `export:pdf` | 1 (preview only) | 10/nap |

Sliding window algoritmus, 5 perc bucket.

### Rationale
- Orchestrator-t bevezetni a FreeTier út elejére: felesleges hop, nincs tenant resolver szükség, nincs Kernel proxy
- Redis counter natív: `INCR` + `EXPIRE` két művelettel atomikus sliding window
- Business scope-onkénti külön kulcs (`nest` vs `export`) finomhangolható tier-enként (Growth Strategy FT8 paywall granularitás)

### Consequences
- FreeTier.Api middleware chain: `Turnstile → NginxEdge → RateLimitMiddleware(Redis) → Handler`
- 429 response: `Retry-After` + `X-RateLimit-Reset` ISO timestamp
- Config fájl: `freetier.ratelimits.json` (tenant-agnostic, host-scoped)
- Monitoring: Plausible event `ratelimit_hit` + 429 log → Grafana alert v2

### Growth Strategy konzisztencia
✅ Konform · GS PQ4 "mind az 5 layer" mind megvan, PRIMARY explicit megjelölve

---

## FT-5 — Upgrade flow v1 mechanizmus

### Status
🟢 PROPOSED · **Manuális: form → Slack notif + admin provisioning**

### Context
GS FT2: hibrid (own auth → Keycloak upgrade). GS §12: "Upgrade flow stubbed v1, full v2". Nyitott: ki provisionolja a Kernel tenant-ot, és hogyan mozog a workspace?

### Decision

**v1 folyamat (Q3 2026):**
1. FreeTier workspace "Upgrade to SpaceOS" CTA → form
2. Form mezői: company name, VAT, contact email, expected user count, expected monthly nest volume
3. Backend: `FreeTierUpgradeRequested` domain event → Slack webhook `#spaceos-sales` + email `sales@joinerytech.hu`
4. Admin (Gábor vagy jövőbeli sales hire): 
   - Keycloak realm tenant create (manuális)
   - Kernel `seed-tenant.sh` futtatás VPS-en
   - Workspace JSON export → import új tenant-ba
   - Credentials email a user-nek (30 nap trial)
5. 30 nap után Stripe checkout link v1.5 (külön feladat)

**v2 folyamat (Q4 2026 vagy később):**
- Stripe checkout session flow
- `stripe.customer.subscription.created` webhook → `ProvisionTenantAsync(upgradeRequestId)`
- Keycloak Admin REST API: realm + user auto-create
- Kernel `/api/tenants` auto-seed (KERNEL-090 után működik)
- Automated workspace migration: `FreeTier.workspace_id` → Kernel `Joinery.project_id`

### Rationale
- v1 scope realizmus: Doorstar az egyetlen manufacturer, sales volumen alacsony — manuális provisioning elégséges 5-10 upgrade-ig
- Korai tanulás: manuális folyamat során feltárul, mi az a pár mező amit automata-val is másolni kell
- Stripe v1-ben elkerülhető — jelentős integráció, v1 nem blokkolja
- Workspace export formátum viszont v1-ben is kell: `WorkspaceExportV1` record JSON-ba szerializálva

### Consequences
- `UpgradeRequest` aggregate a FreeTier.Api-ban (status: Pending → Approved → Provisioned → Completed)
- `FreeTierUpgradeRequested` domain event + Slack webhook handler
- `WorkspaceExportV1` DTO — Session D deliverable spec része
- Brevo email template HU/EN — transactional flow
- **NEM része v1-nek:** Stripe SDK, Keycloak Admin API integráció, Kernel auto-seed

### Growth Strategy konzisztencia
✅ Konform · GS FT2 + §12 "stubbed v1" explicitté téve

---

## Összesítés — Delta a Growth Strategy becsléshez

| Komponens | GS becslés (§5.6) | FT-1..FT-5 után | Delta | Indoklás |
|---|---|---|---|---|
| `SpaceOS.FreeTier.Api` projekt + systemd + nginx | 1 | 1 | — | — |
| `SpaceOS.Nesting.Algorithms` NuGet extract | 1 | 0 | -1 | ✅ KÉSZ (2026-04-20, commit 3e87954) |
| `POST /nest` anonymous + Turnstile | 1 | 1 | — | — |
| FreeTier auth (magic link + session + expiry) | 2 | 2 | — | — |
| FreeTier user + workspace model + migrations | 1.5 | 1 | -0.5 | User-scoped egyszerűbb mint tenant-scoped (FT-1) |
| Share token + read-only + export | 1 | 1 | — | — |
| L2 interaktív SVG visualization | 2 | 2 | — | — |
| 4 LabelStrategy + QRCoder | 1.5 | 1.5 | — | — |
| Post-result full report UX | 1 | 1 | — | — |
| QuestPDF branded PDF | 1 | 1 | — | — |
| Landing page (EN + HU) | 2 | 2 | — | — |
| Plausible + Brevo | 1 | 1 | — | — |
| Privacy + GDPR DPA | 1 | 1 | — | — |
| Upgrade flow stubbed v1 | 1 | 0.5 | -0.5 | Slack notif only (FT-5) |
| **Redis deploy + integration** | 0 (implicit) | 0.5 | +0.5 | FT-2 explicit |
| Tesztek | 2 | 2 | — | — |
| **Összesen** | **19** | **17.5** | **-1.5** | Net javulás |

---

## Jóváhagyás

| Fél | Döntés | Dátum |
|---|---|---|
| **Gábor** (Founder · Decision maker) | ✅ FT-1 · ✅ FT-2 · ✅ FT-3 · ✅ FT-4 · ✅ FT-5 — mind elfogadva | 2026-04-20 |
| **Architect** (Design lead) | ✅ Mini-spec technikailag zárva | 2026-04-20 |

### Ha egy döntésnél Gábor más irányt választ

- FT-1: `user-scoped` helyett `tenant-per-user` → +3 dev nap (Kernel-kompatibilis, de overkill)
- FT-2: Redis helyett PG-only → -0.5 nap infra, +1 nap cron cleanup worker
- FT-3: Subdomain helyett Portal subpath → -0.5 nap nginx, +1 nap Portal bundle split
- FT-4: FreeTier.Api helyett Orch middleware → +1 nap Orch route config + tenant resolver override
- FT-5: Manuális helyett full-auto Stripe → +8-12 dev nap (külön sprint)

---

## Következő lépés (Session D indító gate)

Gábor jóváhagyás után:

1. Architect átdolgozza ezt a dokumentumot `APPROVED` státuszra
2. Skill aktiválás: `/spaceos-arch-planner` a Session D arch doc-ra (skeleton már készen: `SpaceOS_FreeTier_Architecture_v4_Skeleton.md`)
3. Első review pipeline lefut: `sub-database-designer` + `sub-database-schema-designer` → v2
4. Utána: `sub-senior-security` → v3
5. Végül: `sub-senior-backend` → v4 (implementation-ready)

Becsült design effort Session D-re: **~2 dev nap** (architect session hossz).
