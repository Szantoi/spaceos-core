#!/bin/bash
# =============================================================================
# send-status-email.sh — SpaceOS státusz email küldő (Brevo API)
# Futtatás: bash /opt/spaceos/scripts/send-status-email.sh [--test]
# Cron (napi reggel 8): 0 8 * * * bash /opt/spaceos/scripts/send-status-email.sh
# =============================================================================

set -euo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
CONF="$SPACEOS_ROOT/scripts/email.conf"
LOG="$SPACEOS_ROOT/logs/dispatcher/email.log"

# ── Config betöltés ────────────────────────────────────────────────────────────

if [ ! -f "$CONF" ]; then
  echo "❌ Hiányzó config: $CONF"
  echo "   Másold be a Brevo API kulcsot és a célcímet."
  exit 1
fi

source "$CONF"

if [[ "$BREVO_API_KEY" == "xkeysib-ILLESZD"* ]]; then
  echo "❌ Töltsd ki a Brevo API kulcsot a $CONF fájlban!"
  exit 1
fi

mkdir -p "$(dirname "$LOG")"

# ── Státusz adatok gyűjtése ────────────────────────────────────────────────────

TODAY=$(date +"%Y-%m-%d")
NOW=$(date +"%Y-%m-%d %H:%M")

# Motivátor adatok (JSON)
MDATA=$(bash "$SPACEOS_ROOT/scripts/motivator.sh" --json 2>/dev/null || echo '{}')
PERCENT=$(echo "$MDATA" | grep -o '"percent":[0-9]*' | cut -d: -f2)
DONE=$(echo "$MDATA" | grep -o '"done":[0-9]*' | cut -d: -f2)
TOTAL=$(echo "$MDATA" | grep -o '"total":[0-9]*' | cut -d: -f2)
STREAK=$(echo "$MDATA" | grep -o '"streak":[0-9]*' | cut -d: -f2)
FE_TESTS=$(echo "$MDATA" | grep -o '"fe_tests":[0-9]*' | cut -d: -f2)
BE_TESTS=$(echo "$MDATA" | grep -o '"backend_tests":[0-9]*' | cut -d: -f2)
NEXT_WORLD=$(echo "$MDATA" | grep -o '"next_world":"[^"]*"' | cut -d'"' -f4)
MSG=$(echo "$MDATA" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
MILESTONE=$(echo "$MDATA" | grep -o '"milestone":"[^"]*"' | cut -d'"' -f4)

# UNREAD inbox-ok
UNREAD_LIST=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/*/inbox/" 2>/dev/null || true)
UNREAD_COUNT=$(echo "$UNREAD_LIST" | grep -c . 2>/dev/null || echo "0")

# Aktív task-ok
ACTIVE_TASKS=$(ls "$SPACEOS_ROOT/docs/tasks/active/" 2>/dev/null | head -10 || echo "")
ACTIVE_COUNT=$(echo "$ACTIVE_TASKS" | grep -c . 2>/dev/null || echo "0")

# Tmux session-ök
SESSIONS=$(tmux ls 2>/dev/null | awk '{print $1}' | tr '\n' ' ' || echo "—")

# Git log utolsó 5 commit
GIT_LOG=$(cd "$SPACEOS_ROOT" && git log --oneline -5 2>/dev/null | sed 's/</\&lt;/g; s/>/\&gt;/g' || echo "—")

# Progress bar HTML
BAR_FILLED=$(( PERCENT * 300 / 100 ))
BAR_EMPTY=$(( 300 - BAR_FILLED ))

# ── UNREAD táblázat generálás ──────────────────────────────────────────────────

UNREAD_ROWS=""
if [ -n "$UNREAD_LIST" ]; then
  while IFS= read -r f; do
    TERM=$(echo "$f" | sed 's|.*/mailbox/\([^/]*\)/.*|\1|')
    BASE=$(basename "$f" .md)
    UNREAD_ROWS="${UNREAD_ROWS}<tr><td style='padding:4px 8px;color:#dc2626;'>⚠️</td><td style='padding:4px 8px;font-weight:600;'>${TERM^^}</td><td style='padding:4px 8px;font-family:monospace;font-size:13px;'>${BASE}</td></tr>"
  done <<< "$UNREAD_LIST"
else
  UNREAD_ROWS="<tr><td colspan='3' style='padding:4px 8px;color:#16a34a;'>✅ Minden olvasva</td></tr>"
fi

# ── Aktív task-ok táblázat ────────────────────────────────────────────────────

TASK_ROWS=""
if [ -n "$ACTIVE_TASKS" ]; then
  while IFS= read -r t; do
    [ -z "$t" ] && continue
    TASK_ROWS="${TASK_ROWS}<tr><td style='padding:4px 8px;'>⚡</td><td style='padding:4px 8px;font-family:monospace;font-size:13px;'>${t}</td></tr>"
  done <<< "$ACTIVE_TASKS"
else
  TASK_ROWS="<tr><td colspan='2' style='padding:4px 8px;color:#6b7280;'>Nincs aktív task</td></tr>"
fi

# ── Git log táblázat ──────────────────────────────────────────────────────────

GIT_ROWS=""
while IFS= read -r line; do
  [ -z "$line" ] && continue
  HASH="${line%% *}"
  CMSG="${line#* }"
  GIT_ROWS="${GIT_ROWS}<tr><td style='padding:4px 8px;font-family:monospace;color:#6b7280;font-size:12px;'>${HASH}</td><td style='padding:4px 8px;font-size:13px;'>${CMSG}</td></tr>"
done <<< "$GIT_LOG"

# ── Milestone badge ───────────────────────────────────────────────────────────

MILESTONE_HTML=""
if [ -n "$MILESTONE" ]; then
  MILESTONE_HTML="<div style='background:#fef3c7;border:1px solid #f59e0b;border-radius:8px;padding:12px 16px;margin:16px 0;font-size:15px;font-weight:600;color:#92400e;'>${MILESTONE}</div>"
fi

# ── Streak ikon ──────────────────────────────────────────────────────────────

if [ "${STREAK:-0}" -ge 7 ]; then
  STREAK_ICON="🔥"
elif [ "${STREAK:-0}" -ge 3 ]; then
  STREAK_ICON="⚡"
else
  STREAK_ICON="✨"
fi

# ── HTML email felépítése ──────────────────────────────────────────────────────

HTML=$(cat <<HTML
<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
<body style="margin:0;padding:0;background:#f9fafb;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
<div style="max-width:640px;margin:0 auto;padding:24px 16px;">

  <!-- Header -->
  <div style="background:linear-gradient(135deg,#1c1917 0%,#292524 100%);border-radius:12px;padding:28px 32px;margin-bottom:20px;">
    <div style="color:#f59e0b;font-size:13px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:6px;">SpaceOS Dispatcher</div>
    <div style="color:#ffffff;font-size:22px;font-weight:700;margin-bottom:4px;">Napi Státusz Riport</div>
    <div style="color:#a8a29e;font-size:13px;">${NOW}</div>
  </div>

  ${MILESTONE_HTML}

  <!-- Progress + Streak -->
  <div style="display:flex;gap:12px;margin-bottom:16px;">
    <div style="flex:1;background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;">
      <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:8px;">Portal haladás</div>
      <div style="font-size:28px;font-weight:800;color:#1c1917;margin-bottom:6px;">${PERCENT}%</div>
      <div style="background:#e5e7eb;border-radius:4px;height:8px;margin-bottom:8px;overflow:hidden;">
        <div style="background:#f59e0b;height:100%;width:${PERCENT}%;border-radius:4px;"></div>
      </div>
      <div style="font-size:13px;color:#6b7280;">${DONE} / ${TOTAL} világ kész</div>
    </div>
    <div style="flex:0 0 140px;background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;text-align:center;">
      <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:8px;">Streak</div>
      <div style="font-size:40px;">${STREAK_ICON}</div>
      <div style="font-size:28px;font-weight:800;color:#1c1917;">${STREAK}</div>
      <div style="font-size:12px;color:#6b7280;">nap</div>
    </div>
  </div>

  <!-- Tesztek -->
  <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;margin-bottom:16px;">
    <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:14px;">Tesztek</div>
    <div style="display:flex;gap:24px;">
      <div><div style="font-size:24px;font-weight:700;color:#1c1917;">${BE_TESTS}</div><div style="font-size:12px;color:#6b7280;">Backend</div></div>
      <div style="color:#e5e7eb;font-size:24px;">|</div>
      <div><div style="font-size:24px;font-weight:700;color:#1c1917;">${FE_TESTS}</div><div style="font-size:12px;color:#6b7280;">Frontend</div></div>
      <div style="color:#e5e7eb;font-size:24px;">|</div>
      <div><div style="font-size:24px;font-weight:700;color:#f59e0b;">$((${BE_TESTS} + ${FE_TESTS}))</div><div style="font-size:12px;color:#6b7280;">Összes</div></div>
    </div>
  </div>

  <!-- UNREAD inbox-ok -->
  <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;margin-bottom:16px;">
    <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:12px;">UNREAD Inbox (${UNREAD_COUNT})</div>
    <table style="width:100%;border-collapse:collapse;">${UNREAD_ROWS}</table>
  </div>

  <!-- Aktív task-ok -->
  <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;margin-bottom:16px;">
    <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:12px;">Aktív Task-ok (${ACTIVE_COUNT})</div>
    <table style="width:100%;border-collapse:collapse;">${TASK_ROWS}</table>
  </div>

  <!-- Git log -->
  <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;margin-bottom:16px;">
    <div style="color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:12px;">Utolsó Commitok</div>
    <table style="width:100%;border-collapse:collapse;">${GIT_ROWS}</table>
  </div>

  <!-- Következő lépés -->
  <div style="background:#fffbeb;border:1px solid #fde68a;border-radius:10px;padding:20px;margin-bottom:16px;">
    <div style="color:#92400e;font-size:12px;font-weight:600;letter-spacing:0.05em;text-transform:uppercase;margin-bottom:8px;">Következő világ</div>
    <div style="font-size:18px;font-weight:700;color:#1c1917;margin-bottom:4px;">${NEXT_WORLD}</div>
    <div style="font-size:13px;color:#6b7280;">Prototípus: <a href="https://joinerytech.hu/proto/" style="color:#f59e0b;">joinerytech.hu/proto/</a></div>
  </div>

  <!-- Motiváló üzenet -->
  <div style="background:#1c1917;border-radius:10px;padding:20px 24px;margin-bottom:20px;">
    <div style="color:#f59e0b;font-size:13px;font-weight:600;margin-bottom:8px;">💡 Motiváció</div>
    <div style="color:#e7e5e4;font-size:15px;line-height:1.6;font-style:italic;">"${MSG}"</div>
  </div>

  <!-- Tmux session-ök -->
  <div style="background:#f3f4f6;border-radius:8px;padding:12px 16px;margin-bottom:20px;font-family:monospace;font-size:12px;color:#6b7280;">
    <strong>Tmux session-ök:</strong> ${SESSIONS}
  </div>

  <!-- Footer -->
  <div style="text-align:center;color:#9ca3af;font-size:12px;">
    SpaceOS Dispatcher · <a href="https://joinerytech.hu" style="color:#9ca3af;">joinerytech.hu</a> · Automatikus riport
  </div>

</div>
</body>
</html>
HTML
)

# ── Brevo API hívás ───────────────────────────────────────────────────────────

SUBJECT="SpaceOS Státusz — ${TODAY} · ${DONE}/${TOTAL} világ · ${PERCENT}%"
[ "${1:-}" = "--test" ] && SUBJECT="[TEST] ${SUBJECT}"

PAYLOAD=$(cat <<JSON
{
  "sender": {"name": "${FROM_NAME}", "email": "${FROM_EMAIL}"},
  "to": [{"email": "${TO_EMAIL}", "name": "${TO_NAME}"}],
  "subject": "${SUBJECT}",
  "htmlContent": $(printf '%s' "$HTML" | python3 -c "import sys,json; print(json.dumps(sys.stdin.read()))")
}
JSON
)

RESPONSE=$(curl -s -w "\n%{http_code}" \
  -X POST "https://api.brevo.com/v3/smtp/email" \
  -H "accept: application/json" \
  -H "api-key: ${BREVO_API_KEY}" \
  -H "content-type: application/json" \
  -d "$PAYLOAD")

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -1)

if [ "$HTTP_CODE" = "201" ]; then
  echo "✅ Email elküldve: ${TO_EMAIL} (${SUBJECT})"
  echo "$(date +%Y-%m-%dT%H:%M:%S) OK $TO_EMAIL \"$SUBJECT\"" >> "$LOG"
else
  echo "❌ Email küldés sikertelen (HTTP $HTTP_CODE)"
  echo "   Response: $BODY"
  echo "$(date +%Y-%m-%dT%H:%M:%S) ERROR $HTTP_CODE $BODY" >> "$LOG"
  exit 1
fi
