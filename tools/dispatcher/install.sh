#!/usr/bin/env bash
set -euo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
INSTALL_DIR="$SPACEOS_ROOT/tools/dispatcher"

echo "SpaceOS Dispatcher v4 — Telepítés"
echo "══════════════════════════════════"

# Függőségek
for cmd_pkg in "inotifywait:inotify-tools" "tmux:tmux"; do
    cmd="${cmd_pkg%%:*}"; pkg="${cmd_pkg#*:}"
    if command -v "$cmd" >/dev/null 2>&1; then
        echo "  ✅ $cmd"
    else
        echo "  → $pkg telepítése..."
        apt-get install -y "$pkg"
    fi
done

command -v claude >/dev/null 2>&1 && echo "  ✅ claude" || { echo "  ❌ claude hiányzik"; exit 1; }

# Könyvtárak
mkdir -p "$INSTALL_DIR" "$SPACEOS_ROOT/logs/dispatcher"
for t in kernel orchestrator portal joinery abstractions e2e infra identity; do
    mkdir -p "$SPACEOS_ROOT/docs/mailbox/$t/inbox" "$SPACEOS_ROOT/docs/mailbox/$t/outbox"
done

# Default mód
[[ -f "$INSTALL_DIR/mode" ]] || echo "notify" > "$INSTALL_DIR/mode"

# Másolás
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ "$SCRIPT_DIR" != "$INSTALL_DIR" ]]; then
    cp "$SCRIPT_DIR/spaceos-dispatcher.sh" "$INSTALL_DIR/"
fi
chmod +x "$INSTALL_DIR/spaceos-dispatcher.sh"

# Jogok
chown -R gabor:gabor "$INSTALL_DIR"
chown -R gabor:gabor "$SPACEOS_ROOT/logs/dispatcher"

# Alias-ok
ALIASES='
# SpaceOS Dispatcher v4
alias sd="/opt/spaceos/tools/dispatcher/spaceos-dispatcher.sh"
alias sd-status="sd --status"
alias sd-stop="sd --stop"
alias sd-dry="sd --dry-run"
alias sd-log="tail -f /opt/spaceos/logs/dispatcher/dispatcher.log"
alias sd-ls="tmux -S /tmp/spaceos.tmux ls 2>/dev/null || echo \"Nincs session\""
alias sd-mode="sd --mode"
alias tm="tmux -S /tmp/spaceos.tmux"
'

for bashrc in /home/gabor/.bashrc /root/.bashrc; do
    if [[ -f "$bashrc" ]]; then
        sed -i '/# SpaceOS Dispatcher/,/^$/d' "$bashrc" 2>/dev/null || true
        echo "$ALIASES" >> "$bashrc"
        echo "  ✅ Alias-ok → $bashrc"
    fi
done

echo ""
echo "✅ Kész!"
echo ""
echo "  Indítás:"
echo "    source ~/.bashrc"
echo "    sd --launch        # root + kernel (persistent)"
echo "    sd --daemon        # dispatcher háttérben"
echo ""
echo "  On-demand session-ök automatikusan indulnak inbox-ra."
echo "  DONE/BLOCKED után automatikusan leállnak."
echo ""
echo "  RAM: $(free -m | awk '/^Mem:/ {print $7}')MB szabad"
