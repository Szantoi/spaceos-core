---
id: MSG-ROOT-015-VPS-OPERATOR
from: root
to: vps-operator
type: operational-directive
priority: critical
status: READ
created: 2026-06-17
---

# VPS Operator Task — Voyage AI API Key Setup

## Situation

**Nexus Knowledge Service (Phase 1)** is 95% complete and blocked on a single external dependency:
- Embedding backend requires **Voyage AI API key**
- Free tier available (25M tokens/month, zero cost)
- Once key is configured on VPS, Nexus can complete Phase 1 within 5-10 minutes

**Timeline:** 20 minutes total (5 min registration + 5 min key gen + 10 min VPS setup)

---

## Executive Summary

```
VPS TASK: Configure Voyage AI API key on VPS for Knowledge Service
├─ STEP 1: Register at https://dash.voyageai.com/ (manual, 5 min)
├─ STEP 2: Generate API key (manual, 5 min)
├─ STEP 3: Update VPS .env file (SSH, 10 min)
└─ STEP 4: Notify ROOT when complete
```

---

## Detailed Steps

### STEP 1: Voyage AI Registration (Manual, 5 minutes)

**Location:** https://dash.voyageai.com/

1. Open browser
2. Click **"Sign Up"**
3. Enter your email
4. Create password
5. Check email for verification link
6. Click verification link
7. You should now see: **Voyage AI Dashboard**
8. **Accept free tier** (no payment method required)

**Free Tier Details:**
- **Limit:** 25M tokens/month
- **Cost:** $0 (always free tier)
- **Model:** voyage-3-lite
- **Perfect for:** Our docs/knowledge (~500K tokens) ✅

---

### STEP 2: Generate API Key (Manual, 5 minutes)

**Location:** Voyage AI Dashboard (https://dash.voyageai.com/)

1. Log into dashboard (should be already logged in)
2. Look for **"API Keys"** section (left sidebar or main menu)
3. Click **"Create API Key"** or **"Generate Key"** button
4. A new key appears (format: `pa-XXXXXXXXXXXXXXXXX...`)
5. **IMMEDIATELY COPY IT** to clipboard or notepad
6. Save it temporarily (this is the only time it displays)

**Example key format:**
```
pa-0b0c0e0e1e2a3c4d5e6f7g8h9i0j1k2l3m
```

---

### STEP 3: VPS Configuration (SSH, 10 minutes)

**Host:** 109.122.222.198 · **User:** gabor

```bash
# 1. SSH to VPS
ssh gabor@109.122.222.198

# 2. Navigate to knowledge service
cd /opt/spaceos/spaceos-nexus/knowledge-service

# 3. Verify current state
cat .env
# You should see something like:
#   VOYAGE_API_KEY=your_voyage_key_here  (commented or placeholder)

# 4. Add the real API key (replace the placeholder)
# Option A - Simple append (recommended):
echo "VOYAGE_API_KEY=pa-<YOUR-COPIED-KEY>" >> .env

# Option B - Edit the file:
nano .env
# Find: VOYAGE_API_KEY=...
# Replace with: VOYAGE_API_KEY=pa-<YOUR-KEY>
# Save: Ctrl+X, then Y, then Enter

# 5. VERIFY the key was added correctly:
grep "^VOYAGE_API_KEY" .env
# Should show: VOYAGE_API_KEY=pa-XXXX...

# 6. Check file permissions (should be readable)
ls -la .env
# Should show: -rw-rw-r-- or similar

# 7. Done! The VPS is now configured.
```

**Example interaction:**
```bash
gabor@vps:~$ cd /opt/spaceos/spaceos-nexus/knowledge-service
gabor@vps:knowledge-service$ echo "VOYAGE_API_KEY=pa-0b0c0e0e1e2a3c4d5e6f7g8h9i0j1k2l3m" >> .env
gabor@vps:knowledge-service$ grep "^VOYAGE_API_KEY" .env
VOYAGE_API_KEY=pa-0b0c0e0e1e2a3c4d5e6f7g8h9i0j1k2l3m
gabor@vps:knowledge-service$ echo "✓ Voyage API key configured"
```

---

### STEP 4: Notify ROOT (Immediate)

Once VPS is configured, reply with:

```
✅ Voyage AI key configured on VPS
   Location: /opt/spaceos/spaceos-nexus/knowledge-service/.env
   Status: Ready for Nexus indexing
```

---

## Definition of Done

- [ ] Voyage AI account created (free tier)
- [ ] API key generated and copied
- [ ] VPS .env updated with VOYAGE_API_KEY=pa-...
- [ ] Verification: `grep "^VOYAGE_API_KEY" .env` shows the key
- [ ] File permissions correct: `-rw-rw-r--`
- [ ] Notified ROOT of completion

---

## Success Indicators

**After configuration, ROOT will:**
1. Send continuation message to Nexus terminal
2. Nexus will run: `npm run index` (indexing ~200 chunks)
3. Nexus will run: `npm run dev` (server on port 3456)
4. Nexus will run: `./scripts/test-rag.sh` (5 validation queries)
5. Nexus sends DONE message → Phase 2 unlocked ✅

**You should see (in Nexus logs):**
```
✓ Voyage AI initialized
✓ ChromaDB connected
✓ 200 chunks embedded and stored
✓ Knowledge search operational
✓ All 5 tests passing
```

---

## Troubleshooting

**Q: I can't find the API Keys section**
- A: Try the left sidebar menu in Voyage dashboard, or look for "Settings" → "API Keys"

**Q: The key disappeared after I generated it**
- A: Go back to API Keys, delete the old key, create a new one, copy immediately

**Q: SSH connection fails**
- A: Verify VPS IP is 109.122.222.198 and you have SSH credentials

**Q: grep shows nothing**
- A: The key wasn't added correctly. Try again with `echo "VOYAGE_API_KEY=pa-YOUR-KEY" >> .env`

---

## Reference Documentation

- **Full setup guide:** `/opt/spaceos/docs/knowledge/deployment/VOYAGE_AI_SETUP_RUNBOOK.md`
- **Blocking task:** MSG-INFRA-054 (Nexus Phase 1 Knowledge Service)
- **Next step:** ROOT sends MSG-NEXUS-004 (continuation)

---

## Timeline

- **Now:** You perform steps 1-3 (20 min)
- **After:** ROOT sends continuation to Nexus
- **Then:** Nexus indexes docs/knowledge (5-10 min)
- **Finally:** Phase 1 complete, Fázis 2 unlocked ✅

---

## Priority

🔴 **CRITICAL** — This is the only remaining blocker for Nexus Phase 1.
No other tasks depend on this beyond Nexus Knowledge Service.

**Your action unblocks:** Datahaven/Resonance infrastructure (Phase 2 planning)

---

**Status:** AWAITING EXECUTION
**Assigned to:** VPS Operator (Gabor or equivalent)
**Escalation:** If blocked beyond 30 minutes, notify ROOT immediately
