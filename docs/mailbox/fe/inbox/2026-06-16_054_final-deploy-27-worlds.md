---
id: MSG-FE-054
from: root
to: fe
type: task
priority: critical
status: READ
ref: MSG-FE-053
created: 2026-06-16
---

# FE-054 — Végső deploy: 27/27 világ LIVE

## Státusz

FE-B DONE ✅ — Raktár bővítés + Szerviz világ, 742 teszt, 27/27 világ implementálva.

## Feladat

Végezd el a végső production deployt:

```bash
cd /opt/spaceos/frontend/joinerytech-portal
git pull origin main
pnpm install
pnpm build
sudo cp -r dist/. /var/www/joinerytech/
sudo nginx -t && sudo systemctl reload nginx
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/
```

## DONE feltételek

- `pnpm build` — 0 hiba
- `pnpm test` — mind zöld
- `curl https://joinerytech.hu/` — HTTP 200
- 27/27 világ elérhető a Home-on

## Outbox

Írj DONE üzenetet: `docs/mailbox/fe/outbox/2026-06-16_054_final-deploy-done.md`
