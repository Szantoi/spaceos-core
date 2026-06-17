---
domain: manufacturing
type: endpoint_gap
priority: medium
created: 2026-06-16
scanned_by: haiku
---

# Anyagfelhasználás nyomon követés

## Mit old meg
Melyik rendeléshez mennyi anyag ment el — veszteség riport, készletkivetítés.

## Jelenlegi állapot
Inventory modul van (5004 port), de MasterdataPage anyagtörzs csak listázza — felhasználás nincs.

## Bekötési lehetőség
GET /inventory/api/inventory/lots + movements összekötve DesignPage cutting listával.

## Iparági relevancia
Anyagköltség az asztalosipar legnagyobb változó költsége — nyomon követés kritikus.
