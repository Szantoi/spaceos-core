# SpaceOS Knowledge Service

RAG (Retrieval-Augmented Generation) service for SpaceOS knowledge base.

## Features

- ✅ Indexes `/opt/spaceos/docs/knowledge/**/*.md` recursively
- ✅ ChromaDB vector store (persistent, Docker-based)
- ✅ Three embedding backends (priority order):
  1. **Voyage AI** (`voyage-3-lite`, 1024 dims) — production recommended
  2. **Google Gemini** (`text-embedding-004`, 768 dims) — requires model name fix
  3. **Local** (`all-MiniLM-L6-v2`) — CPU-dependent, may fail on QEMU/old arch
- ✅ Express REST API (`GET/POST /api/knowledge/search`)
- ✅ Auto-indexing on startup (if store empty)

## Setup

### 1. ChromaDB

```bash
cd /opt/spaceos/spaceos-nexus
docker compose up -d
```

### 2. Embedding API Key

**Option A: Voyage AI (recommended)**

1. Get API key: https://dash.voyageai.com/
2. Add to `.env`:
   ```bash
   VOYAGE_API_KEY=your_key_here
   ```

**Option B: Google Gemini (requires fix)**

1. Fix model name in `src/embeddings.ts:66`:
   ```ts
   modelName: 'text-embedding-004',  // was: gemini-embedding-001
   ```
2. Ensure `GOOGLE_API_KEY` is set in `.env`

### 3. Install & Run

```bash
npm install
npm run dev
```

## Usage

### Index knowledge base

```bash
npm run index
```

### Start server

```bash
npm run dev
# Or production:
npm run build && npm start
```

### Test

```bash
../scripts/test-rag.sh
```

### API Endpoints

**Health**
```bash
curl http://localhost:3456/health
```

**Search (GET)**
```bash
curl "http://localhost:3456/api/knowledge/search?q=EF+Core+migration&topK=5"
```

**Search (POST)**
```bash
curl -X POST http://localhost:3456/api/knowledge/search \
  -H 'Content-Type: application/json' \
  -d '{"q": "React testing patterns", "topK": 3}'
```

**Re-index**
```bash
curl -X POST http://localhost:3456/api/knowledge/index
```

## Architecture

```
knowledge-service/
├── src/
│   ├── server.ts        — Express API
│   ├── indexer.ts       — Markdown indexing pipeline
│   ├── vectorStore.ts   — ChromaDB client
│   └── embeddings.ts    — Multi-backend embedding provider
├── package.json
├── tsconfig.json
└── .env
```

## Status

- ✅ Code complete
- ⚠️  Google Gemini model name needs fix (`text-embedding-004`)
- ⚠️  Voyage AI key needed for production (free tier available)
- ✅ ChromaDB running and accessible
- ✅ 21 knowledge docs ready (`/opt/spaceos/docs/knowledge/`)

## Next Steps (Phase 2)

1. Marvin integration for planning pipeline
2. MCP server tools (`search_knowledge`)
3. Guardrail service integration
