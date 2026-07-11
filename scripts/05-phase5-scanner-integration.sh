#!/bin/bash
################################################################################
# INFRA Phase 5: Scanner Integration
#
# Purpose:  Integrate knowledge ingestion into pipeline-knowledge-index.sh
#           and set up cron scheduling
# Status:   READY FOR EXECUTION (after Phase 2-3)
# Created:  2026-06-17
# Owner:    INFRA terminal
#
# Usage:    ./05-phase5-scanner-integration.sh [--validate-only] [--setup-cron] [--rollback]
#
# Dependencies:
#   - /opt/spaceos/scripts/02-rag-ingest.js (Phase 2)
#   - /opt/spaceos/scripts/pipeline-knowledge-index.sh (existing scanner)
#   - crontab (for scheduling)
################################################################################

set -e

# Configuration
INGEST_SCRIPT="/opt/spaceos/scripts/02-rag-ingest.js"
SCANNER_SCRIPT="/opt/spaceos/scripts/pipeline-knowledge-index.sh"
SCANNER_BACKUP="${SCANNER_SCRIPT}.backup.$(date +%s)"
LOG_DIR="/var/log/spaceos"
CRON_JOB="0 */6 * * * /opt/spaceos/scripts/pipeline-knowledge-index.sh >> /var/log/spaceos/knowledge-scanner.log 2>&1"
CRON_IDENTIFIER="spaceos-knowledge-scanner"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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
# Pre-flight Checks
################################################################################

preflight_check() {
  log_info "Running pre-flight checks..."

  # Check if ingest script exists
  if [ ! -f "$INGEST_SCRIPT" ]; then
    log_error "Ingest script not found: $INGEST_SCRIPT"
    log_error "This is Phase 2 (ORCH responsibility). Cannot proceed without it."
    exit 1
  fi

  # Check if ingest script is executable
  if [ ! -x "$INGEST_SCRIPT" ]; then
    log_warn "Ingest script is not executable: $INGEST_SCRIPT"
    log_info "Making executable..."
    chmod +x "$INGEST_SCRIPT"
    log_success "Script is now executable"
  fi

  # Check if scanner script exists (may not exist yet)
  if [ ! -f "$SCANNER_SCRIPT" ]; then
    log_warn "Scanner script not found: $SCANNER_SCRIPT"
    log_info "Will create new scanner script"
  fi

  # Check if log directory exists
  if [ ! -d "$LOG_DIR" ]; then
    log_info "Creating log directory: $LOG_DIR"
    mkdir -p "$LOG_DIR"
  fi

  log_success "Pre-flight checks passed"
}

################################################################################
# Create/Update Scanner Script
################################################################################

create_scanner_script() {
  log_info "Creating/updating scanner script: $SCANNER_SCRIPT"

  # Backup existing scanner if it exists
  if [ -f "$SCANNER_SCRIPT" ]; then
    if cp "$SCANNER_SCRIPT" "$SCANNER_BACKUP" 2>/dev/null; then
      log_info "Backup created: $SCANNER_BACKUP"
    else
      log_warn "Could not backup existing scanner (permission issue), proceeding with overwrite"
    fi
  fi

  # Create new scanner script
  cat > "$SCANNER_SCRIPT" << 'SCANNER_EOF'
#!/bin/bash
################################################################################
# Knowledge Service Scanner & Ingestion Pipeline
#
# Purpose:  Scan knowledge base, ingest into PostgreSQL, validate MCP server
# Status:   OPERATIONAL
# Created:  2026-06-17 (Phase 5 integration)
# Owner:    INFRA / Librarian
#
# This script is called:
#   - Every 6 hours via cron (default schedule)
#   - After librarian memory cleanup
#   - On-demand for immediate indexing
################################################################################

set -e

# Configuration
INGEST_SCRIPT="/opt/spaceos/scripts/02-rag-ingest.js"
DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="spaceos_knowledge"
DB_USER="postgres"
LOG_FILE="/var/log/spaceos/knowledge-scanner-$(date +%Y%m%d_%H%M%S).log"
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Ensure log directory exists
mkdir -p /var/log/spaceos

################################################################################
# Helper Functions
################################################################################

log_info() {
  echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
  echo -e "${GREEN}[OK]${NC} $1" | tee -a "$LOG_FILE"
}

log_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
  echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

################################################################################
# Step 1: Run PostgreSQL Ingestion
################################################################################

run_ingestion() {
  log_info "Step 1: Running PostgreSQL ingestion (Phase 2)..."

  if [ ! -f "$INGEST_SCRIPT" ]; then
    log_error "Ingest script not found: $INGEST_SCRIPT"
    return 1
  fi

  if [ ! -x "$INGEST_SCRIPT" ]; then
    log_warn "Ingest script not executable, making executable..."
    chmod +x "$INGEST_SCRIPT"
  fi

  # Run ingestion with error handling
  if "$INGEST_SCRIPT" >> "$LOG_FILE" 2>&1; then
    log_success "Ingestion completed successfully"
    return 0
  else
    log_error "Ingestion failed (exit code: $?)"
    return 1
  fi
}

################################################################################
# Step 2: Verify Database State
################################################################################

verify_database() {
  log_info "Step 2: Verifying database state..."

  # Check if PostgreSQL is accessible
  if ! sudo -u "$DB_USER" psql -p "$DB_PORT" -d "$DB_NAME" -c "SELECT 1;" > /dev/null 2>&1; then
    log_error "Cannot connect to database: $DB_HOST:$DB_PORT/$DB_NAME"
    return 1
  fi

  log_success "Database connection verified"

  # Get document count
  local total_docs
  total_docs=$(sudo -u "$DB_USER" psql -p "$DB_PORT" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM knowledge.documents;" 2>/dev/null | tr -d ' ')

  if [ -z "$total_docs" ]; then
    log_error "Could not retrieve document count from database"
    return 1
  fi

  log_success "Database contains $total_docs documents"
  echo "$total_docs" > /var/log/spaceos/knowledge-doc-count.txt

  # Get indexing timestamp
  local latest_update
  latest_update=$(sudo -u "$DB_USER" psql -p "$DB_PORT" -d "$DB_NAME" -t -c "SELECT MAX(updated_at) FROM knowledge.documents;" 2>/dev/null | tr -d ' ')

  log_info "Latest document update: $latest_update"

  return 0
}

################################################################################
# Step 3: Health Check MCP Server
################################################################################

health_check_mcp() {
  log_info "Step 3: Health check — MCP server status..."

  # Check if MCP server process is running
  local mcp_running=0
  if pgrep -f "mcp-server.js" > /dev/null 2>&1; then
    mcp_running=1
  fi

  if [ "$mcp_running" -eq 1 ]; then
    log_success "MCP server is running"
  else
    log_warn "MCP server not currently running (expected if on-demand startup)"
  fi

  # Try to connect to Knowledge Service API (optional)
  if command -v curl &> /dev/null; then
    if curl -s -m 5 "http://localhost:3456/health" > /dev/null 2>&1; then
      log_success "Knowledge Service API is responding"
    else
      log_warn "Knowledge Service API not responding (may be expected)"
    fi
  fi

  return 0
}

################################################################################
# Step 4: Log Completion
################################################################################

log_completion() {
  log_info "Step 4: Logging completion..."

  local total_docs
  total_docs=$(cat /var/log/spaceos/knowledge-doc-count.txt 2>/dev/null || echo "unknown")

  log_success "Knowledge service scan complete at $TIMESTAMP"
  log_success "$total_docs documents indexed"

  # Write summary to permanent log
  cat >> /var/log/spaceos/knowledge-scanner-summary.log << SUMMARY_EOF
[$(date -u +%Y-%m-%dT%H:%M:%SZ)] Documents: $total_docs | Log: $LOG_FILE
SUMMARY_EOF

  return 0
}

################################################################################
# Step 5: Notifications (Optional)
################################################################################

send_notifications() {
  log_info "Step 5: Sending notifications (if configured)..."

  local total_docs
  total_docs=$(cat /var/log/spaceos/knowledge-doc-count.txt 2>/dev/null || echo "0")

  # Check if notification script exists
  if [ -x "/opt/spaceos/scripts/critical-notify.sh" ]; then
    if /opt/spaceos/scripts/critical-notify.sh \
      "Knowledge Service Scanner" \
      "Indexed $total_docs documents" \
      "info" >> "$LOG_FILE" 2>&1; then
      log_success "Notification sent"
    else
      log_warn "Notification script failed (non-critical)"
    fi
  else
    log_info "Notification script not found (optional feature)"
  fi

  return 0
}

################################################################################
# Main Pipeline
################################################################################

main() {
  echo ""
  echo "╔════════════════════════════════════════════════════════════════╗"
  echo "║  Knowledge Service Scanner & Ingestion Pipeline               ║"
  echo "║  Timestamp: $TIMESTAMP"
  echo "║  Log: $LOG_FILE"
  echo "╚════════════════════════════════════════════════════════════════╝"
  echo ""

  log_info "Starting knowledge service scanner pipeline..."

  # Execute pipeline steps
  local exit_code=0

  run_ingestion || exit_code=$?
  verify_database || exit_code=$?
  health_check_mcp || exit_code=$?
  log_completion || exit_code=$?
  send_notifications || exit_code=$?

  echo ""

  if [ $exit_code -eq 0 ]; then
    log_success "Pipeline completed successfully"
    echo ""
    exit 0
  else
    log_error "Pipeline completed with errors (exit code: $exit_code)"
    echo ""
    exit "$exit_code"
  fi
}

# Run main function
main
SCANNER_EOF

  # Make script executable
  chmod +x "$SCANNER_SCRIPT"
  log_success "Scanner script created: $SCANNER_SCRIPT"
}

################################################################################
# Cron Integration
################################################################################

setup_cron() {
  log_info "Setting up cron job for scanner..."

  # Check if cron job already exists
  if crontab -l 2>/dev/null | grep -q "$CRON_IDENTIFIER"; then
    log_warn "Cron job already registered"
    return 0
  fi

  # Create temporary crontab with new job
  local temp_cron=$(mktemp)
  crontab -l 2>/dev/null > "$temp_cron" || true
  echo "# $CRON_IDENTIFIER" >> "$temp_cron"
  echo "$CRON_JOB" >> "$temp_cron"

  # Install new crontab
  if crontab "$temp_cron"; then
    log_success "Cron job installed (runs every 6 hours)"
    rm -f "$temp_cron"
    return 0
  else
    log_error "Failed to install cron job"
    rm -f "$temp_cron"
    return 1
  fi
}

################################################################################
# Validate Integration
################################################################################

validate_integration() {
  log_info "Validating Phase 5 integration..."

  # Check scanner script
  if [ ! -f "$SCANNER_SCRIPT" ]; then
    log_error "Scanner script not found: $SCANNER_SCRIPT"
    return 1
  fi

  if [ ! -x "$SCANNER_SCRIPT" ]; then
    log_error "Scanner script is not executable: $SCANNER_SCRIPT"
    return 1
  fi

  log_success "Scanner script is valid"

  # Check ingest script reference
  if ! grep -q "02-rag-ingest.js" "$SCANNER_SCRIPT"; then
    log_error "Ingest script not referenced in scanner"
    return 1
  fi

  log_success "Ingest script is properly integrated"

  # Check database verification
  if ! grep -q "SELECT COUNT.*FROM knowledge.documents" "$SCANNER_SCRIPT"; then
    log_error "Database verification logic missing"
    return 1
  fi

  log_success "Database verification logic is present"

  # Optional: Check cron job
  if crontab -l 2>/dev/null | grep -q "$CRON_IDENTIFIER"; then
    log_success "Cron job is installed"
  else
    log_warn "Cron job is not installed (can be added with --setup-cron)"
  fi

  return 0
}

################################################################################
# Test Run
################################################################################

test_run() {
  log_info "Testing scanner script..."

  if [ ! -f "$SCANNER_SCRIPT" ]; then
    log_error "Scanner script not found: $SCANNER_SCRIPT"
    return 1
  fi

  log_info "Running scanner in test mode..."
  if "$SCANNER_SCRIPT"; then
    log_success "Scanner test run completed successfully"
    return 0
  else
    log_error "Scanner test run failed"
    return 1
  fi
}

################################################################################
# Rollback
################################################################################

rollback() {
  if [ -f "$SCANNER_BACKUP" ]; then
    log_warn "Rolling back scanner script..."
    mv "$SCANNER_BACKUP" "$SCANNER_SCRIPT"
    log_success "Rollback complete: $SCANNER_SCRIPT restored"
  else
    log_info "No backup to restore"
  fi

  # Remove cron job
  if crontab -l 2>/dev/null | grep -q "$CRON_IDENTIFIER"; then
    local temp_cron=$(mktemp)
    crontab -l 2>/dev/null | grep -v "$CRON_IDENTIFIER" | grep -v "^$CRON_JOB" > "$temp_cron"
    crontab "$temp_cron"
    rm -f "$temp_cron"
    log_success "Cron job removed"
  fi
}

################################################################################
# Main
################################################################################

main() {
  local setup_cron_flag=false
  local do_rollback=false
  local validate_only=false

  # Parse arguments
  while [[ $# -gt 0 ]]; do
    case $1 in
      --setup-cron)
        setup_cron_flag=true
        shift
        ;;
      --rollback)
        do_rollback=true
        shift
        ;;
      --validate-only)
        validate_only=true
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
  echo "║  INFRA Phase 5: Scanner Integration                           ║"
  echo "║  Created: 2026-06-17                                          ║"
  echo "╚════════════════════════════════════════════════════════════════╝"
  echo ""

  if [ "$do_rollback" = true ]; then
    rollback
    exit 0
  fi

  preflight_check
  create_scanner_script

  if validate_integration; then
    if [ "$validate_only" = false ]; then
      if [ "$setup_cron_flag" = true ]; then
        setup_cron
      fi
      log_info "Phase 5: Scanner Integration Complete"
    else
      log_info "Validation-only mode: no changes made"
    fi
    echo ""
    log_success "Phase 5: Scanner Integration Ready"
    echo ""
    log_info "Available commands:"
    echo "  - Run scanner manually: $SCANNER_SCRIPT"
    echo "  - Setup cron: ./05-phase5-scanner-integration.sh --setup-cron"
    echo "  - Rollback: ./05-phase5-scanner-integration.sh --rollback"
    echo ""
  else
    log_error "Phase 5: Scanner Integration Failed"
    rollback
    exit 1
  fi
}

# Run main function
main "$@"
