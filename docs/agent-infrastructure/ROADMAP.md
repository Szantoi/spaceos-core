# SpaceOS Nexus — Agent Infrastructure Roadmap

> Három módszer szintézise: JoineryTech.McpServer + Marvin + SpaceOS bash pipeline
> Fejlesztő terminál: NEXUS (`/opt/spaceos/spaceos-nexus/`)
> Tervdokumentum: `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v*.md` (Architect készíti)

---

## Fázis 1 — McpServer Knowledge Service
**Státusz:** KÉSZ — Voyage API key kell éles működéshez (2026-06-17)

- [x] JoineryTech.McpServer klónozás → `spaceos-nexus/mcp-server/`
- [x] `indexKnowledgeBase.ts` adaptáció: `docs/knowledge/**/*.md` olvasás
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/indexer.ts`
  - Minden `.md` fájlt indexel (nem csak `.knowledge.md`)
- [x] Gemini → voyage-3-lite embedding csere
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/embeddings.ts`
  - Priority: Voyage AI → Google Gemini → Local
  - API: `https://api.voyageai.com/v1/embeddings` · Env: `VOYAGE_API_KEY`
  - ⚠️  Google Gemini model name fix szükséges: `text-embedding-004` (nem `gemini-embedding-001`)
- [x] ChromaDB Docker service (`docker-compose.yml`)
  - `spaceos-nexus/docker-compose.yml` · Port: 8001 → Fut (`spaceos_chromadb` konténer)
- [x] Engineering knowledge fájlok átmásolva → `docs/knowledge/engineering/`
  - 7 fájl: backend_dotnet, database_efcore, efcore_installation, frontend_react, testing_*
- [x] `POST /api/knowledge/search` endpoint implementálva
  - Kész: `spaceos-nexus/knowledge-service/src/server.ts` (port 3456)
  - Endpoints: GET/POST /api/knowledge/search, POST /api/knowledge/index, GET /health
  - Teszt script: `spaceos-nexus/scripts/test-rag.sh`
  - README: `spaceos-nexus/knowledge-service/README.md`
  - **Teszteléshez:** `VOYAGE_API_KEY` kell (https://dash.voyageai.com/ — free tier)
- [ ] Librarian cron kiegészítés: indexer hívás knowledge sync után
- [ ] Haiku scanner: `search_knowledge` tool bekötve

## Fázis 2 — Marvin Planning Pipeline
**Státusz:** VÁRAKOZÁS (Fázis 1 után)

- [ ] Marvin telepítés (`pip install marvin`)
- [ ] Agent definíciók: scanner, selector, debater_a, debater_b, synthesizer
- [ ] plan-scan.sh → Marvin Task
- [ ] plan-select.sh → Marvin Task (WebSearch tool)
- [ ] plan-debate.sh → Marvin Tasks (párhuzamos)
- [ ] McpServer tool bekötve Marvin-ba
- [ ] Bash cron kikapcs → Marvin Scheduler

## Fázis 3 — Marvin Reviewer + Nightwatch
**Státusz:** VÁRAKOZÁS (Slice 2 előtt)

- [ ] reviewer.sh → Marvin Task
- [ ] nightwatch.sh → Marvin Scheduler
- [ ] WorkflowStateTracker bekötés
- [ ] RbacFilter bekötés
