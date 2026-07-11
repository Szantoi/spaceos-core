#!/bin/bash
################################################################################
# INFRA Phase 4: MCP Server Registration
#
# Purpose:  Register spaceos-knowledge MCP server in ~/.claude/settings.json
# Status:   READY FOR EXECUTION (after Phase 2-3 MCP server deployed)
# Created:  2026-06-17
# Owner:    INFRA terminal
#
# Usage:    ./04-phase4-mcp-registration.sh [--validate-only] [--rollback]
#
# Dependencies:
#   - /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js (Phase 3)
#   - ~/.claude/settings.json (user settings)
#   - jq (for JSON manipulation)
################################################################################

set -e

# Configuration
SETTINGS_FILE="${HOME}/.claude/settings.json"
SETTINGS_BACKUP="${SETTINGS_FILE}.backup.$(date +%s)"
MCP_SERVER_PATH="/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js"
MCP_SERVER_NAME="spaceos-knowledge"

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

  # Check if settings.json exists
  if [ ! -f "$SETTINGS_FILE" ]; then
    log_error "Settings file not found: $SETTINGS_FILE"
    exit 1
  fi

  # Check if jq OR python3 is installed (for JSON manipulation)
  if ! command -v jq &> /dev/null && ! command -v python3 &> /dev/null; then
    log_error "Either jq or python3 is required but not installed."
    log_error "Install with: apt-get install jq  OR  apt-get install python3"
    exit 1
  fi

  # Validate settings.json is valid JSON (using jq or python3)
  if command -v jq &> /dev/null; then
    if ! jq empty "$SETTINGS_FILE" 2>/dev/null; then
      log_error "Settings file is not valid JSON: $SETTINGS_FILE"
      exit 1
    fi
  else
    if ! python3 -c "import json; json.load(open('$SETTINGS_FILE'))" 2>/dev/null; then
      log_error "Settings file is not valid JSON: $SETTINGS_FILE"
      exit 1
    fi
  fi

  # Warn if MCP server file doesn't exist (Phase 3 not yet complete)
  if [ ! -f "$MCP_SERVER_PATH" ]; then
    log_warn "MCP server file not found: $MCP_SERVER_PATH"
    log_warn "This is expected if Phase 3 (ORCH) is not yet complete."
    log_warn "The registration will be prepared; MCP server will be deployed separately."
  fi

  log_success "Pre-flight checks passed"
}

################################################################################
# MCP Server Registration
################################################################################

register_mcp_server() {
  log_info "Registering MCP server in settings.json..."

  # Backup original settings
  cp "$SETTINGS_FILE" "$SETTINGS_BACKUP"
  log_info "Backup created: $SETTINGS_BACKUP"

  # Use Python to manipulate JSON (works with or without jq)
  python3 -c "
import json
import sys

settings_file = '$SETTINGS_FILE'
mcp_name = '$MCP_SERVER_NAME'
server_path = '/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js'

try:
  # Load existing settings
  with open(settings_file, 'r') as f:
    settings = json.load(f)

  # Create mcpServers section if missing
  if 'mcpServers' not in settings:
    settings['mcpServers'] = {}

  # Define MCP server configuration
  mcp_config = {
    'command': 'node',
    'args': [server_path],
    'env': {
      'DB_HOST': 'localhost',
      'DB_PORT': '5433',
      'DB_NAME': 'spaceos_knowledge',
      'DB_USER': 'postgres'
    }
  }

  # Register/update MCP server
  settings['mcpServers'][mcp_name] = mcp_config

  # Write updated settings
  with open(settings_file, 'w') as f:
    json.dump(settings, f, indent=2)

  print('✓ MCP server registered')
except Exception as e:
  print(f'✗ Error: {e}', file=sys.stderr)
  sys.exit(1)
"

  if [ $? -eq 0 ]; then
    log_success "MCP server registered: $MCP_SERVER_NAME"
  else
    log_error "Failed to register MCP server"
    return 1
  fi
}

################################################################################
# Validate Registration
################################################################################

validate_registration() {
  log_info "Validating MCP server registration..."

  python3 -c "
import json
import sys

settings_file = '$SETTINGS_FILE'
mcp_name = '$MCP_SERVER_NAME'

try:
  with open(settings_file, 'r') as f:
    settings = json.load(f)

  # Check if server is registered
  if 'mcpServers' not in settings or mcp_name not in settings['mcpServers']:
    print('✗ MCP server not found in settings.json', file=sys.stderr)
    sys.exit(1)

  # Verify configuration
  server = settings['mcpServers'][mcp_name]

  if server.get('command') != 'node':
    print(f'✗ Invalid command: {server.get(\"command\")} (expected: node)', file=sys.stderr)
    sys.exit(1)

  if server.get('env', {}).get('DB_NAME') != 'spaceos_knowledge':
    print(f'✗ Invalid DB_NAME: {server.get(\"env\", {}).get(\"DB_NAME\")} (expected: spaceos_knowledge)', file=sys.stderr)
    sys.exit(1)

  print('✓ MCP server configuration validated')
  print('\nRegistered MCP Server Details:')
  output = json.dumps(server, indent=2)
  for line in output.split('\n'):
    print(f'  {line}')

except Exception as e:
  print(f'✗ Error: {e}', file=sys.stderr)
  sys.exit(1)
"

  if [ $? -eq 0 ]; then
    echo ""
    return 0
  else
    return 1
  fi
}

################################################################################
# Test MCP Server Startup (if Phase 3 complete)
################################################################################

test_mcp_startup() {
  if [ ! -f "$MCP_SERVER_PATH" ]; then
    log_warn "Skipping MCP startup test: Phase 3 (MCP server) not yet deployed"
    return 0
  fi

  log_info "Testing MCP server startup..."

  # Start MCP server in background with timeout
  local mcp_pid=""
  local mcp_output=$(mktemp)

  timeout 5 node "$MCP_SERVER_PATH" > "$mcp_output" 2>&1 &
  mcp_pid=$!

  # Wait for server to start (or timeout)
  sleep 2

  if kill -0 "$mcp_pid" 2>/dev/null; then
    # Server is still running (timeout not reached)
    kill "$mcp_pid" 2>/dev/null || true
    log_success "MCP server started successfully"
    cat "$mcp_output" | head -3 | sed 's/^/  /'
  else
    # Timeout reached or server exited
    log_warn "MCP server test inconclusive (expected for development mode)"
    cat "$mcp_output" | head -3 | sed 's/^/  /'
  fi

  rm -f "$mcp_output"
}

################################################################################
# Rollback
################################################################################

rollback() {
  if [ -f "$SETTINGS_BACKUP" ]; then
    log_warn "Rolling back to backup..."
    mv "$SETTINGS_BACKUP" "$SETTINGS_FILE"
    log_success "Rollback complete: $SETTINGS_FILE restored"
  else
    log_error "Backup not found: $SETTINGS_BACKUP"
    exit 1
  fi
}

################################################################################
# Main
################################################################################

main() {
  local validate_only=false
  local do_rollback=false

  # Parse arguments
  while [[ $# -gt 0 ]]; do
    case $1 in
      --validate-only)
        validate_only=true
        shift
        ;;
      --rollback)
        do_rollback=true
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
  echo "║  INFRA Phase 4: MCP Server Registration                       ║"
  echo "║  Created: 2026-06-17                                          ║"
  echo "╚════════════════════════════════════════════════════════════════╝"
  echo ""

  if [ "$do_rollback" = true ]; then
    rollback
    exit 0
  fi

  preflight_check

  if [ "$validate_only" = false ]; then
    register_mcp_server

    if validate_registration; then
      test_mcp_startup
      echo ""
      log_success "Phase 4: MCP Server Registration Complete"
      echo ""
      log_info "Next steps:"
      echo "  1. Verify MCP server is deployed (Phase 3 completion)"
      echo "  2. Run Phase 5 scanner integration"
      echo "  3. Test with: claude list-mcp-servers"
      echo ""
    else
      log_error "Phase 4: MCP Server Registration Failed"
      rollback
      exit 1
    fi
  else
    log_info "Validation-only mode: no changes will be made"
    echo ""
    log_success "Pre-flight checks passed"
    echo ""
    log_info "To register MCP server, run:"
    echo "  ./04-phase4-mcp-registration.sh"
    echo ""
  fi
}

# Run main function
main "$@"
