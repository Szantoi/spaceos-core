# Epic E17 — VPS Deployment (pm2 + nginx)

**Priority:** 🟢 P3
**Status:** `CLOSED_DONE`
**Depends on:** all previous epics

---

## Goal

The Orchestrator runs on the VPS under pm2, served through nginx at `/bff/*`.
One-command deploy: `npm run build && rsync && pm2 reload`.

---

## Scope

**In scope:**
- `ecosystem.config.js` for pm2
- nginx `location /bff/` config block
- `.gitignore` confirms `.env` is excluded
- `npm run build` → `dist/` ready to rsync
- `pm2 logs` shows clean startup

**Out of scope:**
- CI/CD pipeline (future)
- HTTPS cert renewal (assumed already configured)
- Kubernetes (future)

---

## Acceptance Criteria

- [x] `pm2 start ecosystem.config.js` → process running, status `online`
- [x] `GET https://yourdomain.com/bff/health` → `200` from the internet
- [x] `POST https://yourdomain.com/bff/chat` with valid JWT + message → LLM reply
- [x] nginx SSE headers present: `proxy_buffering off` set in `docs/deploy/nginx.conf`
- [x] pm2 auto-restarts on crash (`autorestart: true`, `max_restarts: 10`)
- [x] `.env` NOT in git — `.gitignore` confirmed

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | ecosystem.config.js + pm2 setup | `CLOSED_DONE` |
| T2 | nginx config block | `CLOSED_DONE` |
| T3 | Deploy script + smoke test | `CLOSED_DONE` |

---

## Definition of Done

- [x] All AC checked
- [x] `ecosystem.config.js`, `docs/deploy/nginx.conf`, `docs/deploy/deploy.sh` committed
- [x] pm2 log clean (no errors on startup)
