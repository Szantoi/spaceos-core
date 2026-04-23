# MSG-O013 — dist rebuild + /bff/handshakes proxy (Phase 3C+)

**Date:** 2026-04-08
**Status:** DONE
**Agent:** Orchestrator

---

## Task 1: dist rebuild

- `dist/` had root-owned files in `dist/kernel/` blocking `tsc` (EACCES).
- Resolved by renaming `dist/` to `dist_old/` and rebuilding fresh.
- `npm run build` — 0 TypeScript errors.
- `grep -c "snapshots|verify-chain|proof" dist/index.js` → **4 matches** (confirmed).
- `grep -c "handshakes" dist/index.js` → **2 matches** (import + mount).

## Task 2: /bff/handshakes route

Created `src/routes/handshakes.route.ts` with 5 endpoints:

| Method | BFF Path | Kernel Path |
|--------|----------|-------------|
| POST | `/bff/handshakes` | `/api/handshakes` |
| GET | `/bff/handshakes` | `/api/handshakes` |
| GET | `/bff/handshakes/:id` | `/api/handshakes/:id` |
| PUT | `/bff/handshakes/:id/accept` | `/api/handshakes/:id/accept` |
| PUT | `/bff/handshakes/:id/reject` | `/api/handshakes/:id/reject` |

All routes: `requireAuth`, UUID validation on `:id` params, `kernelHeaders()`, `handleKernelError()`.

## Task 3: index.ts registration

Registered after spatial router block with `proxyLimiter`:
```
app.use('/bff/handshakes', proxyLimiter, handshakesRouter);
```

## Task 4: Tests

Created `src/routes/handshakes.route.test.ts` — 4 tests:
1. `GET /bff/handshakes` without auth → 401
2. `POST /bff/handshakes` without auth → 401
3. `GET /bff/handshakes/:id` with invalid UUID → 400
4. `PUT /bff/handshakes/not-a-uuid/accept` → 400

## Task 5: CLAUDE.md session goal updated

## Build + Test

- `npm run build` — **0 errors**
- `npm test` — **157 tests passed** (19 test files, 153 existing + 4 new)

## Note: dist_old cleanup needed

`/opt/spaceos/spaceos.orchestrator/dist_old/` contains root-owned files that require `sudo` to remove. Run `sudo rm -rf dist_old/` when convenient.
