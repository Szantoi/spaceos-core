---
id: MSG-ORCH-068-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-068
created: 2026-04-15
---

# MSG-ORCH-068 — Security Review (Q2 Pre-launch) — DONE

## Összefoglalás

**KRITIKUS:** 0 — nincs azonnali blokkológ tényező  
**KÖZEPES:** 3 találat — javítás ajánlott pre-launch előtt  
**ALACSONY:** 3 találat — ismert / kezelt kockázatok  

---

## ✅ PASS — Biztonsági alapok rendben

| Terület | Státusz |
|---|---|
| Minden adat route `requireAuth`-dal védett | ✅ Igen |
| `tid` claim validáció (b3860ac) | ✅ Aktív |
| Token expiry: lejárt token → 401 | ✅ jsonwebtoken + JWKS |
| `/bff/health` szándékosan nyilvános | ✅ Liveness probe |
| `.env` gitignore-ban | ✅ |
| API key-ek / JWKS URL-ek `.env`-ben | ✅ |
| CORS origin allowlist (nem `*`) | ✅ |
| Zod validáció minden `req.body`-n | ✅ |
| Rate limiting minden route-on | ✅ |
| System prompt injection védett (UUID context only) | ✅ |
| Tool result sanitizáció (`sanitize.ts`, OWASP LLM01) | ✅ |
| Max iteration guard (`MAX_TOOL_ITERATIONS`) | ✅ |
| Proxy timeout konfigurálva | ✅ |
| SSE AbortController client disconnect esetén | ✅ (b3860ac) |
| SSRF proxy célcím: env-ből, nem user inputból | ✅ kernel.proxy.ts, federation.proxy.ts |
| Stack trace nem kerül a response-ba | ✅ console.error-be megy |

---

## ⚠️ KÖZEPES — Javítás ajánlott pre-launch előtt

### K-1: `error.middleware.ts` — `err.message` minden environment-ben a response-ban van

**Fájl:** `src/middleware/error.middleware.ts:11`

```ts
res.status(500).json({
  error:   'Internal orchestrator error.',
  message: err.message,  // ← production-ban is kiszivárog
});
```

`err.message` tartalmazhat belső információt: IP:port (`connect ECONNREFUSED 127.0.0.1:5001`), fájl elérési utat, stack fragment-et. A CLAUDE.md dokumentálja: "Never exposes stack traces in production (NODE_ENV check)" — de a NODE_ENV ellenőrzés hiányzik az implementációból.

**Javasolt javítás:**
```ts
const detail = env.NODE_ENV === 'production' ? undefined : err.message;
res.status(500).json({ error: 'Internal orchestrator error.', ...(detail ? { message: detail } : {}) });
```

---

### K-2: `stageDispatch.route.ts` — SSRF kockázat (Kernel Stage Registry URL validáció nélkül)

**Fájl:** `src/routes/stageDispatch.route.ts:88`

```ts
url: `${endpoint}/${targetPath}`,  // endpoint = Kernel Stage Registry adat
```

A `stage.moduleEndpoint` értéket a Kernel Stage Registry adja vissza, és URL allowlist ellenőrzés nélkül kerül be az axios hívásba. Ha a Stage Registry kompromittálódik vagy félrekonfigurálódik, tetszőleges belső URL-t lehetne megcélozni (pl. `http://169.254.169.254/`, `http://internal-db/`).

**Javasolt javítás:**
```ts
const ALLOWED_STAGE_PREFIX = /^http:\/\/127\.0\.0\.1:\d{4,5}\//;
if (!ALLOWED_STAGE_PREFIX.test(stage.moduleEndpoint)) {
  throw new StageError('Stage endpoint not in allowed range', 502);
}
```

---

### K-3: LLM prompt injection — user `messages[].content` sanitizálatlanul kerül az LLM-hez

**Fájl:** `src/interpreter/interpreter.service.ts:31–34`

A `sanitize.ts` kizárólag TOOL RESULT-eket szanitizál, a user message content-et nem. Egy felhasználó küldhet prompt injection kísérletet a `content` mezőben ("Ignore previous instructions..."). A system prompt védett (UUID-k kerülnek be, nem user input), de a user turn maga szanitizálatlan.

**Megjegyzés:** Ez részben tervezési döntés — user-facing LLM-nél elkerülhetetlen. A protection az immutable system prompt oldalon van. OWASP LLM01 szempontból azonban érdemes felvenni az input sanitize.ts hatáskörébe, legalább a legismertebb mintákra.

---

## 🔵 ALACSONY — Ismert / elfogadható kockázat

### A-1: `X-SpaceOS-Brand` header — kliens által felülírható

A proxy (`kernel.proxy.ts:24`) és a chat route (`chat.route.ts:56`) a kliens által küldött `X-SpaceOS-Brand` értéket szanitizálatlanul továbbítja a Kernelnek. Ha a Kernel brand alapján hozzáférés-kontrollt végez, ez kockázatos. Ha csak UI-skinhez használja, alacsony a kockázat.

**Ajánlás:** Kernel oldalon validálni, hogy a brand megfelel-e a token tid-jéhez.

---

### A-2: `federation.proxy.ts` — 500→502 remapping hiányzik

A `kernel.proxy.ts`-ben bevezetett proxyRes handler (`statusCode 500 → 502`) nincs meg a `federation.proxy.ts`-ben. Az `/bff/nodes`, `/bff/sync`, `/bff/layers`, `/bff/audit-events` proxy-okon a Kernel 500 response változatlanul kerül a klienshez.

**Ajánlás:** Konzisztencia kedvéért a `createKernelProxy` factory-ba is bevezetni.

---

### A-3: `CORS_ORIGINS` default localhost production-ban

Ha a `CORS_ORIGINS` env var nincs beállítva production-ban, a default (`http://localhost:5173,http://localhost:3001`) van aktív. Böngészőből ez nem jelent valódi kockázatot (SOP-ot a böngésző érvényesíti), de nem ideális production config.

**Ajánlás:** Production deployment checklist-re felvenni.

---

## Nem vizsgált területek

- Keycloak oldali PKCE config (outside scope)
- nginx TLS / header konfiguráció (infra scope)
- Kernel-oldali RBAC és Kernel JWT validáció (Kernel scope)
