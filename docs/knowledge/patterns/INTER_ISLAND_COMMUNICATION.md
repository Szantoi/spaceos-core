# Szigetek Közötti Kommunikáció (Inter-Island Protocol)

> **Dátum:** 2026-07-11
> **Státusz:** LIVE
> **Minden szigetnek így kell kommunikálnia egymással!**

---

## 📞 Telefonhívás Analógia

**A federation kommunikáció olyan mint egy telefonhívás:**

1. **Tudnod kell KIT akarsz megszólítani** (melyik sziget?)
2. **Tudnod kell a "telefonszámát"** (a `to:` mező értéke)
3. **Ha rossz számot hívsz, senki nem veszi fel**

### Telefonkönyv

```
┌─────────────────────────────────────────────────────────────────┐
│                        TELEFONKÖNYV                              │
├──────────────────┬────────────────┬─────────────────────────────┤
│  Ki?             │  "Szám" (to:)  │  Mikor hívd?                │
├──────────────────┼────────────────┼─────────────────────────────┤
│  SpaceOS         │  spaceos       │  Koordináció, stratégia     │
│  JoineryTech     │  joinerytech   │  Platform fejlesztés        │
│  Doorstar        │  doorstar      │  Ügyfél-specifikus munka    │
│  Nexus           │  nexus         │  Infrastruktúra             │
│  Cabinet (külső) │  cabinet-bridge│  Külső partner kommunikáció │
└──────────────────┴────────────────┴─────────────────────────────┘
```

### Mi történik ha rossz "számot" hívsz?

```
❌ to: cabinet          →  Nem létező terminál, üzenet elvész
✅ to: cabinet-bridge   →  Működik, Cabinet megkapja

❌ to: root             →  Rossz terminál, nem a koordinátor
✅ to: spaceos          →  Működik, SpaceOS koordinátor megkapja

❌ to: backend          →  Ez terminál, nem sziget!
✅ to: joinerytech      →  Működik, JoineryTech sziget megkapja
```

### Szabály

> **Mindig a SZIGET nevét használd, nem a terminál nevét!**
>
> Szigetek: `nexus`, `joinerytech`, `doorstar`, `spaceos`, `cabinet-bridge`
>
> Terminálok (NE HASZNÁLD címzésre): `backend`, `frontend`, `conductor`, `root`

---

## Architektúra Áttekintés

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   NEXUS     │     │ JOINERYTECH │     │  DOORSTAR   │     │   SPACEOS   │
│  (infra)    │◄───►│ (platform)  │◄───►│ (customer)  │◄───►│   (orch)    │
│  port 3456  │     │  port 3458  │     │  port 3460  │     │  port 3462  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │                   │
       └───────────────────┴───────────────────┴───────────────────┘
                                    │
                           Federation Protocol
                                    │
                           ┌─────────────────┐
                           │    CABINET      │
                           │ (external/local)│
                           │  via bridge     │
                           └─────────────────┘
```

---

## Sziget Címzési Táblázat

| Sziget | `to:` érték | Inbox Lokáció | Port |
|--------|------------|---------------|------|
| Nexus | `nexus` | `/opt/nexus/terminals/nexus/inbox/` | 3456 |
| JoineryTech | `joinerytech` | `/opt/joinerytech/terminals/joinerytech/inbox/` | 3458 |
| Doorstar | `doorstar` | `/opt/doorstar/terminals/doorstar/inbox/` | 3460 |
| SpaceOS | `spaceos` | `/opt/spaceos/terminals/spaceos/inbox/` | 3462 |
| Cabinet (ext) | `cabinet-bridge` | `/opt/nexus/terminals/cabinet-bridge/inbox/` | - |

---

## Üzenet Formátum (Egységes)

### Frontmatter

```yaml
---
id: MSG-<SZIGET>-NNN
from: <küldő-sziget>
to: <cél-sziget>
type: info | task | question | blocked | done
priority: low | medium | high | critical
status: UNREAD
created: YYYY-MM-DD
ref: MSG-XXX-NNN       # opcionális hivatkozás
subject: "Rövid leírás"
content_hash: sha256...  # automatikusan generálódik
---
```

### Fájlnév

```
YYYY-MM-DD_NNN_slug.md
```

Példa: `2026-07-11_001_production-api-draft.md`

---

## Sziget → Sziget Kommunikáció

### Példa: SpaceOS → JoineryTech

```yaml
---
id: MSG-JOINERYTECH-042
from: spaceos
to: joinerytech
type: task
priority: high
status: UNREAD
created: 2026-07-11
subject: "Production modul OpenAPI draft"
---

# Production Modul OpenAPI

Kérlek készítsd el a Production modul OpenAPI draft-ját.

## Követelmények
- BomLine séma integrálása
- CutPiece endpoint

---
_SpaceOS Root — 2026-07-11_
```

### Példa: Doorstar → SpaceOS

```yaml
---
id: MSG-SPACEOS-015
from: doorstar
to: spaceos
type: done
priority: medium
status: UNREAD
created: 2026-07-11
ref: MSG-DOORSTAR-042
subject: "6-stage FSM implementáció DONE"
---

# 6-Stage FSM — DONE

A gyártási workflow 6 stage-es FSM implementálva.

## Tesztek
- Unit: 45/45 ✅
- Integration: 12/12 ✅

---
_Doorstar Backend — 2026-07-11_
```

---

## Routing Szabályok (islands.yaml)

```yaml
federation:
  routing_rules:
    # SpaceOS mindenkinek küldhet (orchestrator)
    - from: spaceos
      to: ["*"]
      types: ["*"]

    # Nexus csak SpaceOS-nak és JoineryTech-nek
    - from: nexus
      to: [spaceos, joinerytech]
      types: [info, release, bug_report]

    # JoineryTech Doorstar-nak és SpaceOS-nak
    - from: joinerytech
      to: [spaceos, doorstar]
      types: [info, release, config_update]

    # Doorstar csak SpaceOS-nak és Cabinet-nek
    - from: doorstar
      to: [spaceos, cabinet]
      types: [production_data, query]

    # Cabinet csak Doorstar-nak és SpaceOS-nak
    - from: cabinet
      to: [doorstar, spaceos]
      types: [production_data, query]
```

---

## Üzenet Típusok

| Típus | Mikor használd | Példa |
|-------|---------------|-------|
| `info` | Tájékoztató | Verzió frissítés, státusz |
| `task` | Feladat kiadás | "Implementáld az API-t" |
| `question` | Kérdés | "Melyik formátumot válasszuk?" |
| `blocked` | Blokkolva | "NuGet csomag hiányzik" |
| `done` | Feladat kész | "Implementáció DONE" |
| `release` | Új verzió | "v2.1.0 released" |
| `production_data` | Gyártási adat | CuttingCompleted event |

---

## Életciklus

```
UNREAD → READ → (feldolgozás) → ACK/DONE/BLOCKED → ARCHIVE
```

### Státusz Értékek

| Státusz | Jelentés |
|---------|----------|
| `UNREAD` | Új, még nem olvasott |
| `READ` | Olvasott, feldolgozás alatt |
| `ACK` | Visszaigazolt |
| `SENT` | Router továbbította |
| `DONE` | Befejezett |
| `BLOCKED` | Blokkolva |

---

## Federation Router

### Automatikus Routing (cron */5)

```bash
/opt/spaceos/scripts/federation-router.sh route
```

A router három lépésben működik:

1. **Message Routing:** Végignézi minden sziget `outbox/` mappáját, `to:` mező alapján a célba másolja, `status: UNREAD` → `status: SENT`
2. **Cabinet Routing:** Külön kezeli a Cabinet üzeneteket
3. **Auto-Acknowledgement:** Automatikusan frissíti a forrás outbox státuszt amikor válasz érkezik

### Auto-Acknowledgement (Notification Loop Fix)

**Probléma:** A régi rendszer UNREAD → SENT frissítést csinált, de amikor a cél válaszolt (ref: mező), a forrás outbox SENT státuszban maradt, végtelen notification loop-ot okozva.

**Megoldás:** A router most automatikusan frissíti a forrás outbox státuszt SENT → ACK amikor:
- A cél sziget válaszol
- A válasz üzenet tartalmaz `ref:` mezőt
- A hivatkozott üzenet státusza SENT

**Példa:**

```yaml
# SpaceOS outbox (előtte)
status: SENT
id: MSG-FEDERATION-003

# Nexus inbox response
ref: MSG-FEDERATION-003  ← Router észleli

# SpaceOS outbox (utána)
status: ACK  ← Automatikusan frissült
id: MSG-FEDERATION-003
```

**Unit test:**

```bash
/opt/spaceos/scripts/test-federation-ack.sh
```

### Manuális Parancsok

```bash
# Routing státusz (részletes)
/opt/spaceos/scripts/federation-router.sh status

# Manuális routing (route + ack)
/opt/spaceos/scripts/federation-router.sh route

# Csak acknowledgement futtatás
/opt/spaceos/scripts/federation-router.sh ack
```

**Státusz kimenet:**
```
=== Federation Outbox Status ===
  spaceos: 5 total (2 UNREAD, 1 SENT, 2 ACK)
  nexus: 3 total (1 UNREAD, 0 SENT, 2 ACK)
```

### Notification Deduplication

A router automatikusan deduplikál notification-öket `content_hash` alapján, hogy megakadályozza az azonos üzenet többszöri értesítését.

**State fájl:** `/opt/spaceos/logs/federation-notifications.state`

**Formátum:**
```
MSG-ID|timestamp|content_hash
MSG-FEDERATION-003|2026-07-11T14:03:18+00:00|a66ad22a6b203f134d4493bad764f6158399f3a1b4c62dd55d1f15dbac404f78
```

**Használat kódban:**
```bash
# Check if already sent
if check_notification_sent "$msg_id" "$content_hash"; then
    echo "Already notified, skipping"
    exit 0
fi

# Mark as sent after notification
mark_notification_sent "$msg_id" "$content_hash"
```

---

## MCP Integráció

Szigetek közötti kommunikáció MCP-n keresztül is lehetséges:

```
# Üzenet küldés
send_message(
  from: "spaceos",
  to: "joinerytech",
  type: "task",
  priority: "high",
  content: "..."
)

# Inbox olvasás
list_inbox(terminal: "joinerytech")

# Üzenet olvasás
read_message(terminal: "joinerytech", message_id: "MSG-JOINERYTECH-042")
```

---

## Külső Partnerek (Bridge Pattern)

A külső partnerek (pl. Cabinet) `bridge` terminálként csatlakoznak:

```
┌─────────────────┐
│ Cabinet (local) │
│  poll-based     │
└────────┬────────┘
         │ SFTP/SSH poll (20 perc)
         ▼
┌─────────────────┐
│ cabinet-bridge  │  ← VPS oldali végpont
│ /opt/nexus/...  │
└─────────────────┘
```

### Bridge Szabályok

1. Bridge terminálnak **NEM küldhetsz `task` típusú üzenetet**
2. Bridge csak `info`, `question`, `blocked` típust fogad
3. Bridge automatikusan ACK-ol (`content_hash` hozzáadás)

---

## Hibakeresés

### 1. Üzenet nem érkezik meg

```bash
# Ellenőrizd a cél inbox-ot
ls /opt/<sziget>/terminals/<terminál>/inbox/

# Ellenőrizd a routing logot
tail -20 /opt/spaceos/logs/federation-router.log
```

### 2. Rossz szigetre ment

```bash
# Keresd meg az összes inbox-ban
grep -rl "MSG-XXX-NNN" /opt/*/terminals/*/inbox/
```

### 3. Health check

```bash
/opt/spaceos/scripts/island-health-monitor.sh status
```

---

## Összefoglaló

1. **Minden sziget ugyanazt a formátumot használja** (frontmatter + markdown)
2. **`to:` mező = cél sziget neve** (nexus, joinerytech, doorstar, spaceos)
3. **Külső partnerek bridge-en keresztül** (cabinet-bridge)
4. **Federation router automatikusan routing-ol** (cron */5)
5. **Auto-acknowledgement** — forrás outbox automatikusan ACK amikor válasz érkezik (notification loop fix)
6. **Notification deduplication** — content_hash alapú idempotency
7. **MCP API is használható** (send_message, list_inbox)

---

_Utolsó frissítés: 2026-07-11 — 4-sziget + Cabinet federation működik + Notification loop fix (MSG-NEXUS-001)_
