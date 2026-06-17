#!/bin/bash
# =============================================================================
# yaml-parser.sh — Egyszerű YAML parser bash-hez
#
# Használat:
#   source yaml-parser.sh
#   yaml_get "config.yaml" "section.key" "default"
#   yaml_get_list "config.yaml" "section.list_key"
# =============================================================================

# Egyszerű kulcs kiolvasása (pl. "reviewer.model_a")
yaml_get() {
    local file="$1"
    local key="$2"
    local default="${3:-}"
    local value

    local section=$(echo "$key" | cut -d. -f1)
    local field=$(echo "$key" | cut -d. -f2-)

    if [ "$section" = "$field" ]; then
        # Egyszerű kulcs (nincs pont)
        value=$(grep -E "^${key}:" "$file" 2>/dev/null | head -1 | sed 's/^[^:]*:\s*//' | tr -d '"'"'" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
    else
        # Nested kulcs
        value=$(awk -v section="$section" -v field="$field" '
            /^[a-z_]+:/ { current_section = $1; gsub(/:/, "", current_section) }
            current_section == section && $1 ~ field":" {
                gsub(/^[^:]*:\s*/, "");
                gsub(/["'"'"']/, "");
                gsub(/^[[:space:]]+|[[:space:]]+$/, "");
                print;
                exit
            }
        ' "$file" 2>/dev/null)
    fi

    echo "${value:-$default}"
}

# Lista kiolvasása (pl. "verdict.approve_keywords")
yaml_get_list() {
    local file="$1"
    local key="$2"
    local section=$(echo "$key" | cut -d. -f1)
    local field=$(echo "$key" | cut -d. -f2-)

    awk -v section="$section" -v field="$field" '
        /^[a-z_]+:/ { current_section = $1; gsub(/:/, "", current_section); in_list = 0 }
        current_section == section && $0 ~ field":" { in_list = 1; next }
        in_list && /^[[:space:]]*-/ {
            gsub(/^[[:space:]]*-[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
        in_list && /^[[:space:]]*[a-z_]+:/ { in_list = 0 }
    ' "$file" 2>/dev/null
}

# Szegmensek kiolvasása (speciális YAML struktúra)
yaml_get_segments() {
    local file="$1"

    awk '
        /^segments:/ { in_segments = 1; next }
        in_segments && /^[a-z_]+:/ && !/^[[:space:]]/ { exit }
        in_segments && /^[[:space:]]*- name:/ {
            gsub(/^[[:space:]]*- name:[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
    ' "$file" 2>/dev/null
}

# Szegmens adatok kiolvasása
yaml_get_segment_field() {
    local file="$1"
    local segment_name="$2"
    local field="$3"

    awk -v seg="$segment_name" -v fld="$field" '
        /^segments:/ { in_segments = 1; next }
        in_segments && /^[a-z_]+:/ && !/^[[:space:]]/ { exit }
        in_segments && /^[[:space:]]*- name:/ {
            gsub(/^[[:space:]]*- name:[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            current_seg = $0
        }
        in_segments && current_seg == seg && $0 ~ fld":" {
            gsub(/^[^:]*:[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
    ' "$file" 2>/dev/null
}

# Szegmens sources lista kiolvasása
yaml_get_segment_sources() {
    local file="$1"
    local segment_name="$2"

    awk -v seg="$segment_name" '
        /^segments:/ { in_segments = 1; next }
        in_segments && /^[a-z_]+:/ && !/^[[:space:]]/ { exit }
        in_segments && /^[[:space:]]*- name:/ {
            gsub(/^[[:space:]]*- name:[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            current_seg = $0
            in_sources = 0
        }
        in_segments && current_seg == seg && /sources:/ { in_sources = 1; next }
        in_segments && current_seg == seg && /source:/ {
            gsub(/^[^:]*:[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
        in_segments && current_seg == seg && in_sources && /^[[:space:]]*-/ {
            gsub(/^[[:space:]]*-[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
        in_segments && current_seg == seg && in_sources && /^[[:space:]]*[a-z_]+:/ { in_sources = 0 }
    ' "$file" 2>/dev/null
}

# Template placeholder csere
template_replace() {
    local template="$1"
    local placeholder="$2"
    local value="$3"

    echo "${template//"{{${placeholder}}}"/$value}"
}
