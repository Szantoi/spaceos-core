#!/bin/bash
# =============================================================================
# pipeline-docs.sh — Dokumentáció frissítés + következő task (claude)
#
# Egyetlen felelőssége:
#   Claude Sonnet futtatása hogy frissítse README.md + Codebase_Status.md
#   és meghatározza + létrehozza a következő inbox üzenetet.
#
# Args: $1 = done_file  $2 = terminal
# Output (stdout utolsó sora): PIPELINE_RESULT: DONE|NEXT:<file_vagy_NONE>|TESTS:<n>
# Hívja: pipeline.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

DONE_FILE="$1"
TERMINAL="$2"

DONE_CONTENT=$(cat "$DONE_FILE")
README_CONTENT=$(cat "$SPACEOS_ROOT/docs/tasks/README.md")
STATUS_CONTENT=$(head -10 "$SPACEOS_ROOT/docs/Codebase_Status.md")
DOMAIN_MATRIX=$(head -50 "$SPACEOS_ROOT/docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md" 2>/dev/null || echo "")

PROMPT="# SpaceOS Auto Pipeline — Dokumentáció frissítés

Te a SpaceOS Root terminál automatizált pipeline-ja vagy.
A reviewerek jóváhagytak egy DONE-t — frissítsd a dokumentációt és határozd meg a következő feladatot.

## DONE üzenet (elfogadott):
${DONE_CONTENT}

## Jelenlegi README.md:
${README_CONTENT}

## Codebase_Status fejléc:
${STATUS_CONTENT}

## FE Domain Matrix:
${DOMAIN_MATRIX}

## Feladatod:

### A. README.md frissítés
Fájl: $SPACEOS_ROOT/docs/tasks/README.md
- A DONE task sorát frissítsd ✅-re
- Következő kiadható task: add hozzá 🔵-vel

### B. Codebase_Status.md fejléc
Fájl: $SPACEOS_ROOT/docs/Codebase_Status.md
- Első sor ('Utolsó frissítés'): mai dátum + 1 soros összefoglaló

### C. Következő inbox üzenet
A folyamatban lévő roadmap alapján határozd meg a következő task-ot.

**Routing szabály:**
- Ha a DONE üzenet 'assignee: nexus' vagy Datahaven/Resonance témájú → inbox: $SPACEOS_ROOT/docs/mailbox/nexus/inbox/
- Minden más esetben → inbox: $SPACEOS_ROOT/docs/mailbox/conductor/inbox/
  (A Conductor koordinálja a terminálokat, NEM közvetlenül a termináloknak megy az inbox)

- Ha nincs következő task → jelezd: 'NEXT: NONE'
- Ha van → hozz létre a megfelelő mailboxba: YYYY-MM-DD_NNN_slug.md
  Frontmatter: id, from: pipeline, to: <terminál>, type: task, priority, status: UNREAD, model: sonnet, ref, created

## Utolsó sor (kötelező, parsing-hoz):
PIPELINE_RESULT: DONE|NEXT:<inbox_fajlnev_vagy_NONE>|TESTS:<tesztszam>"

echo "$PROMPT" | claude -p --model sonnet \
  --allowedTools "Edit,Write,Read,Bash" \
  2>/dev/null

echo "$TIMESTAMP Docs frissítve: $(basename $DONE_FILE)" >> "$LOG_DIR/pipeline.log"
