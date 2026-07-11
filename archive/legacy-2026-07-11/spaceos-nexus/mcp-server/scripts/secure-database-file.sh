#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# secure-database-file.sh
#
# TASK-09-04A: Database File Permissions & Security
#
# Sets ownership and permissions on database files for production deployment.
# IDEMPOTENT: Safe to run multiple times.
#
# Usage:
#   ./scripts/secure-database-file.sh [DATABASE_FILE]
#
# Default:
#   ./scripts/secure-database-file.sh database/agent.db
#
# Permissions: 0640 (rw-r-----)
#   - Owner (user): read, write
#   - Group: read
#   - Others: none (DENIED)
# ─────────────────────────────────────────────────────────────────────────────

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default database file
DB_FILE="${1:-database/agent.db}"
DB_DIR=$(dirname "$DB_FILE")

# MCP Server user (created during deployment)
MCP_USER="_mcp-server"
MCP_GROUP="_mcp-server"

# Desired permissions (octal)
DESIRED_PERMS="0640"
DESIRED_PERMS_READABLE="rw-r-----"

echo -e "${YELLOW}[secure-database-file.sh]${NC} Securing database file permissions..."
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 1: Validate inputs
# ─────────────────────────────────────────────────────────────────────────────

if [ -z "$DB_FILE" ]; then
  echo -e "${RED}[ERROR]${NC} Database file path required"
  echo "Usage: $0 [DATABASE_FILE]"
  exit 1
fi

echo -e "${YELLOW}[Step 1]${NC} Validating database file path..."
echo "  Database file: $DB_FILE"
echo "  Target owner: $MCP_USER:$MCP_GROUP"
echo "  Target permissions: $DESIRED_PERMS ($DESIRED_PERMS_READABLE)"
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 2: Check if database file exists
# ─────────────────────────────────────────────────────────────────────────────

if [ ! -f "$DB_FILE" ]; then
  echo -e "${YELLOW}[WARNING]${NC} Database file does not exist: $DB_FILE"
  echo "  This is expected on first deployment (seeder will create it)"
  exit 0
fi

echo -e "${YELLOW}[Step 2]${NC} Checking current permissions..."
CURRENT_PERMS=$(stat -c '%a' "$DB_FILE" 2>/dev/null || stat -f '%OLp' "$DB_FILE" 2>/dev/null | tail -c 4)
CURRENT_USER=$(stat -c '%U' "$DB_FILE" 2>/dev/null || stat -f '%Su' "$DB_FILE" 2>/dev/null)
CURRENT_GROUP=$(stat -c '%G' "$DB_FILE" 2>/dev/null || stat -f '%Sg' "$DB_FILE" 2>/dev/null)

echo "  Current permissions: $CURRENT_PERMS"
echo "  Current owner: $CURRENT_USER:$CURRENT_GROUP"
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 3: Set file ownership
# ─────────────────────────────────────────────────────────────────────────────

echo -e "${YELLOW}[Step 3]${NC} Setting file ownership..."

if [ "$(id -u)" -eq 0 ]; then
  # Running as root (production deployment)
  if ! grep -q "^$MCP_USER" /etc/passwd 2>/dev/null; then
    echo -e "${YELLOW}[WARNING]${NC} MCP server user '$MCP_USER' not found"
    echo "  Ownership not changed (user may not exist in this environment)"
  else
    chown "$MCP_USER:$MCP_GROUP" "$DB_FILE"
    echo -e "${GREEN}✓${NC} Ownership set: $MCP_USER:$MCP_GROUP"
  fi
else
  # Not running as root (local development)
  echo -e "${YELLOW}[INFO]${NC} Not running as root (local development?)"
  echo "  Skipping ownership change (would need sudo)"
  echo "  In production, ensure deployment runs as root or uses sudo"
fi

echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 4: Set file permissions
# ─────────────────────────────────────────────────────────────────────────────

echo -e "${YELLOW}[Step 4]${NC} Setting file permissions..."

chmod 0640 "$DB_FILE"

NEW_PERMS=$(stat -c '%a' "$DB_FILE" 2>/dev/null || stat -f '%OLp' "$DB_FILE" 2>/dev/null | tail -c 4)
echo -e "${GREEN}✓${NC} Permissions set: $NEW_PERMS ($DESIRED_PERMS_READABLE)"

echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 5: Also secure WAL-related files (if they exist)
# ─────────────────────────────────────────────────────────────────────────────

echo -e "${YELLOW}[Step 5]${NC} Securing WAL-related files..."

# WAL file
if [ -f "${DB_FILE}-wal" ]; then
  chmod 0640 "${DB_FILE}-wal"
  echo -e "${GREEN}✓${NC} WAL file permissions: 0640"
fi

# Shared memory file
if [ -f "${DB_FILE}-shm" ]; then
  chmod 0640 "${DB_FILE}-shm"
  echo -e "${GREEN}✓${NC} Shared memory file permissions: 0640"
fi

echo ""

# ─────────────────────────────────────────────────────────────────────────────
# Step 6: Verify final state
# ─────────────────────────────────────────────────────────────────────────────

echo -e "${YELLOW}[Step 6]${NC} Verifying final state..."

FINAL_PERMS=$(stat -c '%a' "$DB_FILE" 2>/dev/null || stat -f '%OLp' "$DB_FILE" 2>/dev/null | tail -c 4)
FINAL_USER=$(stat -c '%U' "$DB_FILE" 2>/dev/null || stat -f '%Su' "$DB_FILE" 2>/dev/null)
FINAL_GROUP=$(stat -c '%G' "$DB_FILE" 2>/dev/null || stat -f '%Sg' "$DB_FILE" 2>/dev/null)

echo "  File: $DB_FILE"
echo "  Permissions: $FINAL_PERMS (expected: $DESIRED_PERMS)"
echo "  Owner: $FINAL_USER:$FINAL_GROUP"
echo ""

# Validate
if [ "$FINAL_PERMS" = "$DESIRED_PERMS" ] || [ "$FINAL_PERMS" = "0640" ]; then
  echo -e "${GREEN}✓ SUCCESS${NC}: Database file secured correctly"
  echo -e "  Database file is ${GREEN}READ-ONLY${NC} to agents (query_only enforcement via PRAGMA)"
  exit 0
else
  echo -e "${RED}✗ VALIDATION FAILED${NC}: Permissions != $DESIRED_PERMS"
  echo "  Actual: $FINAL_PERMS"
  exit 1
fi
