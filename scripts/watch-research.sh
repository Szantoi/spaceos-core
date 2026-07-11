#!/bin/bash
# =============================================================================
# watch-research.sh — Autonóm kutatási rendszer
#
# Periodikusan elindítja az Explorer terminált kutatási feladattal,
# majd az eredményeket továbbítja a Librarian-nak szintetizálásra.
#
# Használat: crontab -e → */60 * * * * /opt/spaceos/scripts/watch-research.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

STATE_FILE="$SPACEOS_ROOT/scripts/.research-state"
LOG_FILE="$LOG_DIR/research.log"
INBOX_DIR="$SPACEOS_ROOT/terminals"

# Kutatási témák rotációja
RESEARCH_TOPICS=(
  "chat_history_mining"    # Chat history bányászat az elmúlt 24 órából
  "codebase_changes"       # Kódbázis változások elemzése (git log)
  "done_patterns"          # DONE outboxok mintakeresése
  "external_trends"        # Külső tech trendek (web search)
)

log() {
  echo "$(date '+%Y-%m-%d %H:%M:%S') [RESEARCH] $1" >> "$LOG_FILE"
}

# Olvassa az utolsó kutatási témát
get_last_topic_index() {
  if [ -f "$STATE_FILE" ]; then
    cat "$STATE_FILE" | grep "^last_topic_index=" | cut -d= -f2
  else
    echo "0"
  fi
}

# Menti az utolsó kutatási témát
save_topic_index() {
  echo "last_topic_index=$1" > "$STATE_FILE"
  echo "last_run=$(date +%s)" >> "$STATE_FILE"
}

# Ellenőrzi, hogy az Explorer fut-e
is_explorer_running() {
  tmux_s has-session -t spaceos-explorer 2>/dev/null
}

# Ellenőrzi, hogy az Explorer IDLE-e (nincs aktív inbox)
is_explorer_idle() {
  local unread=$(grep -rl "status: UNREAD" "$INBOX_DIR/explorer/inbox/" 2>/dev/null | wc -l)
  [ "$unread" -eq 0 ]
}

# Létrehozza a kutatási inbox üzenetet
create_research_inbox() {
  local topic="$1"
  local today=$(date +%Y-%m-%d)
  local seq=$(ls "$INBOX_DIR/explorer/inbox/" 2>/dev/null | wc -l)
  seq=$((seq + 1))
  local filename="${today}_$(printf '%03d' $seq)_autonomous-research-${topic}.md"
  local filepath="$INBOX_DIR/explorer/inbox/$filename"

  local content=""

  case "$topic" in
    chat_history_mining)
      content="---
id: MSG-EXPLORER-AUTO-$(date +%s)
from: root
to: explorer
type: task
priority: low
status: UNREAD
model: haiku
created: $today
auto_generated: true
---

# Autonóm Kutatás: Chat History Bányászat

## Kontextus
Ez egy automatikusan generált kutatási feladat. Célod az elmúlt 24 óra chat history-jának elemzése.

## Kutatási feladatok

1. **Legutóbbi conversation fájlok azonosítása:**
   \`\`\`bash
   ls -lt ~/.claude/projects/-opt-spaceos/*.jsonl | head -5
   \`\`\`

2. **Mintakeresés a legutóbbi session-ökben:**
   - Milyen problémák merültek fel?
   - Milyen megoldások születtek?
   - Van-e ismétlődő téma?

3. **Git log elemzés:**
   \`\`\`bash
   git -C /opt/spaceos log --oneline --since=\"24 hours ago\"
   \`\`\`

## Elvárt output

DONE outbox-ban:
- 2-3 azonosított minta vagy tanulság
- Javaslat a Librarian-nak szintetizálásra
- Ha nincs érdekes eredmény, azt is jelezd röviden
"
      ;;

    codebase_changes)
      content="---
id: MSG-EXPLORER-AUTO-$(date +%s)
from: root
to: explorer
type: task
priority: low
status: UNREAD
model: haiku
created: $today
auto_generated: true
---

# Autonóm Kutatás: Kódbázis Változások

## Kontextus
Ez egy automatikusan generált kutatási feladat. Célod az utolsó 24 óra kódbázis változásainak elemzése.

## Kutatási feladatok

1. **Git log elemzés:**
   \`\`\`bash
   git -C /opt/spaceos log --oneline --since=\"24 hours ago\" --stat
   \`\`\`

2. **Mely modulok változtak?**
   - backend/ változások
   - frontend/ változások
   - docs/ változások

3. **Van-e új pattern vagy refactor?**
   - Új fájlok létrehozása
   - Jelentős átstrukturálás

## Elvárt output

DONE outbox-ban:
- Összefoglaló a fő változásokról
- Új minták azonosítása (ha van)
- Javaslat dokumentálásra
"
      ;;

    done_patterns)
      content="---
id: MSG-EXPLORER-AUTO-$(date +%s)
from: root
to: explorer
type: task
priority: low
status: UNREAD
model: haiku
created: $today
auto_generated: true
---

# Autonóm Kutatás: DONE Outbox Minták

## Kontextus
Ez egy automatikusan generált kutatási feladat. Célod a legutóbbi DONE outbox üzenetek elemzése.

## Kutatási feladatok

1. **Mai/tegnapi DONE üzenetek keresése:**
   \`\`\`bash
   find /opt/spaceos/terminals/*/outbox/ -name \"*.md\" -mtime -1 | xargs grep -l \"type: done\" 2>/dev/null
   \`\`\`

2. **Mintakeresés a DONE üzenetekben:**
   - Milyen típusú feladatok készültek el?
   - Van-e közös minta a megoldásokban?
   - Milyen teszteket futtattak?

3. **BLOCKED üzenetek ellenőrzése:**
   - Van-e megoldatlan blocker?
   - Mi a blocker oka?

## Elvárt output

DONE outbox-ban:
- 2-3 azonosított implementációs minta
- Javaslat knowledge doc frissítésre
- BLOCKED-ok listája (ha van)
"
      ;;

    external_trends)
      content="---
id: MSG-EXPLORER-AUTO-$(date +%s)
from: root
to: explorer
type: task
priority: low
status: UNREAD
model: haiku
created: $today
auto_generated: true
---

# Autonóm Kutatás: Külső Tech Trendek

## Kontextus
Ez egy automatikusan generált kutatási feladat. Célod releváns tech trendek keresése.

## Kutatási témák (válassz 1-2-t)

### .NET 8 / C#
- Minimal API best practices
- EF Core 8 újdonságok
- Clean Architecture patterns

### React 18/19
- Server Components
- Zustand state management
- TanStack Query patterns

### PostgreSQL
- RLS performance tips
- Multi-tenant patterns

### Faipari SaaS
- Konkurens megoldások (CutList Plus, Cabinet Vision)
- Manufacturing SaaS trendek

## Elvárt output

DONE outbox-ban:
- 1-2 releváns külső forrás link
- Rövid összefoglaló a tanulságokról
- Javaslat a Librarian olvasólistájához
"
      ;;
  esac

  echo "$content" > "$filepath"
  log "Created research inbox: $filename (topic: $topic)"
  echo "$filepath"
}

# Ellenőrzi az Explorer DONE outbox-át és továbbítja Librarian-nak
check_explorer_done_and_forward() {
  local today=$(date +%Y-%m-%d)
  local done_files=$(find "$INBOX_DIR/explorer/outbox/" -name "*.md" -mtime -1 2>/dev/null | xargs grep -l "auto_generated: true" 2>/dev/null | xargs grep -l "type: done" 2>/dev/null)

  for done_file in $done_files; do
    local forwarded_marker="$done_file.forwarded"
    if [ ! -f "$forwarded_marker" ]; then
      # Librarian inbox létrehozása
      local seq=$(ls "$INBOX_DIR/librarian/inbox/" 2>/dev/null | wc -l)
      seq=$((seq + 1))
      local lib_filename="${today}_$(printf '%03d' $seq)_auto-synthesis-from-explorer.md"
      local lib_filepath="$INBOX_DIR/librarian/inbox/$lib_filename"

      cat > "$lib_filepath" << EOF
---
id: MSG-LIBRARIAN-AUTO-$(date +%s)
from: root
to: librarian
type: task
priority: low
status: UNREAD
model: haiku
ref: $(basename "$done_file" .md)
created: $today
auto_generated: true
---

# Autonóm Szintetizálás: Explorer Eredmények

## Kontextus
Az Explorer terminál befejezte az autonóm kutatást. Kérlek szintetizáld az eredményeket.

## Explorer DONE
$(cat "$done_file")

## Feladatok

1. **Értékeld az Explorer eredményeit:**
   - Van-e szintetizálásra érdemes minta?
   - Releváns-e a SpaceOS kontextusában?

2. **Ha releváns:**
   - Frissítsd a megfelelő knowledge doc-ot
   - Vagy hozz létre olvasólista bejegyzést

3. **Ha nem releváns:**
   - Jelezd röviden miért

## Elvárt output

DONE outbox-ban:
- Mit szintetizáltál (ha valamit)
- Melyik knowledge doc frissült
- Ha nem volt szintetizálnivaló, miért
EOF

      touch "$forwarded_marker"
      log "Forwarded Explorer DONE to Librarian: $lib_filename"
    fi
  done
}

# -----------------------------------------------------------------------------
# MAIN
# -----------------------------------------------------------------------------

log "=== Research watch started ==="

# 1. Ellenőrizd, hogy az Explorer IDLE-e
if ! is_explorer_idle; then
  log "Explorer has UNREAD inbox, skipping research trigger"
  exit 0
fi

# 2. Ellenőrizd az Explorer DONE-jait és továbbítsd Librarian-nak
check_explorer_done_and_forward

# 3. Rotáld a kutatási témát
last_idx=$(get_last_topic_index)
next_idx=$(( (last_idx + 1) % ${#RESEARCH_TOPICS[@]} ))
topic="${RESEARCH_TOPICS[$next_idx]}"

log "Selected research topic: $topic (index: $next_idx)"

# 4. Hozz létre inbox üzenetet az Explorer-nek
inbox_file=$(create_research_inbox "$topic")

# 5. Mentsd az állapotot
save_topic_index "$next_idx"

# 6. Telegram értesítés (opcionális, csak ha van topic)
# tg "🔬 *Autonóm kutatás* indult: \`$topic\`"

log "=== Research watch completed ==="
