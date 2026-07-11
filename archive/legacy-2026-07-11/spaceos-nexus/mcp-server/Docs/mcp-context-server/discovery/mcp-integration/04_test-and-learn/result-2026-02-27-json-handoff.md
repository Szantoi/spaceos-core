---
id: result-mcp-handoff
hypothesis: hyp-mcp-json-handoff
experiment: exp-003-handoff
status: success
date: 2026-02-27
---

# Kísérlet Eredménye: Strukturált JSON Handoff az Üzenetváltásoknál (MVE)

## Eredmény Összefoglaló
A kísérlet **sikeresen igazolta**, hogy az MCP Tool registryn keresztül biztosított `trigger_handoff` eszköz helyesen tud fogadni, tárolni és visszaadni összetett JSON payloadokat.

## Tényleges Eredmények
A `test-handoff.ps1` tesztszkript az alábbi funkciókat hívta meg hibátlanul:
1. `trigger_handoff`: Sikeresen befogadta a JSON-t a `sourceRole`, `targetRole`, `summary` és egy összetett `actionItems` array struktúrával.
2. `get_pending_handoffs`: Szerepkör alapján sikeresen leszűrte és strukturált JSON reprezentációban visszaadta a várakozó feladatokat.

A korábbi transzkódolási hiba (UTF-8 transcoding exception) rámutatott, hogy a tesztkörnyezetek (mint a PowerShell) alapértelmezetten elronthatják a JSON payload karakterkódolását, de az MCP Tool definíció maga stabil.

## Visszabonthatósági jelentés
Az MVE teljesítette a "Reversible Prototype Rule"-t. Jelenleg a tool regisztráció a `Program.cs`-be van hardcode-olva, ami egy commit váltsással (revert) könnyen törölhető, nem érinti a fő domain logikát, amíg a System Architect nem ad zöld utat a véglegesítésnek. A tárolás a HandoffMessage adatbázis táblán keresztül történt.

## Konklúzió / Tanulságok
Az "Architecture Decision" ADR-012 helyes és robusztusabb, mint a kizárólagosan markdown fájlokkal történő aszinkron üzenetváltás. A kliens AI-k így garantáltan JSON formátumban veszik/adják át a feladatokat.
