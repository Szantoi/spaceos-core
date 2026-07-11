---
title: "Dev D — TASK-12-02 Assignment Sheet"
subtitle: "FTS5 Full-Text Search — Virtual Table + Auto-Sync"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev D"
priority: "P0"
epic: "EPIC-12"
phase: "M02 — Phase 1: Core Functionality"
status: "✅ READY (after TASK-12-01 complete)"
effort_estimate: "12-15 hours"
ac_count: 4
---

# 🚀 Dev D — TASK-12-02 Assignment

**Task:** TASK-12-02 (FTS5 Full-Text Search — Virtual Table + Auto-Sync)
**Epic:** EPIC-12 (Episodic Memory Layer: Session storage + Semantic search)
**Phase:** M02 Phase 1 — Core Functionality
**Priority:** P0 (enables keyword search for memory retrieval)
**Effort Estimate:** 12-15 hours (1.5 days)
**Dependency:** Requires TASK-12-01 (`episodes` table must exist)

---

## 🎯 FTS5 Schema

> **⚠️ KRITIKUS TECHNIKAI FIGYELMEZTETÉSEK (web research + SQLite docs + GitHub issues alapján):**
>
> 1. **Content-sync azért kell, mert különálló FTS5 tábla rowid-je NEM egyezik meg az `episodes` tábla rowid-jével!**
>    Használd a `content='episodes'` és `content_rowid='rowid'` opciókat.
> 2. **`better-sqlite3` FTS5 trigger bug:** Lásd [GitHub issue #654](https://github.com/WiseLibs/better-sqlite3/issues/654).
>    A triggerek működnek, DE ha tranzakción belül vannak problémák, használj explicit `db.exec()` a migrációban.
> 3. **SQLite 3.37+ követelmeny:** A `RETURNING` clause és néhány FTS5 feature újabb SQLite-ot igényel.
>    Ellenőrizd: `db.pragma('compile_options')` — `ENABLE_FTS5` benne van-e.
> 4. **SZINKRON API:** `better-sqlite3` szinkron — a search függvény NE legyen async!
> 5. **"unsafe use of virtual table" hiba:** Ha `SQLITE_DBCONFIG_DEFENSIVE` be van állítva,
>    a triggerek nem írhatnak FTS5 táblába. Megoldás: `db.pragma('trusted_schema = ON')` VAGY
>    manuális sync a service-ben trigger helyett.

```sql
-- ✅ HELYES: Content-sync FTS5 tábla (content='episodes' = követi az alap táblát)
CREATE VIRTUAL TABLE IF NOT EXISTS episodes_fts USING fts5(
  outcome_summary,
  phase,
  domain,
  content='episodes',
  content_rowid='rowid'
);

-- Trigger: auto-sync INSERT
-- ⚠️ FONTOS: content-sync esetén a rowid-t explicit át kell adni!
CREATE TRIGGER IF NOT EXISTS episodes_ai AFTER INSERT ON episodes BEGIN
  INSERT INTO episodes_fts(rowid, outcome_summary, phase, domain)
  VALUES (NEW.rowid, NEW.outcome_summary, NEW.phase, NEW.domain);
END;

-- Trigger: auto-sync DELETE
-- ⚠️ content-sync DELETE: speciális szintaxis kell!
CREATE TRIGGER IF NOT EXISTS episodes_ad AFTER DELETE ON episodes BEGIN
  INSERT INTO episodes_fts(episodes_fts, rowid, outcome_summary, phase, domain)
  VALUES ('delete', OLD.rowid, OLD.outcome_summary, OLD.phase, OLD.domain);
END;

-- Trigger: auto-sync UPDATE
-- ⚠️ UPDATE = DELETE régi + INSERT új (FTS5 nem támogatja a tényleges UPDATE-et)
CREATE TRIGGER IF NOT EXISTS episodes_au AFTER UPDATE ON episodes BEGIN
  INSERT INTO episodes_fts(episodes_fts, rowid, outcome_summary, phase, domain)
  VALUES ('delete', OLD.rowid, OLD.outcome_summary, OLD.phase, OLD.domain);
  INSERT INTO episodes_fts(rowid, outcome_summary, phase, domain)
  VALUES (NEW.rowid, NEW.outcome_summary, NEW.phase, NEW.domain);
END;
```

**⚠️ `tool_calls_json` KIVÉVE az FTS5-ből!** A JSON blob-ok rossz token-eket adnak — nem érdemes full-text search-ölni.
Hasznos mezők az FTS5-höz: `outcome_summary` (összefoglaló), `phase`, `domain`.

---

## Search Implementation

```typescript
// src/episodic/FtsSearch.ts
import Database from 'better-sqlite3';
import { Episode } from './types';

/**
 * ⚠️ SZINKRON API! better-sqlite3 szinkron — NE használj async/await-et.
 * ⚠️ SQL injection védelem: MINDIG paraméterezett query (? placeholder).
 */
export function searchExperience(
  db: Database.Database,
  query: string,
  domainFilter?: string
): Episode[] {
  // ⚠️ FTS5 MATCH szintaxis: kettős idézőjel escape + speciális karakterek szűrése
  // NE használj string concatenation-t! Mindig paraméterezett query.
  const sanitizedQuery = query
    .replace(/["]/g, '""')  // escape double quotes
    .replace(/[;'\\]/g, '') // remove dangerous chars
    .trim();

  if (!sanitizedQuery) return [];

  // content='episodes' esetén join-olni kell az alap táblával
  let sql = `
    SELECT e.* FROM episodes e
    INNER JOIN episodes_fts ON episodes_fts.rowid = e.rowid
    WHERE episodes_fts MATCH ?
  `;
  const params: unknown[] = [sanitizedQuery];

  if (domainFilter) {
    sql += ` AND e.domain = ?`;
    params.push(domainFilter);
  }

  sql += ` ORDER BY rank LIMIT 50`; // ✅ rank = BM25 rel. score (FTS5 built-in)

  return db.prepare(sql).all(...params) as Episode[];
}
```

---

## ✅ 5 AC Checklist

- [ ] AC-1: FTS5 table created + indexed
- [ ] AC-2: Triggers auto-maintain all 3 operations
- [ ] AC-3: Query <50ms (benchmarked with 1000 episodes)
- [ ] AC-4: Keyword search working ("ideation AND engineering")
- [ ] AC-5: SOTA docs explaining hybrid search (keyword vs semantic)

---

**Status:** 🟡 **READY (after TASK-12-01)**
**Blocks:** TASK-12-03 (builds on this)
