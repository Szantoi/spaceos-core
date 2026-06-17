---
id: MSG-FE-044
from: root
to: fe
type: task
priority: high
status: READ
created: 2026-05-29
---

# FE-044 — DesignPage paraméter wizard API bekötés

## Háttér

A DesignPage (`/w/design`) jelenleg teljesen mock adaton fut (`PARAM_TEMPLATES` mock tömb).
Az Abstractions API él — a wizard bekötése most elvégezhető.

## API

```
GET  /abstractions/api/modules/templates              → sablon lista (picker-hez)
GET  /abstractions/api/modules/templates/{id}         → sablon részletek + paraméterek
PUT  /abstractions/api/modules/templates/{id}/parameters/{key}   → paraméter beállítás
POST /abstractions/api/modules/templates/{id}/calculate           → számítás eredmény
GET  /abstractions/api/modules/templates/{id}/cutting-list        → vágólista előnézet
```

## Feladat

### 1. Sablon választó

- `GET /abstractions/api/modules/templates` → sablon kártyák / lista
- Mock fallback ha API nem elérhető
- Kiválasztáskor navigáljon a paraméter wizardba

### 2. Paraméter wizard

- `GET /abstractions/api/modules/templates/{id}` → dinamikus paraméterek betöltése
- Paraméter szerkesztő form: kulcs → érték input (szám mező)
- `PUT /abstractions/api/modules/templates/{id}/parameters/{key}` → paraméter mentés
- Összes paraméter módosítható az API-ból jövő definíció alapján

### 3. Számítás / előnézet

- "Számítás indítása" gomb → `POST /abstractions/api/modules/templates/{id}/calculate`
- Eredmény megjelenítése (összefoglalók, méretek)
- Opcionális: `GET /{id}/cutting-list` → vágólista preview szekció

### Meglévő mock megőrzése

Ha a sablon nem töltődik be API-ból → marad a `PARAM_TEMPLATES` mock (fallback).

## Build + test gate

- `pnpm build` → 0 TS hiba
- `pnpm test` → minden zöld, min. +10 új teszt

## DONE kritériumok

- [ ] Sablon picker API-ból tölt (mock fallback)
- [ ] Paraméter wizard dinamikus (API template definíció alapján)
- [ ] Paraméter mentés (`PUT`) + számítás (`POST /calculate`) bekötve
- [ ] `pnpm build` → 0 hiba
- [ ] `pnpm test` → minden zöld (+10 legalább)
- [ ] Outbox DONE commit hash-sel
