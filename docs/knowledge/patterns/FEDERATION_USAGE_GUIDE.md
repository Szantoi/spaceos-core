# Federation Használati Útmutató

> **Dátum:** 2026-07-11
> **Státusz:** LIVE — működik mindkét irányban

---

## Gyors Összefoglaló

| Irány | `to:` mező | Inbox lokáció |
|-------|-----------|---------------|
| **VPS → Cabinet** | `cabinet-bridge` | `/opt/nexus/terminals/cabinet-bridge/inbox/` |
| **Cabinet → VPS** | `spaceos` vagy `doorstar` | `/opt/nexus/terminals/spaceos/inbox/` |

---

## VPS → Cabinet Üzenetküldés

### 1. Fájl létrehozása

```bash
# Fájlnév formátum: YYYY-MM-DD_NNN_slug.md
/opt/nexus/terminals/cabinet-bridge/inbox/2026-07-11_001_my-message.md
```

### 2. Frontmatter (KÖTELEZŐ)

```yaml
---
id: MSG-CABINET-BRIDGE-NNN
from: spaceos
to: cabinet-bridge
type: info          # info | question | blocked (NEM task!)
priority: medium    # low | medium | high
status: UNREAD
created: 2026-07-11
ref: MSG-XXX-NNN    # opcionális - hivatkozott üzenet
subject: "Rövid leírás"
---
```

### 3. Tartalom (Markdown)

```markdown
# Üzenet Címe

## Összefoglaló
...

## Részletek
...

---
_VPS SpaceOS Root — 2026-07-11_
```

### 4. Érvényes üzenet típusok Cabinet felé

| Típus | Mikor használd |
|-------|----------------|
| `info` | Tájékoztató, státusz frissítés |
| `question` | Kérdés, válasz szükséges |
| `blocked` | Blokkoló probléma jelzése |

**TILOS:** `task` típus — a Cabinet bridge nem agent, nem tud feladatot végrehajtani!

---

## Cabinet → VPS Üzenetküldés

A Cabinet az MCP `send_message` tool-t használja:

```
send_message(
  to: "spaceos",      # vagy "doorstar"
  type: "info",
  priority: "medium",
  content: "..."
)
```

Az üzenet ide kerül: `/opt/nexus/terminals/spaceos/inbox/`

---

## FONTOS: Két Gyökérkönyvtár

A VPS-en **két különböző** terminál struktúra van:

| Könyvtár | Ki használja | MCP figyeli? |
|----------|--------------|--------------|
| `/opt/nexus/terminals/` | MCP szerver | ✅ IGEN |
| `/opt/spaceos/terminals/` | SpaceOS root (Write tool) | ❌ NEM |

### Szabály

**Mindig a `/opt/nexus/terminals/` alá írj!**

Ha véletlenül `/opt/spaceos/` alá írtál, másold át:
```bash
cp /opt/spaceos/terminals/cabinet-bridge/inbox/*.md \
   /opt/nexus/terminals/cabinet-bridge/inbox/
```

---

## Üzenet Életciklus

```
1. UNREAD  — Új üzenet, még nem feldolgozott
2. READ    — Címzett olvasta
3. ACK     — Címzett visszaigazolta (content_hash hozzáadva)
4. SENT    — Router továbbította (outbox-ból)
```

### Cabinet ACK jelzés

Amikor a Cabinet feldolgoz egy üzenetet, hozzáadja:
```yaml
status: ACK
acknowledged: 2026-07-11
content_hash: sha256...
```

---

## Federation Router (Automatikus)

A `/opt/spaceos/scripts/federation-router.sh` 5 percenként fut (cron):
- Olvassa az outbox mappákat
- `to:` mező alapján célba másolja
- `status: UNREAD` → `status: SENT`

### Manuális futtatás

```bash
# Routing
/opt/spaceos/scripts/federation-router.sh route

# Státusz
/opt/spaceos/scripts/federation-router.sh status
```

---

## Hibakeresés

### 1. Üzenet nem érkezik meg Cabinet-nek

Ellenőrizd:
```bash
# Jó helyre írtad?
ls /opt/nexus/terminals/cabinet-bridge/inbox/

# Státusz UNREAD?
grep "status:" /opt/nexus/terminals/cabinet-bridge/inbox/*.md
```

### 2. Cabinet üzenete nem érkezik VPS-re

Ellenőrizd:
```bash
# Cabinet melyik terminálra küldte?
ls /opt/nexus/terminals/spaceos/inbox/
ls /opt/nexus/terminals/doorstar/inbox/

# Vagy rossz terminálra ment?
ls /opt/nexus/terminals/sárkány/inbox/  # legacy alias
```

### 3. "to: cabinet" nem működik

Ez rossz! A helyes: `to: cabinet-bridge`

---

## Példa Üzenet

```markdown
---
id: MSG-CABINET-BRIDGE-004
from: spaceos
to: cabinet-bridge
type: info
priority: medium
status: UNREAD
created: 2026-07-11
subject: "Production API Draft - Review kérés"
---

# Production API Draft

## Összefoglaló

A BomLine sémátok alapján elkészült a Production API draft.

## Kérdések

1. A `surface` mező értékkészlete végleges?
2. A `grain` mezőnél kell "Nincs" opció?

## Következő lépés

Kérlek review-záljátok és küldjetek visszajelzést.

---
_VPS SpaceOS Root — 2026-07-11_
```

---

## Kapcsolódó Dokumentáció

- `islands.yaml` — `/opt/spaceos/config/islands.yaml`
- Federation Router — `/opt/spaceos/scripts/federation-router.sh`
- Health Monitor — `/opt/spaceos/scripts/island-health-monitor.sh`

---

_Utolsó frissítés: 2026-07-11 — Federation működik mindkét irányban_
