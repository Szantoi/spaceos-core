#!/usr/bin/env bash
# ============================================================================
# SpaceOS Dispatcher v4 — Lifecycle Manager
# ============================================================================
#
# 8GB VPS optimalizáció: csak 2-3 session fut egyszerre.
#
# Persistent (mindig fut):
#   spaceos-root          ← koordinátor
#   spaceos-kernel        ← legaktívabb terminál
#
# On-demand (inbox → indul → DONE → leáll):
#   spaceos-orch, spaceos-portal, spaceos-joinery,
#   spaceos-abstractions, spaceos-infra, spaceos-e2e, spaceos-sales
#
# Közös tmux socket: /tmp/spaceos.tmux
# Root mód: notify | semi | auto (runtime-ban váltható)
#
# Használat:
#   sd --launch          # Persistent session-ök indítása
#   sd --daemon          # Dispatcher háttérben
#   sd --mode notify     # Mód váltás
#   sd --status          # Állapot + memória
#   sd --stop            # Dispatcher leállítás
#   sd --kill-all        # Minden session leállítása
#
# ============================================================================

set -euo pipefail

# ── Konfiguráció ──────────────────────────────────────────────────────────────

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
MAILBOX_DIR="$SPACEOS_ROOT/docs/mailbox"
LOG_DIR="$SPACEOS_ROOT/logs/dispatcher"
PID_FILE="$LOG_DIR/dispatcher.pid"
LOG_FILE="$LOG_DIR/dispatcher.log"
MODE_FILE="$SPACEOS_ROOT/tools/dispatcher/mode"
TMUX_SOCKET="/tmp/spaceos.tmux"
DRY_RUN=false
DAEMON_MODE=false

# Terminálok: [mailbox_név]="tmux_session:repo_path"
# 2026-04-30: Restructured — backend/, frontend/, törölt terminálok eltávolítva
declare -A TERMINALS=(
    # Backend services
    [kernel]="spaceos-kernel:/opt/spaceos/backend/spaceos-kernel"
    [orchestrator]="spaceos-orch:/opt/spaceos/backend/spaceos-orchestrator"
    [joinery]="spaceos-joinery:/opt/spaceos/backend/spaceos-modules-joinery"
    [abstractions]="spaceos-abstractions:/opt/spaceos/backend/spaceos-modules-abstractions"
    [inventory]="spaceos-inventory:/opt/spaceos/backend/spaceos-modules-inventory"
    [cutting]="spaceos-cutting:/opt/spaceos/backend/spaceos-modules-cutting"
    [procurement]="spaceos-procurement:/opt/spaceos/backend/spaceos-modules-procurement"
    [identity]="spaceos-identity:/opt/spaceos/backend/spaceos-modules-identity"
    [sales]="spaceos-sales:/opt/spaceos/backend/spaceos-modules-sales"
    # NuGet libraries
    [cabinet]="spaceos-cabinet:/opt/spaceos/backend/spaceos-modules-cabinet"
    # Frontend
    [fe]="spaceos-fe:/opt/spaceos/frontend/joinerytech-portal"
)

# Persistent terminálok — nincs (on-demand only, 8GB VPS)
PERSISTENT=()

# Root koordinátor session
ROOT_SESSION="spaceos-root"

# Cooldown
COOLDOWN=30
SETTLE_SECONDS=2

# Session indítás utáni várakozás (claude startup)
SESSION_STARTUP_WAIT=8

declare -A LAST_DISPATCH

# ── Segédfüggvények ──────────────────────────────────────────────────────────

ts()    { date '+%Y-%m-%d %H:%M:%S'; }
epoch() { date '+%s'; }

log()      { echo "[$(ts)] [$1] ${*:2}" | tee -a "$LOG_FILE" 2>/dev/null || echo "[$(ts)] [$1] ${*:2}"; }
log_info() { log "INFO" "$@"; }
log_warn() { log "WARN" "$@"; }
log_send() { log "SEND" "$@"; }
log_task() { log "TASK" "$@"; }
log_root() { log "ROOT" "$@"; }
log_life() { log "LIFE" "$@"; }

ensure_dirs() {
    mkdir -p "$LOG_DIR" 2>/dev/null || true
    for name in "${!TERMINALS[@]}"; do
        mkdir -p "$MAILBOX_DIR/$name/inbox" "$MAILBOX_DIR/$name/outbox" 2>/dev/null || true
    done
}

tm() { tmux -S "$TMUX_SOCKET" "$@"; }

session_of() { local c="${TERMINALS[$1]}"; echo "${c%%:*}"; }
repodir_of() { local c="${TERMINALS[$1]}"; echo "${c#*:}"; }

is_unread()   { grep -q "status: UNREAD" "$1" 2>/dev/null; }
get_field()   { grep "^${2}:" "$1" 2>/dev/null | head -1 | sed "s/^${2}:[[:space:]]*//"; }

session_alive() { tm has-session -t "$1" 2>/dev/null; }

get_mode() {
    if [[ -f "$MODE_FILE" ]]; then
        cat "$MODE_FILE" | tr -d '[:space:]'
    else
        echo "notify"
    fi
}

set_mode() {
    echo "$1" > "$MODE_FILE"
    log_info "Mód átállítva: $1"
}

is_persistent() {
    local name="$1"
    for p in "${PERSISTENT[@]}"; do
        [[ "$p" == "$name" ]] && return 0
    done
    return 1
}

get_mem_mb() {
    free -m | awk '/^Mem:/ {print $7}'
}

# ── Session lifecycle ─────────────────────────────────────────────────────────

ensure_socket() {
    if ! tm list-sessions &>/dev/null; then
        tm new-session -d -s _init
        tm kill-session -t _init 2>/dev/null || true
    fi
    chmod 777 "$TMUX_SOCKET" 2>/dev/null || true
}

start_session() {
    local name="$1"
    local fresh="${2:-false}"   # true = -c nélkül (szerepkör frissítés)
    local session
    session="$(session_of "$name")"
    local repo
    repo="$(repodir_of "$name")"

    if session_alive "$session"; then
        log_life "[$name] Már fut: $session"
        return 0
    fi

    local avail_mb
    avail_mb=$(get_mem_mb)
    if (( avail_mb < 400 )); then
        log_warn "[$name] Kevés memória (${avail_mb}MB szabad) — session NEM indítva!"
        return 1
    fi

    local claude_cmd
    if [[ "$fresh" == "true" ]]; then
        claude_cmd="claude --dangerously-skip-permissions"
        log_life "[$name] Fresh start (nincs -c): $session @ $repo"
    else
        claude_cmd="claude --dangerously-skip-permissions -c || claude --dangerously-skip-permissions"
        log_life "[$name] Session elindítva: $session @ $repo (szabad RAM: ${avail_mb}MB)"
    fi

    ensure_socket
    tm new-session -d -s "$session"
    tm send-keys -t "$session" "cd $repo" Enter
    sleep 1
    tm send-keys -t "$session" "$claude_cmd" Enter

    sleep "$SESSION_STARTUP_WAIT"

    # -c folytatásnál küldünk egy "folytasd a munkát!" üzenetet
    # hogy a terminál felvegye a kontextust és folytassa
    if [[ "$fresh" != "true" ]]; then
        tm send-keys -l -t "$session" "folytasd a munkát!"
        sleep 0.5
        tm send-keys -t "$session" Enter
        log_life "[$name] 'folytasd a munkát!' elküldve"
    fi

    return 0
}

# Szerepkör frissítés: graceful /exit → friss claude (nincs -c, nincs kill-session)
refresh_session() {
    local name="$1"
    if [[ -z "${TERMINALS[$name]:-}" ]]; then
        echo "Ismeretlen terminál: $name"
        echo "Elérhető: ${!TERMINALS[*]}"
        return 1
    fi
    local session
    session="$(session_of "$name")"

    if ! session_alive "$session"; then
        echo "  ⬜ $name — nem fut, fresh start..."
        start_session "$name" "true"
        echo "  ✅ $name → $session (fresh start)"
        return
    fi

    echo "  🔄 $name → /exit..."
    tm send-keys -t "$session" "/exit" Enter
    sleep 3
    echo "  🔄 $name → claude újraindítás (nincs -c)..."
    tm send-keys -t "$session" "claude --dangerously-skip-permissions" Enter
    sleep 5
    echo "  ✅ $name → frissítve"
}

refresh_all_sessions() {
    local mem_mb
    mem_mb=$(get_mem_mb)
    echo "Szerepkör frissítés — minden session fresh restart (nincs -c)"
    echo "Szabad RAM: ${mem_mb}MB"
    echo ""

    # 1. Futó sessionok: /exit küldés
    local to_restart=()
    for name in "${!TERMINALS[@]}"; do
        local session
        session="$(session_of "$name")"
        if session_alive "$session"; then
            echo "  🔄 $name → /exit..."
            tm send-keys -t "$session" "/exit" Enter
            to_restart+=("$name")
        fi
    done

    # Várakozás Claude kilépésre
    if [[ ${#to_restart[@]} -gt 0 ]]; then
        echo ""
        echo "  Várakozás Claude kilépésre (3s)..."
        sleep 3
        for name in "${to_restart[@]}"; do
            local session
            session="$(session_of "$name")"
            echo "  🔄 $name → claude újraindítás..."
            tm send-keys -t "$session" "claude --dangerously-skip-permissions" Enter
            echo "  ✅ $name → frissítve"
        done
    fi

    # 2. Nem futó sessionok: fresh start (RAM gate)
    local started=()
    for name in "${!TERMINALS[@]}"; do
        local session
        session="$(session_of "$name")"
        session_alive "$session" && continue   # már újraindult fentebb
        # RAM gate: legalább 400MB szabad kell
        local avail
        avail=$(get_mem_mb)
        if (( avail < 400 )); then
            echo "  ⚠️  $name — kevés RAM (${avail}MB), kihagyva"
            continue
        fi
        echo "  ▶  $name → fresh start..."
        start_session "$name" "true"
        started+=("$name")
        echo "  ✅ $name → elindítva"
    done

    if [[ ${#to_restart[@]} -eq 0 && ${#started[@]} -eq 0 ]]; then
        echo "  Nincs elvégzett művelet."
    fi

    echo ""
    mem_mb=$(get_mem_mb)
    echo "Kész. Szabad RAM: ${mem_mb}MB"
}

stop_session() {
    local name="$1"
    local session
    session="$(session_of "$name")"

    if is_persistent "$name"; then
        log_life "[$name] Persistent — NEM állítom le"
        return 0
    fi

    if ! session_alive "$session"; then
        return 0
    fi

    tm kill-session -t "$session" 2>/dev/null || true
    log_life "[$name] Session leállítva: $session (RAM felszabadítva)"
}

# ── Inbox dispatch ────────────────────────────────────────────────────────────

dispatch() {
    local name="$1" msg_file="$2"
    local session
    session="$(session_of "$name")"
    local basename
    basename="$(basename "$msg_file")"
    local priority
    priority="$(get_field "$msg_file" "priority")"
    local msg_type
    msg_type="$(get_field "$msg_file" "type")"

    # Cooldown
    local now
    now=$(epoch)
    local last="${LAST_DISPATCH[$name]:-0}"
    if (( now - last < COOLDOWN )); then
        log_warn "[$name] Cooldown aktív, kihagyva: $basename"
        return 1
    fi

    # On-demand session indítás ha nem fut
    if ! session_alive "$session"; then
        log_life "[$name] On-demand session indítás (inbox trigger)..."
        if ! start_session "$name"; then
            log_warn "[$name] Session indítás sikertelen — feladat várakozik: $basename"
            return 1
        fi
    fi

    # Globális dedup: mkdir atomi → scan + inotify race condition ellen
    local _dispatch_dedup="$LOG_DIR/.dispatch_dedup_$(echo "$msg_file" | md5sum | cut -d' ' -f1).lock"
    if ! mkdir "$_dispatch_dedup" 2>/dev/null; then
        log_info "[$name] Dedup skip (már el lett küldve): $basename"
        return 0
    fi
    ( sleep 90 && rm -rf "$_dispatch_dedup" ) &

    local instruction="Új feladat érkezett. Olvasd el és hajtsd végre: mailbox/inbox/${basename} — Prioritás: ${priority}, Típus: ${msg_type}"

    if $DRY_RUN; then
        log_info "[DRY-RUN] [$name] → $session: $instruction"
        return 0
    fi

    tm send-keys -l -t "$session" "$instruction"
    sleep 0.5
    tm send-keys -t "$session" Enter
    LAST_DISPATCH[$name]=$now

    # Dispatch timestamp rögzítése inbox watchdog számára
    local _ts_file="$LOG_DIR/.inbox_dispatch_ts_$(echo "$msg_file" | md5sum | cut -d' ' -f1)"
    echo "$(epoch)" > "$_ts_file"

    log_send "[$name] → $session: $basename (priority: $priority)"
}

# ── Outbox dispatch (válasz → root) ──────────────────────────────────────────

dispatch_to_root() {
    local terminal="$1" outbox_file="$2"
    local filename
    filename="$(basename "$outbox_file")"
    local msg_type
    msg_type="$(get_field "$outbox_file" "type")"
    # Normalizálás: type:response → done (protokoll sértés recovery)
    [[ "$msg_type" == "response" ]] && msg_type="done"
    local mode
    mode="$(get_mode)"

    if ! session_alive "$ROOT_SESSION"; then
        log_warn "[root] ⬜ $ROOT_SESSION NEM FUT"
        return 1
    fi

    local icon="📨"
    case "$msg_type" in
        done)     icon="✅" ;;
        blocked)  icon="🔴" ;;
        question) icon="❓" ;;
    esac

    log_root "$icon [$terminal] $msg_type: $filename (mód: $mode)"

    if $DRY_RUN; then
        log_info "[DRY-RUN] [root] ← [$terminal] $msg_type ($mode mód)"
        # On-demand leállítás dry-run-ban is logoljuk
        if [[ "$msg_type" == "done" || "$msg_type" == "blocked" ]] && ! is_persistent "$terminal"; then
            log_info "[DRY-RUN] [$terminal] On-demand session leállna"
        fi
        return 0
    fi

    # Root session-be küldés (mód alapján)
    case "$mode" in
        notify)
            tm send-keys -l -t "$ROOT_SESSION" \
                "$icon Outbox üzenet érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Típus: $msg_type. Olvasd el és döntsd el mi legyen."
            sleep 0.5
            tm send-keys -t "$ROOT_SESSION" Enter
            ;;
        semi)
            local instruction=""
            case "$msg_type" in
                done)
                    instruction="DONE érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Dolgozd fel a spaceos-root skill szerint: 1) Olvasd el a DONE üzenetet 2) Ellenőrizd: build+test zöld? Security review megvan? 3) Ha rendben: outbox status UNREAD→READ, task active/→archive/, frissítsd README.md és Codebase_Status.md 4) Ha hiányos: írj visszadobás inbox üzenetet. FONTOS: NE adj ki új feladatot — csak a feldolgozást végezd el, aztán állj meg és jelezd mi a következő lépés."
                    ;;
                blocked)
                    instruction="BLOCKED érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Olvasd el mi blokkol, és javasolj megoldást. NE adj ki automatikusan új feladatot."
                    ;;
                question)
                    instruction="QUESTION érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Olvasd el a kérdést és válaszolj inbox üzenetben."
                    ;;
                *)
                    instruction="$icon Outbox: [$terminal] docs/mailbox/$terminal/outbox/$filename — Típus: $msg_type. Olvasd el."
                    ;;
            esac
            tm send-keys -l -t "$ROOT_SESSION" "$instruction"
            sleep 0.5
            tm send-keys -t "$ROOT_SESSION" Enter
            ;;
        auto)
            local instruction=""
            case "$msg_type" in
                done)
                    instruction="DONE érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Dolgozd fel a spaceos-root skill szerint a TELJES pipeline-t: 1) Olvasd el a DONE üzenetet 2) Ellenőrizd: build+test zöld? Security review megvan? 3) Ha rendben: fogadd el → outbox status UNREAD→READ, task active/→archive/, frissítsd README.md és Codebase_Status.md 4) Ha van következő task a docs/tasks/new/ mappában ami kiadható: mozdítsd active/-ba és írj inbox üzenetet a megfelelő terminálnak 5) Ha hiányos DONE: írj visszadobás inbox üzenetet 6) Mindig kövesd a cross-project sorrendet: Kernel → Orchestrator → Portal"
                    ;;
                blocked)
                    instruction="BLOCKED érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Olvasd el, döntsd el mi kell, és ha tudsz: válaszolj inbox üzenetben automatikusan. Ha emberi döntés kell, jelezd."
                    ;;
                question)
                    instruction="QUESTION érkezett: [$terminal] docs/mailbox/$terminal/outbox/$filename — Olvasd el a kérdést. Ha a válasz egyértelmű a docs/vision/ vagy az EPIC.md alapján, válaszolj inbox üzenetben. Ha nem egyértelmű, jelezd hogy emberi döntés kell."
                    ;;
                *)
                    instruction="$icon Outbox: [$terminal] docs/mailbox/$terminal/outbox/$filename — Dolgozd fel."
                    ;;
            esac
            tm send-keys -l -t "$ROOT_SESSION" "$instruction"
            sleep 0.5
            tm send-keys -t "$ROOT_SESSION" Enter
            ;;
    esac

    # On-demand session leállítása DONE vagy BLOCKED után
    if [[ "$msg_type" == "done" || "$msg_type" == "blocked" ]]; then
        if ! is_persistent "$terminal"; then
            log_life "[$terminal] DONE/BLOCKED → on-demand session leállítása (5mp cleanup)..."
            (
                sleep 5
                stop_session "$terminal"
            ) &
        fi
    fi
}

# ── Inbox figyelés ────────────────────────────────────────────────────────────

watch_inboxes() {
    local watch_dirs=()
    for name in "${!TERMINALS[@]}"; do
        local d="$MAILBOX_DIR/$name/inbox"
        [[ -d "$d" ]] && watch_dirs+=("$d")
    done
    [[ ${#watch_dirs[@]} -eq 0 ]] && return

    inotifywait -m -e create -e moved_to --format '%w %f' \
        "${watch_dirs[@]}" 2>/dev/null | while read -r dir filename; do

        [[ "$filename" == *.md ]] || continue
        sleep "$SETTLE_SECONDS"

        local full="${dir}${filename}"

        # Dedup: ha már feldolgozott, skip
        local dedup_marker="$LOG_DIR/.dedup_inbox_$(echo "$full" | md5sum | cut -d' ' -f1)"
        [[ -f "$dedup_marker" ]] && continue
        touch "$dedup_marker"

        local name=""
        for n in "${!TERMINALS[@]}"; do
            [[ "$dir" == *"/$n/inbox/"* ]] && { name="$n"; break; }
        done
        [[ -z "$name" ]] && continue

        if is_unread "$full"; then
            log_info "[$name] Új UNREAD inbox: $filename"
            dispatch "$name" "$full" || true
        fi

        # Dedup marker törlése 60mp után
        ( sleep 60 && rm -f "$dedup_marker" ) &
    done &
}

# ── Outbox figyelés ───────────────────────────────────────────────────────────

watch_outboxes() {
    local dirs=()
    for name in "${!TERMINALS[@]}"; do
        local d="$MAILBOX_DIR/$name/outbox"
        [[ -d "$d" ]] && dirs+=("$d")
    done
    [[ ${#dirs[@]} -eq 0 ]] && return

    inotifywait -m -e create -e moved_to --format '%w %f' \
        "${dirs[@]}" 2>/dev/null | while read -r dir filename; do

        [[ "$filename" == *.md ]] || continue
        sleep 1

        local full="${dir}${filename}"

        # Dedup
        local dedup_marker="$LOG_DIR/.dedup_outbox_$(echo "$full" | md5sum | cut -d' ' -f1)"
        [[ -f "$dedup_marker" ]] && continue
        touch "$dedup_marker"

        local terminal
        terminal=$(echo "$dir" | grep -oP '(?<=mailbox/)[^/]+(?=/outbox)')

        if is_unread "$full"; then
            dispatch_to_root "$terminal" "$full"
        fi

        ( sleep 60 && rm -f "$dedup_marker" ) &
    done &
}

# ── UNREAD scan ───────────────────────────────────────────────────────────────

scan_unread_inboxes() {
    log_info "UNREAD inbox scan..."
    for name in "${!TERMINALS[@]}"; do
        local inbox="$MAILBOX_DIR/$name/inbox"
        [[ -d "$inbox" ]] || continue
        for f in "$inbox"/*.md; do
            [[ -f "$f" ]] || continue
            is_unread "$f" && dispatch "$name" "$f" || true
        done
    done
}

scan_unread_outboxes() {
    log_info "UNREAD outbox scan..."
    for name in "${!TERMINALS[@]}"; do
        local outbox="$MAILBOX_DIR/$name/outbox"
        [[ -d "$outbox" ]] || continue
        for f in "$outbox"/*.md; do
            [[ -f "$f" ]] || continue
            is_unread "$f" && dispatch_to_root "$name" "$f" || true
        done
    done
}

# ── Fő watcher ────────────────────────────────────────────────────────────────

start_watching() {
    local mode
    mode="$(get_mode)"
    local mem_mb
    mem_mb=$(get_mem_mb)

    log_info "═══════════════════════════════════════════════"
    log_info "SpaceOS Dispatcher v4 — Lifecycle Manager"
    log_info "Socket: $TMUX_SOCKET"
    log_info "Root mód: $mode"
    log_info "Szabad RAM: ${mem_mb}MB"
    log_info "Persistent: ${PERSISTENT[*]}"
    log_info "On-demand: a többi (DONE után leáll)"
    log_info "Dry-run: $DRY_RUN"
    log_info "═══════════════════════════════════════════════"

    echo $$ > "$PID_FILE"

    # Session állapotok
    log_info "Terminálok:"
    for name in "${!TERMINALS[@]}"; do
        local s
        s="$(session_of "$name")"
        local type="on-demand"
        is_persistent "$name" && type="persistent"
        if session_alive "$s"; then
            log_info "  ✅ $name → $s ($type)"
        else
            if is_persistent "$name"; then
                log_warn "  ⬜ $name → $s ($type) NEM FUT!"
            else
                log_info "  💤 $name → $s ($type, igény szerint indul)"
            fi
        fi
    done
    if session_alive "$ROOT_SESSION"; then
        log_info "  ✅ root → $ROOT_SESSION (persistent)"
    else
        log_warn "  ⬜ root → $ROOT_SESSION NEM FUT!"
    fi

    # UNREAD scan
    scan_unread_inboxes
    scan_unread_outboxes

    # Watcher-ek indítása
    watch_inboxes
    watch_outboxes

    log_info "Figyelés indítva. Ctrl+C a leállításhoz."

    # Fő loop
    local _loop_count=0
    while true; do
        sleep 60
        (( _loop_count++ )) || true

        local current_mode
        current_mode="$(get_mode)"
        if [[ "$current_mode" != "$mode" ]]; then
            log_info "Mód változás: $mode → $current_mode"
            mode="$current_mode"
        fi

        # Watchdog: 2 percenként UNREAD outbox re-scan
        # Max 3 retry / fájl — exponenciális backoff (2min, 4min, 8min), utána megáll.
        # Rate limit esetén nem áraszt el: 3 kísérlet után "emberi beavatkozás" log.
        if (( _loop_count % 2 == 0 )); then
            local _stuck=0
            local _now
            _now=$(epoch)
            for _wname in "${!TERMINALS[@]}"; do
                local _wout="$MAILBOX_DIR/$_wname/outbox"
                [[ -d "$_wout" ]] || continue
                for _wf in "$_wout"/*.md; do
                    [[ -f "$_wf" ]] || continue
                    is_unread "$_wf" || continue

                    local _hash
                    _hash=$(echo "$_wf" | md5sum | cut -d' ' -f1)
                    local _cnt_file="$LOG_DIR/.wdog_cnt_$_hash"
                    local _ts_file="$LOG_DIR/.wdog_ts_$_hash"
                    local _cnt=0
                    local _last_ts=0
                    [[ -f "$_cnt_file" ]] && _cnt=$(cat "$_cnt_file")
                    [[ -f "$_ts_file"  ]] && _last_ts=$(cat "$_ts_file")

                    # Max 3 retry után megáll
                    if (( _cnt >= 3 )); then
                        log_warn "[watchdog] SKIP (max retry): $_wname/$(basename "$_wf") — emberi beavatkozás szükséges?"
                        continue
                    fi

                    # Exponenciális backoff: 120s, 240s, 480s
                    local _backoff=$(( 120 * (1 << _cnt) ))
                    if (( _now - _last_ts < _backoff )); then
                        continue
                    fi

                    (( _stuck++ )) || true
                    echo $(( _cnt + 1 )) > "$_cnt_file"
                    echo "$_now"        > "$_ts_file"
                    log_warn "[watchdog] Retry #$(( _cnt + 1 ))/3 — $_wname/$(basename "$_wf")"
                    dispatch_to_root "$_wname" "$_wf" || true
                done
            done
            (( _stuck > 0 )) && log_warn "[watchdog] $_stuck stuck outbox újraküldve" || true
        fi

        # ── Inbox watchdog: ha 5 perce UNREAD és nem dolgozza a terminál → ROOT értesítés
        local _now_inbox
        _now_inbox=$(epoch)
        local _inbox_timeout=300   # 5 perc
        for _iname in "${!TERMINALS[@]}"; do
            local _winbox="$MAILBOX_DIR/$_iname/inbox"
            [[ -d "$_winbox" ]] || continue
            for _if in "$_winbox"/*.md; do
                [[ -f "$_if" ]] || continue
                is_unread "$_if" || continue

                local _ihash
                _ihash=$(echo "$_if" | md5sum | cut -d' ' -f1)
                local _its_file="$LOG_DIR/.inbox_dispatch_ts_$_ihash"
                local _ialert_file="$LOG_DIR/.inbox_alert_$_ihash"
                [[ -f "$_its_file" ]] || continue   # még nem lett dispatchelve

                local _idispatch_ts
                _idispatch_ts=$(cat "$_its_file")
                local _ielapsed=$(( _now_inbox - _idispatch_ts ))

                # Max 1 értesítés / fájl (nehogy spammelje a ROOT-ot)
                [[ -f "$_ialert_file" ]] && continue

                if (( _ielapsed > _inbox_timeout )); then
                    touch "$_ialert_file"
                    local _ibase
                    _ibase=$(basename "$_if")
                    local _imsg="⚠️ [inbox-watchdog] $_iname terminál ${_ielapsed}mp óta NEM olvasta: $_ibase — ellenőrizd a konzolt, kézzel kell Enter?"
                    log_warn "[inbox-watchdog] $_iname: $_ibase (${_ielapsed}s)"
                    if session_alive "$ROOT_SESSION"; then
                        tm send-keys -l -t "$ROOT_SESSION" "$_imsg"
                        sleep 0.5
                        tm send-keys -t "$ROOT_SESSION" Enter
                    fi
                fi
            done
        done
    done
}

# ── Launch ────────────────────────────────────────────────────────────────────

launch_persistent() {
    local mem_mb
    mem_mb=$(get_mem_mb)
    echo "SpaceOS Dispatcher v4 — Persistent session-ök"
    echo "Szabad RAM: ${mem_mb}MB"
    echo ""

    ensure_socket

    # Root koordinátor
    if session_alive "$ROOT_SESSION"; then
        echo "  ⏭️  root → $ROOT_SESSION (már fut)"
    else
        tm new-session -d -s "$ROOT_SESSION"
        tm send-keys -t "$ROOT_SESSION" "cd /opt/spaceos" Enter
        sleep 1
        tm send-keys -t "$ROOT_SESSION" "claude --dangerously-skip-permissions -c || claude --dangerously-skip-permissions" Enter
        echo "  ✅ root → $ROOT_SESSION"
        sleep 3
    fi

    # Persistent terminálok
    for name in "${PERSISTENT[@]}"; do
        start_session "$name"
        local s
        s="$(session_of "$name")"
        if session_alive "$s"; then
            echo "  ✅ $name → $s (persistent)"
        else
            echo "  ❌ $name → $s (indítás sikertelen!)"
        fi
    done

    echo ""
    echo "On-demand terminálok (inbox-ra indulnak automatikusan):"
    for name in "${!TERMINALS[@]}"; do
        is_persistent "$name" && continue
        echo "  💤 $name → $(session_of "$name")"
    done

    mem_mb=$(get_mem_mb)
    echo ""
    echo "Szabad RAM: ${mem_mb}MB"
    echo ""
    echo "Parancsok:"
    echo "  tm attach -t spaceos-root"
    echo "  tm attach -t spaceos-kernel"
    echo "  Ctrl+B, S → session váltó"
}

# ── Status ────────────────────────────────────────────────────────────────────

show_status() {
    local mem_mb
    mem_mb=$(get_mem_mb)
    local mem_total
    mem_total=$(free -m | awk '/^Mem:/ {print $2}')
    local mem_used
    mem_used=$(free -m | awk '/^Mem:/ {print $3}')

    echo "SpaceOS Dispatcher v4 — Lifecycle Manager"
    echo "═══════════════════════════════════════════"

    if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
        echo "Dispatcher: ✅ FUT (PID: $(cat "$PID_FILE"))"
    else
        echo "Dispatcher: ⏹️  NEM FUT"
    fi

    echo "Root mód:   $(get_mode)"
    echo "RAM:        ${mem_used}MB / ${mem_total}MB (szabad: ${mem_mb}MB)"
    echo ""

    echo "Persistent (mindig fut):"
    if session_alive "$ROOT_SESSION"; then
        echo "  ✅ root → $ROOT_SESSION"
    else
        echo "  ⬜ root → $ROOT_SESSION"
    fi
    for name in "${PERSISTENT[@]}"; do
        local s
        s="$(session_of "$name")"
        if session_alive "$s"; then
            echo "  ✅ $name → $s"
        else
            echo "  ⬜ $name → $s"
        fi
    done

    echo ""
    echo "On-demand (igény szerint):"
    for name in "${!TERMINALS[@]}"; do
        is_persistent "$name" && continue
        local s
        s="$(session_of "$name")"
        if session_alive "$s"; then
            echo "  🟢 $name → $s (aktív — leáll DONE után)"
        else
            echo "  💤 $name → $s (alszik)"
        fi
    done

    echo ""
    echo "UNREAD inbox:"
    local found=false
    for name in "${!TERMINALS[@]}"; do
        local count=0
        local inbox_files=("$MAILBOX_DIR/$name/inbox/"*.md)
        for f in "${inbox_files[@]}"; do
            [[ -f "$f" ]] && is_unread "$f" && count=$((count + 1))
        done
        if [[ $count -gt 0 ]]; then
            echo "  📨 $name: $count"
            found=true
        fi
    done
    $found || echo "  (nincs)"

    echo ""
    echo "UNREAD outbox:"
    found=false
    for name in "${!TERMINALS[@]}"; do
        local count=0
        local outbox_files=("$MAILBOX_DIR/$name/outbox/"*.md)
        for f in "${outbox_files[@]}"; do
            [[ -f "$f" ]] && is_unread "$f" && count=$((count + 1))
        done
        if [[ $count -gt 0 ]]; then
            echo "  📬 $name: $count"
            found=true
        fi
    done
    $found || echo "  (nincs)"

    echo ""
    echo "Utolsó log:"
    tail -5 "$LOG_FILE" 2>/dev/null || echo "  (üres)"
}

# ── Stop / Kill ───────────────────────────────────────────────────────────────

stop_dispatcher() {
    if [[ -f "$PID_FILE" ]]; then
        local pid
        pid=$(cat "$PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null
            pkill -P "$pid" 2>/dev/null || true
            rm -f "$PID_FILE"
            echo "Dispatcher leállítva (PID: $pid)."
        else
            rm -f "$PID_FILE"
            echo "Dispatcher nem futott."
        fi
    else
        echo "PID fájl nem található."
    fi
}

kill_all_sessions() {
    echo "Minden session leállítása..."
    for name in "${!TERMINALS[@]}"; do
        local s
        s="$(session_of "$name")"
        if session_alive "$s"; then
            tm kill-session -t "$s" 2>/dev/null
            echo "  ✖ $name → $s"
        fi
    done
    if session_alive "$ROOT_SESSION"; then
        tm kill-session -t "$ROOT_SESSION" 2>/dev/null
        echo "  ✖ root → $ROOT_SESSION"
    fi
    echo "Kész. Szabad RAM: $(get_mem_mb)MB"
}

stop_all_terminals() {
    echo "Projekt terminálok leállítása (root megmarad)..."
    local stopped=0
    for name in "${!TERMINALS[@]}"; do
        local s
        s="$(session_of "$name")"
        if session_alive "$s"; then
            tm kill-session -t "$s" 2>/dev/null
            echo "  ✖ $name → $s"
            (( stopped++ )) || true
        fi
    done
    if [[ $stopped -eq 0 ]]; then
        echo "  (nincs futó projekt terminál)"
    fi
    echo "Kész. Root: $(session_alive "$ROOT_SESSION" && echo "✅ fut" || echo "⬜ nem fut") | Szabad RAM: $(get_mem_mb)MB"
}

# ── Kézi session start/stop ──────────────────────────────────────────────────

manual_start() {
    local name="$1"
    if [[ -z "${TERMINALS[$name]:-}" ]]; then
        echo "Ismeretlen terminál: $name"
        echo "Elérhető: ${!TERMINALS[*]}"
        return 1
    fi
    echo "Session indítása: $name"
    start_session "$name" "false"
    echo "Csatlakozás: tm attach -t $(session_of "$name")"
}

manual_stop() {
    local name="$1"
    if [[ -z "${TERMINALS[$name]:-}" ]]; then
        echo "Ismeretlen terminál: $name"
        return 1
    fi
    stop_session "$name"
    echo "Kész. Szabad RAM: $(get_mem_mb)MB"
}

# ── Help ──────────────────────────────────────────────────────────────────────

show_help() {
    cat <<'EOF'
SpaceOS Dispatcher v4 — Lifecycle Manager

  Indítás:
    sd --launch          Persistent session-ök (root + kernel)
    sd --start <név>         Egy on-demand session indítása (-c, folytatja)
    sd --stop-session <név>  Egy session leállítása
    sd --refresh <név>       Szerepkör frissítés: leáll + fresh start (nincs -c)
    sd --refresh-all         Minden futó session fresh restart (nincs -c)

  Dispatcher:
    sd --daemon          Háttérben futtatás
    sd --dry-run         Teszt mód
    sd                   Előtérben futtatás

  Root mód:
    sd --mode notify     Csak értesít
    sd --mode semi       Feldolgoz, megáll új task előtt
    sd --mode auto       Teljes automata

  Állapot:
    sd --status          Mi fut, RAM, UNREAD-ek
    sd --stop-all        Projekt terminálok leállítása (root megmarad)
    sd --kill-all        Minden session leállítása (root is)

  Egyéb:
    sd --stop            Dispatcher leállítás
    sd --help            Ez az üzenet

  Tmux:
    tm attach -t spaceos-kernel
    tm ls
    Ctrl+B, S → session váltó
    Ctrl+B, D → leválás
EOF
}

# ── Entry point ───────────────────────────────────────────────────────────────

main() {
    case "${1:-}" in
        --help|-h)       show_help; exit 0 ;;
        --status)        ensure_dirs; show_status; exit 0 ;;
        --stop)          stop_dispatcher; exit 0 ;;
        --kill-all)      kill_all_sessions; exit 0 ;;
        --stop-all)      stop_all_terminals; exit 0 ;;
        --launch)        ensure_dirs; launch_persistent; exit 0 ;;
        --start)
            ensure_dirs
            [[ -n "${2:-}" ]] || { echo "Használat: sd --start <terminál_név>"; exit 1; }
            manual_start "$2"; exit 0
            ;;
        --refresh)
            ensure_dirs
            [[ -n "${2:-}" ]] || { echo "Használat: sd --refresh <terminál_név>"; exit 1; }
            refresh_session "$2"; exit 0
            ;;
        --refresh-all)
            ensure_dirs
            refresh_all_sessions; exit 0
            ;;
        --stop-session)
            ensure_dirs
            [[ -n "${2:-}" ]] || { echo "Használat: sd --stop-session <terminál_név>"; exit 1; }
            manual_stop "$2"; exit 0
            ;;
        --mode)
            if [[ -n "${2:-}" && "$2" =~ ^(notify|semi|auto)$ ]]; then
                ensure_dirs; set_mode "$2"
                echo "Mód: $2"
            else
                echo "Használat: sd --mode [notify|semi|auto]"
                echo "Jelenlegi: $(get_mode)"
            fi
            exit 0
            ;;
        --dry-run)   DRY_RUN=true ;;
        --daemon)    DAEMON_MODE=true ;;
    esac

    ensure_dirs

    for cmd in inotifywait tmux; do
        command -v "$cmd" >/dev/null 2>&1 || { echo "Hiányzik: $cmd"; exit 1; }
    done

    if $DAEMON_MODE; then
        nohup "$0" > /dev/null 2>&1 &
        local pid=$!
        echo "$pid" > "$PID_FILE"
        echo "Dispatcher indítva (PID: $pid, mód: $(get_mode))"
        echo "Szabad RAM: $(get_mem_mb)MB"
        echo "Log: tail -f $LOG_FILE"
        exit 0
    fi

    trap 'log_info "Leállítva."; rm -f "$PID_FILE"; exit 0' SIGTERM SIGINT
    start_watching
}

main "$@"
