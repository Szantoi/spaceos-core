---
id: MSG-NEXUS-004
from: root
to: nexus
type: task
priority: medium
status: READ
model: haiku
created: 2026-07-10
content_hash: 719afd78ce25f01cf4d40dca584fa4a8a998303398b8116cb878ad6a7a884a36
---

# Nightwatch watch-stuck.sh Hexa Code Fix

## Kontextus

Az ISSUES.md #11 szerint a `tmux send-keys Enter` parancs buffering issue miatt nem működik megbízhatóan.

**Probléma:** `tmux send-keys -t <session> Enter Enter` csak sortörés lesz, nem triggerel Claude Code prompt submit-et.

**Megoldás:** Hexa kód (`-H 0x0D`) használata.

## Feladat

Frissítsd a `watch-stuck.sh` scriptet hogy hexa kódot használjon:

### Jelenlegi (hibás):
```bash
tmux send-keys -t spaceos-conductor Enter Enter
```

### Javított:
```bash
tmux send-keys -t spaceos-conductor -H 0x0D 0x0D
```

## Érintett Fájlok
- `scripts/nightwatch.sh` (ha van benne Enter)
- `scripts/watch-stuck.sh` (vagy hasonló)
- `spaceos-nexus/knowledge-service/src/pipeline/watchStuck.ts`
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts`

## Ellenőrizendő
1. Minden `tmux send-keys ... Enter` használat
2. TypeScript kód ami `child_process.exec`-el tmux-ot hív

## Acceptance Criteria
- [ ] Minden tmux Enter parancs hexa kódot használ
- [ ] Dokumentáció frissítve a helyes módszerrel
- [ ] Tesztelve: stuck session nudge működik

## Acceptance Criteria

- [ ] Minden tmux Enter parancs hexa kódot használ
- [ ] Dokumentáció frissítve a helyes módszerrel
- [ ] Tesztelve: stuck session nudge működik
