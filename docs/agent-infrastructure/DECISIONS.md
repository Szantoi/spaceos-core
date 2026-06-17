# Datahaven + Resonance — Alapítói döntések

> Keletkezett: 2026-06-16 · Root session (Sárkány) + Gábor
> Státusz: LEZÁRT — ezek az alapok, nem nyithatók újra vita nélkül

---

## 1. Elnevezési rendszer (Shadowrun lore)

| Fogalom | Jelentés | Shadowrun megfelelő |
|---|---|---|
| **Datahaven** | Független agent koordinációs hub | Titkos szerverközpont, céges kéztől védve |
| **Resonance** | AI/LLM daemon fejlesztői környezet | A Mátrix mélyén lévő erő amit Technomancerek használnak |
| **Daemon** | Autonóm agent entitás (volt: "terminál") | Háttérben futó autonóm Mátrix entitás |
| **Sárkány** | A Root intelligencia neve | — |

## 2. Projekt szeparáció

**Datahaven és Resonance SpaceOS-tól FÜGGETLEN projektek.**

- SpaceOS = termék (magyar faipar ERP)
- Datahaven = általános célú agent koordinációs réteg
- Resonance = általános célú daemon fejlesztői környezet
- Datahaven *kiszolgálja* SpaceOS-t, de nem *része* SpaceOS-nak

**Miért:** Ha Datahaven+Resonance SpaceOS-ba van ágyazva, a Joinery/Kernel/FE terminálok összekeverednek az agent infrastruktúra munkával. Külön repo → tiszta felelősség.

## 3. Datahaven architektúra

Három módszer szintézise egy koherens rendszerré:

```
Módszer 1 — JoineryTech.McpServer  (TypeScript, MCP protokoll, RBAC, RAG, FSM)
Módszer 2 — Marvin (PrefectHQ)     (Python, Tasks, Threads, explicit orchestráció)
Módszer 3 — SpaceOS bash pipeline  (meglévő: nightwatch, reviewer, planning)
                    ↓
               Datahaven
```

**Három fázis — fokozatos migráció, élő Doorstar nem szakad meg:**

| Fázis | Mit | Mikor |
|---|---|---|
| 1 | McpServer knowledge service (RAG, ChromaDB, voyage-3-lite) | Slice 1 közben, párhuzamos |
| 2 | Marvin átveszi a planning pipeline-t (plan-scan/select/debate) | Slice 1 vége |
| 3 | Marvin reviewer + nightwatch, RbacFilter, WorkflowStateTracker | Slice 2 előtt |

## 4. Resonance architektúra

Resonance = ahol Daemonok születnek és fejlődnek.

Resonance artifaktok (már meglévők, ide tartoznak):
- `.claude/skills/` — spaceos-arch-planner, ddd-arch-planner, spaceos-frontend-arch-planner
- `database/roles/` (McpServer-ből) — architect.role.md, backend_developer.role.md stb.
- `database/knowledge/` (McpServer-ből) — engineering/*.knowledge.md
- DWI workflow (Discover→Why→Implement Handoff)
- reviewer-context.md — daemon-tudás injekció review-hoz

Resonance **tervez, tesztel és javít** Daemonokat.
Daemonok **futnak és koordinálnak** Datahaven-en keresztül.

## 5. Daemon átnevezés

SpaceOS terminálok = Daemonok. A CLAUDE.md fájlok = Daemon definíciók.

```
kernel.daemon      (volt: KERNEL terminál)
architect.daemon   (volt: ARCHITECT terminál)
librarian.daemon   (volt: LIBRARIAN terminál)
fe.daemon          (volt: FE terminál)
tester.daemon      (volt: TESTER terminál)
... stb.
```

A SpaceOS-on belül a "terminál" szó megmarad a napi kommunikációban — de a mélyebb architektúra leírásban "daemon".

## 6. Embedding + RAG döntés

- **Embedding model:** voyage-3-lite (Anthropic API — vendor egységesség)
- **Vector store:** ChromaDB (JoineryTech.McpServer referencia implementáció alapján)
- **Fallback:** MemoryVectorStore (fejlesztéshez, ChromaDB nélkül)
- **Forrás:** `docs/knowledge/**/*.md` + `database/knowledge/**/*.knowledge.md`

## 7. VPS kapacitás

Gábor gondoskodik a bővítésről ha szükséges — nem blokkol.
Marvin + McpServer + ChromaDB = extra process terhelés, elfogadott.

## 8. GitHub repók

```
github.com/Szantoi/datahaven     ← McpServer adaptáció + Marvin + koordináció
github.com/Szantoi/resonance     ← Daemon tervező + skill rendszer + role definíciók
github.com/Szantoi/JoineryTech.McpServer  ← referencia (beolvad Datahaven-be)
```

Státusz: repo-k létrehozása folyamatban (gh CLI telepítés vagy Gábor manuálisan).

## 9. spaceos-arch-planner — kötelező

Minden tervdokumentumhoz (Architect daemon outputja) kötelező a v1→v4 pipeline:
- v1 Draft → v2 DB review → v3 Security → v4 Backend
- Minden döntés ADR bejegyzéssel zárul
- "IMPLEMENTÁCIÓRA KÉSZ" státusz minimum v2 után

**Miért:** Korábban döntések születtek de nem maradt nyomuk. A skill kikényszeríti az artifact képzést.

## 10. Root (Sárkány) szerepe

A nightwatch+reviewer+pipeline automatizálásával a Root felszabadult a manuális DONE feldolgozástól.
Root most: stratégia, architektúra, daemon tervezés, Datahaven/Resonance építés.
Root NEM: DONE üzenetek kézi feldolgozása, kódírás.
