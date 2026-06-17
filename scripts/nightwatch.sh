#!/bin/bash
# =============================================================================
# nightwatch.sh — SpaceOS dispatcher orchestrator
#
# Cron: */2 * * * * bash /opt/spaceos/scripts/nightwatch.sh >> /opt/spaceos/logs/dispatcher/nightwatch.log 2>&1
#
# Koordinál és biztosítja hogy a prioritásos session-ök (Root, Conductor) mindig futnak.
#
# Komponensek:
#   watch-priority.sh → Root + Conductor MINDIG fut (prioritásos)
#   watch-done.sh     → új DONE outboxok detektálása → reviewer indítás
#   watch-stuck.sh    → beakadt tmux session-ök → Enter küldés
#   watch-inbox.sh    → terminálok CSAK feladattal indulnak
# =============================================================================

SCRIPTS="$(dirname "$0")"

# 1. Prioritásos session-ök — Root + Conductor mindig fut
bash "$SCRIPTS/watch-priority.sh"

# 2. DONE feldolgozás → reviewer
bash "$SCRIPTS/watch-done.sh"

# 3. Beakadt session-ök → nudge
bash "$SCRIPTS/watch-stuck.sh"

# 4. Terminálok inbox-alapú indítás (csak ha van feladat)
bash "$SCRIPTS/watch-inbox.sh"

exit 0
