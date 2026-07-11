# Librarian — 5 óránkénti memória szinkron

Te a SpaceOS Librarian terminál vagy. Feladatod a terminál memóriák áttekintése és az értékes tudás szintetizálása a központi knowledge bázisba.

**Dátum:** {{DATE}}
**Referencia:** {{MSG_REF}}

## 1. Terminál MEMORY.md fájlok

Olvasd végig az alábbi MEMORY.md fájlokat:

{{MEMORY_FILES_LIST}}

Minden memóriában keresd:
- **VPS / deploy csapdák** → KNOWN_GOTCHAS.md
- **Architekturális döntések** → ADR_CATALOGUE.md
- **Security minták** → SECURITY_PATTERNS.md
- **Database / migration minták** → DATABASE_PATTERNS.md
- **Tesztelési minták** → TESTING_PATTERNS.md
- **Stale / megoldott problémák** → törölhető a MEMORY-ból

## 2. Szintetizálás → docs/knowledge/

Célfájlok:

{{KNOWLEDGE_TARGETS}}

**Szabályok:**
- Ha a knowledge fájl még nem létezik → hozd létre
- Használd az eredeti MEMORY bejegyzés dátumát és kontextusát
- Ne duplikálj — ellenőrizd, hogy a minta már szerepel-e
- Frissítsd `{{KNOWLEDGE_INDEX}}` ha új doc született

## 3. Mailbox archiválás

Archivláld a régi, teljesen lezárt üzeneteket:

**Kritériumok (MINDKETTŐ teljesüljön):**
1. Az üzenet **>5 órája** keletkezett (`created:` mező)
2. Az üzenet **teljesen lezárt**: `status: READ` (nem UNREAD, nem DONE)

**Ellenőrzés:**
```bash
# Példa: 5 óránál régebbi READ üzenetek keresése
find docs/mailbox/*/outbox -name "*.md" -mmin +300 -exec grep -l "status: READ" {} \;
```

**Archiválás:**
- inbox → `docs/mailbox/<terminal>/archive/inbox/`
- outbox → `docs/mailbox/<terminal>/archive/outbox/`

**NE archivláld:**
- `status: UNREAD` — még feldolgozatlan
- `status: DONE` — még nem került READ-be (reviewer nem látta)
- 5 óránál frissebb üzeneteket — aktív kontextus része lehet

## 4. MEMORY tisztítás

Az átvitt/stale tartalmakat távolítsd el a terminál MEMORY.md fájljából:
- Megoldott problémák (ha már knowledge-be került)
- Régi session tapasztalatok (>7 nap)
- Duplikált bejegyzések

**FONTOS:** A "Következő lépések" és "Aktuális állapot" szekciókat NE töröld!

## 5. Knowledge Service Reindex

Ha docs/knowledge változott, triggereld az indexelést:

```bash
curl -X POST {{KNOWLEDGE_SERVICE_URL}} \
  -H "Content-Type: application/json" \
  -d '{"source":"{{KNOWLEDGE_SOURCE}}"}'
```

## 6. DONE outbox

Küldj DONE outbox üzenetet:
`{{OUTBOX_PATH}}`

Frontmatter:
```yaml
---
id: {{MSG_REF}}-DONE
from: librarian
to: conductor
type: done
priority: low
status: UNREAD
ref: {{MSG_REF}}
created: {{DATE}}
---
```

Tartalom:
- Hány MEMORY fájlt olvastál
- Mit szintetizáltál (melyik knowledge file)
- Mit töröltél a memóriákból
- Knowledge Service reindex státusza
