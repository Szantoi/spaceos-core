# SpaceOS Memory Format

> Terminálok hideg indítás memóriája.
> Minden terminálnak saját memory fájlja van.

## Fájl struktúra

```
docs/memory/
  fe.md           # FE terminál memória
  kernel.md       # Kernel terminál memória
  conductor.md    # Conductor memória
  ...
```

## Memory fájl formátum

```markdown
# <TERMINÁL> Memory

Utolsó frissítés: YYYY-MM-DD HH:MM

## Aktuális állapot
- Mit csináltam utoljára
- Hol tartok a feladatban
- Mi van DONE, mi van folyamatban

## Fontos kontextus
- Releváns döntések
- Függőségek más termináloktól
- Blokkolók ha vannak

## Következő lépések
- Mi a TODO ha folytatom
- Mire kell figyelni

## Tapasztalatok (session-ből)
- Mi működött jól
- Mi volt nehéz
- Csapdák amikbe belefutottam
```

## Használat

### Session végén (DONE vagy leállítás előtt)
A terminál frissíti a saját memory fájlját az aktuális állapottal.

### Session indításkor
A trigger üzenet tartalmazza: `Memory: docs/memory/<terminál>.md`
A terminál beolvassa és folytatja ahol abbahagyta.

## Szabályok

1. **Tömör** — max 50 sor, lényegre törő
2. **Aktuális** — mindig a legutóbbi session állapota
3. **Actionable** — következő lépések egyértelműek
4. **Nem duplikál** — nem másolja az inbox/outbox tartalmát
