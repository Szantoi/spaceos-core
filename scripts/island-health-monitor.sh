#!/bin/bash
# Island Health Monitor — Automatikus sziget állapot ellenőrzés
# Cron: * * * * * /opt/spaceos/scripts/island-health-monitor.sh
#
# Percenként ellenőrzi minden sziget knowledge service-ét
# Ha 3+ egymást követő failure, alertet küld

set -euo pipefail

LOG_FILE="/opt/spaceos/logs/island-health.log"
STATE_FILE="/opt/spaceos/config/.island-health-state"
ALERT_THRESHOLD=3  # 3 egymást követő failure után alert

mkdir -p "$(dirname "$LOG_FILE")"
mkdir -p "$(dirname "$STATE_FILE")"

log() {
    echo "[$(date -Iseconds)] $1" >> "$LOG_FILE"
}

# Sziget konfiguráció
declare -A ISLANDS=(
    ["nexus"]="3456:/opt/nexus"
    ["joinerytech"]="3458:/opt/joinerytech"
    ["doorstar"]="3460:/opt/doorstar"
    ["spaceos"]="3462:/opt/spaceos"
)

# State betöltés
load_state() {
    if [ -f "$STATE_FILE" ]; then
        source "$STATE_FILE"
    else
        # Inicializáció
        for island in "${!ISLANDS[@]}"; do
            eval "FAILURES_${island}=0"
            eval "ALERTED_${island}=false"
        done
    fi
}

# State mentés
save_state() {
    {
        for island in "${!ISLANDS[@]}"; do
            eval "echo FAILURES_${island}=\$FAILURES_${island}"
            eval "echo ALERTED_${island}=\$ALERTED_${island}"
        done
    } > "$STATE_FILE"
}

# Health check egy szigetre
check_island() {
    local island=$1
    local config=${ISLANDS[$island]}
    local port=${config%%:*}

    local response
    local http_code

    response=$(curl -s -w "\n%{http_code}" "http://localhost:$port/health" --connect-timeout 5 --max-time 10 2>/dev/null || echo -e "\n000")
    http_code=$(echo "$response" | tail -1)

    if [ "$http_code" = "200" ]; then
        return 0
    else
        return 1
    fi
}

# Alert küldés (Telegram)
send_alert() {
    local island=$1
    local status=$2  # "DOWN" vagy "UP"
    local config=${ISLANDS[$island]}
    local port=${config%%:*}

    local message
    if [ "$status" = "DOWN" ]; then
        message="🔴 ISLAND DOWN: $island (port $port) - $ALERT_THRESHOLD+ consecutive failures"
    else
        message="🟢 ISLAND RECOVERED: $island (port $port) - Back online"
    fi

    # Telegram küldés ha token elérhető
    local token_file="/opt/spaceos/config/.telegram-alert-token"
    local chat_id_file="/opt/spaceos/config/.telegram-alert-chatid"

    if [ -f "$token_file" ] && [ -f "$chat_id_file" ]; then
        local token=$(cat "$token_file")
        local chat_id=$(cat "$chat_id_file")

        curl -s -X POST "https://api.telegram.org/bot${token}/sendMessage" \
            -d "chat_id=${chat_id}" \
            -d "text=${message}" \
            -d "parse_mode=Markdown" \
            > /dev/null 2>&1 || true
    fi

    log "ALERT: $message"
}

# Auto-repair kísérlet
try_auto_repair() {
    local island=$1
    local config=${ISLANDS[$island]}
    local path=${config#*:}
    local port=${config%%:*}

    log "AUTO-REPAIR: Attempting to restart $island..."

    # Knowledge service újraindítás
    local service_path="$path/src/${island}-nexus/knowledge-service"

    if [ ! -d "$service_path" ]; then
        # Fallback path-ok
        service_path="$path/nexus-core/knowledge-service"
        [ ! -d "$service_path" ] && service_path="$path/${island}-nexus/knowledge-service"
    fi

    if [ -d "$service_path" ]; then
        # Kill existing process
        local pid=$(lsof -ti ":$port" 2>/dev/null || true)
        if [ -n "$pid" ]; then
            kill "$pid" 2>/dev/null || true
            sleep 2
        fi

        # Start new process
        cd "$service_path"
        TERMINALS_PATH="$path/terminals" \
        KNOWLEDGE_BASE_PATH="$path/docs/knowledge" \
        PORT="$port" \
        ISLAND_ID="$island" \
        node dist/server.js > "$path/logs/knowledge-service.log" 2>&1 &

        sleep 5

        # Verify
        if check_island "$island"; then
            log "AUTO-REPAIR: $island successfully restarted"
            return 0
        fi
    fi

    log "AUTO-REPAIR: Failed to restart $island"
    return 1
}

# Fő logika
main() {
    load_state

    local all_healthy=true

    for island in "${!ISLANDS[@]}"; do
        local failures_var="FAILURES_${island}"
        local alerted_var="ALERTED_${island}"

        if check_island "$island"; then
            # Health OK
            local failures=${!failures_var}
            local alerted=${!alerted_var}

            if [ "$alerted" = "true" ]; then
                # Recovery alert
                send_alert "$island" "UP"
                eval "${alerted_var}=false"
            fi

            eval "${failures_var}=0"
            log "HEALTH OK: $island"
        else
            # Health FAIL
            local failures=${!failures_var}
            failures=$((failures + 1))
            eval "${failures_var}=$failures"

            log "HEALTH FAIL: $island (failure count: $failures)"
            all_healthy=false

            if [ "$failures" -ge "$ALERT_THRESHOLD" ]; then
                local alerted=${!alerted_var}

                if [ "$alerted" != "true" ]; then
                    # Még nem alerteltünk
                    # Próbálj auto-repair-t
                    if try_auto_repair "$island"; then
                        eval "${failures_var}=0"
                    else
                        send_alert "$island" "DOWN"
                        eval "${alerted_var}=true"
                    fi
                fi
            fi
        fi
    done

    save_state

    if [ "$all_healthy" = "true" ]; then
        log "ALL ISLANDS HEALTHY"
    fi
}

# Main
case "${1:-run}" in
    run)
        main
        ;;
    status)
        echo "=== Island Health Status ==="
        for island in "${!ISLANDS[@]}"; do
            config=${ISLANDS[$island]}
            port=${config%%:*}
            if check_island "$island"; then
                echo "  $island (port $port): ✅ HEALTHY"
            else
                echo "  $island (port $port): ❌ UNHEALTHY"
            fi
        done
        ;;
    reset)
        rm -f "$STATE_FILE"
        echo "State reset"
        ;;
    *)
        echo "Usage: $0 [run|status|reset]"
        ;;
esac
