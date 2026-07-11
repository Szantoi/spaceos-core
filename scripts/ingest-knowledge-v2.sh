#!/usr/bin/env bash
# SpaceOS Knowledge Base Ingestion Script (v2)
# Simplified for loop version
# Usage: bash /opt/spaceos/scripts/ingest-knowledge-v2.sh

set -euo pipefail

DB_PORT=5433
DB_NAME="spaceos"
DOCS_ROOT="/opt/spaceos/docs"

echo "[INFO] Starting knowledge base ingestion at $(date -Iseconds)"
echo "[INFO] Scanning docs/ directory..."

count=0
success=0

# Build list of files (excluding patterns)
mapfile -t files < <(find "$DOCS_ROOT" -type f -name "*.md" \
    | grep -v -E '/(mailbox|planning|tasks)/')

echo "[INFO] Found ${#files[@]} files to process"

for file_path in "${files[@]}"; do
    count=$((count + 1))

    # Skip if file not readable
    if [[ ! -r "$file_path" ]]; then
        echo "⊘ ${file_path#/opt/spaceos/} (permission denied)"
        continue
    fi

    # Extract title
    title=$(grep -m 1 "^# " "$file_path" | sed 's/^# //' || basename "$file_path" .md)

    # Read content
    content=$(cat "$file_path")

    # Compute hash
    content_hash=$(echo -n "$content" | sha256sum | awk '{print $1}')

    # Word count
    word_count=$(echo "$content" | wc -w)

    # Determine category
    source_type="knowledge"
    category="NULL"
    terminal="NULL"

    if [[ "$file_path" == *"/architecture/"* ]]; then
        category="'architecture'"
    elif [[ "$file_path" == *"/vision/"* ]]; then
        category="'vision'"
    elif [[ "$file_path" == *"/knowledge/security/"* ]]; then
        category="'security'"
    elif [[ "$file_path" == *"/knowledge/deployment/"* ]]; then
        category="'deployment'"
    elif [[ "$file_path" == *"/knowledge/patterns/"* ]]; then
        category="'patterns'"
    elif [[ "$file_path" == *"/knowledge/context/"* ]]; then
        category="'context'"
    elif [[ "$file_path" == *"/knowledge/"* ]]; then
        # Extract subdirectory
        subdir=$(echo "$file_path" | sed -n 's|.*/knowledge/\([^/]*\)/.*|\1|p')
        if [[ -n "$subdir" ]]; then
            category="'$subdir'"
        else
            category="'knowledge'"
        fi
    elif [[ "$file_path" == */docs/* ]] && [[ "$file_path" != *"/knowledge/"* ]]; then
        category="'system'"
    fi

    # Relative path
    rel_path=$(echo "$file_path" | sed "s|^/opt/spaceos/||")

    # Escape single quotes
    content_escaped=$(echo "$content" | sed "s/'/''/g")
    title_escaped=$(echo "$title" | sed "s/'/''/g")

    # UPSERT
    if sudo -u postgres psql -p "$DB_PORT" -d "$DB_NAME" -v ON_ERROR_STOP=1 -q > /dev/null 2>&1 <<EOF
INSERT INTO knowledge.documents (file_path, source_type, category, terminal, title, content, content_hash, word_count)
VALUES ('$rel_path', '$source_type', $category, $terminal, '$title_escaped', '$content_escaped', '$content_hash', $word_count)
ON CONFLICT (file_path) DO UPDATE SET
    title = EXCLUDED.title,
    content = EXCLUDED.content,
    content_hash = EXCLUDED.content_hash,
    word_count = EXCLUDED.word_count,
    updated_at = now();
EOF
    then
        success=$((success + 1))
        echo "✓ $rel_path ($word_count words)"
    else
        echo "✗ $rel_path (SQL error)"
    fi
done

echo ""
echo "[INFO] Ingestion complete: $success/$count files successfully indexed"
echo ""

# Statistics
echo "📊 Documents by category:"
sudo -u postgres psql -p "$DB_PORT" -d "$DB_NAME" -t -c "
SELECT '  ' || source_type || '/' || COALESCE(category, 'root') || ': ' || COUNT(*) || ' documents'
FROM knowledge.documents
GROUP BY source_type, category
ORDER BY source_type, category;
"

echo ""
total=$(sudo -u postgres psql -p "$DB_PORT" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM knowledge.documents;")
echo "📚 Total documents: $total"
