#!/bin/bash
# Island Setup Script
# Használat: ./setup-island.sh <island_name> <role> <port> <host_type> [description]

set -e

ISLAND="$1"
ROLE="$2"
PORT="$3"
HOST_TYPE="${4:-vps}"  # vps | local | cloud
DESCRIPTION="${5:-$ROLE island}"

if [ -z "$ISLAND" ] || [ -z "$ROLE" ] || [ -z "$PORT" ]; then
    echo "Használat: $0 <island_name> <role> <port> [host_type] [description]"
    echo "Példa: $0 myisland platform 3464 vps 'My Platform Island'"
    echo ""
    echo "Host típusok:"
    echo "  vps   - VPS szerver (belső MCP token)"
    echo "  local - Lokális gép (federation bridge)"
    echo "  cloud - Cloud provider (API key + OAuth)"
    exit 1
fi

ISLAND_LOWER=$(echo "$ISLAND" | tr '[:upper:]' '[:lower:]')
ISLAND_TITLE=$(echo "$ISLAND" | sed 's/\b\(.\)/\u\1/g')
BASE_PATH="/opt/$ISLAND_LOWER"

echo "=== Island Setup: $ISLAND_TITLE ==="
echo "Mappa: $BASE_PATH"
echo "Port: $PORT"
echo "Szerep: $ROLE"
echo "Host: $HOST_TYPE"
echo ""

# 1. Mappa struktúra
echo "[1/8] Mappa struktúra létrehozása..."
mkdir -p "$BASE_PATH"/{config,docs/knowledge,logs,scripts,src,terminals}

# 2. Terminálok
echo "[2/8] Terminálok létrehozása..."
for t in root conductor backend frontend designer architect librarian explorer; do
    mkdir -p "$BASE_PATH/terminals/$t"/{inbox,outbox,archive}
done
mkdir -p "$BASE_PATH/terminals/federation"/{inbox,outbox}

# 3. Token generálás
echo "[3/8] MCP token generálás..."
openssl rand -base64 32 > "$BASE_PATH/config/.mcp-token"
chmod 600 "$BASE_PATH/config/.mcp-token"
echo "    Token: $BASE_PATH/config/.mcp-token"

# 4. Hosting konfiguráció
echo "[4/8] Hosting konfiguráció..."
cat > "$BASE_PATH/config/hosting.yaml" << EOF
# Island Hosting Configuration
# Generated: $(date -Iseconds)

island:
  id: "$ISLAND_LOWER"
  name: "$ISLAND_TITLE"
  role: "$ROLE"
  host_type: "$HOST_TYPE"           # vps | local | cloud
  host_address: "127.0.0.1"         # Belső: localhost, Külső: IP/domain
  port: $PORT
  public_access: false              # true = nginx proxy szükséges

security:
  mcp_token_file: ".mcp-token"
  federation_only: false            # true = csak federation, nincs MCP
  allowed_origins:
    - "https://datahaven.joinerytech.hu"
  rate_limit:
    requests_per_minute: 100
  sha256_verification: true         # Federation üzenetek hash ellenőrzés

federation:
  enabled: true
  bridge_polling_sec: 30            # Külső partnerek polling intervallum
  message_retention_days: 30
EOF

# 5. CLAUDE.md
echo "[5/8] CLAUDE.md létrehozása..."
cat > "$BASE_PATH/CLAUDE.md" << EOF
# CLAUDE.md — $ISLAND_TITLE Sziget ($ROLE)

> $DESCRIPTION

---

## SZIGET IDENTITY

**Név:** $ISLAND_TITLE
**Szerep:** $ROLE
**Port range:** $PORT-$((PORT+1))
**Host:** $HOST_TYPE
**tmux prefix:** $ISLAND_LOWER-

---

## HOSTING ÉS BIZTONSÁG

**Host típus:** $HOST_TYPE
**Token:** \`config/.mcp-token\`
**Konfiguráció:** \`config/hosting.yaml\`

| Tulajdonság | Érték |
|-------------|-------|
| MCP Token | Sziget-specifikus (NEM megosztott!) |
| Federation | SHA-256 hash ellenőrzés |
| Külső elérés | Nginx reverse proxy + HTTPS |

---

## TERMINÁLOK

| Terminál | Szerep |
|----------|--------|
| **root** | Stratégia, prioritizálás |
| **conductor** | Feladatkiosztás, koordináció |
| **backend** | Backend fejlesztés |
| **frontend** | Frontend fejlesztés |
| **designer** | UI/UX |

---

## KAPCSOLAT MÁS SZIGETEKKEL

**Federation inbox:** \`terminals/federation/inbox/\`
**Federation outbox:** \`terminals/federation/outbox/\`

---

## SERVICES

| Service | Port | Leírás |
|---------|------|--------|
| Knowledge Service | $PORT | MCP API |

---

_$ISLAND_TITLE Sziget — $ROLE — Host: $HOST_TYPE_
EOF

# 6. Knowledge-service symlink
echo "[6/8] Knowledge-service beállítása..."
mkdir -p "$BASE_PATH/src/${ISLAND_LOWER}-nexus"
if [ -d "/opt/nexus/src/nexus-core/knowledge-service" ]; then
    ln -sf /opt/nexus/src/nexus-core/knowledge-service "$BASE_PATH/src/${ISLAND_LOWER}-nexus/knowledge-service"
    echo "    Symlink létrehozva"
else
    echo "    FIGYELEM: /opt/nexus/src/nexus-core/knowledge-service nem található"
fi

# 7. Permissions
echo "[7/8] Jogosultságok beállítása..."
chmod -R 770 "$BASE_PATH"
chmod 600 "$BASE_PATH/CLAUDE.md"
chmod 600 "$BASE_PATH/config/.mcp-token"
chmod 640 "$BASE_PATH/config/hosting.yaml"

# 8. Összefoglaló
echo "[8/8] Kész!"
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  $ISLAND_TITLE Sziget létrehozva"
echo "╠══════════════════════════════════════════════════════════════╣"
echo "║  Mappa:     $BASE_PATH"
echo "║  Port:      $PORT"
echo "║  Host:      $HOST_TYPE"
echo "║  Token:     $BASE_PATH/config/.mcp-token"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo "Struktúra:"
ls -la "$BASE_PATH"
echo ""
echo "Knowledge-service indítás:"
echo "  cd $BASE_PATH/src/${ISLAND_LOWER}-nexus/knowledge-service"
echo "  MCP_AUTH_TOKEN=\$(cat $BASE_PATH/config/.mcp-token) \\"
echo "  TERMINALS_PATH=$BASE_PATH/terminals \\"
echo "  KNOWLEDGE_BASE_PATH=$BASE_PATH/docs/knowledge \\"
echo "  PORT=$PORT \\"
echo "  ISLAND_ID=$ISLAND_LOWER \\"
echo "  node dist/server.js"
echo ""
echo "Health check:"
echo "  curl -s http://localhost:$PORT/health"
echo ""
echo "FONTOS: Ne felejtsd el frissíteni a /opt/spaceos/config/federation.yaml fájlt!"
