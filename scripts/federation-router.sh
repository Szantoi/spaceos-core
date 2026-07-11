#!/bin/bash
# Federation Router — Automatikus outbox → inbox átvitel
# Cron: */5 * * * * /opt/spaceos/scripts/federation-router.sh
#
# Minden sziget outbox-ából a címzett inbox-ába másolja az üzeneteket
# A "to:" frontmatter mező alapján dönti el a célt

set -euo pipefail

LOG_FILE="/opt/spaceos/logs/federation-router.log"
mkdir -p "$(dirname "$LOG_FILE")"
REGISTRY="/opt/spaceos/config/islands.yaml"

log() {
    echo "[$(date -Iseconds)] $1" >> "$LOG_FILE"
}

# Sziget path lekérése
get_island_path() {
    local island=$1
    case "$island" in
        nexus) echo "/opt/nexus" ;;
        joinerytech) echo "/opt/joinerytech" ;;
        doorstar) echo "/opt/doorstar" ;;
        spaceos) echo "/opt/spaceos" ;;
        cabinet) echo "/opt/doorstar" ;;  # Cabinet -> Doorstar federation inbox
        *) echo "" ;;
    esac
}

# Frontmatter "to:" mező kiolvasása
get_to_field() {
    local file=$1
    grep -m1 "^to:" "$file" 2>/dev/null | sed 's/to:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Frontmatter "from:" mező kiolvasása
get_from_field() {
    local file=$1
    grep -m1 "^from:" "$file" 2>/dev/null | sed 's/from:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Frontmatter "status:" mező kiolvasása
get_status_field() {
    local file=$1
    grep -m1 "^status:" "$file" 2>/dev/null | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Frontmatter "ref:" mező kiolvasása
get_ref_field() {
    local file=$1
    grep -m1 "^ref:" "$file" 2>/dev/null | sed 's/ref:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Frontmatter "id:" mező kiolvasása
get_id_field() {
    local file=$1
    grep -m1 "^id:" "$file" 2>/dev/null | sed 's/id:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Frontmatter "content_hash:" mező kiolvasása
get_content_hash_field() {
    local file=$1
    grep -m1 "^content_hash:" "$file" 2>/dev/null | sed 's/content_hash:[[:space:]]*//' | tr -d '"' | tr -d "'"
}

# Összes sziget outbox feldolgozása
route_messages() {
    local routed=0
    local errors=0

    for island in nexus joinerytech doorstar spaceos; do
        island_path=$(get_island_path "$island")
        outbox_path="$island_path/terminals/federation/outbox"

        if [ ! -d "$outbox_path" ]; then
            continue
        fi

        # Minden outbox fájl feldolgozása
        for file in "$outbox_path"/*.md; do
            [ -f "$file" ] || continue

            filename=$(basename "$file")
            to=$(get_to_field "$file")
            from=$(get_from_field "$file")
            status=$(get_status_field "$file")

            # Csak UNREAD státuszú üzeneteket routolunk
            if [ "$status" != "UNREAD" ]; then
                continue
            fi

            if [ -z "$to" ]; then
                log "SKIP: $filename - no 'to:' field"
                continue
            fi

            # Cél sziget meghatározása
            target_path=$(get_island_path "$to")

            if [ -z "$target_path" ]; then
                log "ERROR: Unknown target '$to' in $filename"
                errors=$((errors + 1))
                continue
            fi

            target_inbox="$target_path/terminals/federation/inbox"

            # Ellenőrzés: már létezik-e a cél inbox-ban?
            if [ -f "$target_inbox/$filename" ]; then
                log "SKIP: $filename already in $to inbox"
                continue
            fi

            # Másolás
            if cp "$file" "$target_inbox/$filename"; then
                log "ROUTED: $filename ($from -> $to)"
                routed=$((routed + 1))

                # Eredeti fájl státusz frissítése (UNREAD -> SENT)
                sed -i 's/^status: UNREAD/status: SENT/' "$file"
            else
                log "ERROR: Failed to copy $filename to $target_inbox"
                errors=$((errors + 1))
            fi
        done
    done

    log "Routing complete: $routed routed, $errors errors"
    echo "Routed: $routed, Errors: $errors"
}

# Cabinet speciális kezelés: SpaceOS outbox -> Doorstar inbox (ha to: cabinet)
route_cabinet_messages() {
    local spaceos_outbox="/opt/spaceos/terminals/federation/outbox"
    local doorstar_inbox="/opt/doorstar/terminals/federation/inbox"
    local routed=0

    for file in "$spaceos_outbox"/*.md; do
        [ -f "$file" ] || continue

        filename=$(basename "$file")
        to=$(get_to_field "$file")
        status=$(get_status_field "$file")

        if [ "$to" = "cabinet" ] && [ "$status" = "UNREAD" ]; then
            if [ ! -f "$doorstar_inbox/$filename" ]; then
                if cp "$file" "$doorstar_inbox/$filename"; then
                    log "CABINET: $filename -> Doorstar inbox"
                    sed -i 's/^status: UNREAD/status: SENT/' "$file"
                    routed=$((routed + 1))
                fi
            fi
        fi
    done

    echo "Cabinet messages routed: $routed"
}

# Auto-acknowledge outbox messages when target responds
acknowledge_outbox_responses() {
    local acknowledged=0
    local errors=0

    log "=== Auto-acknowledgement scan started ==="

    # Scan all island inboxes for messages with ref: field
    for island in nexus joinerytech doorstar spaceos; do
        island_path=$(get_island_path "$island")
        inbox_path="$island_path/terminals/federation/inbox"

        if [ ! -d "$inbox_path" ]; then
            continue
        fi

        # Check each inbox message for ref: field
        for inbox_file in "$inbox_path"/*.md; do
            [ -f "$inbox_file" ] || continue

            ref=$(get_ref_field "$inbox_file")

            # Skip if no ref field
            if [ -z "$ref" ]; then
                continue
            fi

            # Find the referenced outbox message across all islands
            local found=0
            for source_island in nexus joinerytech doorstar spaceos; do
                source_path=$(get_island_path "$source_island")
                outbox_path="$source_path/terminals/federation/outbox"

                if [ ! -d "$outbox_path" ]; then
                    continue
                fi

                # Search for outbox message with matching id
                for outbox_file in "$outbox_path"/*.md; do
                    [ -f "$outbox_file" ] || continue

                    msg_id=$(get_id_field "$outbox_file")
                    status=$(get_status_field "$outbox_file")

                    # If this is the referenced message and it's SENT
                    if [ "$msg_id" = "$ref" ] && [ "$status" = "SENT" ]; then
                        # Update SENT → ACK
                        if sed -i 's/^status: SENT$/status: ACK/' "$outbox_file"; then
                            log "ACK: $msg_id (response received in $island inbox)"
                            acknowledged=$((acknowledged + 1))
                            found=1
                            break
                        else
                            log "ERROR: Failed to update status for $msg_id"
                            errors=$((errors + 1))
                        fi
                    fi
                done

                if [ $found -eq 1 ]; then
                    break
                fi
            done
        done
    done

    log "Auto-acknowledgement complete: $acknowledged acknowledged, $errors errors"
    echo "Acknowledged: $acknowledged, Errors: $errors"
}

# Notification deduplication check
# Tracks sent notifications in a state file to prevent re-sending
NOTIFICATION_STATE_FILE="/opt/spaceos/logs/federation-notifications.state"

check_notification_sent() {
    local msg_id=$1
    local content_hash=$2

    # Create state file if it doesn't exist
    touch "$NOTIFICATION_STATE_FILE"

    # Check if already notified using either msg_id or content_hash
    if grep -q "^${msg_id}|" "$NOTIFICATION_STATE_FILE" 2>/dev/null; then
        return 0  # Already sent
    fi

    if [ -n "$content_hash" ] && grep -q "|${content_hash}$" "$NOTIFICATION_STATE_FILE" 2>/dev/null; then
        return 0  # Already sent (same content)
    fi

    return 1  # Not sent yet
}

mark_notification_sent() {
    local msg_id=$1
    local content_hash=$2
    local timestamp=$(date -Iseconds)

    echo "${msg_id}|${timestamp}|${content_hash}" >> "$NOTIFICATION_STATE_FILE"
}

# Main
case "${1:-route}" in
    route)
        log "=== Federation Router started ==="
        route_messages
        route_cabinet_messages
        acknowledge_outbox_responses
        ;;
    status)
        echo "=== Federation Outbox Status ==="
        for island in nexus joinerytech doorstar spaceos; do
            path=$(get_island_path "$island")
            outbox="$path/terminals/federation/outbox"
            if [ -d "$outbox" ]; then
                count=$(ls "$outbox"/*.md 2>/dev/null | wc -l)
                unread=$(grep -l "status: UNREAD" "$outbox"/*.md 2>/dev/null | wc -l)
                sent=$(grep -l "status: SENT" "$outbox"/*.md 2>/dev/null | wc -l)
                ack=$(grep -l "status: ACK" "$outbox"/*.md 2>/dev/null | wc -l)
                echo "  $island: $count total ($unread UNREAD, $sent SENT, $ack ACK)"
            fi
        done
        ;;
    ack)
        # Manual acknowledgement run
        log "=== Manual acknowledgement run ==="
        acknowledge_outbox_responses
        ;;
    *)
        echo "Usage: $0 [route|status|ack]"
        echo ""
        echo "Commands:"
        echo "  route  - Route messages and auto-acknowledge responses (default)"
        echo "  status - Show federation outbox status"
        echo "  ack    - Run acknowledgement scan only"
        ;;
esac
