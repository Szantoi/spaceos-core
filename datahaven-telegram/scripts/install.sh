#!/bin/bash
# SpaceOS Datahaven Telegram Bot - Install Script

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
SERVICE_NAME="datahaven-telegram"

echo "=== SpaceOS Datahaven Telegram Bot Installation ==="
echo ""

# Check Python
if ! command -v python3 &> /dev/null; then
    echo "ERROR: python3 not found. Please install Python 3.10+"
    exit 1
fi

PYTHON_VERSION=$(python3 -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}")')
echo "Python version: $PYTHON_VERSION"

# Create venv
echo ""
echo "Creating virtual environment..."
cd "$PROJECT_DIR"
python3 -m venv venv
source venv/bin/activate

# Install dependencies
echo "Installing dependencies..."
pip install --upgrade pip
pip install -r requirements.txt

# Check config
if [ ! -f "$PROJECT_DIR/config/.env" ]; then
    echo ""
    echo "WARNING: config/.env not found!"
    echo "Please copy config/.env.example to config/.env and configure:"
    echo "  - TELEGRAM_BOT_TOKEN"
    echo "  - ALLOWED_USERS"
    echo "  - ADMIN_USERS"
    echo ""
fi

# Check if token is set
if [ -f "$PROJECT_DIR/config/.env" ]; then
    source "$PROJECT_DIR/config/.env"
    if [ -z "$TELEGRAM_BOT_TOKEN" ]; then
        echo "WARNING: TELEGRAM_BOT_TOKEN is not set in config/.env"
    fi
fi

echo ""
echo "=== Installation complete ==="
echo ""
echo "To run manually:"
echo "  cd $PROJECT_DIR"
echo "  source venv/bin/activate"
echo "  python src/bot.py"
echo ""
echo "To install as systemd service:"
echo "  sudo bash $SCRIPT_DIR/systemd-install.sh"
echo ""
