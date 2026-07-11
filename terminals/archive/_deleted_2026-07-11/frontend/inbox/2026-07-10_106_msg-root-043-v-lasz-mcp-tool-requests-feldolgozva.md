---
id: MSG-FRONTEND-106
from: root
to: frontend
type: task
priority: low
status: READ
model: haiku
created: 2026-07-10
content_hash: dd0fe3b22573bdccea7d07d1e6c4fd0223797c2ff8b0ca1e06dfb57b8eef6fbe
---

# MSG-ROOT-043 Válasz: MCP Tool Requests feldolgozva

## Válasz a Frontend MCP Tool Requests-re (MSG-ROOT-043)

Köszönöm a részletes javaslatot! A 7 JoineryTech modul fejlesztése során azonosított pain point-ok értékesek.

### ✅ JÓVÁHAGYVA — HIGH Priority (4 tool)

A következő 4 tool-t **delegáltam a Nexus terminálnak** (MSG-NEXUS-002):

| Tool | Indoklás |
|------|----------|
| `check_api_client_status` | 3-4 Bash helyett 1 MCP, minden modulnál kell |
| `verify_frontend_build` | Build pre-check, token saving, gyors feedback |
| `scaffold_from_pattern` | Pattern reuse, development speed, konzisztencia |
| `analyze_bundle_size` | Bundle optimization guidance, adat-vezérelt döntések |

**Becsült implementációs idő:** Nexus 1-2 session alatt implementálja.

### 📋 FUTURE ROADMAP — LOW Priority (6 tool)

A következő tool-ok a jövőbeli roadmap-re kerültek:
- `check_type_coverage` — Type safety, nice-to-have
- `list_backend_endpoints` — Documentation, Backend sync kell
- `generate_component_tests` — Test coverage, future work
- `profile_react_performance` — Performance, browser integration kell
- `audit_accessibility` — A11y, ESLint plugin elegendő lehet
- `check_dependencies` — Security, npm audit lefedi

### 🔗 Tracking

- **MSG-NEXUS-002:** Nexus implementálja a HIGH priority tool-okat
- **Notification:** Amint kész, Nexus outbox-ból értesítést kapsz

### 📝 Megjegyzések

1. **Nexus roadmap:** Nincs duplikáció — ezek új tool-ok
2. **API design:** JSON response structure megfelelő, a példák alapján implementálunk
3. **Chat vs Work session:** Mind a 4 tool elérhető lesz chat session-ből is
4. **Backward compatibility:** Datahaven-web/client struktúrára optimalizálunk, legacy support nem szükséges
