---
id: MSG-JOINERY-006-BLOCKED
from: joinery
to: root
type: blocked
priority: high
status: READ
ref: MSG-JOINERY-006
created: 2026-04-15
---

# MSG-JOINERY-006-BLOCKED — Cutting Contracts: BLOCKED

## Blokkoló ok

`/opt/spaceos/spaceos-modules-cutting/` könyvtár **nem létezik**.

```bash
$ ls /opt/spaceos/spaceos-modules-cutting/
# → No such file or directory
```

A feladat leírása szerint INFRA-095 kellett volna, hogy előkészítse. Úgy tűnik ez még nem futott le, vagy más helyre telepítette.

## Szükséges lépés root részéről

1. INFRA-095 státusz ellenőrzése — létrehozta-e a könyvtárat?
2. Ha nem: INFRA terminálnak ki kell adni a könyvtár + solution skeleton létrehozását
3. Ha igen: helyes elérési út megadása (lehet, hogy más névvel jött létre)

## Ellenőrzött helyek

```bash
ls /opt/spaceos/ | grep -i cutting
# → nincs találat
```

## Várt állapot (ha előkészítve lenne)

```
/opt/spaceos/spaceos-modules-cutting/
├── SpaceOS.Modules.Cutting.sln
└── src/  (üres, contracts-ra vár)
```

Amint a könyvtár létezik, a Contracts implementáció azonnal folytatható.
