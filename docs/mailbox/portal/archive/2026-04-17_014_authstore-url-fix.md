---
id: MSG-PORTAL-014
from: root
to: portal
type: task
priority: critical
status: UNREAD
ref: DEBUG-001
created: 2026-04-17
---

# PORTAL-014 — authStore `/bff/api/auth/me` URL fix

## Probléma

`https://joinerytech.hu/` folyamatosan újra tölt. Root ok:

`packages/@spaceos/api-client/src/stores/authStore.ts:55` hibás URL-t hív:

```ts
// HIBÁS:
const res = await fetch('/bff/api/auth/me', { ... });

// HELYES (Orchestrator route: GET /bff/auth/me):
const res = await fetch('/bff/auth/me', { ... });
```

A `/bff/api/auth/me` → 404 → auth fail → redirect → 404 → végtelen loop.

## Feladat

1. Javítsd az URL-t `authStore.ts`-ben: `/bff/api/auth/me` → `/bff/auth/me`
2. Ellenőrizd, hogy nincs más hely ahol `/bff/api/auth/` prefix szerepel
3. `pnpm turbo build` — 0 error
4. Deploy: dist csere + nginx reload (mint INFRA-151-ben)

```bash
cd /opt/spaceos/design-portal
# fix authStore.ts
pnpm turbo build
# dist csere:
sudo rm -rf /opt/spaceos/design-portal/apps/joinerytech/dist.bak
sudo cp -r /opt/spaceos/design-portal/apps/joinerytech/dist /opt/spaceos/design-portal/apps/joinerytech/dist.bak
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

5. Verify: `curl -s https://joinerytech.hu/bff/auth/me` → 401 (nem 404)
6. Böngészőben ellenőrizd: az oldal nem tölt újra folyamatosan

## DONE feltételek

- [ ] `authStore.ts` javítva (`/bff/auth/me`)
- [ ] `pnpm turbo build` 0 error
- [ ] `/bff/auth/me` → 401 (nem 404) — curl verify
- [ ] Browser: nincs redirect loop
- [ ] Commit + push
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
