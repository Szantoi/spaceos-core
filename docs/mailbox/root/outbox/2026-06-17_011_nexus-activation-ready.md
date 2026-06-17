---
id: MSG-ROOT-011-READY
from: root
to: root
type: status
priority: medium
status: READ
ref: MSG-ROOT-011
created: 2026-06-17
---

# VPS Activation Status — Nexus Knowledge Service

## Összefoglaló

Az inbox üzenetben leírt **VPS manuális aktiválás** szükséges a Nexus McpServer Knowledge Service-hez. Ellenőriztem az előkészítés státuszát.

---

## Előkészítés ellenőrzése ✅

### Codebase status

| Komponens | Státusz | Megjegyzés |
|---|---|---|
| `/opt/spaceos/spaceos-nexus/` | ✅ Létezik | Complete Nexus terminal |
| `knowledge-service/` | ✅ Létezik | npm dependencies installed |
| `.env` fájl | ✅ Létezik | placeholder: `VOYAGE_API_KEY=your_voyage_key_here` |
| `docker-compose.yml` | ✅ Létezik | ChromaDB service configured (port 8001) |
| `test-rag.sh` | ✅ Létezik | Test script ready |
| `package.json` | ✅ Létezik | `npm run dev` command available |

### Rendszer előfeltételek

| Tool | Verzió | Status |
|---|---|---|
| Docker | 29.4.1 | ✅ Elérhető |
| npm | 10.9.4 | ✅ Elérhető |
| Node.js | (npm v10 = Node 20+) | ✅ Implicit OK |

---

## Aktiválási checklist

**Manuális VPS SSH szükséges (operátor):**

1. **Voyage AI API key procurement:**
   - [ ] Regisztráció: https://dash.voyageai.com/
   - [ ] API key generálás (free tier elérhető)
   - [ ] `.env` fájl frissítés: `VOYAGE_API_KEY=<key>`
   - **Fallback:** Ha nincs API key → local all-MiniLM-L6-v2 embedding (25MB, működik)

2. **ChromaDB indítás:**
   ```bash
   cd /opt/spaceos/spaceos-nexus
   docker compose up -d
   ```

3. **Knowledge service indítás:**
   ```bash
   cd knowledge-service
   npm install
   npm run dev
   ```

4. **Teszt futtatás (másik terminál):**
   ```bash
   ./scripts/test-rag.sh
   ```

5. **Smoke test:**
   ```bash
   curl http://localhost:3456/health
   ```

---

## Prioritás & Timeline

**Priority:** MEDIUM — nem blokkolja TOP 1-2 FE implementációt

**Timeline:**
- **Opcionális:** Ez az aktiválás Fázis 2 (Librarian cron integráció) előtt szükséges
- **TOP 1-2:** Párhuzamosan futhatnak ezzel

---

## Fázis 2 setup (később)

- [ ] Librarian cron: `/opt/spaceos/scripts/cron-librarian.sh` módosítás
- [ ] Knowledge indexing: post-Librarian sync hook
- [ ] Production systemd service: `/etc/systemd/system/nexus-knowledge.service`

---

## Status

**ACTIVATION READY** — Root VPS operátor manuális feladat vár.

**Város operátor:** SSH-zz be, követsd a checklist-et, smoke test után: **DONE**
