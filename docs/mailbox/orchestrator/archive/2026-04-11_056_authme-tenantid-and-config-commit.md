---
id: MSG-ORCHESTRATOR-056
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-E2E-001-DONE
created: 2026-04-11
---

# MSG-ORCHESTRATOR-056 — /bff/auth/me tenantId fix + JWKS config commit

## Kontextus

Az E2E terminál (MSG-E2E-001-DONE) a JWT chain javítása során két Orchestrator-t érintő változtatást azonosított:

1. **VPS `.env` módosítás** — commitálni kell a repóba
2. **`/bff/auth/me` bug** — `tenantId: undefined` a válaszban

---

## 1. feladat — JWKS/JWT config commit

A VPS-en alkalmazott `.env` módosítás:

```diff
- JWKS_URI=http://localhost:8080/realms/spaceos/protocol/openid-connect/certs
- JWT_ISSUER=http://localhost:8080/realms/spaceos
+ JWKS_URI=http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/certs
+ JWT_ISSUER=https://joinerytech.hu/auth/realms/spaceos
```

Commitáld be a `.env.example` (vagy `config/` fájl) változtatást a `spaceos-orchestrator` repóba, és frissítsd a VPS-en is ha szükséges.

---

## 2. feladat — /bff/auth/me tenantId visszaadása

### Tünet (E2E 28-keycloak-auth, 1 fail)

```
GET /bff/auth/me → 200, de tenantId: undefined
```

### Elvárás

```json
{
  "sub": "...",
  "email": "...",
  "tenantId": "<uuid>",
  "roles": [...]
}
```

### Teendő

A Keycloak JWT-ben a `tenantId` claim neve valószínűleg `tenant_id` vagy egy custom mapper által beállított claim. Az `/bff/auth/me` handler-ben:

1. Ellenőrizd, milyen claim névvel kerül be a `tenantId` a tokenbe (Keycloak Script Mapper vagy custom attribute)
2. Az auth/me endpoint response-ban map-eld ki ezt a claim-et

Valószínű fix (az aktuális handler-ben):

```typescript
// authMe.route.ts (vagy hasonló)
const tenantId = decoded['tenant_id'] ?? decoded['tenantId'] ?? decoded['https://spaceos.io/tenantId'];
```

Ha a claim egyáltalán nincs a tokenben, INFRA terminálnak kell Keycloak mapperre. Ezt az esetet jelezd MSG-ORCHESTRATOR-056-DONE-ban.

---

## Definition of Done

- [ ] JWKS/JWT config változtatás commitálva (`spaceos-orchestrator` repo)
- [ ] `GET /bff/auth/me` válaszban `tenantId` nem undefined
- [ ] E2E 28-keycloak-auth összes assertion zöld
- [ ] Meglévő 176 teszt zöld
- [ ] 0 TS build error

## Visszajelzés

Outboxba: `MSG-ORCHESTRATOR-056-DONE`

Ha a tenantId a tokenből hiányzik (INFRA-t igényel), jelezd a DONE-ban.
