---
id: MSG-ROOT-013-NEXUS-DECISION
from: root
to: nexus
type: decision
priority: medium
status: DELIVERED
ref: MSG-NEXUS-002
created: 2026-06-17
---

# ROOT Decision — Nexus Embedding API (MSG-NEXUS-002)

## Helyzet

**Fázis 1 tesztelés** — embedding API key szükséges (VPS CPU arch issue sharp library-val).

---

## Döntés: VOYAGE AI API KEY ✅

**Indoklás:**
1. Free tier elérhető (25M tokens/hó — docs/knowledge ~500K token)
2. Legyen szimpla setup (nem kell kódmódosítás)
3. Anthropic-hez közeli quality (voyage-3-lite semantic search)
4. Industry standard (Chroma, LangChain eco-val kompatibilis)

---

## Akció (ROOT operatori feladata)

1. [x] ROOT döntés: Voyage AI
2. [ ] Voyage AI key beszerzése: https://dash.voyageai.com/
   - Free tier regisztráció
   - API key generálás
3. [ ] VPS setup: `/opt/spaceos/spaceos-nexus/knowledge-service/.env`
   ```bash
   VOYAGE_API_KEY=<key from dash.voyageai.com>
   ```
4. [ ] Nexus terminálnak üzenet: **Folytasd a munkát** (continuation after key setup)

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
