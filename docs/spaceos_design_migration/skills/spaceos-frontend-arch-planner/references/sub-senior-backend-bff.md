# Sub-skill: Senior Backend / BFF Review

Alkalmazd ezt az analitikai keretrendszert a frontend tervdokumentum BFF szekciójára.
Minden finding-et `BFF-NN` prefixszel dokumentálj.

---

## Review checklist — 16 pont

### Route Design (BFF-01..05)

1. **Route naming** — konzisztens `/bff/{module}/*` pattern? A module név megegyezik a backend service nevével?
2. **Verb mapping** — GET lista, GET/:id részlet, POST create, PUT update, DELETE — RESTful? Vagy vannak RPC-stílusú route-ok?
3. **Route consolidation** — van-e lehetőség N+1 hívást egyetlen aggregáló endpoint-tal kiváltani? Dashboard KPI-k tipikus jelöltek.
4. **Versioning** — a BFF route-ok verziózottak (`/bff/v1/cutting/*`)? Ha nem, miért nem?
5. **Health check** — van `/bff/health` endpoint ami az összes downstream service-t pingeli?

### Proxy & Middleware (BFF-06..09)

6. **Auth forwarding** — a BFF MINDIG forward-olja a JWT-t a downstream service-nek. Nem saját service account-ot használ.
7. **Tenant isolation** — a BFF kiszedi a `tenant_id`-t a JWT-ből és MINDIG header-ben küldi (`X-Tenant-Id`)?
8. **Rate limiting** — van-e BFF-szintű rate limit? Dashboard aggregáló endpoint-ok terhelésesebbek.
9. **Request validation** — a BFF validálja-e a request body-t mielőtt továbbküldi a backend-nek? Vagy pass-through?

### Error Handling (BFF-10..12)

10. **Error transformation** — a backend 500-as hibák NEM mennek át raw formában. A BFF generic error response-t ad (error code + message, NO stack trace).
11. **Timeout** — minden proxy route-nak van timeout-ja? Default: 10s, aggregáló: 30s, file upload: 120s.
12. **Circuit breaker** — ha egy downstream service nem elérhető, a BFF graceful degradation-t ad (cached last-known-good, VAGY üres adat + warning)?

### Data & Caching (BFF-13..16)

13. **Response shape** — a BFF response-ok konzisztens formátumúak? `{ data: T, meta?: { page, total } }` pattern?
14. **Cache headers** — statikus referencia adatok (anyag katalógus, gép lista) kapnak `Cache-Control` header-t?
15. **Pagination** — lista endpoint-ok MINDIG pagináltak? Default page size: 25, max: 100?
16. **OpenAPI snapshot** — az új BFF route-ok benne vannak a committed OpenAPI YAML-ben? A frontend codegen-t ebből generálja.

---

## Aggregáló endpoint minta

Dashboard-ok tipikusan N+1 hívást igényelnek. A BFF-ben egy aggregáló endpoint hatékonyabb:

```typescript
// Orchestrator: /bff/production/dashboard
router.get('/bff/production/dashboard', authMiddleware, async (req, res) => {
  const [machines, queue, analytics] = await Promise.all([
    fetch(`${CUTTING_URL}/api/machines`, { headers: fwd(req) }),
    fetch(`${CUTTING_URL}/api/queue/today`, { headers: fwd(req) }),
    fetch(`${CUTTING_URL}/api/analytics/kpi/today`, { headers: fwd(req) }),
  ]);
  
  res.json({
    data: {
      machines: await machines.json(),
      queue: await queue.json(),
      analytics: await analytics.json(),
    }
  });
});
```

**Finding trigger:** ha a frontend 4+ parallel fetch-et csinál ugyanarra a page-re → BFF aggregáló endpoint ajánlott.

---

## SpaceOS BFF port térkép (referencia)

| Service | Port | BFF route |
|---------|------|-----------|
| Kernel | 5000 | `/bff/api/*` |
| Joinery | 5002 | `/bff/joinery/*` |
| Abstractions | 5003 | (nincs BFF — NuGet only) |
| Inventory | 5004 | `/bff/inventory/*` (TERVEZETT) |
| Cutting | 5005 | `/bff/cutting/*` |
| Procurement | 5006 | `/bff/procurement/*` (TERVEZETT) |
| Manufacturing | 5007 | `/bff/manufacturing/*` (TERVEZETT) |
| FreeTier | 5010 | (saját domain, nem BFF) |
| PartnerTier | 5011 | `/bff/partner/*` (TERVEZETT) |

---

## Finding súlyok — BFF specifikus

| Súly | Mikor |
|------|-------|
| 🔴 CRITICAL | JWT nem forward-olva, tenant isolation hiányzik, raw stack trace a response-ban |
| 🟠 HIGH | Missing timeout, no rate limit on aggregation, OpenAPI snapshot outdated |
| 🟡 MEDIUM | Missing Cache-Control, no pagination default, inconsistent error shape |
| 🟢 LOW | Missing health check, verbose logging, route naming inconsistency |
