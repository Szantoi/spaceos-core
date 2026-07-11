#!/usr/bin/env bash
# SpaceOS Knowledge Base Ingestion Script (Simplified Bash version)
# Indexes docs/ directory into knowledge.documents table
# Usage: bash /opt/spaceos/scripts/ingest-knowledge-simple.sh

set -euo pipefail

DB_CONN="postgresql:///spaceos?host=/var/run/postgresql&port=5433&user=postgres"
DOCS_ROOT="/opt/spaceos/docs"

# Excluded paths (glob patterns)
EXCLUDE_PATTERNS=("mailbox" "planning" "tasks")

echo "[INFO] Starting knowledge base ingestion at $(date -Iseconds)"
echo "[INFO] Scanning docs/ directory..."

count=0
success=0

# Find all .md files
while IFS= read -r -d '' file_path; do
    # Check if file matches any exclude pattern
    skip=0
    for pattern in "${EXCLUDE_PATTERNS[@]}"; do
        if [[ "$file_path" == *"/$pattern/"* ]]; then
            skip=1
            break
        fi
    done

    if [[ $skip -eq 1 ]]; then
        continue
    fi

    ((count++))

    # Extract title (first # heading or filename)
    title=$(grep -m 1 "^# " "$file_path" | sed 's/^# //' || basename "$file_path" .md)

    # Read file content
    content=$(cat "$file_path")

    # Compute SHA-256 hash
    content_hash=$(echo -n "$content" | sha256sum | awk '{print $1}')

    # Word count
    word_count=$(echo "$content" | wc -w)

    # Determine source_type and category
    source_type="knowledge"
    category="NULL"
    terminal="NULL"

    if [[ "$file_path" == *"/architecture/"* ]]; then
        category="'architecture'"
    elif [[ "$file_path" == *"/vision/"* ]]; then
        category="'vision'"
    elif [[ "$file_path" == *"/knowledge/"* ]]; then
        # Extract subdirectory
        subdir=$(echo "$file_path" | sed -n 's|.*/knowledge/\([^/]*\)/.*|\1|p')
        if [[ -n "$subdir" ]]; then
            category="'$subdir'"
        fi
    elif [[ "$file_path" == *"/docs/"* ]] && [[ "$file_path" != *"/knowledge/"* ]] && [[ "$file_path" != *"/architecture/"* ]] && [[ "$file_path" != *"/vision/"* ]]; then
        category="'system'"
    fi

    # Relative path
    rel_path=$(echo "$file_path" | sed "s|^/opt/spaceos/||")

    # Escape single quotes in content for SQL
    content_escaped=$(echo "$content" | sed "s/'/''/g")
    title_escaped=$(echo "$title" | sed "s/'/''/g")

    # UPSERT into PostgreSQL
    sudo -u postgres psql -p 5433 -d spaceos -v ON_ERROR_STOP=1 > /dev/null 2>&1 <<EOF
INSERT INTO knowledge.documents (file_path, source_type, category, terminal, title, content, content_hash, word_count)
VALUES ('$rel_path', '$source_type', $category, $terminal, '$title_escaped', '$content_escaped', '$content_hash', $word_count)
ON CONFLICT (file_path) DO UPDATE SET
    title = EXCLUDED.title,
    content = EXCLUDED.content,
    content_hash = EXCLUDED.content_hash,
    word_count = EXCLUDED.word_count,
    updated_at = now();
EOF

    if [[ $? -eq 0 ]]; then
        ((success++))
        echo "✓ $rel_path ($word_count words)"
    else
        echo "✗ $rel_path (SQL error)"
    fi

done < <(find "$DOCS_ROOT" -type f -name "*.md" -print0)

echo ""
echo "[INFO] Ingestion complete: $success/$count files successfully indexed"
echo ""

# Display statistics
echo "📊 Documents by category:"
sudo -u postgres psql -p 5433 -d spaceos -t -c "
SELECT source_type || '/' || COALESCE(category, 'root') || ': ' || COUNT(*) || ' documents'
FROM knowledge.documents
GROUP BY source_type, category
ORDER BY source_type, category;
"

echo ""
echo "📚 Total documents:"
sudo -u postgres psql -p 5433 -d spaceos -t -c "SELECT COUNT(*) FROM knowledge.documents;"
