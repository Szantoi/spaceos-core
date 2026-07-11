# Nexus — Agent Orchestration Server

> **KÖTELEZŐ OLVASMÁNY MINDEN TERMINÁLNAK**
>
> A Nexus a SpaceOS agent infrastruktúra központi szervere.
> Minden terminál kommunikáció ezen keresztül történik.

---

## Mi a Nexus?

A **Nexus** egy **önálló termék** a SpaceOS mellett. Célja:
- AI agent flották koordinálása
- Terminal session management
- MCP-alapú kommunikáció
- Memória és tudásbázis kezelés
- Audit logging és monitoring

**FONTOS:** A Nexus NEM SpaceOS kód! Külön fejlődik, külön repository.

---

## Elérhető Szolgáltatások

| Szolgáltatás | Port | Leírás |
|---|---|---|
| **Knowledge Service** | 3456 | MCP server, Session API, Memory API |
| **Datahaven Dashboard** | 443 | Web UI monitoring |

---

## 1. Session Management API

### Session indítás prompttal
```bash
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<terminál>",
    "model": "sonnet|opus|haiku",
    "prompt": "Feladat leírása...",
    "fromTerminal": "<saját terminál>"
  }'
```

### Prompt injection futó session-be
```bash
curl -X POST http://localhost:3456/api/session/inject \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<terminál>",
    "prompt": "Üzenet...",
    "fromTerminal": "<saját terminál>"
  }'
```

### Wake-up (session indítás + inbox olvasás)
```bash
curl -X POST http://localhost:3456/api/session/wake \
  -H "Content-Type: application/json" \
  -d '{"terminal": "<terminál>", "fromTerminal": "<saját terminál>"}'
```

### Session státusz lekérdezés
```bash
# Egy terminál
curl -s http://localhost:3456/api/session/<terminál>

# Összes terminál
curl -s http://localhost:3456/api/sessions/all

# Audit logok
curl -s http://localhost:3456/api/sessions/logs?days=1
```

### Jogosultságok

| Kezdeményező | Irányíthat |
|---|---|
| **root** | mindenkit |
| **conductor** | architect, librarian, explorer, backend, frontend, designer |
| **többi** | csak saját magát |

---

## 2. Memory API (MCP)

A Nexus SQLite FTS5 alapú memória rendszert tartalmaz hibrid kereséssel.

### Memória olvasás
```bash
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "read_memory",
      "arguments": {"terminal": "<terminál>"}
    },
    "id": 1
  }'
```

### Memóriához hozzáfűzés (AJÁNLOTT)
```bash
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "append_memory",
      "arguments": {
        "terminal": "<terminál>",
        "content": "## Tanult minta\n- xyz"
      }
    },
    "id": 1
  }'
```

### Memória felülírás (óvatosan!)
```bash
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "write_memory",
      "arguments": {
        "terminal": "<terminál>",
        "content": "## Tisztított memória\n..."
      }
    },
    "id": 1
  }'
```

### Memória típusok

| Típus | Mire való | Példa |
|---|---|---|
| `semantic` | Preferenciák, tények, döntések | "User prefers TypeScript strict mode" |
| `episodic` | Beszélgetés összefoglalók | "Session 2026-06-21: Fixed auth bug" |
| `procedural` | How-to tudás, minták | "To deploy: run scripts/deploy.sh" |

---

## 3. Datahaven Dashboard API

**URL:** https://datahaven.joinerytech.hu
**Auth Token:** `dev-token-spaceos-dashboard-2026`

### Terminal státusz regisztráció
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "<terminál>",
    "status": "working|idle",
    "currentTask": "Mit csinálsz éppen"
  }'
```

### Dashboard lekérdezés
```bash
curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  https://datahaven.joinerytech.hu/api/dashboard
```

---

## 4. Knowledge Service API

### Tudásbázis keresés
```bash
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "search_knowledge",
      "arguments": {"query": "keresési kifejezés"}
    },
    "id": 1
  }'
```

---

## KÖTELEZŐ HASZNÁLATI SZABÁLYOK

### 1. Session indítás MINDIG MCP-n keresztül
```bash
# ❌ TILOS - nincs audit, nincs jogosultság ellenőrzés
tmux send-keys -t spaceos-backend "..." Enter

# ✅ KÖTELEZŐ - MCP API-n keresztül
curl -X POST http://localhost:3456/api/session/inject -d '...'
```

### 2. Session kezdetén státusz regisztráció
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<terminál>","status":"working","currentTask":"Session started"}'
```

### 3. Session végén idle státusz
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"<terminál>","status":"idle"}'
```

### 4. Tanult minták mentése memóriába
Ha valami fontosat tanultál (minta, workaround, döntés):
```bash
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"append_memory","arguments":{"terminal":"<terminál>","content":"## Tanult minta\n- ..."}},"id":1}'
```

---

## VISSZAJELZÉS GYŰJTÉS

A Nexus aktív fejlesztés alatt áll. **Segíts jobbá tenni!**

### Mit jelezz vissza?
- Milyen MCP eszközre lenne szükséged?
- Melyik meglévő eszköz működött jól?
- Melyik workflow lépés körülményes?

### Hol jelezd?
DONE outbox üzenetben:
```markdown
## MCP Visszajelzés

### Használt eszközök ✅
- Session Management API (működött)
- Memory API append (működött)

### Hiányzó eszközök 🔧
- Hasznos lenne batch session starter
- Nincs inbox count lekérdező
```

---

## Hibakeresés

### Knowledge Service nem válaszol
```bash
# Ellenőrzés
curl -s http://localhost:3456/health

# Log
tail -50 /tmp/knowledge-service.log

# Újraindítás
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm start
```

### Session API hiba
```bash
# Audit log ellenőrzés
curl -s http://localhost:3456/api/sessions/logs?days=1 | jq '.[-5:]'
```

---

---

## 5. Agent Tanítási Rendszer (Marveen Skills)

A Nexus a **Marveen** projektből adaptált skill rendszert használ az agent önfejlesztéshez.

### Elérhető skill-ek

| Skill | Parancs | Mire való |
|---|---|---|
| **retrospective** | `/retrospective` | Session elemzés, skill/memória javaslatok |
| **handoff** | `/handoff` | Kontextus átadás session váltáskor |
| **skill-management** | `/skills` | Skill-ek listázása, módosítása, törlése |
| **skill-factory** | auto | Új skill létrehozása tanult mintából |

### Session Modell: Hideg Indítás + Intelligens Memória

A Marveen/Nexus **hideg indítással** dolgozik — nem tölti vissza az előző beszélgetést.
Helyette **intelligens memória rendszerrel** kompenzál:

```
Session indítás
    ↓
CLAUDE.md + NEXUS_USAGE.md beolvasás
    ↓
Memory API lekérdezés (hot/warm memóriák)
    ↓
Kontextus építés releváns memóriákból
    ↓
Munka
    ↓
Session végén: /retrospective vagy /handoff
```

### Retrospective — Session Elemzés

**Mikor használd:**
- Komplex session után (5+ tool call, hibakezelés)
- Ha a user korrigált ("nem így, hanem úgy")
- Kontextus ablak kimerülés előtt

**Mit csinál:**
1. Elemzi mi működött / mi nem
2. Javasol skill módosításokat
3. Javasol memória frissítéseket
4. User jóváhagyás után végrehajtja

```bash
# Retrospective indítása
/retrospective

# Csak skill fókusz
/retrospective focus=skills

# Csak memória fókusz
/retrospective focus=memory
```

### Handoff — Kontextus Átadás

**Mikor használd:**
- Kontextus limit közeledik
- Feladat másik session-ben folytatódik
- Másik agent-nek delegálás

**Mit generál:**
```markdown
# Handoff: {cél}

## Goal
[Mi a feladat]

## Current Progress
[Elvégzett munka]

## What Worked
[Működő megoldások]

## What Didn't Work
[Sikertelen próbálkozások]

## Next Steps
[Konkrét lépések]
```

```bash
# Handoff generálás
/handoff purpose="Continue the API integration"

# Másik agent-nek
/handoff purpose="Debug auth flow" target=backend
```

### Memória Típusok és Életciklusok

| Típus | Élettartam | Mire való | Példa |
|---|---|---|---|
| **hot** | 24-48 óra | Aktív feladat kontextus | "Working on PR #42" |
| **warm** | 1-2 hét | Stabil preferenciák | "User prefers TypeScript strict" |
| **cold** | Hosszú távú | Architekturális döntések | "ADR-039: Event sourcing" |
| **shared** | Örök | Cross-agent tudás | "Production deploy requires VPN" |

### Automatikus Memória Mentés

A rendszer automatikusan ment ha az üzenet tartalmaz:
- "my", "I prefer", "remember that"
- "tanítsd meg", "jegyezd meg"
- User korrekció után

### Napi Összefoglaló (Daily Digest)

Automatikusan fut minden nap:
1. Összegyűjti az elmúlt 24 óra memóriáit
2. AI összefoglalót generál
3. Epizodikus memóriába menti

---

## Verzió

- **Nexus Knowledge Service:** 1.0.0
- **Marveen Skills:** Adaptált 2026-06-21
- **Utolsó frissítés:** 2026-06-21
- **Dokumentáció:** `/opt/spaceos/spaceos-nexus/NEXUS_USAGE.md`

---

> **Kérdés?** Jelezd a Root terminálnak vagy nyiss issue-t a Nexus repoban.
