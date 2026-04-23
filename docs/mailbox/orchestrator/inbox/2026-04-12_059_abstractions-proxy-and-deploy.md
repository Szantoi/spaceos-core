---
id: MSG-ORCH-059
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-E2E-005-DONE-K1
created: 2026-04-12
---

# MSG-ORCH-059 — Abstractions BFF proxy + VPS deploy

## Kontextus

Két teendő egyben:

1. **Abstractions BFF proxy hiányzik** — az E2E terminál nem tudja megírni a `34-abstractions` tesztet, mert nincs `/bff/abstractions/*` route az Orchestratorban
2. **ORCH-058 fix (commit `9d02196`) nincs VPS-en** — a `requireAuth` chat middleware sorrend fix commitálva, de pm2 restart nem történt → `26-sse-chat-live` E2E teszt még mindig 429-et kap 401 helyett

---

## 1. feladat — `/bff/abstractions/*` proxy route

Hozz létre egy új route fájlt a Joinery proxy mintájára:

```typescript
// src/routes/abstractions.route.ts (minta: joinery.route.ts)

import { Router } from 'express';
import { createProxyMiddleware } from 'http-proxy-middleware';

const ABSTRACTIONS_BASE_URL = process.env.ABSTRACTIONS_BASE_URL ?? 'http://127.0.0.1:5003';

const abstractionsProxy = createProxyMiddleware({
  target: ABSTRACTIONS_BASE_URL,
  changeOrigin: false,
  pathRewrite: { '^/bff/abstractions': '/api' },  // vagy amilyen prefix az Abstractions API-n van
});

const router = Router();
router.use('/', abstractionsProxy);

export default router;
```

Regisztrálás `src/index.ts`-ben:
```typescript
import abstractionsRouter from './routes/abstractions.route';
// ...
app.use('/bff/abstractions', requireAuth, abstractionsRouter);
```

**Env var** — `.env` + VPS `orchestrator.env`:
```ini
ABSTRACTIONS_BASE_URL=http://127.0.0.1:5003
```

**Ellenőrizd** az Abstractions API path prefix-ét (`/api/...` vagy root `/...`) — a `pathRewrite` ennek megfelelően állítandó.

---

## 2. feladat — VPS deploy (ORCH-058 + ORCH-059 együtt)

```bash
cd /opt/spaceos/spaceos-orchestrator
git fetch origin
git checkout develop
git pull origin develop
# Ellenőrzés:
git log --oneline -3  # 9d02196 (requireAuth fix) és az új commit látható legyen

# pm2 restart
pm2 restart spaceos-orchestrator
pm2 status

# Ellenőrzés:
curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/bff/abstractions/health
# Elvárás: 200 (vagy 401 ha auth szükséges — nem 404)

curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/bff/chat  # no auth
# Elvárás: 401 (nem 429)
```

---

## Definition of Done

- [ ] `GET /bff/abstractions/health` → 200 (Orchestrator proxy-z az 5003-ra)
- [ ] `GET /bff/abstractions/...` no-auth → 401 (requireAuth megvédi)
- [ ] `GET /bff/chat` no-auth → 401 (ORCH-058 fix VPS-en él)
- [ ] Meglévő **178 teszt zöld**
- [ ] Commit + push + pm2 restart

## Visszajelzés

Outboxba: `MSG-ORCH-059-DONE`

## Blokkoló hatás

Unblokkol:
- **E2E `34-abstractions`** teszt megírása és futtatása
- **E2E `26-sse-chat-live`** 429 → 401 javítása (ORCH-058 deploy)
