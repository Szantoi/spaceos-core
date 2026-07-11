---
id: MSG-BACKEND-099
from: conductor
to: backend
type: task
priority: high
status: READ
model: haiku
ref: MSG-CONDUCTOR-056
epic: DATAHAVEN-UI-V2
created: 2026-06-30
content_hash: fa812462a1e6a1a8b1d5e6236cfe6f5e9a042e07d01a40f228820d5503f9bb41
---

# Domain Focus API Endpoints — Datahaven UI Phase 1

Implementáld a `/api/planning/domain-focus` GET és PUT endpoint-okat a JoineryTech UI Focus Area Panel-hez.

## Context

A Datahaven UI Planning page-re kerül egy új Focus Area Panel, amely a `docs/planning/domain-focus.md` fájlt jeleníti meg és szerkeszti.

**Architektúra spec:** `/opt/spaceos/docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

## Task

Implementáld a következő két endpoint-ot a `datahaven-web/src/routes/planningRoutes.js` fájlba:

### 1. GET /api/planning/domain-focus

**Funkció:** Olvassa be a `docs/planning/domain-focus.md` fájlt és adja vissza JSON formátumban.

**Response példa:**
```json
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: Milyen funkció segíti...\n- **Backend kapcsolhatóság**: ...",
  "updated_at": "2026-06-30T12:34:56Z"
}
```

**Implementációs elvárások:**
- Használd a `fs.promises.readFile()` metódust
- Parse-old a YAML frontmatter-t (gray-matter library)
- Return 404 ha a fájl nem létezik
- Return 500 ha parse error

### 2. PUT /api/planning/domain-focus

**Funkció:** Frissíti a `docs/planning/domain-focus.md` fájl domain vagy criteria mezőjét.

**Request body példa:**
```json
{
  "domain": "sales",           // Opcionális
  "criteria": "- New criteria" // Opcionális
}
```

**Response példa:**
```json
{
  "success": true,
  "domain": "sales",
  "criteria": "- New criteria",
  "updated_at": "2026-06-30T12:35:01Z"
}
```

**Implementációs elvárások:**
- Validáld a domain értéket (allowed: manufacturing, sales, logistics, finance, quality, hr, all)
- Atomic write (írj temp fájlba, majd rename)
- Parse + write YAML frontmatter (gray-matter)
- Return 400 ha invalid domain
- Return 500 ha write error

## Acceptance Criteria

- [ ] GET endpoint működik és JSON-t ad vissza
- [ ] PUT endpoint frissíti a fájlt
- [ ] Domain validáció működik (7 allowed érték)
- [ ] Atomic write implementálva (temp file + rename)
- [ ] Error handling 404/400/500 kódokkal
- [ ] Manual test: `curl http://localhost:3456/api/planning/domain-focus`

## Technical Notes

**Meglévő minták:**
- `/api/graph/epics` endpoint már létezik a graphRoutes.js-ben → hasonló pattern
- Használd a `planningService.js` fájlt ha service layer kell (opcionális)

**Dependencies:**
- `gray-matter` package — YAML frontmatter parse/write (már telepítve)
- `fs.promises` — async file I/O

## Next Steps

Ha ez kész, a Frontend terminal implementálja a UI komponenst amely ezt az API-t használja.

**Deadline:** Ez a task blokkol minden Frontend UI munkát, prioritás!
