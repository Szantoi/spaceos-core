---
id: MSG-ARCHITECT-010
from: root
to: architect
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-23
content_hash: 593f1bcdaec6e7ee1e0ccc939e1949cbe02c9c78f83c3b09a7e44c5dccfb9e9b
---

# Datahaven UI bővítés — Focus terület + Flow szerkesztő

## Kontextus

A Datahaven Dashboard-ot bővíteni szeretnénk két új funkcióval:

1. **Conductor Focus Area megjelenítés** — `docs/planning/domain-focus.md` tartalmának megjelenítése és szerkesztése
2. **Flow/Workflow Editor** — `docs/projects/EPICS.yaml` vizualizálása Mermaid alapon + szerkesztés

## Jelenlegi állapot

A Datahaven Dashboard 4 oldallal rendelkezik:
- `/` — Dashboard (terminálok, üzenetek)
- `/kanban.html` — Dual-track Kanban
- `/planning.html` — Planning pipeline (Ideas → Queue)
- `/projects.html` — Gantt timeline

## Feladat

Tervezd meg az új UI komponenseket:

### 1. Focus Area Panel

**Mit jelenítsen meg:**
- Aktuális domain fókusz: `domain: manufacturing`
- Szempont lista (markdown)
- Domain választó dropdown a 7 lehetséges értékkel

**Helye:**
- Planning oldalon? Dashboard-on? Új oldal?

### 2. Flow/Workflow Editor

**Mit jelenítsen meg:**
- EPICS.yaml epic-ek Mermaid graph formában
- Epic → Epic dependency nyilak
- Státusz színkódok (done=zöld, active=sárga, pending=szürke, blocked=piros)
- Kattintható node-ok részletekkel

**Interakció:**
- Epic státusz változtatás (kattintás → dropdown)
- Dependency hozzáadás/törlés (drag-drop vagy form)
- Export Mermaid diagram

**Technikai szempontok:**
- Létezik Graph API: `localhost:3456/api/graph/epics`, `localhost:3456/api/graph/mermaid/epic/EPICS`
- A Planning oldal már tartalmaz workflow tab-ot (de üres)

## Elvárt output

1. UI mockup leírás mindkét komponenshez
2. Komponens elhelyezési javaslat (melyik oldalra)
3. Adatfolyam diagram: honnan jön az adat → hogyan jut el a UI-ra
4. CSS/Design irányelvek (illeszkedés a meglévő Datahaven stílushoz)

## Referencia fájlok

- `/opt/spaceos/docs/planning/domain-focus.md`
- `/opt/spaceos/docs/projects/EPICS.yaml`
- `/opt/spaceos/datahaven-web/public/planning.html`
- `/opt/spaceos/datahaven-web/public/css/styles.css`
- `/opt/spaceos/spaceos-nexus/knowledge-service/src/graph/` (Graph API)
