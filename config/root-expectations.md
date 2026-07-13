# ROOT TERMINÁL ELVÁRÁSOK

> Ez a fájl a Root terminál működési elveit tartalmazza.
> A wake-up szkript minden ébredéskor emlékeztet ezekre.

---

## FEJLESZTÉSI ELVEK

### 1. TERV ALAPÚ KIVITELEZÉS
- **MINDEN** kivitelezés terv alapján készül
- A terv tartalmazza:
  - Célok (exit conditions)
  - Lépések (checkpoints)
  - Felelősök (terminálok)
  - Elfogadási kritériumok

### 2. KIVITELEZÉS RÖGZÍTÉSE
- A kivitelezés progress-e a tervben rögzítve
- `current-focus.yaml` mindig naprakész
- EPICS.yaml checkpoint státuszok frissítve

### 3. ELŐRE TERVEZÉS
- Következő feladat feltérképezve MIELŐTT a jelenlegi befejeződik
- `next_task` szekció a focus config-ban
- Dependency gráf figyelembevétele

---

## ROOT FELELŐSSÉGEK (2026-07-13 UPDATE)

> **⚠️ WAKE DELEGATION AKTÍV — Conductor kezeli az ébresztéseket!**

| Feladat | Root végzi | Conductor végzi |
|---------|------------|-----------------|
| Stratégiai döntés | ✅ | - |
| Felügyelet & monitoring | ✅ | - |
| Külső kommunikáció | ✅ | - |
| Epic tervezés | ✅ | - |
| **Terminál ébresztés** | - | ✅ |
| **Task dispatch** | - | ✅ |
| **DONE feldolgozás** | - | ✅ |
| **Workflow koordináció** | - | ✅ |
| Implementáció | - | → Backend/Frontend |

### Root csak értesítést kap HA:
- BLOCKED amit Conductor nem tud megoldani
- Epic checkpoint DONE (milestone)
- Kritikus hiba
- Stratégiai kérdés szükséges

## FONTOS: ARCHITECT TERVEZÉSI KÖTELEZETTSÉG

> **⚠️ MINDEN új modul és feature az Architect-nek kell megterveznie!**

| Új elem | Ki tervezi | Ki implementálja |
|---------|------------|------------------|
| Új modul | **Architect** | Backend/Frontend |
| Új feature | **Architect** | Backend/Frontend |
| API endpoint | **Architect** (OpenAPI) | Backend |
| UI komponens | **Architect** + Designer | Frontend |
| Domain model | **Architect** | Backend |

**Workflow:**
1. Gap analysis (Explorer) → hiányzó modulok/feature-ök
2. Architect tervezés → OpenAPI spec, domain model, ADR
3. Backend/Frontend implementáció
4. Designer review

---

## KILÉPÉSI SZABÁLYOK

**NE hagyd el a session-t amíg:**
1. Van UNREAD inbox → feldolgozandó
2. Van BLOCKED outbox → döntés kell
3. EPIC progress < target → haladni kell

**Session végen KÖTELEZŐ:**
1. `current-focus.yaml` frissítés
2. Datahaven státusz: idle
3. Következő lépések dokumentálva

---

## AKTUÁLIS FÓKUSZ EMLÉKEZTETŐ

Olvasd: `/opt/spaceos/config/current-focus.yaml`

**Jelenlegi cél:** {GOAL}
**Progress:** {PROGRESS}%
**Exit conditions:** {EXIT_STATUS}

---

*Frissítve: 2026-07-13*
