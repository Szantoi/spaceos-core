#!/bin/bash
################################################################################
# INFRA: Diagnose & Fix joinerytech.hu 403 Forbidden
#
# Purpose:  Check nginx config + document root + fix 403 error
# Status:   Ready for VPS SSH execution
# Created:  2026-06-17
# Owner:    INFRA (MSG-INFRA-059)
################################################################################

set -e

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  INFRA nginx 403 Diagnostic & Fix (joinerytech.hu)           ║"
echo "║  MSG-INFRA-059                                                ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_ok() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ============================================================================
# STEP 1: Check nginx configuration
# ============================================================================

log_info "Step 1: Checking nginx configuration..."
echo ""

if [ ! -f /etc/nginx/sites-enabled/joinerytech.conf ] && \
   [ ! -f /etc/nginx/sites-enabled/joinerytech ] && \
   [ ! -f /etc/nginx/sites-available/joinerytech.conf ] && \
   [ ! -f /etc/nginx/sites-available/joinerytech ]; then
  log_error "nginx joinerytech config not found in /etc/nginx/sites-*/"
  log_info "Searching all nginx configs..."
  find /etc/nginx -type f -name "*joinerytech*" 2>/dev/null || log_warn "No joinerytech config found"
  echo ""
  echo "Checking /etc/nginx/sites-enabled/:"
  ls -la /etc/nginx/sites-enabled/ || log_warn "Directory not accessible"
  exit 1
fi

# Found config — show it
for config in /etc/nginx/sites-enabled/joinerytech* /etc/nginx/sites-available/joinerytech*; do
  if [ -f "$config" ]; then
    log_ok "Found: $config"
    echo ""
    echo "=== nginx config content ==="
    cat "$config"
    echo "=== end config ==="
    echo ""
  fi
done

# ============================================================================
# STEP 2: Check document root
# ============================================================================

log_info "Step 2: Checking document root..."
echo ""

# Extract document root from nginx config
DOC_ROOT=$(grep -oP 'root\s+\K[^;]+' /etc/nginx/sites-enabled/joinerytech* 2>/dev/null | head -1 || \
           grep -oP 'root\s+\K[^;]+' /etc/nginx/sites-available/joinerytech* 2>/dev/null | head -1)

if [ -z "$DOC_ROOT" ]; then
  log_error "Could not extract document root from nginx config"
  exit 1
fi

DOC_ROOT=$(echo "$DOC_ROOT" | tr -d ' ')
log_info "Document root: $DOC_ROOT"

if [ ! -d "$DOC_ROOT" ]; then
  log_error "Document root does not exist: $DOC_ROOT"
  echo "Creating directory..."
  sudo mkdir -p "$DOC_ROOT"
  log_ok "Directory created"
fi

# Check permissions
log_info "Checking permissions..."
ls -lad "$DOC_ROOT" | awk '{print "  " $0}'

OWNER=$(ls -ld "$DOC_ROOT" | awk '{print $3}')
if [ "$OWNER" != "www-data" ] && [ "$OWNER" != "root" ] && [ "$OWNER" != "nginx" ]; then
  log_warn "Document root owned by $OWNER (not www-data/nginx)"
fi

# Check if readable by nginx
if [ ! -r "$DOC_ROOT" ]; then
  log_error "Document root not readable by current user"
  echo "Fixing permissions..."
  sudo chmod 755 "$DOC_ROOT"
  log_ok "Permissions fixed"
fi

# ============================================================================
# STEP 3: Check for index files
# ============================================================================

log_info "Step 3: Checking for index files..."
echo ""

if [ -f "$DOC_ROOT/index.html" ]; then
  log_ok "index.html exists"
  ls -lh "$DOC_ROOT/index.html" | awk '{print "  " $0}'
else
  log_error "index.html NOT found in $DOC_ROOT"
  echo "Searching for index files..."
  find "$DOC_ROOT" -name "index.html" -o -name "index.js" 2>/dev/null | head -5 || log_warn "No index files found"
fi

# ============================================================================
# STEP 4: Check nginx error log
# ============================================================================

log_info "Step 4: Checking nginx error log..."
echo ""

if [ -f /var/log/nginx/error.log ]; then
  log_ok "nginx error.log found"
  echo "Recent errors (last 10 lines):"
  tail -10 /var/log/nginx/error.log | awk '{print "  " $0}'
else
  log_warn "nginx error.log not found"
fi

# ============================================================================
# STEP 5: Test nginx syntax
# ============================================================================

log_info "Step 5: Testing nginx config syntax..."
echo ""

if sudo nginx -t 2>&1; then
  log_ok "nginx config syntax OK"
else
  log_error "nginx config has syntax errors"
  exit 1
fi

# ============================================================================
# STEP 6: Check if nginx is running
# ============================================================================

log_info "Step 6: Checking nginx status..."
echo ""

if systemctl is-active --quiet nginx; then
  log_ok "nginx is running"
  sudo systemctl status nginx | grep "Active:" | awk '{print "  " $0}'
else
  log_warn "nginx is NOT running"
  echo "Starting nginx..."
  sudo systemctl start nginx
  log_ok "nginx started"
fi

# ============================================================================
# STEP 7: Test HTTP request
# ============================================================================

log_info "Step 7: Testing HTTP request to localhost..."
echo ""

if curl -s -o /dev/null -w "%{http_code}" http://localhost/ | grep -q "200\|304"; then
  log_ok "HTTP localhost returns OK"
else
  local_code=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/)
  log_warn "HTTP localhost returns: $local_code"
fi

# ============================================================================
# SUMMARY
# ============================================================================

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  DIAGNOSTIC SUMMARY                                           ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
log_ok "nginx configuration found"
log_ok "Document root: $DOC_ROOT"
log_ok "nginx syntax valid"
log_ok "nginx running"
echo ""
log_info "Next steps:"
echo "  1. Deploy frontend build to: $DOC_ROOT"
echo "  2. Set correct permissions (755)"
echo "  3. Reload nginx: sudo systemctl reload nginx"
echo "  4. Test: curl -I https://joinerytech.hu/"
echo ""
