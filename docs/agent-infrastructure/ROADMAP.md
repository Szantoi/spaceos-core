# SpaceOS Nexus — Agent Infrastructure Roadmap

> Három módszer szintézise: JoineryTech.McpServer + Marvin + SpaceOS bash pipeline
> Fejlesztő terminál: NEXUS (`/opt/spaceos/spaceos-nexus/`)
> Tervdokumentum: `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v*.md` (Architect készíti)

---

## Fázis 1 — McpServer Knowledge Service
**Státusz:** IMPLEMENTÁLVA — TESZTELÉSRE VÁR (2026-06-16)

- [x] JoineryTech.McpServer klónozás → `spaceos-nexus/mcp-server/`
- [x] `indexKnowledgeBase.ts` adaptáció: `docs/knowledge/**/*.md` olvasás
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/indexer.ts`
  - Minden `.md` fájlt indexel (nem csak `.knowledge.md`)
- [x] Gemini → voyage-3-lite embedding csere
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/embeddings.ts`
  - API: `https://api.voyageai.com/v1/embeddings` · Env: `VOYAGE_API_KEY`
- [x] ChromaDB Docker service (`docker-compose.yml`)
  - `spaceos-nexus/docker-compose.yml` · Port: 8001 (nem ütközik meglévőkkel)
- [x] Engineering knowledge fájlok átmásolva → `docs/knowledge/engineering/`
  - 7 fájl: backend_dotnet, database_efcore, efcore_installation, frontend_react, testing_*
- [ ] `POST /api/knowledge/search` endpoint tesztelve élő VPS-en
  - Kész: `spaceos-nexus/knowledge-service/src/server.ts` (port 3456)
  - Teszt: `spaceos-nexus/scripts/test-rag.sh`
  - **Szükséges:** VPS-en `VOYAGE_API_KEY` beállítása + `docker compose up -d` + `npm run dev`
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
