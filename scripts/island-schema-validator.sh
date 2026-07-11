#!/bin/bash
# Island Schema Validator — Sziget struktúra konzisztencia ellenőrzés
# Használat: ./island-schema-validator.sh [validate|fix|report]
#
# Ellenőrzi, hogy minden sziget megfelel-e az egységes struktúrának

set -euo pipefail

LOG_FILE="/opt/spaceos/logs/island-validator.log"
REGISTRY="/opt/spaceos/config/islands.yaml"

mkdir -p "$(dirname "$LOG_FILE")"

log() {
    echo "[$(date -Iseconds)] $1" | tee -a "$LOG_FILE"
}

# Színek (ha terminálban)
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Sziget definíciók
declare -A ISLANDS=(
    ["nexus"]="/opt/nexus"
    ["joinerytech"]="/opt/joinerytech"
    ["doorstar"]="/opt/doorstar"
    ["spaceos"]="/opt/spaceos"
)

# Kötelező fájlok és mappák
REQUIRED_STRUCTURE=(
    "CLAUDE.md:file"
    "config:dir"
    "docs:dir"
    "logs:dir"
    "scripts:dir"
    "src:dir"
    "terminals:dir"
    "terminals/federation:dir"
    "terminals/federation/inbox:dir"
    "terminals/federation/outbox:dir"
    "terminals/federation/archive:dir"
)

# Opcionális (javasolt) elemek
RECOMMENDED_STRUCTURE=(
    "config/.mcp-token:file"
    "config/hosting.yaml:file"
    "docs/knowledge:dir"
)

# Egy sziget validálása
validate_island() {
    local island=$1
    local path=${ISLANDS[$island]}
    local errors=0
    local warnings=0

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  Validating: $island ($path)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    # Kötelező elemek
    for item in "${REQUIRED_STRUCTURE[@]}"; do
        local name=${item%%:*}
        local type=${item#*:}
        local full_path="$path/$name"

        if [ "$type" = "file" ]; then
            if [ -f "$full_path" ]; then
                echo -e "  ${GREEN}✓${NC} $name"
            else
                echo -e "  ${RED}✗${NC} $name (missing file)"
                errors=$((errors + 1))
            fi
        elif [ "$type" = "dir" ]; then
            if [ -d "$full_path" ]; then
                echo -e "  ${GREEN}✓${NC} $name/"
            else
                echo -e "  ${RED}✗${NC} $name/ (missing directory)"
                errors=$((errors + 1))
            fi
        fi
    done

    # Javasolt elemek
    echo ""
    echo "  Recommended:"
    for item in "${RECOMMENDED_STRUCTURE[@]}"; do
        local name=${item%%:*}
        local type=${item#*:}
        local full_path="$path/$name"

        if [ "$type" = "file" ]; then
            if [ -f "$full_path" ]; then
                echo -e "  ${GREEN}✓${NC} $name"
            else
                echo -e "  ${YELLOW}⚠${NC} $name (recommended)"
                warnings=$((warnings + 1))
            fi
        elif [ "$type" = "dir" ]; then
            if [ -d "$full_path" ]; then
                echo -e "  ${GREEN}✓${NC} $name/"
            else
                echo -e "  ${YELLOW}⚠${NC} $name/ (recommended)"
                warnings=$((warnings + 1))
            fi
        fi
    done

    # CLAUDE.md tartalom ellenőrzés
    echo ""
    echo "  CLAUDE.md checks:"
    if [ -f "$path/CLAUDE.md" ]; then
        if grep -q "^# " "$path/CLAUDE.md"; then
            echo -e "  ${GREEN}✓${NC} Has header"
        else
            echo -e "  ${YELLOW}⚠${NC} No header found"
            warnings=$((warnings + 1))
        fi

        if grep -qi "island" "$path/CLAUDE.md" || grep -qi "sziget" "$path/CLAUDE.md"; then
            echo -e "  ${GREEN}✓${NC} Contains island identity"
        else
            echo -e "  ${YELLOW}⚠${NC} Missing island identity"
            warnings=$((warnings + 1))
        fi
    fi

    # Token ellenőrzés
    echo ""
    echo "  Security:"
    if [ -f "$path/config/.mcp-token" ]; then
        local token_len=$(wc -c < "$path/config/.mcp-token")
        if [ "$token_len" -gt 20 ]; then
            echo -e "  ${GREEN}✓${NC} Token exists (${token_len} chars)"
        else
            echo -e "  ${YELLOW}⚠${NC} Token too short"
            warnings=$((warnings + 1))
        fi
    else
        echo -e "  ${YELLOW}⚠${NC} No MCP token"
        warnings=$((warnings + 1))
    fi

    # Összegzés
    echo ""
    if [ "$errors" -eq 0 ] && [ "$warnings" -eq 0 ]; then
        echo -e "  ${GREEN}✓ VALID${NC} - No issues"
    elif [ "$errors" -eq 0 ]; then
        echo -e "  ${YELLOW}⚠ VALID with warnings${NC} - $warnings warnings"
    else
        echo -e "  ${RED}✗ INVALID${NC} - $errors errors, $warnings warnings"
    fi

    return $errors
}

# Fix hiányzó struktúra
fix_island() {
    local island=$1
    local path=${ISLANDS[$island]}

    log "Fixing structure for $island..."

    for item in "${REQUIRED_STRUCTURE[@]}"; do
        local name=${item%%:*}
        local type=${item#*:}
        local full_path="$path/$name"

        if [ "$type" = "dir" ] && [ ! -d "$full_path" ]; then
            mkdir -p "$full_path"
            log "  Created: $full_path"
        fi
    done

    # Token generálás ha hiányzik
    if [ ! -f "$path/config/.mcp-token" ]; then
        mkdir -p "$path/config"
        openssl rand -base64 32 > "$path/config/.mcp-token"
        chmod 600 "$path/config/.mcp-token"
        log "  Generated token: $path/config/.mcp-token"
    fi

    echo "Fixed structure for $island"
}

# Report generálás
generate_report() {
    local report_file="/opt/spaceos/logs/island-validation-report-$(date +%Y%m%d-%H%M%S).md"

    {
        echo "# Island Validation Report"
        echo ""
        echo "Generated: $(date -Iseconds)"
        echo ""
        echo "## Summary"
        echo ""

        local total_errors=0
        local total_warnings=0

        for island in "${!ISLANDS[@]}"; do
            local path=${ISLANDS[$island]}
            local errors=0
            local warnings=0

            echo "### $island ($path)"
            echo ""

            # Count issues
            for item in "${REQUIRED_STRUCTURE[@]}"; do
                local name=${item%%:*}
                local type=${item#*:}
                local full_path="$path/$name"

                if [ "$type" = "file" ] && [ ! -f "$full_path" ]; then
                    echo "- ❌ Missing: \`$name\`"
                    errors=$((errors + 1))
                elif [ "$type" = "dir" ] && [ ! -d "$full_path" ]; then
                    echo "- ❌ Missing: \`$name/\`"
                    errors=$((errors + 1))
                fi
            done

            for item in "${RECOMMENDED_STRUCTURE[@]}"; do
                local name=${item%%:*}
                local type=${item#*:}
                local full_path="$path/$name"

                if [ "$type" = "file" ] && [ ! -f "$full_path" ]; then
                    echo "- ⚠️ Recommended: \`$name\`"
                    warnings=$((warnings + 1))
                elif [ "$type" = "dir" ] && [ ! -d "$full_path" ]; then
                    echo "- ⚠️ Recommended: \`$name/\`"
                    warnings=$((warnings + 1))
                fi
            done

            if [ "$errors" -eq 0 ] && [ "$warnings" -eq 0 ]; then
                echo "- ✅ All checks passed"
            fi

            echo ""
            total_errors=$((total_errors + errors))
            total_warnings=$((total_warnings + warnings))
        done

        echo "## Totals"
        echo ""
        echo "- **Errors:** $total_errors"
        echo "- **Warnings:** $total_warnings"
        echo "- **Status:** $([ $total_errors -eq 0 ] && echo '✅ PASS' || echo '❌ FAIL')"

    } > "$report_file"

    echo "Report generated: $report_file"
}

# Registry validálás
validate_registry() {
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  Validating: islands.yaml"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    if [ ! -f "$REGISTRY" ]; then
        echo -e "  ${RED}✗${NC} Registry file not found"
        return 1
    fi

    # YAML szintaxis ellenőrzés (egyszerű)
    if grep -qE "^  [a-z]+:$" "$REGISTRY"; then
        echo -e "  ${GREEN}✓${NC} Contains island definitions"
    else
        echo -e "  ${RED}✗${NC} No island definitions found"
        return 1
    fi

    # Port egyediség
    local ports=$(grep "knowledge:" "$REGISTRY" | awk '{print $2}' | sort)
    local unique_ports=$(echo "$ports" | uniq)

    if [ "$ports" = "$unique_ports" ]; then
        echo -e "  ${GREEN}✓${NC} All ports are unique"
    else
        echo -e "  ${RED}✗${NC} Duplicate ports detected"
        return 1
    fi

    # Version check
    if grep -q "^version:" "$REGISTRY"; then
        local version=$(grep "^version:" "$REGISTRY" | head -1 | awk '{print $2}' | tr -d '"')
        echo -e "  ${GREEN}✓${NC} Version: $version"
    else
        echo -e "  ${YELLOW}⚠${NC} No version field"
    fi

    echo ""
    echo -e "  ${GREEN}✓ VALID${NC}"
    return 0
}

# Main
case "${1:-validate}" in
    validate)
        total_errors=0

        # Registry
        validate_registry || total_errors=$((total_errors + 1))

        # Islands
        for island in "${!ISLANDS[@]}"; do
            validate_island "$island" || total_errors=$((total_errors + 1))
        done

        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        if [ "$total_errors" -eq 0 ]; then
            echo -e "  ${GREEN}ALL VALIDATIONS PASSED${NC}"
        else
            echo -e "  ${RED}VALIDATION FAILED${NC} - $total_errors issues"
        fi
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        exit $total_errors
        ;;
    fix)
        for island in "${!ISLANDS[@]}"; do
            fix_island "$island"
        done
        echo ""
        echo "Done. Run 'validate' to verify."
        ;;
    report)
        generate_report
        ;;
    *)
        echo "Island Schema Validator"
        echo ""
        echo "Usage: $0 [validate|fix|report]"
        echo ""
        echo "Commands:"
        echo "  validate    Check all islands for schema compliance"
        echo "  fix         Create missing directories and tokens"
        echo "  report      Generate markdown report"
        ;;
esac
