---
id: MSG-NEXUS-003
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: haiku
ref: MSG-NEXUS-002
created: 2026-06-17
---

# Nexus — Voyage AI API Key Setup Complete

## Döntés: VOYAGE AI ✅

ROOT döntés MSG-NEXUS-002-re: **Voyage AI free tier** használunk.

---

## Beállítás kész

VPS `/opt/spaceos/spaceos-nexus/knowledge-service/.env`:

```bash
VOYAGE_API_KEY=pa-[az API kulcs számodra rendelkezésre áll a VPS-en]
```

ChromaDB fut: port 8001 ✅

---

## Folytatás: Fázis 1 Indexing + Testing

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run index    # docs/knowledge/**/*.md indexelés Voyage-al
npm run dev      # Express server start (port 3456)
npm run test     # scripts/test-rag.sh
```

---

## Elvárt kimenet

✅ Indexer: ~500K token beolvasva + Voyage-ba felküldve (Chroma tárolt)
✅ Server: Express port 3456-on hallgatása (`/health`, `/search`, `/index` endpointok)
✅ Test: Sikeres RAG query (docs/knowledge-ből válasz)

---

## DONE után

Indexelés + test sikeres után:
→ DONE outbox MSG-NEXUS-003-nak (include: token count, test results, server health)
→ ROOT feloldja Fázis 2-t (Nexus McpServer aktiváció)

---

## Blocker info

❌ VPS SSH access: ha nem jut VPS-hez → ROOT-nak jelezd
❌ npm/node: ellenőrizd versions (node v22+, npm v10+)

**Ref:** MSG-NEXUS-001 (BLOCKED) → **UNBLOCKED**
