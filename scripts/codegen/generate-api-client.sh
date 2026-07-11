#!/bin/bash
#
# generate-api-client.sh — Unified API client generation wrapper
# Part of SpaceOS Code Generator Toolchain (ADR-050 Phase 2)
#
# Usage:
#   ./generate-api-client.sh [target]
#
# Targets:
#   portal       - Generate React Query hooks for Portal (Orval)
#   orchestrator - Generate TypeScript client for Orchestrator (NSwag)
#   all          - Generate both (default)
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SPACEOS_ROOT="/opt/spaceos"
PORTAL_DIR="$SPACEOS_ROOT/datahaven-web/client"
ORCHESTRATOR_DIR="$SPACEOS_ROOT/backend/spaceos-orchestrator"
KERNEL_URL="${KERNEL_API_URL:-http://localhost:5000}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

check_kernel_api() {
    log_info "Checking Kernel API availability at $KERNEL_URL..."
    if ! curl -s --max-time 5 "$KERNEL_URL/openapi/v1.json" > /dev/null 2>&1; then
        log_error "Kernel API not reachable at $KERNEL_URL"
        log_warn "Start Kernel API or set KERNEL_API_URL environment variable"
        exit 1
    fi
    log_success "Kernel API is available"
}

generate_portal() {
    log_info "Generating Portal API client (Orval)..."

    if [ ! -d "$PORTAL_DIR" ]; then
        log_error "Portal directory not found: $PORTAL_DIR"
        exit 1
    fi

    cd "$PORTAL_DIR"

    if [ ! -f "orval.config.ts" ]; then
        log_error "orval.config.ts not found in $PORTAL_DIR"
        exit 1
    fi

    npm run generate:api

    log_success "Portal API client generated"
    log_info "Output: $PORTAL_DIR/src/api/generated/kernel/"
}

generate_orchestrator() {
    log_info "Generating Orchestrator API client (NSwag)..."

    if [ ! -d "$ORCHESTRATOR_DIR" ]; then
        log_error "Orchestrator directory not found: $ORCHESTRATOR_DIR"
        exit 1
    fi

    cd "$ORCHESTRATOR_DIR"

    if [ ! -f "nswag.json" ]; then
        log_error "nswag.json not found in $ORCHESTRATOR_DIR"
        exit 1
    fi

    npm run generate:api

    log_success "Orchestrator API client generated"
    log_info "Output: $ORCHESTRATOR_DIR/src/api/generated/kernel-api-client.ts"
}

show_usage() {
    echo "Usage: $0 [target]"
    echo ""
    echo "Targets:"
    echo "  portal       - Generate React Query hooks for Portal (Orval)"
    echo "  orchestrator - Generate TypeScript client for Orchestrator (NSwag)"
    echo "  all          - Generate both (default)"
    echo ""
    echo "Environment variables:"
    echo "  KERNEL_API_URL - Kernel API base URL (default: http://localhost:5000)"
}

main() {
    local target="${1:-all}"

    case "$target" in
        portal)
            check_kernel_api
            generate_portal
            ;;
        orchestrator)
            check_kernel_api
            generate_orchestrator
            ;;
        all)
            check_kernel_api
            generate_portal
            generate_orchestrator
            log_success "All API clients generated successfully!"
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            log_error "Unknown target: $target"
            show_usage
            exit 1
            ;;
    esac
}

main "$@"
