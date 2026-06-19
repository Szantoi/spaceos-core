# Telepítési útmutató

## Előfeltételek

### Rendszer
- Linux (Debian/Ubuntu ajánlott)
- Python 3.10+
- systemd (service kezeléshez)

### Datahaven Core
A bot működéséhez szükséges a [datahaven-core](https://github.com/user/datahaven-core) telepítése:

```bash
# Klónozás
git clone https://github.com/user/datahaven-core.git /home/gabor/datahaven-core

# Datahaven instance létrehozása
mkdir -p /opt/spaceos/datahaven
cd /opt/spaceos/datahaven
ln -s /home/gabor/datahaven-core/cli/datahaven-ask.sh ask
```

### Telegram Bot Token
1. Nyisd meg a [@BotFather](https://t.me/botfather) chatot Telegram-on
2. Küldj `/newbot` parancsot
3. Kövesd az utasításokat (név, username)
4. Másold ki a kapott tokent

### User ID lekérése
1. Nyisd meg a [@userinfobot](https://t.me/userinfobot) chatot
2. Küldj bármilyen üzenetet
3. A bot visszaadja az ID-dat

## Telepítés lépésről lépésre

### 1. Repository klónozása

```bash
cd /opt/spaceos
git clone https://github.com/user/datahaven-telegram.git
cd datahaven-telegram
```

### 2. Automatikus telepítés

```bash
bash scripts/install.sh
```

Ez a script:
- Létrehozza a Python virtual environment-et
- Telepíti a függőségeket
- Ellenőrzi a konfigurációt

### 3. Manuális telepítés (alternatíva)

```bash
# Virtual environment
python3 -m venv venv
source venv/bin/activate

# Függőségek
pip install --upgrade pip
pip install -r requirements.txt
```

### 4. Konfiguráció

```bash
cp config/.env.example config/.env
nano config/.env
```

**Kötelező beállítások:**
```bash
TELEGRAM_BOT_TOKEN=your-bot-token
ALLOWED_USERS=your-telegram-user-id
ADMIN_USERS=your-telegram-user-id
```

**Opcionális beállítások:**
```bash
DATAHAVEN_HOME=/opt/spaceos/datahaven
DATAHAVEN_CORE=/home/gabor/datahaven-core
KNOWLEDGE_URL=http://localhost:3456
BOT_DAEMON_ID=telegram-bot
POLL_INTERVAL=30
LOG_LEVEL=INFO
```

### 5. Tesztelés

```bash
source venv/bin/activate
python src/bot.py
```

Ha sikeres, látod:
```
INFO - Starting Datahaven Telegram Bot...
INFO - Application started
```

Próbáld ki a `/start` parancsot Telegram-on.

### 6. Systemd service telepítés

```bash
sudo bash scripts/systemd-install.sh
```

Ez létrehozza: `/etc/systemd/system/datahaven-telegram.service`

### 7. Service indítása

```bash
# Indítás
sudo systemctl start datahaven-telegram

# Auto-start engedélyezése
sudo systemctl enable datahaven-telegram

# Státusz ellenőrzése
sudo systemctl status datahaven-telegram
```

## Ellenőrzések

### Service fut?
```bash
sudo systemctl status datahaven-telegram
```

### Logok
```bash
journalctl -u datahaven-telegram -f
```

### Bot válaszol?
Küldj `/status` parancsot Telegram-on.

## Frissítés

```bash
cd /opt/spaceos/datahaven-telegram
git pull

# Függőségek frissítése (ha requirements.txt változott)
source venv/bin/activate
pip install -r requirements.txt

# Service újraindítás
sudo systemctl restart datahaven-telegram
```

## Eltávolítás

```bash
# Service leállítása és törlése
sudo systemctl stop datahaven-telegram
sudo systemctl disable datahaven-telegram
sudo rm /etc/systemd/system/datahaven-telegram.service
sudo systemctl daemon-reload

# Fájlok törlése
rm -rf /opt/spaceos/datahaven-telegram
```

## Hibaelhárítás

### "python: command not found"
```bash
sudo apt install python3 python3-venv python3-pip
```

### "ModuleNotFoundError: No module named 'telegram'"
```bash
source venv/bin/activate
pip install -r requirements.txt
```

### "TELEGRAM_BOT_TOKEN not set"
Ellenőrizd hogy a `config/.env` fájlban be van-e állítva a token.

### Service nem indul
```bash
journalctl -u datahaven-telegram -n 50
```

### Permission denied
```bash
sudo chown -R $USER:$USER /opt/spaceos/datahaven-telegram
```
