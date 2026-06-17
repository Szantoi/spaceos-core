#!/bin/bash
# =============================================================================
# cron-librarian.sh — Librarian periodikus ütemezés
#
# Cron: 0 */5 * * * SPACEOS_ROOT=/opt/spaceos bash /opt/spaceos/scripts/cron-librarian.sh
#
# Egyetlen felelőssége:
#   5 óránként létrehoz egy inbox üzenetet a Librarian-nak ha nincs már UNREAD.
#   Feladatok:
#     1. Terminál memóriák kiolvasása + stale bejegyzések törlése
#     2. Értékes minták szintetizálása docs/knowledge/-be
#     3. DONE outbox küldés az eredményről
# =============================================================================

source "$(dirname "$0")/common.sh"

CLAUDE_PROJ="/home/gabor/.claude/projects"
LIBRARIAN_INBOX="$SPACEOS_ROOT/docs/mailbox/librarian/inbox"
mkdir -p "$LIBRARIAN_INBOX"

# Ha már van UNREAD librarian inbox → nem kell új
if grep -rl "status: UNREAD" "$LIBRARIAN_INBOX/" 2>/dev/null | grep -q .; then
  echo "$TIMESTAMP Librarian skip — már van UNREAD inbox" >> "$LOG_DIR/pipeline.log"
  exit 0
fi

DATE=$(date +%Y-%m-%d)
LAST_NUM=$(ls "$LIBRARIAN_INBOX"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
NEXT_NUM=$(printf "%03d" $(( ${LAST_NUM:-0} + 1 )))
LIB_FILE="${LIBRARIAN_INBOX}/${DATE}_${NEXT_NUM}_memory-sync.md"

cat > "$LIB_FILE" <<EOF
---
id: MSG-LIBRARIAN-${NEXT_NUM}
from: cron
to: librarian
type: task
priority: low
status: UNREAD
model: haiku
created: ${DATE}
---

# Librarian — 5 óránkénti memória szinkron

Olvasd el a CLAUDE.md-edet a részletes szabályokért.

## 1. Memória tisztítás

Menj végig ezeken a memória mappákon:

**Aktív terminálok (új útvonalak):**
- \`${CLAUDE_PROJ}/-opt-spaceos-frontend-joinerytech-portal/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-backend-spaceos-kernel/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-backend-spaceos-orchestrator/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-backend-spaceos-modules-joinery/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-infra/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-e2e/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-spaceos-architect/memory/\`

**Régi útvonalak (valószínűleg stale tartalom):**
- \`${CLAUDE_PROJ}/-opt-spaceos-SpaceOS-Kerner/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-spaceos-orchestrator/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-spaceos-modules-joinery/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-spaceos-doorstar-portal/memory/\`
- \`${CLAUDE_PROJ}/-opt-spaceos-design-portal/memory/\`

**Minden mappában:**
- Olvasd el a MEMORY.md indexet
- Minden \`project_*.md\` fájlnál: ha CLOSED_DONE vagy régi sprint → töröld
- Értékes tartalmat (VPS gotcha, arch döntés, security pattern) előbb mentsd
- \`user_*.md\` és \`feedback_*.md\` → NE töröld, ezek kellenek a terminálnak
- Törlés után: frissítsd a MEMORY.md indexet

## 2. Szintetizálás → docs/knowledge/

Ha törlés előtt értékes tartalmat találtál:
- VPS / deploy csapda → \`${SPACEOS_ROOT}/docs/knowledge/deployment/KNOWN_GOTCHAS.md\`
- Arch döntés → \`${SPACEOS_ROOT}/docs/knowledge/architecture/ADR_CATALOGUE.md\`
- Security minta → \`${SPACEOS_ROOT}/docs/knowledge/security/SECURITY_PATTERNS.md\`
- DB / migration minta → \`${SPACEOS_ROOT}/docs/knowledge/patterns/DATABASE_PATTERNS.md\`
- Terminál összefoglaló → \`${SPACEOS_ROOT}/docs/knowledge/context/<TERMINÁL>_CONTEXT.md\`

Ha a knowledge fájl még nem létezik → hozd létre.
Frissítsd \`${SPACEOS_ROOT}/docs/knowledge/INDEX.md\`-t ha új doc született.

## 3. DONE outbox

Amikor kész vagy, küldj DONE outbox üzenetet:
\`${SPACEOS_ROOT}/docs/mailbox/librarian/outbox/${DATE}_${NEXT_NUM}_librarian-done.md\`

Frontmatter: id: MSG-LIBRARIAN-${NEXT_NUM}-DONE, from: librarian, to: root,
type: done, priority: low, status: UNREAD, ref: MSG-LIBRARIAN-${NEXT_NUM}, created: ${DATE}

Tartalom: mit töröltél, mit szintetizáltál, hány fájl maradt.
EOF

echo "$TIMESTAMP Librarian inbox létrehozva: $(basename $LIB_FILE)" >> "$LOG_DIR/pipeline.log"
tg "📚 *Librarian — memória szinkron indul* (5 óránkénti cron)"
