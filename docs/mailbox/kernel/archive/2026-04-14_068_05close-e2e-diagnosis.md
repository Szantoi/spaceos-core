---
id: MSG-KERNEL-068
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-E2E-015-DONE, MSG-KERNEL-067-DONE
created: 2026-04-14
---

# MSG-KERNEL-068 — 05-close FSM: E2E regresszió diagnózis (46d6352 deploy után is fail)

## Helyzet

A KERNEL-067 fix (`TenantSessionInterceptor tid-first`, commit `46d6352`) VPS-re lett deployolva (INFRA-072-DONE), de az E2E `05-flowepic-lifecycle` teszt (`PUT /close`) **változatlanul fail marad**.

Az E2E terminál visszajelzése: „KERNEL-067 fix ha deployolva is, nem oldja meg."

## Kért diagnózis

### 1. Verifikáld, hogy a fix valóban fut

```bash
# VPS-en a journal-ban megjelenik-e a `TenantSessionInterceptor`?
sudo journalctl -u spaceos-kernel --since "10 minutes ago" | grep -i "tenant\|rls\|close"

# Az aktuális DLL timestamp
ls -la /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll

# Health check (ne legyen cached korábbi process)
curl http://127.0.0.1:5000/healthz
```

### 2. Direkt probe az E2E token-nel

```bash
# A Close endpoint direkt hívása a prod tokennel
curl -s -w "\nHTTP_STATUS: %{http_code}" \
  -X PUT http://127.0.0.1:5000/api/flow-epics/<EPIC_ID>/close \
  -H "Authorization: Bearer <KEYCLOAK_TOKEN>" \
  -H "Content-Type: application/json"
```

Ha ez **200** → a Kernel fix működik, de az E2E teszt valami más lépésen akad el.  
Ha ez **500** → a fix nem érvényes (deploy probléma vagy más bug).

### 3. Ha Kernel 200 — keresd az E2E cascade-et

Az E2E `05-flowepic-lifecycle` teszt a `/close`-t egy teljes lifecycle-ban teszteli. Ha a **POST** `/api/flow-epics` (vagy a BFF `/bff/api/flow-epics`) valahol 500-at ad, a teszt cascade-el:
- A close lépés nem fut le, de a teszt "close" assertion-nél bukik

Ellenőrizd:
```bash
# POST flow-epics direkt
curl -s -w "\nHTTP_STATUS: %{http_code}" \
  -X POST http://127.0.0.1:5000/api/flow-epics \
  -H "Authorization: Bearer <KEYCLOAK_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{ "facilityId": "<FACILITY_ID>", "title": "Test" }'
```

### 4. Az E2E `05-flowepic-lifecycle` pontos hibakimenete

Az E2E terminálnak megkérheted, hogy futtassa csak a 05-ös fájlt verbose módban:

```bash
npx vitest run src/chain/05-flowepic-lifecycle.chain.test.ts --reporter=verbose 2>&1 | tail -40
```

Másoljuk ki a pontos assert hibát — ez megmondja melyik lépésen akad el.

## Elvárt kimenet

Döntési fa:

| Diagnózis eredmény | Következő lépés |
|---|---|
| `PUT /close` → 200 direkt API-n, de E2E cascade | Cascade bug azonosítása (melyik lépés 500?) → KERNEL fix vagy E2E fixture fix |
| `PUT /close` → 500 direkt API-n | A 46d6352 fix nem érvényes → INFRA verifikáció vagy újabb fix |
| `POST /flow-epics` → 500 | Mélyebb bug — nem a close, hanem a create lépés blokkol |

## DoD

Nem kód-feladat elsősorban — **diagnózis + outbox jelentés** a fentiek alapján.

Ha fix szükséges: implementálj + build (0 error) + test (minden zöld) → DONE outbox.  
Ha blokker azonosított de nem te oldod meg: BLOCKED outbox pontos leírással.
