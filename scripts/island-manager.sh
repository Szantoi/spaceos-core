#!/bin/bash
# Island Manager — Autonóm sziget kezelő
# Használat: ./island-manager.sh <command> [args]
#
# Commands:
#   status              - Összes sziget állapota
#   health              - Health check minden szigetre
#   validate            - Registry validáció
#   create <name> <role> <port> - Új sziget (requires_approval=true esetén kérdez)
#   restart <island>    - Sziget újraindítás
#   next-port           - Következő szabad port

set -e

REGISTRY="/opt/spaceos/config/islands.yaml"
LOG_FILE="/opt/spaceos/logs/island-manager.log"

log() {
    echo "[$(date -Iseconds)] $1" | tee -a "$LOG_FILE"
}

# Parse YAML (egyszerű verzió - yq nélkül)
get_islands() {
    grep -E "^  [a-z]+:$" "$REGISTRY" | sed 's/://g' | tr -d ' '
}

get_island_port() {
    local island=$1
    grep -A 10 "^  $island:" "$REGISTRY" | grep "knowledge:" | head -1 | awk '{print $2}'
}

get_island_path() {
    local island=$1
    grep -A 10 "^  $island:" "$REGISTRY" | grep "path:" | head -1 | awk '{print $2}'
}

# Commands
cmd_status() {
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║  Island Registry Status                                       ║"
    echo "╠══════════════════════════════════════════════════════════════╣"

    for island in $(get_islands); do
        port=$(get_island_port "$island")
        path=$(get_island_path "$island")

        # Health check
        if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
            status="✅ ONLINE"
        else
            status="❌ OFFLINE"
        fi

        printf "║  %-15s Port: %-5s %s\n" "$island" "$port" "$status"
    done

    echo "╚══════════════════════════════════════════════════════════════╝"
}

cmd_health() {
    log "Health check started"
    local failures=0

    for island in $(get_islands); do
        port=$(get_island_port "$island")

        response=$(curl -s -w "\n%{http_code}" "http://localhost:$port/health" 2>/dev/null || echo -e "\n000")
        http_code=$(echo "$response" | tail -1)
        body=$(echo "$response" | head -n -1)

        if [ "$http_code" = "200" ]; then
            log "  $island: OK (port $port)"
            # Extract island_id from response if available
            island_id=$(echo "$body" | grep -o '"island_id":"[^"]*"' | cut -d'"' -f4 2>/dev/null || echo "n/a")
            echo "  $island: ✅ OK (island_id=$island_id)"
        else
            log "  $island: FAILED (port $port, http=$http_code)"
            echo "  $island: ❌ FAILED (http=$http_code)"
            ((failures++))
        fi
    done

    if [ $failures -gt 0 ]; then
        log "Health check: $failures failures"
        return 1
    fi
    log "Health check: all OK"
    return 0
}

cmd_validate() {
    echo "Validating island registry..."

    local errors=0

    # Check required fields for each island
    for island in $(get_islands); do
        port=$(get_island_port "$island")
        path=$(get_island_path "$island")

        echo -n "  $island: "

        # Port check
        if [ -z "$port" ]; then
            echo "❌ missing port"
            ((errors++))
            continue
        fi

        # Path check
        if [ ! -d "$path" ]; then
            echo "❌ path not found: $path"
            ((errors++))
            continue
        fi

        # CLAUDE.md check
        if [ ! -f "$path/CLAUDE.md" ]; then
            echo "⚠️ missing CLAUDE.md"
        fi

        # Token check
        if [ ! -f "$path/config/.mcp-token" ]; then
            echo "⚠️ missing token file"
        fi

        # Port uniqueness (basic check)
        port_count=$(grep "knowledge: $port" "$REGISTRY" | wc -l)
        if [ "$port_count" -gt 1 ]; then
            echo "❌ duplicate port: $port"
            ((errors++))
            continue
        fi

        echo "✅ OK"
    done

    if [ $errors -gt 0 ]; then
        echo ""
        echo "Validation failed: $errors errors"
        return 1
    fi

    echo ""
    echo "Validation passed!"
    return 0
}

cmd_next_port() {
    # Find next available port
    local start=3456
    local end=3480

    used_ports=$(grep "knowledge:" "$REGISTRY" | awk '{print $2}' | sort -n)

    for port in $(seq $start $end); do
        if ! echo "$used_ports" | grep -q "^$port$"; then
            # Double check port is not in use
            if ! lsof -i ":$port" > /dev/null 2>&1; then
                echo "$port"
                return 0
            fi
        fi
    done

    echo "No available ports in range $start-$end" >&2
    return 1
}

cmd_restart() {
    local island=$1

    if [ -z "$island" ]; then
        echo "Usage: $0 restart <island>"
        return 1
    fi

    port=$(get_island_port "$island")
    path=$(get_island_path "$island")

    if [ -z "$port" ]; then
        echo "Island not found: $island"
        return 1
    fi

    log "Restarting $island..."

    # Kill existing process
    pid=$(lsof -ti ":$port" 2>/dev/null || true)
    if [ -n "$pid" ]; then
        log "  Killing PID $pid"
        kill "$pid" 2>/dev/null || true
        sleep 2
    fi

    # Start new process
    log "  Starting knowledge-service on port $port"
    cd "$path/src/${island}-nexus/knowledge-service"

    MCP_AUTH_TOKEN=$(cat "$path/config/.mcp-token" 2>/dev/null || echo "default-token") \
    TERMINALS_PATH="$path/terminals" \
    KNOWLEDGE_BASE_PATH="$path/docs/knowledge" \
    PORT="$port" \
    ISLAND_ID="$island" \
    node dist/server.js > "$path/logs/knowledge-service.log" 2>&1 &

    sleep 3

    # Verify
    if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
        log "  $island restarted successfully"
        echo "✅ $island restarted (port $port)"
    else
        log "  $island restart FAILED"
        echo "❌ $island restart failed"
        return 1
    fi
}

cmd_create() {
    local name=$1
    local role=$2
    local port=$3

    if [ -z "$name" ] || [ -z "$role" ]; then
        echo "Usage: $0 create <name> <role> [port]"
        echo "Roles: infra, platform, customer, orchestration"
        return 1
    fi

    # Auto-assign port if not provided
    if [ -z "$port" ]; then
        port=$(cmd_next_port)
        echo "Auto-assigned port: $port"
    fi

    # Check if requires approval
    requires_approval=$(grep "requires_approval:" "$REGISTRY" | head -1 | awk '{print $2}')

    if [ "$requires_approval" = "true" ]; then
        echo ""
        echo "╔══════════════════════════════════════════════════════════════╗"
        echo "║  APPROVAL REQUIRED                                            ║"
        echo "╠══════════════════════════════════════════════════════════════╣"
        echo "║  Island: $name"
        echo "║  Role:   $role"
        echo "║  Port:   $port"
        echo "╚══════════════════════════════════════════════════════════════╝"
        echo ""
        read -p "Approve island creation? [y/N] " confirm
        if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
            echo "Cancelled."
            return 1
        fi
    fi

    # Run setup script
    /opt/spaceos/.claude/skills/island-setup/setup-island.sh "$name" "$role" "$port" "vps"

    # Add to registry
    log "Adding $name to registry..."
    # (Registry update would go here - simplified for now)

    echo ""
    echo "✅ Island created. Remember to update islands.yaml manually!"
}

# Main
case "${1:-status}" in
    status)
        cmd_status
        ;;
    health)
        cmd_health
        ;;
    validate)
        cmd_validate
        ;;
    next-port)
        cmd_next_port
        ;;
    restart)
        cmd_restart "$2"
        ;;
    create)
        cmd_create "$2" "$3" "$4"
        ;;
    *)
        echo "Island Manager — Autonóm sziget kezelő"
        echo ""
        echo "Usage: $0 <command> [args]"
        echo ""
        echo "Commands:"
        echo "  status              Show all islands status"
        echo "  health              Health check all islands"
        echo "  validate            Validate registry"
        echo "  next-port           Get next available port"
        echo "  restart <island>    Restart island"
        echo "  create <n> <r> [p]  Create new island"
        ;;
esac
