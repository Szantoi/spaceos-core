#!/bin/bash
# =============================================================================
# datahaven-ask.sh — Kérdés-válasz a SpaceOS Knowledge Base-ből
#
# Használat:
#   ./datahaven-ask.sh "Mi az RLS policy?"
#   ./datahaven-ask.sh -m "És hogyan kapcsolódik a JWT-hez?"
#
# Opciók:
#   -m, --with-memory   Előző beszélgetések kontextusa is bekerül
#
# Cache: 1 órán belül ugyanarra a kérdésre cache-ből válaszol
# =============================================================================

set -euo pipefail

KNOWLEDGE_URL="http://localhost:3456/api/knowledge/search"
MEMORY_FILE="/opt/spaceos/logs/datahaven-memory.jsonl"
CACHE_FILE="/opt/spaceos/logs/datahaven-cache.jsonl"
CACHE_TTL=3600  # 1 óra

# Színek
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

WITH_MEMORY=false
if [[ "${1:-}" == "--with-memory" ]] || [[ "${1:-}" == "-m" ]]; then
    WITH_MEMORY=true
    shift
fi

if [[ $# -lt 1 ]]; then
    echo -e "${YELLOW}Használat:${NC} $0 [--with-memory] \"<kérdés>\""
    echo ""
    echo "Opciók:"
    echo "  --with-memory, -m   Előző beszélgetések kontextusa is bekerül"
    echo ""
    echo "Példák:"
    echo "  $0 \"Mi az RLS policy?\""
    echo "  $0 -m \"És hogyan kapcsolódik a JWT-hez?\""
    exit 1
fi

QUESTION="$*"
QUESTION_HASH=$(echo -n "$QUESTION" | md5sum | cut -d' ' -f1)

echo -e "${CYAN}Kérdés:${NC} $QUESTION"
echo ""

# 0. Cache ellenőrzés
if [[ -f "$CACHE_FILE" ]]; then
    CACHED=$(grep "\"hash\":\"$QUESTION_HASH\"" "$CACHE_FILE" 2>/dev/null | tail -1)
    if [[ -n "$CACHED" ]]; then
        CACHE_TS=$(echo "$CACHED" | python3 -c "import sys,json; print(json.load(sys.stdin).get('ts',0))" 2>/dev/null || echo 0)
        NOW=$(date +%s)
        AGE=$((NOW - CACHE_TS))
        if [[ $AGE -lt $CACHE_TTL ]]; then
            echo -e "${GREEN}⚡ Cache találat (${AGE}s régi)${NC}"
            echo ""
            echo "$CACHED" | python3 -c "import sys,json; print(json.load(sys.stdin).get('response',''))" 2>/dev/null
            exit 0
        fi
    fi
fi

# 1. Keresés a Knowledge Service-ben
echo -e "${YELLOW}Keresés a tudásbázisban...${NC}"

SEARCH_RESULT=$(curl -s -X POST "$KNOWLEDGE_URL" \
    -H "Content-Type: application/json" \
    -d "{\"q\": \"$QUESTION\", \"limit\": 5}" 2>/dev/null)

if [[ -z "$SEARCH_RESULT" ]]; then
    echo "Hiba: Knowledge Service nem elérhető (localhost:3456)"
    exit 1
fi

if echo "$SEARCH_RESULT" | grep -q '"error":'; then
    echo "Hiba: $(echo "$SEARCH_RESULT" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("error","ismeretlen"))')"
    exit 1
fi

# 2. Kontextus kinyerése
CONTEXT=$(echo "$SEARCH_RESULT" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    results = data.get('results', [])
    if not results:
        print('Nincs találat.')
    else:
        for r in results[:5]:
            meta = r.get('metadata', {})
            name = meta.get('name', 'N/A')
            source = meta.get('source', '')
            text = r.get('text', '')[:1500]
            score = r.get('score', 0)
            print(f'### {name} (relevancia: {score:.2f})')
            print(f'Forrás: {source}')
            print(text)
            print()
            print('---')
            print()
except Exception as e:
    print(f'Parse hiba: {e}')
" 2>/dev/null)

if [[ "$CONTEXT" == "Nincs találat." ]] || [[ -z "$CONTEXT" ]]; then
    echo "Nincs releváns dokumentum a tudásbázisban."
    exit 0
fi

echo -e "${GREEN}Találatok feldolgozása...${NC}"
echo ""

# 3. Memory kontextus (opcionális)
MEMORY_CONTEXT=""
if [[ "$WITH_MEMORY" == "true" ]] && [[ -f "$MEMORY_FILE" ]]; then
    RECENT_MEMORIES=$(tail -5 "$MEMORY_FILE" | python3 -c "
import sys, json
for line in sys.stdin:
    try:
        d = json.loads(line.strip())
        q = d.get('q', '')
        m = d.get('memory', '')
        if q and m:
            print(f'- Kérdés: {q}')
            print(f'  Válasz: {m}')
    except: pass
" 2>/dev/null)
    if [[ -n "$RECENT_MEMORIES" ]]; then
        MEMORY_CONTEXT="
Előző beszélgetések (emlékezz rájuk):
$RECENT_MEMORIES
"
        echo -e "${YELLOW}+ Memory kontextus betöltve (utolsó 5)${NC}"
    fi
fi

# 4. Claude válaszol
PROMPT="A felhasználó kérdése: $QUESTION
$MEMORY_CONTEXT
Az alábbi dokumentumok relevánsak a SpaceOS tudásbázisból:

$CONTEXT

Válaszolj a kérdésre a fenti kontextus alapján. Legyél tömör és konkrét. Ha a kontextus nem tartalmazza a választ, mondd meg.

A válaszod VÉGÉN adj egy rövid memory bejegyzést JSON formátumban:
\`\`\`json
{\"memory\": \"<1-2 mondatos összefoglaló amit megjegyeztél>\"}
\`\`\`"

echo -e "${CYAN}Válasz:${NC}"
echo ""

RESPONSE=$(claude -p "$PROMPT" --model haiku 2>/dev/null) || {
    echo "Hiba: Claude CLI nem elérhető"
    exit 1
}

echo "$RESPONSE"

# 5. Memory kinyerése és mentése
MEMORY_TEXT=$(echo "$RESPONSE" | grep -oP '"memory":\s*"[^"]*"' | tail -1 | sed 's/"memory":\s*"//' | sed 's/"$//')
Q_ESCAPED=$(echo "$QUESTION" | sed 's/"/\\"/g')
if [[ -n "$MEMORY_TEXT" ]]; then
    TIMESTAMP=$(date -Iseconds)
    M_ESCAPED=$(echo "$MEMORY_TEXT" | sed 's/"/\\"/g')
    echo "{\"ts\":\"$TIMESTAMP\",\"q\":\"$Q_ESCAPED\",\"memory\":\"$M_ESCAPED\"}" >> "$MEMORY_FILE"
    echo ""
    echo -e "${GREEN}✓ Memory mentve${NC}"
fi

# 6. Cache mentés
NOW_TS=$(date +%s)
RESPONSE_ESCAPED=$(echo "$RESPONSE" | python3 -c "import sys,json; print(json.dumps(sys.stdin.read()))" 2>/dev/null | sed 's/^"//;s/"$//')
echo "{\"ts\":$NOW_TS,\"hash\":\"$QUESTION_HASH\",\"q\":\"$Q_ESCAPED\",\"response\":\"$RESPONSE_ESCAPED\"}" >> "$CACHE_FILE"
