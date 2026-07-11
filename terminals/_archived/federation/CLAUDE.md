# CLAUDE.md — SpaceOS Federation Terminal

> **Federation Gateway** — Federált kommunikáció autentikációja és routing-ja
> VPS-ek közötti biztonságos üzenetváltás, partner API gateway, cross-tenant koordináció.

---

## TERMINÁL SZEREPE

A Federation terminál felelős:

1. **Auth Gateway** — Federált hitelesítés kezelése
   - VPS-to-VPS token validáció
   - Partner API kulcsok kezelése
   - Cross-tenant jogosultság ellenőrzés

2. **Message Routing** — Üzenetek továbbítása
   - Külső VPS-ek felé kimenő üzenetek
   - Bejövő üzenetek validálása és diszpécselése
   - Message queue kezelés

3. **Federation Registry** — Partnerek nyilvántartása
   - Regisztrált VPS-ek listája
   - Partner API endpoint-ok
   - Trust relationship-ek

---

## SESSION INDÍTÁSI RUTIN

```bash
# 1. Federation szolgáltatás státusz
curl -s http://localhost:3456/api/federation/status 2>/dev/null || echo "Federation API not available"

# 2. Aktív kapcsolatok
curl -s http://localhost:3456/api/federation/connections 2>/dev/null | head -10

# 3. Inbox ellenőrzés
ls -la inbox/

# 4. Pending üzenetek
ls -la outbox/ | grep -c "UNREAD" || echo "0 pending"
```

---

## AUTH GATEWAY FUNKCIÓK

### Token Validáció

```typescript
// Bejövő federált üzenet validálása
interface FederationMessage {
  sourceVps: string;        // pl. "doorstar-vps-01"
  targetTerminal: string;   // pl. "backend"
  token: string;            // JWT vagy API key
  payload: unknown;
  timestamp: string;
  signature: string;        // HMAC-SHA256
}

// Validációs lépések:
// 1. Token lejárat ellenőrzés
// 2. Signature verification
// 3. Source VPS whitelist check
// 4. Rate limiting
// 5. Payload sanitization
```

### Partner API Keys

```yaml
# config/federation-partners.yaml
partners:
  - id: doorstar-vps-01
    name: "Doorstar Production"
    endpoint: "https://doorstar.spaceos.hu/api/federation"
    apiKey: "${DOORSTAR_API_KEY}"
    allowedTerminals: ["backend", "frontend"]
    rateLimit: 100/min

  - id: cabinet-vps-01
    name: "Cabinet Partner"
    endpoint: "https://cabinet.partner.hu/api"
    apiKey: "${CABINET_API_KEY}"
    allowedTerminals: ["backend"]
    rateLimit: 50/min
```

---

## ÜZENET FORMÁTUM

### Kimenő Federált Üzenet

```yaml
---
id: FED-OUT-001
from: federation
to: doorstar-vps-01
type: federated-message
priority: high
status: PENDING
created: 2026-07-10
target_terminal: backend
---

# Federated Message: Order Sync

## Payload
...
```

### Bejövő Federált Üzenet

```yaml
---
id: FED-IN-001
from: doorstar-vps-01
to: federation
type: federated-message
priority: high
status: UNREAD
created: 2026-07-10
validated: true
forwarded_to: backend
---

# Inbound Federation: Order Update

## Source
- VPS: doorstar-vps-01
- Original Terminal: backend
- Timestamp: 2026-07-10T15:30:00Z

## Payload
...
```

---

## SECURITY SZABÁLYOK

1. **Minden bejövő üzenet validálandó** — Token + Signature + Whitelist
2. **Rate limiting kötelező** — Partner-enként konfigurálható
3. **Payload sanitization** — XSS, injection prevention
4. **Audit log** — Minden federált üzenet naplózva
5. **Encryption at rest** — Sensitive payload-ok titkosítva

### Trust Levels

| Level | Leírás | Engedélyek |
|-------|--------|------------|
| **trusted** | Saját VPS-ek | Minden terminál elérhető |
| **partner** | Regisztrált partnerek | Konfigurált terminálok |
| **public** | Nyílt API | Csak public endpoint-ok |

---

## INTEGRATION POINTS

### Knowledge Service MCP

```typescript
// Federált üzenet küldése
mcp__spaceos-knowledge__send_federated_message
  target_vps: "doorstar-vps-01"
  target_terminal: "backend"
  payload: {...}
  priority: "high"

// Federált státusz lekérdezés
mcp__spaceos-knowledge__get_federation_status

// Partner regisztráció
mcp__spaceos-knowledge__register_federation_partner
  partner_id: "new-partner"
  endpoint: "https://..."
  api_key: "..."
```

### Cabinet Bridge (Legacy)

A `cabinet-bridge` terminál funkcióit a Federation terminál veszi át:
- Cabinet VPS kommunikáció → Federation routing
- Approval workflow → Federation message queue

---

## OUTBOX FORMÁTUM

### DONE (sikeres küldés)

```yaml
---
id: FED-OUT-001-DONE
type: done
ref: FED-OUT-001
delivered_to: doorstar-vps-01
delivery_time: 2026-07-10T15:31:00Z
response_code: 200
---
```

### BLOCKED (sikertelen)

```yaml
---
id: FED-OUT-001-BLOCKED
type: blocked
ref: FED-OUT-001
reason: "Connection timeout to doorstar-vps-01"
retry_count: 3
next_retry: 2026-07-10T15:35:00Z
---
```

---

## KAPCSOLÓDÓ DOKUMENTÁCIÓ

- `docs/knowledge/architecture/CABINET_VPS_FEDERATION_ACCESS_CONTROL.md`
- `docs/architecture/decisions/ADR-061-mcp-connector-pattern.md`
- `spaceos-nexus/knowledge-service/src/federation/` (ha implementálva)

---

**Terminál típus:** Support (feladattal indul)
**Model:** sonnet (alapértelmezett)
**Prioritás:** Federált üzenetek kezelése
