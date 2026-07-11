---
id: exp-003-handoff
title: "Kísérleti Terv: Strukturált JSON Handoff az Üzenetváltásoknál (MVE)"
status: draft
date: 2026-02-27
hypothesis: hyp-mcp-json-handoff
adr: ADR-012
---


## Cél

Bebizonyítani, hogy egy JSON alapú MCP tool (`trigger_handoff`) adatsérülés nélkül képes leváltani a fájlalapú üzenetküldést a Communication Hubban, miközben validálja a payloadot.

## Minimum Viable Experiment (MVE) definíció

1. **Szűkített hatókör:** Az API `Program.cs`-ben már ideiglenesen regisztrált `trigger_handoff` eszközt teszteljük.
2. **Kliens Teszt:** PowerShell / cURL scripttel elküldünk egy összetett JSON hívást a `trigger_handoff` felé az `actionItems` payloadba ágyazva például sortöréseket és idézőjeleket.
3. **Visszaolvasás:** Szintén MCP hívással (`get_pending_handoffs`) le is kérjük a létrehozott message-t.
4. **Visszabonthatóság (Reversible Rule):** Az adatbázis (SQLite) írás jelen állapotban lehet hogy megtörténik (mivel az API fut), de a teszthez egy dummy Role nevet (`TestRole123`) használunk, ami később könnyen kipucolható vagy figyelmen kívül hagyható, nem okoz logikai törést a flow-ban.

## Sikerkritériumok

- [ ] A `trigger_handoff` elfogadja a JSON payloadot ("sourceRole", "targetRole", "summary", "actionItems").
- [ ] A `get_pending_handoffs` visszatérési értékében a string attribútumok érintetlenek/validak, nincsenek konverziós hibák.

## Szerepkör: Experimenter

Kísérletvégrehajtó: **The Experimenter** (`03_prototype` fázis).
