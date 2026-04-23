# Inbox üzenet sablonok

## Feladat kiadás (type: task)

```markdown
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: <terminál>
type: task
priority: critical|high|medium|low
status: UNREAD
ref: <kapcsolódó MSG ID, vesszővel elválasztva>
created: YYYY-MM-DD
---

# MSG-<TERMINAL>-<NNN> — <Feladat rövid neve>

## Kontextus
<Miért szükséges ez a feladat — 2-3 mondat>

## Feladat
<Konkrét teendők, hivatkozásokkal a releváns fájlokra>

## Definition of Done
- [ ] <ellenőrizhető feltétel 1>
- [ ] <ellenőrizhető feltétel 2>
- [ ] DONE outbox: `MSG-<TERMINAL>-<NNN>-DONE`

## Visszajelzés
Outboxba: `MSG-<TERMINAL>-<NNN>-DONE`
```

## Válasz blokkolt terminálnak (type: answer)

```markdown
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: <terminál>
type: answer
priority: high
status: UNREAD
ref: <a BLOCKED üzenet ID-ja>
created: YYYY-MM-DD
---

# MSG-<TERMINAL>-<NNN> — Válasz: <téma>

## Döntés
<A root döntése, egyértelműen>

## Következő lépés
<Mit csináljon a terminál ezután>
```

## Visszadobás (hiányos DONE)

```markdown
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: <terminál>
type: task
priority: high
status: UNREAD
ref: <az eredeti DONE MSG ID>
created: YYYY-MM-DD
---

# MSG-<TERMINAL>-<NNN> — Visszadobás: <feladat neve>

## Hiánylista
- [ ] <konkrét hiány 1>
- [ ] <konkrét hiány 2>

## Elvárás
<Mit kell pótolni, mielőtt újra DONE-t küldhet>
```

## Deploy feladat (INFRA terminálnak)

```markdown
---
id: MSG-INFRA-<NNN>
from: root
to: infra
type: task
priority: high
status: UNREAD
ref: <az előző CODE DONE MSG ID>
created: YYYY-MM-DD
---

# MSG-INFRA-<NNN> — <Service> deploy (<commit hash>)

## Kontextus
<Melyik terminál végezte a kódot, commit hash, mit javít>

## Feladat
```bash
cd /opt/spaceos/<repo>
git pull origin develop
<build parancs>
<restart parancs>
<health check>
```

## Definition of Done
- [ ] VPS-en `<commit>` fut
- [ ] health endpoint → 200
- [ ] DONE outbox: `MSG-INFRA-<NNN>-DONE`
```

---

## Sorszámozás szabályai

- NNN = az adott terminálnál az eddig legmagasabb szám + 1
- Minden terminálnak saját, független számozása van
- A szám az **inbox fájl neve** és az **MSG ID** részei is

```bash
# Következő szám meghatározása:
ls docs/mailbox/<terminál>/inbox/ | sort | tail -1
# pl: 2026-04-14_067_... → következő: 068
```

## Prioritás választás

| Szint | Mikor |
|---|---|
| `critical` | E2E baseline-t blokkolja, Doorstar Q2 happy path-en van |
| `high` | Ismert blokkert javít, de van workaround |
| `medium` | Batch 0 cleanup, tech debt |
| `low` | Backlog, nem blokkolja a közeljövőt |
