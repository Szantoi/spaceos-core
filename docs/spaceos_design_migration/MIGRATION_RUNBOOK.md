# MIGRATION_RUNBOOK.md — Design pipeline Claude.ai → VPS

> Run on the VPS as `gabor`. Everything here is your manual work; the design session
> cannot mutate the system. Estimated time: ~20 min.

## 0. Prerequisites
- This bundle copied to the VPS, e.g.:
  `scp spaceos-design-migration.tar.gz gabor@109.122.222.198:~/`
  `ssh gabor@109.122.222.198 'tar xzf spaceos-design-migration.tar.gz'`
- `ripgrep` installed: `which rg || sudo apt-get install -y ripgrep`

## 1. Place the doc-corpus context + memory files
```bash
cp ~/spaceos-design-migration/CLAUDE.md        ~/spaceos-docs/CLAUDE.md
cp ~/spaceos-design-migration/DESIGN_MEMORY.md ~/spaceos-docs/DESIGN_MEMORY.md
```
Review `DESIGN_MEMORY.md` Section 5 against the newest `Codebase_Status_*.md` and correct any drift.

## 2. Port the skills
```bash
mkdir -p ~/.claude/skills
cp -r ~/spaceos-design-migration/skills/* ~/.claude/skills/
ls ~/.claude/skills/    # expect: spaceos-conductor spaceos-arch-planner ... ddd-arch-planner
```
> Sub-skills are `references/*.md` inside `spaceos-arch-planner` — already included. No separate step.

## 3. Mailbox — no new inbox needed
`spaceos-design` is interactive (you drive it), so it has NO inbox/outbox. It dispatches
to the existing `mailbox/root/inbox/`. Verify that exists:
```bash
ls ~/spaceos-docs/mailbox/root/inbox/ && echo "OK"
```

## 4. Add the persistent session to the dispatcher
Edit `/opt/spaceos/tools/dispatcher/spaceos-dispatcher.sh`, in the `--launch-all` block,
alongside `spaceos-root` (persistent, NOT in the on-demand list):
```bash
tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-design \
  -c /home/gabor/spaceos-docs
```
> Do NOT add `spaceos-design` to the on-demand auto-stop list — it must stay alive like root.

## 5. Launch + start Claude Code in the session
```bash
sd --launch-all
sd --daemon
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-design "claude" Enter   # start Claude Code
```

## 6. Apply the R4 amendment (inside the design session)
Attach and let the design session edit its own doc:
```bash
tm attach -t spaceos-design
```
Then in the session: paste `R4_amendment.md` and ask it to apply the change to
`Design_Pipeline_Strategy_v1.md`. (Editing a file under `~/spaceos-docs/` is allowed.)

## 7. Smoke test
In the `spaceos-design` session, verify the four guarantees:
```
1. pwd                      → /home/gabor/spaceos-docs            (correct WD)
2. "session kickoff"        → reads DESIGN_MEMORY + newest Codebase_Status + pipeline pointer
3. rg -l "ADR-039" .        → finds docs (ripgrep = Project Knowledge replacement works)
4. Ask it to read ~/spaceos-kernel/...  → succeeds (ground-truth read)
   Ask it to WRITE there    → it must REFUSE (hard constraint holds)
```
If all four pass, migration is complete.

## 8. Claude.ai rollback note
Keep the Claude.ai project intact (don't delete) until the VPS session has driven at least
one full v1→v4 doc end-to-end. The skill markdown and memory are now mirrored on the VPS,
but a parallel safety net costs nothing.
