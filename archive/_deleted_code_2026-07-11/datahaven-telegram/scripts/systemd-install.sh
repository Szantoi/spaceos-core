#!/bin/bash
# Install datahaven-telegram as systemd service

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
SERVICE_NAME="datahaven-telegram"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

# Check root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo)"
    exit 1
fi

# Get actual user
ACTUAL_USER="${SUDO_USER:-gabor}"

echo "Installing systemd service for $SERVICE_NAME..."

# Create service file
cat > "$SERVICE_FILE" << EOF
[Unit]
Description=Datahaven Telegram Bot Gateway
After=network.target

[Service]
Type=simple
User=${ACTUAL_USER}
WorkingDirectory=${PROJECT_DIR}
Environment="PATH=${PROJECT_DIR}/venv/bin:/usr/bin"
ExecStart=${PROJECT_DIR}/venv/bin/python ${PROJECT_DIR}/src/bot.py
Restart=always
RestartSec=10

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=${SERVICE_NAME}

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd
systemctl daemon-reload

echo ""
echo "Service installed: $SERVICE_FILE"
echo ""
echo "Commands:"
echo "  sudo systemctl start ${SERVICE_NAME}    # Start"
echo "  sudo systemctl stop ${SERVICE_NAME}     # Stop"
echo "  sudo systemctl status ${SERVICE_NAME}   # Status"
echo "  sudo systemctl enable ${SERVICE_NAME}   # Auto-start on boot"
echo "  journalctl -u ${SERVICE_NAME} -f        # View logs"
echo ""
