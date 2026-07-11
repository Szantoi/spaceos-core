#!/bin/bash
# Phase Dispatch Automation
# Purpose: Automatic sequential phase dispatch when prior phase DONE
# Impact: VERY HIGH (eliminates manual dispatch, consistent formatting)
# Difficulty: MEDIUM
# Author: Explorer Task Research (2026-07-07)
# Usage: Run via cron every 30 min or event-driven (on DONE message creation)

set -euo pipefail

# Configuration
TERMINAL_BASE="/opt/spaceos/terminals"
CONDUCTOR_INBOX="$TERMINAL_BASE/conductor/inbox"
CONDUCTOR_OUTBOX="$TERMINAL_BASE/conductor/outbox"
BACKEND_INBOX="$TERMINAL_BASE/backend/inbox"
BACKEND_OUTBOX="$TERMINAL_BASE/backend/outbox"
LOG_FILE="/opt/spaceos/logs/phase-dispatch.log"

# Current project context (TODO: load from config)
PROJECT="joinerytech-migration"
CURRENT_WEEK=2

# Phase definitions (TODO: load from YAML config)
declare -A PHASES
PHASES[1]="QA Integration Testing"
PHASES[2]="CRM Integration Testing"
PHASES[3]="DMS Application Layer"
PHASES[4]="HR Application Layer"
PHASES[5]="Maintenance Application Layer"
PHASES[6]="QA Application Layer"

declare -A PHASE_DEPS
PHASE_DEPS[1]=""  # No dependencies
PHASE_DEPS[2]="1"  # Depends on Phase 1
PHASE_DEPS[3]="2"  # Depends on Phase 2
PHASE_DEPS[4]="2"  # Depends on Phase 2
PHASE_DEPS[5]="2"  # Depends on Phase 2
PHASE_DEPS[6]="2"  # Depends on Phase 2

declare -A PHASE_TERMINALS
PHASE_TERMINALS[1]="backend"
PHASE_TERMINALS[2]="backend"
PHASE_TERMINALS[3]="backend"
PHASE_TERMINALS[4]="backend"
PHASE_TERMINALS[5]="backend"
PHASE_TERMINALS[6]="backend"

declare -A PHASE_NWT
PHASE_NWT[1]="2-3"
PHASE_NWT[2]="3-4"
PHASE_NWT[3]="4-6"
PHASE_NWT[4]="4-6"
PHASE_NWT[5]="4-6"
PHASE_NWT[6]="4-6"

# Logging
log() {
  echo "[$(date +%Y-%m-%d\ %H:%M:%S)] $1" | tee -a "$LOG_FILE"
}

# Check if phase is complete
is_phase_complete() {
  local phase=$1
  local terminal=${PHASE_TERMINALS[$phase]}
  local pattern="week${CURRENT_WEEK}-phase${phase}"

  # Count DONE messages for this phase
  local done_count=$(grep -l "type: done" "$TERMINAL_BASE/$terminal/outbox/"*"$pattern"* 2>/dev/null | wc -l)

  # Expected: 1 DONE message per phase (adjust if multiple tasks per phase)
  if [ $done_count -ge 1 ]; then
    return 0  # Complete
  else
    return 1  # Not complete
  fi
}

# Check if dependencies are met
dependencies_met() {
  local phase=$1
  local deps=${PHASE_DEPS[$phase]}

  if [ -z "$deps" ]; then
    return 0  # No dependencies
  fi

  # Check all dependency phases
  for dep in ${deps//,/ }; do
    if ! is_phase_complete "$dep"; then
      log "⏳ Phase $phase blocked: waiting for Phase $dep"
      return 1
    fi
  done

  return 0
}

# Check if phase already dispatched
is_phase_dispatched() {
  local phase=$1
  local terminal=${PHASE_TERMINALS[$phase]}
  local pattern="week${CURRENT_WEEK}-phase${phase}"

  # Check if inbox message already exists
  if ls "$TERMINAL_BASE/$terminal/inbox/"*"$pattern"* 1> /dev/null 2>&1; then
    return 0  # Already dispatched
  else
    return 1  # Not dispatched
  fi
}

# Get next message ID for terminal
get_next_msg_id() {
  local terminal=$1
  local inbox_path="$TERMINAL_BASE/$terminal/inbox"

  # Find highest message number
  local last_msg=$(ls -1 "$inbox_path"/*.md 2>/dev/null | grep -oP 'MSG-[A-Z]+-\K\d+' | sort -n | tail -1)
  local next_id=$((last_msg + 1))

  printf "%03d" "$next_id"
}

# Dispatch phase
dispatch_phase() {
  local phase=$1
  local terminal=${PHASE_TERMINALS[$phase]}
  local phase_name=${PHASES[$phase]}
  local nwt=${PHASE_NWT[$phase]}
  local msg_id=$(get_next_msg_id "$terminal")
  local terminal_upper=$(echo "$terminal" | tr '[:lower:]' '[:upper:]')

  local filename="$(date +%Y-%m-%d)_${msg_id}_week${CURRENT_WEEK}-phase${phase}-priority${phase}.md"
  local filepath="$TERMINAL_BASE/$terminal/inbox/$filename"

  log "🎯 Dispatching Phase $phase: $phase_name → $terminal (MSG-$terminal_upper-$msg_id)"

  # Create inbox message
  cat > "$filepath" <<EOF
---
id: MSG-$terminal_upper-$msg_id
from: conductor
to: $terminal
type: task
priority: high
status: UNREAD
model: sonnet
ref: WEEK-$CURRENT_WEEK-PHASE-$phase
created: $(date +%Y-%m-%d)
---

# Week $CURRENT_WEEK Phase $phase: $phase_name

## Context
This is **Priority $phase** in Week $CURRENT_WEEK Phase dispatch.

**Dependencies:** ${PHASE_DEPS[$phase]:-None}
**Estimated Effort:** $nwt NWT

## Acceptance Criteria
- [ ] FSM tests (5-10 tests)
- [ ] Repository tests (8-15 tests)
- [ ] E2E smoke tests (6-10 tests)
- [ ] RLS validation (3-5 tests)
- [ ] Application layer implementation
- [ ] API endpoints functional

## Next Steps
After DONE, Phase $((phase + 1)) may auto-dispatch (if dependencies met).

## Reference Documentation
- Domain model: \`docs/joinerytech/[domain]/domain-model.md\`
- Integration spec: \`docs/joinerytech/[domain]/integration-spec.md\`
- Test patterns: \`.claude/skills/joinerytech-domain-model-workshop/SKILL.md\`

---

**Dispatched by:** auto-phase-transition.sh
**Timestamp:** $(date -u +%Y-%m-%dT%H:%M:%SZ)
EOF

  log "✅ Phase $phase dispatched: $filename"

  # Notify Datahaven (optional)
  if command -v curl &> /dev/null; then
    curl -X POST "https://datahaven.joinerytech.hu/api/dispatch/notify" \
      -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
      -H "Content-Type: application/json" \
      -d "{\"phase\": $phase, \"terminal\": \"$terminal\", \"messageId\": \"MSG-$terminal_upper-$msg_id\"}" \
      --silent --show-error || true
  fi
}

# Main dispatch loop
log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log "🚀 Phase Dispatch Automation - Week $CURRENT_WEEK - $(date +%Y-%m-%d\ %H:%M:%S)"
log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for phase in {1..6}; do
  # Skip if already dispatched
  if is_phase_dispatched "$phase"; then
    log "ℹ️  Phase $phase: already dispatched"
    continue
  fi

  # Check dependencies
  if dependencies_met "$phase"; then
    dispatch_phase "$phase"
  else
    log "⏸️  Phase $phase: waiting for dependencies (${PHASE_DEPS[$phase]})"
  fi
done

log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log "✅ Phase dispatch check complete"
