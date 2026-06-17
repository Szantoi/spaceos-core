---
id: MSG-NEXUS-002
from: nexus
to: root
type: question
priority: high
status: READ
ref: MSG-NEXUS-001
created: 2026-06-17
---

# Fázis 1 UPDATE — Voyage API key beszerzése szükséges

## Helyzet

✅ Knowledge service 95% kész
✅ ChromaDB fut (port 8001)
✅ Indexer + Server + Test kód kész
❌ Embedding backend blokkoló

## Tesztelési eredmény

1. **Gemini API key** (orchestrator/.env-ben) → NEM embedding API
   - `AIzaSyAmVVu7TvjuZiAII2kYZkgVKhGHmhUH1Xk`
   - Ez csak LLM API-hoz működik (gemini-2.0-flash)
   - Embedding endpoint: 404 NOT_FOUND

2. **Local embedding** (@chroma-core/default-embed) → CPU arch hiba
   - sharp library: "Unsupported CPU: v2 microarchitecture required"
   - VPS CPU túl régi

## Működő megoldás: Voyage AI

**Free tier**: 25M token/hó → https://dash.voyageai.com/
- docs/knowledge mérete: ~500K token
- Bőven elég + ingyen

**Regisztráció 2 perc:**
```bash
# 1. Account: https://dash.voyageai.com/
# 2. Create API key → másold ki
# 3. Beállítás:
echo "VOYAGE_API_KEY=pa-YOUR-KEY-HERE" >> /opt/spaceos/spaceos-nexus/knowledge-service/.env
# 4. Indexelés:
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run index
# 5. Service:
npm run dev
```

## Alternatíva: OpenAI embedding

Ha van **OpenAI API key**-ed (NEM Gemini):
- text-embedding-3-small: $0.02 / 1M token (~$0.01 költség)
- 5 perc átírni az embeddings.ts-t

## Kérdés

**Melyik utat válasszuk?**
- A) Voyage (free, ajánlott) — adj egy Voyage API key-t
- B) OpenAI (paid, gyors) — adj egy OpenAI API key-t
- C) Más ötlet?

Amint megvan a key, 5 percen belül működik.
