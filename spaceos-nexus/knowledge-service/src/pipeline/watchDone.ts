// Watch Done - TypeScript equivalent of watch-done.sh
// Scans for DONE outbox messages and triggers reviewer

import { promises as fs } from 'fs';
import * as path from 'path';
import {
  SPACEOS_ROOT,
  getState,
  setState,
  log,
} from './common';
import { handleDoneReview } from './reviewer';
import { runPipeline } from './pipeline';

async function findUnreadDones(): Promise<string[]> {
  const dones: string[] = [];
  const mailboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox');

  try {
    const terminals = await fs.readdir(mailboxPath);

    for (const terminal of terminals) {
      const outboxPath = path.join(mailboxPath, terminal, 'outbox');

      try {
        const files = await fs.readdir(outboxPath);

        for (const file of files) {
          if (!file.endsWith('.md')) continue;

          const filePath = path.join(outboxPath, file);
          const content = await fs.readFile(filePath, 'utf-8');

          if (content.includes('status: UNREAD') && content.includes('type: done')) {
            dones.push(filePath);
          }
        }
      } catch {
        // Outbox doesn't exist for this terminal
      }
    }
  } catch {
    // Mailbox doesn't exist
  }

  return dones;
}

export async function watchDone(): Promise<{ found: number; triggered: string[] }> {
  const now = Math.floor(Date.now() / 1000);
  const triggered: string[] = [];

  const unreadDones = await findUnreadDones();

  if (unreadDones.length === 0) {
    return { found: 0, triggered: [] };
  }

  for (const donePath of unreadDones) {
    const basename = path.basename(donePath, '.md');
    const reviewKey = `review_${basename}`;
    const lastReview = await getState(reviewKey);

    // Review only triggers once per DONE file
    if (!lastReview || lastReview === '0') {
      await log(`[watchDone] Review triggerelve: ${basename}`);
      await setState(reviewKey, String(now));

      // Run TypeScript reviewer (no longer bash)
      try {
        const reviewResult = await handleDoneReview(donePath);

        if (reviewResult.approved) {
          await log(`[watchDone] APPROVED: ${basename} → running pipeline`);

          // Run post-review pipeline
          const pipelineResult = await runPipeline(donePath);
          await log(`[watchDone] Pipeline complete: archived=${pipelineResult.archived}, notified=${pipelineResult.notified}`);
        } else {
          await log(`[watchDone] REJECTED: ${basename} → reject inbox created at ${reviewResult.resultPath}`);
        }

        triggered.push(basename);
      } catch (error) {
        const errorMsg = error instanceof Error ? error.message : 'Unknown error';
        await log(`[watchDone] Reviewer hiba: ${errorMsg}`);
        // Clear state so it can retry on next run
        await setState(reviewKey, '0');
      }
    }
  }

  return { found: unreadDones.length, triggered };
}

// Run standalone
if (require.main === module) {
  watchDone().then(result => {
    console.log(`[watchDone] Found: ${result.found}, Triggered: ${result.triggered.length}`);
    result.triggered.forEach(t => console.log(`  - ${t}`));
  });
}
