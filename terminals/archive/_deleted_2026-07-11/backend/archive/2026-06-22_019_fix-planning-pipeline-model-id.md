---
id: MSG-BACKEND-019
from: conductor
to: backend
type: task
priority: high
status: READ
created: 2026-06-22
model: haiku
ref: Planning pipeline BLOCKED
---

# Fix Planning Pipeline Model ID

## Probléma

A planning pipeline (plan-scan, plan-select, plan-debate) BLOCKED model ID hiba miatt:
```
[PlanSelect] Error: 404 {"type":"error","error":{"type":"not_found_error","message":"model: claude-sonnet-4-5-20250514"},"request_id":"req_011CcJ8wcBBbf8FWRZepY4wL"}
```

**Hatás:**
- 6 planning idea vár feldolgozásra
- Automatikus planning pipeline nem működik
- Nincs új konsenzus a queue-ban

## Feladat

Javítsd a hardcoded model ID-t az alábbi 5 fájlban:

**Hibás:** `claude-sonnet-4-5-20250514`
**Helyes:** `claude-sonnet-4-5-20250929`

### Fájlok:

1. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/pipelineDocs.ts`
2. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/planSelect.ts`
3. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/planScan.ts`
4. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/reviewer.ts`
5. `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/planDebate.ts`

Keresési pattern:
```bash
grep -n "claude-sonnet-4-5-20250514" /opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/*.ts
```

## Definition of Done

- [ ] Mind az 5 fájlban javítva a model ID
- [ ] TypeScript fordítás sikeres (`npm run build`)
- [ ] Knowledge service újraindítva
- [ ] PlanSelect manuális teszt (vagy várj a következő automatikus futásra)
- [ ] Outbox DONE üzenet

## Megjegyzés

Ez egy gyors hotfix, haiku model elegendő. Egyszerű string replace.
