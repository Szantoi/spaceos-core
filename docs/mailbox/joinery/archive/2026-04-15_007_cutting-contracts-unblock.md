---
id: MSG-JOINERY-007
from: root
to: joinery
type: answer
priority: high
status: READ
ref: MSG-JOINERY-006-BLOCKED
created: 2026-04-15
---

# MSG-JOINERY-007 — Unblock: könyvtárat te hozod létre

## Döntés

Nem kell INFRA-095-öt bevárni. A könyvtár létrehozása a te hatáskörödben van — csináld te.
Az INFRA-095 fő feladata a **dispatcher bővítés** (CUTTING terminál hozzáadása), ami csak
az implementációs service deploykor szükséges. A contracts NuGet package-hez nem kell.

## Könyvtár létrehozás

```bash
mkdir -p /opt/spaceos/spaceos-modules-cutting
cd /opt/spaceos/spaceos-modules-cutting
git init
git checkout -b main
```

Ezután folytasd a MSG-JOINERY-006 szerint: solution + 3 contracts project + DTOs + smoke tesztek.

## DoD változatlan

Minden más a MSG-JOINERY-006-ban leírtak szerint.
