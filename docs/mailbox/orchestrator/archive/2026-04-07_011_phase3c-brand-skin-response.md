---
id: MSG-O011
from: architect
to: orchestrator
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3C"
---

# Phase 3C — `brandSkin` a token response-ban (SEC-P3C-07)

A Phase 3C security finding (SEC-P3C-07) szerint a `brand_skin` értéket a frontend **nem dekódolhatja JWT-ből** — azt az Orchestrator adja vissza a token response body-jában, RS256-verifikált JWT-ből kinyerve.

Ez minimális változás — 1 extra mező a meglévő token response-ban.

---

## Változás — `POST /bff/api/auth/token` response

### Jelenlegi response:
```typescript
interface TokenResponse {
  accessToken: string;
  expiresAt:   string;
}
```

### Új response (Phase 3C):
```typescript
interface TokenResponse {
  accessToken: string;
  expiresAt:   string;
  brandSkin:   string;   // ← ÚJ: RS256-verifikált JWT-ből kinyerve
}
```

### Implementáció — `src/routes/auth.route.ts`

Az Orchestrator már elvégzi a JWT RS256-verifikációt a `jwtVerify()` hívásban. A verifikált payload-ból olvasd ki a `brand_skin` claim-et:

```typescript
const decoded = await jwtVerify(token, publicKey);   // már létező verifikáció

return res.json({
  accessToken: token,
  expiresAt:   new Date((decoded.payload.exp ?? 0) * 1000).toISOString(),
  brandSkin:   (decoded.payload['brand_skin'] as string) ?? 'joinerytech',
  // ← 'joinerytech' a default — ha a tenant nem rendelkezik BrandSkinId-val
});
```

**Fontos:**
- A `brand_skin` claim a JWT-ben a Kernel `TokenCommandHandler`-je teszi bele (Kernel migration 0024 után, MSG-K028)
- Phase 3C-ban a legtöbb tenant `brand_skin` claim nélküli JWT-t kap → `'joinerytech'` default
- A Doorstar tenant JWT-jében: `brand_skin: 'doorstar'`

---

## `POST /bff/api/auth/refresh` — szintén frissítendő

Ha az Orchestrator a refresh endpoint-on is visszaad token response-t, ott is add hozzá a `brandSkin` mezőt azonos logikával.

---

## Security ellenőrzések

- [ ] `brandSkin` értéke kizárólag a RS256-verifikált JWT payload-ból jön — nem user-provided input
- [ ] Ismeretlen / hiányzó `brand_skin` claim → `'joinerytech'` default (nem error, nem crash)
- [ ] A response body-ban visszaadott `brandSkin` értéke nem befolyásolja a Kernel-oldali RBAC-ot

---

## Tesztek

```bash
npm test
```

Meglévő 150 teszt zöld marad. Új tesztek (`auth.route.test.ts` bővítés):

```
brandSkin response:
  JWT tartalmaz brand_skin='doorstar' → response.brandSkin === 'doorstar'
  JWT nem tartalmaz brand_skin → response.brandSkin === 'joinerytech'
  JWT brand_skin=null → response.brandSkin === 'joinerytech'
```

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-O011`:
- Token response frissítve: igen/nem
- Refresh endpoint is frissítve: igen/nem
- Teszt eredmény (pass/fail)
- Security review ✅ / ⚠️
