---
id: TASK-17-07
title: "Kod audit: hardcoded domain utv nelkul"
epic: EPIC-17
completed_by: [Your Name]
date: 2026-03-13
pr: []
---

# TASK-17-07: Kod audit – hardcoded domain útvonalak nélkül

## Mi volt a cél?
Kiszűrni minden kódot a `src/` könyvtárból, ahol a `engineering` (vagy más domain) string hardkódolt értékként jelenik meg, és biztosítani, hogy csak teszt fixture-okban forduljon elő.

## Mit találtam?

- A legtöbb `engineering` előfordulás kommentben, dokumentációban vagy teszt fixture-ekben jelent meg.
- Egyetlen helyen volt valódi kód, ahol a `engineering` string szerepelt: a `src/mcp/tools/memory.ts` `save_episode` metódusában, ahol alapértelmezett domainként `engineering` volt megadva.

## Javítás
- A `memory.ts`-ben az alapértelmezett domain `metadata.domain || context.domain || 'engineering'` helyett most `metadata.domain || context.domain || 'unknown'` (vagy csak `context.domain`) használatos, így nem lesz hardkodolt domain.

## Ellenőrzés
- Lefuttattam a `grep -r "engineering" src/` parancsot és ellenőriztem, hogy a találatok kizárólag tesztekben/commentekben fordulnak elő.
- A célzott tesztek (pm-query + multi-domain E2E) zöldek.

## Következő lépések
- Ha szükséges, bővíthetjük a `domain` validációt, hogy csak a `domains` táblában szereplő értékek legyenek érvényesek (jelenleg a `context.domain` bármi lehet).

