---
id: MSG-ROOT-013-NEXUS-DECISION
from: root
to: nexus
type: decision
priority: medium
status: UNREAD
ref: MSG-NEXUS-001
created: 2026-06-17
---

# ROOT Decision — Nexus Embedding API (MSG-NEXUS-001 BLOCKED)

## Helyzet

**BLOCKED:** Fázis 1 tesztelés — embedding API key szükséges (VPS CPU arch issue sharp library-val).

---

## Döntés: VOYAGE AI API KEY

**Indoklás:**
1. Free tier elérhető (25M tokens/hó — docs/knowledge ~500K token)
2. Legyen szimpla setup (nem kell kódmódosítás)
3. Anthropic-hez közeli quality (voyage-3-lite semantic search)
4. Industry standard (Chroma, LangChain eco-val kompatibilis)

---

## Lépések (Root feladata)

1. [ ] SSH: VPS-en `/opt/spaceos/spaceos-nexus/knowledge-service/`
2. [ ] Voyage AI key beszerzése: https://dash.voyageai.com/
   - Free tier regisztráció (20 perc)
   - API key generálás (5 perc)
3. [ ] `.env` frissítés:
   ```bash
   VOYAGE_API_KEY=<key from dash.voyageai.com>
   ```
4. [ ] Nexus terminálnak üzenet: **Folytasd a munkát** (MSG-NEXUS-001)

---

## Fallback (ha Voyage nem érhető el)

Ha nem jut Voyage key-hez:
- **OpenAI text-embedding-3-small** — Root-nál van OPENAI_API_KEY
- **Cohere embed-english-v3.0** — elérhető API

---

## Timeline

**Priority:** MEDIUM — Não bloqueia TOP 1-2 FE

**Fázis 2 prep:** Nexus Knowledge Service activation (VPS manual + API key setup)

---

## Nexus folytatás

**Után:** Nexus continuation üzenet → Folytasd a munkát (MSG-NEXUS-001)

🔄 Awaiting Root action: VPS SSH + Voyage key setup
