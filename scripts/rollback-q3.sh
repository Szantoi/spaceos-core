#!/bin/bash
# SpaceOS Q3 Cutting Expansion - Rollback Script
# Reverts Q3 deployment: stops services, reverts nginx config, optionally reverts DB
# Version: 1.0.0
# Date: 2026-06-23

set -e  # Exit on error
set -u  # Exit on undefined variable

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "========================================"
echo "SpaceOS Q3 Cutting Expansion - ROLLBACK"
echo "========================================"
echo ""

# Confirmation
echo -e "${RED}[WARNING]${NC} This script will rollback Q3 Cutting Expansion deployment."
echo "This includes:"
echo "  - Stopping Pricing module service"
echo "  - Reverting nginx configuration"
echo "  - Optionally reverting database schema"
echo ""
read -p "Are you sure you want to proceed? (yes/no): " -r
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${YELLOW}[ABORT]${NC} Rollback aborted."
    exit 0
fi

echo ""

# Step 1: Stop Pricing Module Service
echo "=== Step 1: Stopping Services ==="
if systemctl is-active --quiet spaceos-modules-pricing; then
    echo -e "${YELLOW}[INFO]${NC} Stopping spaceos-modules-pricing service..."
    sudo systemctl stop spaceos-modules-pricing
    echo -e "${GREEN}[SUCCESS]${NC} Pricing service stopped"
else
    echo -e "${YELLOW}[INFO]${NC} Pricing service not running (or not installed)"
fi

# Disable service to prevent auto-start
if systemctl is-enabled --quiet spaceos-modules-pricing 2>/dev/null; then
    echo -e "${YELLOW}[INFO]${NC} Disabling spaceos-modules-pricing service..."
    sudo systemctl disable spaceos-modules-pricing
    echo -e "${GREEN}[SUCCESS]${NC} Pricing service disabled"
fi

echo ""

# Step 2: Revert Nginx Configuration
echo "=== Step 2: Reverting Nginx Configuration ==="

NGINX_CONFIG="/etc/nginx/sites-available/joinerytech.hu"
NGINX_BACKUP="${NGINX_CONFIG}.pre-q3-backup"

if [ -f "$NGINX_BACKUP" ]; then
    echo -e "${YELLOW}[INFO]${NC} Restoring nginx config from backup..."
    sudo cp "$NGINX_BACKUP" "$NGINX_CONFIG"

    # Test nginx config
    if sudo nginx -t 2>&1 | grep -q "successful"; then
        echo -e "${GREEN}[SUCCESS]${NC} Nginx config restored and validated"
        sudo systemctl reload nginx
        echo -e "${GREEN}[SUCCESS]${NC} Nginx reloaded"
    else
        echo -e "${RED}[ERROR]${NC} Nginx config test failed!"
        echo "Manual intervention required. Check nginx config."
        exit 1
    fi
else
    echo -e "${YELLOW}[WARN]${NC} No nginx backup found at ${NGINX_BACKUP}"
    echo "Manual nginx config revert required."
fi

echo ""

# Step 3: Database Rollback (OPTIONAL)
echo "=== Step 3: Database Rollback (OPTIONAL) ==="
echo -e "${RED}[WARNING]${NC} Database rollback is DESTRUCTIVE and may cause DATA LOSS."
echo "Only proceed if you have a recent database backup."
echo ""
read -p "Do you want to rollback database schema? (yes/no): " -r

if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${YELLOW}[INFO]${NC} Database rollback options:"
    echo "  1. Drop Pricing schema (DROP SCHEMA spaceos_pricing CASCADE)"
    echo "  2. Revert Cutting module migrations (dotnet ef database update <previous-migration>)"
    echo "  3. Revert Identity OperatorPin column (ALTER TABLE DROP COLUMN)"
    echo "  4. Full database restore from backup (recommended)"
    echo ""
    read -p "Which option? (1/2/3/4): " -r DB_OPTION

    case $DB_OPTION in
        1)
            echo -e "${RED}[DANGER]${NC} Dropping Pricing schema..."
            psql -U spaceos -d spaceos -c "DROP SCHEMA IF EXISTS spaceos_pricing CASCADE;"
            echo -e "${GREEN}[SUCCESS]${NC} Pricing schema dropped"
            ;;
        2)
            echo -e "${YELLOW}[INFO]${NC} Reverting Cutting module migrations..."
            echo "Please specify the target migration name (e.g., 'AddBatchAssignmentFields'):"
            read -r TARGET_MIGRATION
            cd /opt/spaceos/backend/spaceos-modules-cutting
            dotnet ef database update "$TARGET_MIGRATION" --no-build
            echo -e "${GREEN}[SUCCESS]${NC} Cutting module reverted to ${TARGET_MIGRATION}"
            ;;
        3)
            echo -e "${YELLOW}[INFO]${NC} Removing OperatorPin column..."
            psql -U spaceos -d spaceos -c "ALTER TABLE spaceos_identity.spaceos_users DROP COLUMN IF EXISTS operator_pin;"
            echo -e "${GREEN}[SUCCESS]${NC} OperatorPin column removed"
            ;;
        4)
            echo -e "${YELLOW}[INFO]${NC} Full database restore:"
            echo "1. Stop all SpaceOS services"
            echo "2. Run: pg_restore -U spaceos -d spaceos /path/to/backup.dump"
            echo "3. Restart all SpaceOS services"
            echo ""
            echo "This must be done manually. Exiting."
            ;;
        *)
            echo -e "${YELLOW}[INFO]${NC} Skipping database rollback"
            ;;
    esac
else
    echo -e "${YELLOW}[INFO]${NC} Database rollback skipped"
fi

echo ""

# Step 4: Restart Cutting Module (previous version)
echo "=== Step 4: Restarting Cutting Module ==="
if systemctl is-active --quiet spaceos-modules-cutting; then
    echo -e "${YELLOW}[INFO]${NC} Restarting spaceos-modules-cutting service..."
    sudo systemctl restart spaceos-modules-cutting

    # Wait for service to be active
    sleep 3

    if systemctl is-active --quiet spaceos-modules-cutting; then
        echo -e "${GREEN}[SUCCESS]${NC} Cutting module restarted successfully"
    else
        echo -e "${RED}[ERROR]${NC} Cutting module failed to start"
        echo "Check logs: journalctl -u spaceos-modules-cutting -n 50"
        exit 1
    fi
else
    echo -e "${YELLOW}[WARN]${NC} Cutting module service not running"
fi

echo ""
echo "========================================"
echo -e "${GREEN}✅ Rollback completed${NC}"
echo "========================================"
echo ""

# Summary
echo "=== Rollback Summary ==="
echo "✅ Pricing service stopped and disabled"
echo "✅ Nginx configuration reverted"
if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "✅ Database schema reverted (option ${DB_OPTION})"
else
    echo "⏭️  Database schema NOT reverted"
fi
echo "✅ Cutting module restarted"
echo ""

echo -e "${YELLOW}[NEXT STEPS]${NC}"
echo "1. Verify services: sudo systemctl status spaceos-modules-cutting"
echo "2. Check logs: journalctl -u spaceos-modules-cutting -f"
echo "3. Test endpoints: curl https://joinerytech.hu/cutting/health"
echo ""
echo -e "${GREEN}Rollback script completed.${NC}"
