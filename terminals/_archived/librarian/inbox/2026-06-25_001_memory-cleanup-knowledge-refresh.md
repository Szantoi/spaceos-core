---
id: MSG-LIBRARIAN-008
from: root
to: librarian
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-25
content_hash: 790fa6c6f567fa245cd47f41747fcbe93f5c108cfea1d228ca714a5b5f86da36
---

# Memória Keresés és Tudásbázis Frissítés

## Feladat

Végezz átfogó memória tisztítást és tudásbázis frissítést az elmúlt napok fejlesztései alapján.

## 1. Memória Audit

Ellenőrizd az összes terminál MEMORY.md fájlját:
- `terminals/*/MEMORY.md`

Keresd meg:
- Elavult információkat (>7 napos, már nem releváns)
- Duplikált bejegyzéseket
- Inkonzisztenciákat terminálok között
- Hiányzó session összefoglalókat

## 2. Tudásbázis Frissítés

Frissítsd a `docs/knowledge/` mappát az új fejlesztésekkel:

### Új témák az elmúlt napokból:
- **Terminal-based DONE review** (Architect + Librarian review flow)
- **Cold mode session management** (MEMORY.md mentés, session kill)
- **Emergency-stop API** (scheduler leállítás)
- **Monitor terminál** (10 perces health check)
- **TaskMessageBox** (DB-backed inbox/outbox)
- **Dispatch Control** (Token Budget, Scheduled Windows)
- **Telegram integráció** (webhook, intent parsing)

### Frissítendő fájlok:
- `docs/knowledge/INDEX.md` — új dokumentumok hozzáadása
- `docs/knowledge/patterns/` — új minták dokumentálása
- `docs/knowledge/context/*_CONTEXT.md` — terminál kontextusok frissítése

## 3. Feldolgozási Napló

Frissítsd a `terminals/librarian/PROCESSED_LOG.md` fájlt:
- Mi lett feldolgozva
- Mi lett archiválva
- Mi lett törölve (elavult)

## 4. Output

Írj DONE outbox-ot a következő tartalommal:
- Hány MEMORY.md lett frissítve
- Hány knowledge doc lett létrehozva/frissítve
- Milyen elavult információk lettek eltávolítva
- INDEX.md változások összefoglalója

## Acceptance Criteria

- [ ] Minden terminál MEMORY.md áttekintve
- [ ] docs/knowledge/INDEX.md frissítve
- [ ] Legalább 2 új pattern dokumentálva
- [ ] PROCESSED_LOG.md naprakész
- [ ] DONE outbox summary
