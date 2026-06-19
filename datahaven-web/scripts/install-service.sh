#!/bin/bash
# Install datahaven-web as a systemd service

set -e

SERVICE_NAME="datahaven-web"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"
INSTALL_DIR="/opt/spaceos/datahaven-web"
USER="${SUDO_USER:-$USER}"

echo "Installing ${SERVICE_NAME} systemd service..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo "Please run as root (sudo)"
  exit 1
fi

# Check Node.js
if ! command -v node &> /dev/null; then
  echo "Error: Node.js not found"
  exit 1
fi

NODE_PATH=$(which node)

# Check if .env exists
if [ ! -f "${INSTALL_DIR}/.env" ]; then
  echo "Warning: .env file not found. Copying from .env.example..."
  cp "${INSTALL_DIR}/.env.example" "${INSTALL_DIR}/.env"
  echo "Please edit ${INSTALL_DIR}/.env and configure your settings."
fi

# Check if node_modules exists
if [ ! -d "${INSTALL_DIR}/node_modules" ]; then
  echo "Installing npm dependencies..."
  cd "${INSTALL_DIR}" && npm install
fi

# Create systemd service file
cat > "${SERVICE_FILE}" << EOF
[Unit]
Description=Datahaven Web Dashboard
After=network.target

[Service]
Type=simple
User=${USER}
WorkingDirectory=${INSTALL_DIR}
ExecStart=${NODE_PATH} ${INSTALL_DIR}/src/server.js
Restart=on-failure
RestartSec=10
StandardOutput=journal
StandardError=journal
Environment=NODE_ENV=production

[Install]
WantedBy=multi-user.target
EOF

echo "Created ${SERVICE_FILE}"

# Reload systemd
systemctl daemon-reload

echo ""
echo "====================================="
echo "  Installation Complete!"
echo "====================================="
echo ""
echo "Commands:"
echo "  sudo systemctl start ${SERVICE_NAME}    # Start service"
echo "  sudo systemctl stop ${SERVICE_NAME}     # Stop service"
echo "  sudo systemctl enable ${SERVICE_NAME}   # Enable auto-start"
echo "  sudo systemctl status ${SERVICE_NAME}   # Check status"
echo "  journalctl -u ${SERVICE_NAME} -f        # View logs"
echo ""
echo "Config: ${INSTALL_DIR}/.env"
echo ""
