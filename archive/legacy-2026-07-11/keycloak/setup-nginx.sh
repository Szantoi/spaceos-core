#!/bin/bash
# Keycloak nginx location blokkok hozzáadása
# Futtatás: sudo bash /opt/spaceos/keycloak/setup-nginx.sh

set -e

NGINX_CONF="/etc/nginx/sites-available/spaceos"

# Backup
cp "$NGINX_CONF" "${NGINX_CONF}.bak.$(date +%Y%m%d%H%M%S)"

# Keycloak location blokkok (joinerytech.hu server blokk végéhez, az első } elé)
# Az auth/admin/ blokknak az auth/ előtt kell lennie (nginx first-match)
KEYCLOAK_LOCATIONS='
    # Keycloak admin konzol — TILTVA externally (SEC-02)
    location /auth/admin/ {
        allow 127.0.0.1;
        deny all;
        proxy_pass http://127.0.0.1:8080/auth/admin/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Keycloak proxy (SEC-02)
    location /auth/ {
        proxy_pass http://127.0.0.1:8080/auth/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }'

# A joinerytech.hu server blokk után (első server }  után az asztalostech komment előtt) szúrja be
# Egyszerűbb módszer: sed-del a "# --- asztalostech" sor elé szúr
sed -i "/# --- asztalostech\.hu (HTTPS) ---/i ${KEYCLOAK_LOCATIONS}\n" "$NGINX_CONF"

# Teszt
nginx -t && echo "✅ Nginx konfig OK" || echo "❌ Hiba — visszaállítás szükséges"
