# BACKEND Terminal TODO

> Utolsó frissítés: 2026-06-24
> Kontextus: Q3 Cutting module + TaskMessageBox rendszer

## Prioritás: HIGH

### 1. Q3 Cutting Module folytatás
**Státusz:** ACTIVE
**Leírás:** A szabászat modul fejlesztése folytatódik. Ellenőrizd az inbox-ot a legfrissebb feladatokért.

**Kapcsolódó fájlok:**
- `docs/tasks/active/` - aktív feladatok
- `terminals/backend/inbox/` - bejövő üzenetek

### 2. MCP Auth Token tesztelés (ha root javította)
**Státusz:** WAITING
**Leírás:** A root terminál javítja az MCP auth token validációt. Ha kész, teszteld az új TaskMessageBox MCP toolokat.

---

## Prioritás: MEDIUM

### 3. .NET 8 Clean Architecture patterns
**Leírás:** A `docs/knowledge/architecture/DOTNET_8_CLEAN_ARCHITECTURE_2026.md` tartalmazza a mintákat.

### 4. EHS (Environmental Health & Safety) sprint
**Státusz:** Week 1 in progress
**Ref:** conductor outbox üzenetek az aktuális állapotról

---

## Referencia: TaskMessageBox

A root terminál létrehozott egy új DB-alapú üzenetkezelő rendszert:

**SQLite DB:** `spaceos-nexus/knowledge-service/data/taskmessagebox.db`

**Tábla:** `messages`
- `id` - MSG-TERMINAL-NNN formátum
- `from_terminal`, `to_terminal`
- `title`, `description`
- `status` - UNREAD/READ/DONE/BLOCKED
- `priority`, `model`
- `content_hash` - SHA-256 integritás

**Auto-render:** A DB rekordok automatikusan .md fájlokba renderelődnek:
`terminals/<to>/inbox/<timestamp>_<seq>_<slug>.md`

**Teszt fájl (működik):**
`/opt/spaceos/terminals/backend/inbox/2026-06-24 14:52:44_001_tmb-test-direct-store-call.md`
