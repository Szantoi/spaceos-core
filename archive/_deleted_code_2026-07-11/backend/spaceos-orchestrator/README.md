# SpaceOS Orchestrator

**Layer 3 BFF** — LLM routing, agentic loop, Kernel proxy.

```
React DesignPortal
    ↓  POST /bff/chat   (NLP → agentic loop)
    ↓  ANY  /bff/api/*  (passthrough proxy)
SpaceOS Orchestrator   ← ez a repo
    ↓  POST /api/*      (JWT forwarded)
SpaceOS.Kernel.Api (C# .NET 8)
    ↕
LLM Provider (Anthropic / OpenAI / Mock)
```

A Kernel nem tud az LLM-ről. Az LLM nem tud a Kernelről. Az Orchestrator az egyetlen komponens, amelyik ismeri mindkettőt.

---

## Repo szerkezete

```
src/
  config/        Zod-validált env változók (env.ts)
  types/         ILlmProvider interface + C# DTO tükrök
  llm/           LLM provider adapterek (Anthropic, OpenAI, Mock)
  interpreter/   Agentic loop + tool registry + kernel action dispatch
  proxy/         Kernel passthrough (http-proxy-middleware)
  middleware/    JWT auth + error handler
  routes/        Express route handlerek (health, auth, chat)
  index.ts       Bootstrap — csak regisztrációk, üzleti logika nélkül

docs/
  epics/         E11–E17 epic dokumentáció (EPIC.md, task fájlok, REVIEW_REPORT.md)
  deploy/        Éles üzemeltetési fájlok (nginx.conf, deploy.sh)
  BACKLOG.md     Master backlog — minden epic státusza

ecosystem.config.js   pm2 process manager konfig
```

---

## Előfeltételek

- Node.js 20 LTS
- npm 10+
- (Éles környezetben) pm2 globálisan telepítve: `npm i -g pm2`

---

## Első indítás

```bash
# 1. Függőségek telepítése
npm install

# 2. Env konfiguráció
cp .env.example .env
# Szerkeszd a .env-t (ld. lent)

# 3. Fejlesztői szerver (hot reload)
npm run dev
```

A szerver elindul: `http://127.0.0.1:3000`

---

## Környezeti változók (`.env`)

| Változó | Leírás | Példa |
|---|---|---|
| `PORT` | Szerver portja | `3000` |
| `NODE_ENV` | `development` / `production` | `development` |
| `KERNEL_BASE_URL` | C# Kernel API URL | `http://localhost:5000` |
| `JWT_SIGNING_KEY` | JWT aláírási kulcs (min. 32 kar.) — ugyanaz, amit a Kernel használ | `your-super-secret-key-...` |
| `LLM_PROVIDER` | `anthropic` / `openai` / `mock` | `anthropic` |
| `ANTHROPIC_API_KEY` | Anthropic API kulcs (ha `LLM_PROVIDER=anthropic`) | `sk-ant-...` |
| `OPENAI_API_KEY` | OpenAI API kulcs (ha `LLM_PROVIDER=openai`) | `sk-...` |
| `MAX_TOOL_ITERATIONS` | Agentic loop max iteráció | `5` |

> **Fontos:** a `.env` soha nem kerül gitbe.

---

## Elérhető scriptek

```bash
npm run dev      # Fejlesztői szerver (tsx watch)
npm run build    # TypeScript fordítás → dist/
npm start        # Lefordított dist/ indítása
npm test         # Vitest tesztfuttatás (50 teszt, 8 fájl)
```

---

## API végpontok

| Metódus | Útvonal | Auth | Leírás |
|---|---|---|---|
| `GET` | `/bff/health` | — | Szerver állapot ellenőrzése |
| `POST` | `/bff/auth/token` | — | JWT token kiadása (dev) |
| `POST` | `/bff/chat` | JWT | NLP → agentic loop → LLM válasz |
| `ANY` | `/bff/api/*` | JWT | Kernel passthrough proxy |

### Példa: health check

```bash
curl http://localhost:3000/bff/health
# → {"status":"ok","uptime":12.3,"llmProvider":"mock"}
```

### Példa: chat

```bash
TOKEN=$(curl -s -X POST http://localhost:3000/bff/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"dev","password":"dev"}' | jq -r '.token')

curl -X POST http://localhost:3000/bff/chat \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello!"}]}'
```

---

## Tesztek futtatása

```bash
npm test
```

```
Test Files  8 passed (8)
     Tests  50 passed (50)
  Duration  ~1.5s
```

---

## Éles telepítés (VPS)

### pm2

```bash
npm run build
pm2 start ecosystem.config.js --env production
pm2 save
pm2 startup   # autostart reboot után
```

### nginx

A `docs/deploy/nginx.conf` tartalmaz egy kész `location` blokkot. Másold be a szervered `server {}` blokkjába.

Főbb beállítások:
- `proxy_buffering off` — SSE kompatibilitás
- 300 s timeout — hosszú agentic loopokhoz
- `proxy_pass_header Authorization` — JWT átadás a Kernelnek

### Deploy script

```bash
bash docs/deploy/deploy.sh
```

A script sorban: `npm ci` → `npm run build` → `pm2 reload` (zero-downtime) → `pm2 save`.

---

## Réteg-függőségi szabály

```
routes → interpreter → llm (ILlmProvider-en át)
routes → proxy → Kernel (http-proxy-middleware)
interpreter → kernel.action (axios — CSAK ITT)
middleware → config/env
MINDEN → types
```

Az `index.ts` csak regisztrál — üzleti logika tilos benne.
