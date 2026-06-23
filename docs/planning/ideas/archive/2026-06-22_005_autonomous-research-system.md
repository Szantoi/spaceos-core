---
id: IDEA-005
title: Autonóm Kutatási Rendszer (Explorer + Librarian)
priority: high
status: idea
domain: agent-infrastructure
author: root
created: 2026-06-22
tags: [explorer, librarian, autonomous, research, knowledge-synthesis]
---

# Autonóm Kutatási Rendszer

## Összefoglaló

Az Explorer és Librarian terminálok jelenleg **igény alapon** dolgoznak — más terminálok
kérésére kutatnak és szintetizálnak. Ez az ötlet egy **autonóm üzemmódot** javasol,
ahol a kutatás és szintetizálás folyamatosan, háttérben zajlik.

## Probléma

1. **Reaktív működés** — Explorer/Librarian csak akkor dolgozik, ha kérés érkezik
2. **Tudás elavulás** — a knowledge base nem frissül automatikusan
3. **Kihagyott tanulási lehetőségek** — napi DONE outboxok feldolgozatlanul maradnak
4. **Külső forrás hiány** — nem követjük aktívan a tech trendeket

## Javasolt Megoldás

### 1. Időzített Kutatási Ciklus

```bash
# watch-research.sh — cron */60 (óránként)
# Elindítja az Explorer terminált kutatási feladattal
```

**Explorer napi rutin:**
- Chat history bányászat (legutóbbi 24 óra)
- Kódbázis változások elemzése (git log)
- DONE outboxok mintakeresése
- Web kutatás aktuális tech témákban

### 2. Librarian Szintetizálási Ciklus

```bash
# watch-synthesis.sh — cron */120 (2 óránként)
# Elindítja a Librarian terminált szintetizálással
```

**Librarian napi rutin:**
- Explorer outbox feldolgozása
- Knowledge doc frissítése
- Olvasólista generálás termináloknak
- Memória tier promóció

### 3. Kutatási Témák Automatikus Azonosítása

**Trigger-ek:**
- Új BLOCKED outbox → kutatás a blocker témájában
- Új modul/feature kész → best practices összegyűjtése
- Heti ciklus → iparági trend scan

### 4. Output Automatikus Routing

```
Explorer DONE → watch-synthesis.sh → Librarian inbox
Librarian DONE → docs/knowledge/ frissítés
             → Terminál specifikus olvasólista
             → Datahaven notification
```

## Implementációs Fázisok

### Phase 1: watch-research.sh (MVP)
- Explorer indítás cron-nal
- Egyszerű kutatási prompt
- DONE outbox írás

### Phase 2: Explorer ↔ Librarian lánc
- Librarian automatikus indítás Explorer DONE után
- Knowledge doc frissítés

### Phase 3: Smart Triggers
- BLOCKED alapú kutatás indítás
- Terminál-specifikus olvasólista generálás

### Phase 4: Feedback Loop
- Terminálok visszajelzése ("hasznos volt"/"nem releváns")
- Kutatási prioritások adaptálása

## Elfogadási Kritériumok

- [ ] `watch-research.sh` cron */60 fut
- [ ] Explorer automatikusan kutat naponta 2-3x
- [ ] Librarian feldolgozza az Explorer eredményeket
- [ ] Knowledge base hetente frissül új mintákkal
- [ ] Terminálok olvasólistát kapnak releváns témákról

## Kockázatok

| Kockázat | Mitigáció |
|---|---|
| Túl sok kutatás, zajos output | Prioritási rendszer, salience scoring |
| Token költség növekedés | haiku model használata kutatásra |
| Elavult külső források | 2024+ szűrő, megbízható források lista |

## Következő Lépések

1. [x] Ötlet dokumentálás
2. [x] watch-research.sh MVP implementáció — `/opt/spaceos/scripts/watch-research.sh`
3. [x] Tesztelés manuális triggerelésssel — működik, inbox létrejön
4. [ ] Cron beállítás — manuális aktiválás szükséges
5. [ ] Monitoring és finomhangolás

## Implementáció Részletei

### watch-research.sh

**Lokáció:** `/opt/spaceos/scripts/watch-research.sh`

**Működés:**
1. Ellenőrzi, hogy az Explorer IDLE-e (nincs UNREAD inbox)
2. Rotálja a kutatási témákat (4 téma körforgásban)
3. Létrehoz egy inbox üzenetet az Explorer-nek
4. Elmenti az állapotot (utolsó téma index)
5. Ellenőrzi az Explorer DONE-jait és továbbítja Librarian-nak

**Kutatási témák (rotáció):**
1. `chat_history_mining` — Chat history bányászat
2. `codebase_changes` — Kódbázis változások elemzése
3. `done_patterns` — DONE outbox minták keresése
4. `external_trends` — Külső tech trendek

**Log fájl:** `/opt/spaceos/logs/dispatcher/research.log`
**Állapot fájl:** `/opt/spaceos/scripts/.research-state`

### Cron beállítás (opcionális)

```bash
# Óránként futtatás
0 * * * * /opt/spaceos/scripts/watch-research.sh >> /opt/spaceos/logs/dispatcher/research-cron.log 2>&1

# Vagy 2 óránként
0 */2 * * * /opt/spaceos/scripts/watch-research.sh >> /opt/spaceos/logs/dispatcher/research-cron.log 2>&1
```

### Manuális futtatás

```bash
/opt/spaceos/scripts/watch-research.sh
```
