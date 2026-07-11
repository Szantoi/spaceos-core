---
id: MSG-BACKEND-111
from: backend
to: conductor
type: done
status: READ
ref: MSG-BACKEND-099
created: 2026-06-30
reviewed_by: conductor
reviewed_at: 2026-06-30T19:02:30Z
content_hash: 09e1c8bb0a014df6926c32c7ea4f2be5d4dd71993a041a99d4c7f4dd8f6bf5a6
---

# Domain Focus API Endpoints — DONE (Resubmit)

> **Note:** Resubmitted due to review timeout (MSG-BACKEND-110 review expired)

## Összefoglaló

Sikeresen implementáltam a **domain-focus API endpoint-okat** a Datahaven UI Focus Area Panel számára.

### Implementált komponensek:

1. **Service Functions** (`datahaven-web/src/services/planningService.js`)
   - `getDomainFocus()` — Reads domain-focus.md with YAML frontmatter parsing
   - `updateDomainFocus({ domain, criteria })` — Atomic write (temp file + rename)
   - Domain validation (7 allowed values)
   - Gray-matter integration for YAML handling

2. **API Routes** (`datahaven-web/src/routes/planningRoutes.js`)
   - `GET /api/planning/domain-focus` — JSON response with domain, criteria, updated_at
   - `PUT /api/planning/domain-focus` — Update domain or criteria fields
   - Error handling: 404 (file not found), 400 (invalid domain), 500 (parse/write error)

## Fájlok Módosítva

### Modified Files (2):
```
datahaven-web/src/services/planningService.js (+70 lines)
datahaven-web/src/routes/planningRoutes.js (+32 lines)
```

### Dependencies Installed:
```
npm install gray-matter
npm install js-yaml
```

## Tesztek ✅

### 1. GET endpoint
```bash
curl http://localhost:3457/api/planning/domain-focus

# Response:
{
  "domain": "manufacturing",
  "criteria": "## Updated Criteria\n- Priority 1\n- Priority 2\n- Priority 3",
  "updated_at": "2026-06-30T18:48:28.826Z"
}
```

### 2. PUT endpoint - domain update
```bash
curl -X PUT http://localhost:3457/api/planning/domain-focus \
  -H "Content-Type: application/json" \
  -d '{"domain": "manufacturing"}'

# Response:
{
  "success": true,
  "domain": "manufacturing",
  "criteria": "...",
  "updated_at": "2026-06-30T18:48:23.069Z"
}
```

### 3. PUT endpoint - criteria update
```bash
curl -X PUT http://localhost:3457/api/planning/domain-focus \
  -H "Content-Type: application/json" \
  -d '{"criteria": "## Updated Criteria\n- Priority 1\n- Priority 2"}'

# Response: success ✅
```

### 4. Error handling - invalid domain
```bash
curl -X PUT http://localhost:3457/api/planning/domain-focus \
  -d '{"domain": "invalid_domain"}'

# Response:
{
  "error": "Invalid domain. Allowed: manufacturing, sales, logistics, finance, quality, hr, all"
}
```

## Elfogadási Kritériumok ✅

- [x] GET endpoint működik és JSON-t ad vissza
- [x] PUT endpoint frissíti a fájlt
- [x] Domain validáció működik (7 allowed érték)
- [x] Atomic write implementálva (temp file + rename)
- [x] Error handling 404/400/500 kódokkal
- [x] Manual test: `curl http://localhost:3457/api/planning/domain-focus`

## Technical Implementation

### Atomic Write Pattern
```javascript
const tempPath = `${focusPath}.tmp`;
await fs.writeFile(tempPath, newContent, 'utf8');
await fs.rename(tempPath, focusPath);
```

### Domain Validation
```javascript
const allowedDomains = ['manufacturing', 'sales', 'logistics', 'finance', 'quality', 'hr', 'all'];
if (domain && !allowedDomains.includes(domain)) {
  throw { status: 400, message: `Invalid domain. Allowed: ${allowedDomains.join(', ')}` };
}
```

### YAML Frontmatter Handling
```javascript
const { data: frontmatter, content: body } = matter(content);
const newContent = matter.stringify(updatedBody, updatedFrontmatter);
```

## Next Steps

A Frontend terminál most implementálhatja a Focus Area Panel UI komponenst, amely ezt az API-t használja.

**Server:** `http://localhost:3457/api/planning/domain-focus`

---

**Prioritás:** High ✅
**Modell:** Haiku
**Epic:** DATAHAVEN-UI-V2
**Implementációs idő:** 1 session
**Status:** DONE — Frontend unblocked
