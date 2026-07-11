#!/bin/bash
################################################################################
# INFRA Phase 4-5: Prerequisites Installation
#
# Purpose:  Ensure all dependencies are installed before executing Phase 4-5
# Status:   REQUIRED before Phase 4 execution
# Created:  2026-06-17
# Owner:    INFRA terminal
#
# Usage:    sudo ./00-PHASE4-5-PREREQUISITES.sh [--check-only]
#
# What this does:
#   - Installs jq (JSON processor) — required for Phase 4
#   - Checks Python3 availability (fallback for JSON manipulation)
#   - Verifies directory permissions
#   - Creates log directory if needed
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

################################################################################
# Helper Functions
################################################################################

log_info() {
  echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
  echo -e "${GREEN}[OK]${NC} $1"
}

log_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
  echo -e "${RED}[ERROR]${NC} $1"
}

################################################################################
# Prerequisite Checks
################################################################################

check_prerequisites() {
  log_info "Checking prerequisites..."

  local missing_deps=0

  # Check jq
  if ! command -v jq &> /dev/null; then
    log_warn "jq not installed (required for Phase 4)"
    missing_deps=$((missing_deps + 1))
  else
    local jq_version=$(jq --version)
    log_success "jq: $jq_version"
  fi

  # Check Python3
  if ! command -v python3 &> /dev/null; then
    log_warn "Python3 not installed (fallback for JSON manipulation)"
    missing_deps=$((missing_deps + 1))
  else
    local python_version=$(python3 --version)
    log_success "Python3: $python_version"
  fi

  # Check Node.js
  if ! command -v node &> /dev/null; then
    log_warn "Node.js not installed (required for MCP server execution)"
    missing_deps=$((missing_deps + 1))
  else
    local node_version=$(node --version)
    log_success "Node.js: $node_version"
  fi

  # Check crontab
  if ! command -v crontab &> /dev/null; then
    log_warn "crontab not available (optional, for Phase 5 scheduling)"
    missing_deps=$((missing_deps + 1))
  else
    log_success "crontab: available"
  fi

  # Check settings.json
  local settings_file="${HOME}/.claude/settings.json"
  if [ ! -f "$settings_file" ]; then
    log_warn "Settings file not found: $settings_file"
    missing_deps=$((missing_deps + 1))
  else
    log_success "Settings file: $settings_file"
  fi

  # Check directory permissions
  local script_dir="/opt/spaceos/scripts"
  if [ ! -w "$script_dir" ]; then
    log_error "Cannot write to script directory: $script_dir"
    missing_deps=$((missing_deps + 1))
  else
    log_success "Script directory writable: $script_dir"
  fi

  if [ $missing_deps -eq 0 ]; then
    log_success "All prerequisites satisfied"
    return 0
  else
    log_warn "Missing $missing_deps prerequisite(s)"
    return 1
  fi
}

################################################################################
# Install Dependencies
################################################################################

install_jq() {
  log_info "Installing jq..."

  if command -v apt-get &> /dev/null; then
    log_info "Updating apt cache..."
    sudo apt-get update -qq

    log_info "Installing jq..."
    sudo apt-get install -y jq

    if command -v jq &> /dev/null; then
      log_success "jq installed: $(jq --version)"
    else
      log_error "Failed to install jq"
      return 1
    fi
  elif command -v yum &> /dev/null; then
    log_info "Installing jq via yum..."
    sudo yum install -y jq

    if command -v jq &> /dev/null; then
      log_success "jq installed: $(jq --version)"
    else
      log_error "Failed to install jq"
      return 1
    fi
  elif command -v brew &> /dev/null; then
    log_info "Installing jq via brew..."
    brew install jq

    if command -v jq &> /dev/null; then
      log_success "jq installed: $(jq --version)"
    else
      log_error "Failed to install jq"
      return 1
    fi
  else
    log_error "Cannot determine package manager (apt, yum, or brew required)"
    return 1
  fi

  return 0
}

create_log_directory() {
  local log_dir="/var/log/spaceos"

  log_info "Checking log directory: $log_dir"

  if [ ! -d "$log_dir" ]; then
    log_info "Creating log directory..."
    sudo mkdir -p "$log_dir"
    log_success "Log directory created"
  else
    log_success "Log directory exists"
  fi

  # Check write permissions
  if [ ! -w "$log_dir" ]; then
    log_warn "Log directory not writable for current user, fixing permissions..."
    sudo chmod 755 "$log_dir"
    sudo chown $(id -u):$(id -g) "$log_dir" 2>/dev/null || true
  fi

  log_success "Log directory ready: $log_dir"
}

################################################################################
# Main
################################################################################

main() {
  local check_only=false

  # Parse arguments
  while [[ $# -gt 0 ]]; do
    case $1 in
      --check-only)
        check_only=true
        shift
        ;;
      *)
        log_error "Unknown argument: $1"
        exit 1
        ;;
    esac
  done

  echo ""
  echo "╔════════════════════════════════════════════════════════════════╗"
  echo "║  INFRA Phase 4-5: Prerequisites Setup                         ║"
  echo "║  Created: 2026-06-17                                          ║"
  echo "╚════════════════════════════════════════════════════════════════╝"
  echo ""

  if [ "$check_only" = true ]; then
    log_info "Check-only mode: no installations will be performed"
    echo ""
    check_prerequisites
    exit $?
  fi

  # First, check what's missing
  if check_prerequisites; then
    echo ""
    log_success "All prerequisites already satisfied"
    echo ""
    create_log_directory
    echo ""
    echo "✅ Environment ready for Phase 4-5 execution"
    echo ""
    exit 0
  fi

  echo ""
  log_warn "Some prerequisites are missing. Installing..."
  echo ""

  # Install jq if missing
  if ! command -v jq &> /dev/null; then
    if install_jq; then
      log_success "jq installation complete"
    else
      log_error "jq installation failed"
      exit 1
    fi
  fi

  # Create log directory
  create_log_directory

  echo ""
  echo "╔════════════════════════════════════════════════════════════════╗"
  echo "║  Prerequisites installation complete                           ║"
  echo "╚════════════════════════════════════════════════════════════════╝"
  echo ""

  log_success "Environment ready for Phase 4-5 execution"
  echo ""
  echo "Next steps:"
  echo "  1. Run Phase 4: ./04-phase4-mcp-registration.sh"
  echo "  2. Run Phase 5: ./05-phase5-scanner-integration.sh"
  echo ""

  exit 0
}

# Run main function
main "$@"
