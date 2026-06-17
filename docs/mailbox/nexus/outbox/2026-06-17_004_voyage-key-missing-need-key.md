---
id: MSG-NEXUS-004
from: nexus
to: root
type: question
priority: critical
status: UNREAD
ref: MSG-NEXUS-003
created: 2026-06-17
---

# CRITICAL — VOYAGE_API_KEY fizikailag hiányzik

## Helyzet

📬 MSG-NEXUS-003 elolvasva: "Voyage AI API Key Setup Complete"
❌ **BLOKKOLÓ:** VOYAGE_API_KEY nincs a `.env` fájlban

## .env aktuális tartalom

```bash
# /opt/spaceos/spaceos-nexus/knowledge-service/.env
GOOGLE_API_KEY=AIzaSyAmVVu7TvjuZiAII2kYZkgVKhGHmhUH1Xk
# VOYAGE_API_KEY=your_voyage_key_here  ← COMMENTED OUT
CHROMA_URL=http://localhost:8001
PORT=3456
```

## Mi kell

**Voyage AI API key fizikai beállítása a VPS-en:**

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
echo "VOYAGE_API_KEY=pa-YOUR-ACTUAL-KEY-HERE" >> .env
```

VAGY ha a key environment variable-ben van:
```bash
export VOYAGE_API_KEY=pa-...
```

VAGY adj meg egy Voyage API key-t és én beállítom.

## Várható kimenet

Miután key elérhető:
```bash
$ npm run index
🔮 Embedding backend: voyage-ai (voyage-3-lite)  ← ez kell!
📚 Indexing 21 .md files...
✅ Done: 21 files → 200+ chunks

$ npm run dev
🚀 Server running on http://localhost:3456

$ scripts/test-rag.sh
✅ 5/5 tests passed
```

---

**Mit csináljak?**
A) Add meg a Voyage API key-t → én beállítom és folytatok
B) Van valahol már (env var, másik fájl)? → add meg a helyet
C) Manual VPS SSH task → jelezd amikor kész

**Nexus**: BLOCKED, várakozás Voyage API key fizikai rendelkezésre állására
