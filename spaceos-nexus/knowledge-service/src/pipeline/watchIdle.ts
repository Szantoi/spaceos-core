// Watch Idle - TypeScript equivalent of watch-idle.sh
// Shuts down idle sessions that have no UNREAD inbox

import {
  SESSIONS,
  listSessions,
  hasSession,
  getSessionActivity,
  hasUnreadInbox,
  isPrioritySession,
  sendKeys,
  sendEnter,
  killSession,
  getState,
  setState,
  deleteState,
  telegram,
  log,
} from './common';

const IDLE_TIMEOUT = 900; // 15 minutes

async function shutdownSession(sessionName: string, terminal: string): Promise<void> {
  // Send /exit command to claude session
  await sendKeys(sessionName, '/exit');
  await new Promise(r => setTimeout(r, 500));
  await sendEnter(sessionName);
  await new Promise(r => setTimeout(r, 2000));

  // If still running, kill session
  if (await hasSession(sessionName)) {
    await killSession(sessionName);
  }

  await log(`Session leállítva (idle + no task): ${sessionName}`);
  await telegram(`💤 *${terminal.toUpperCase()} leállítva*\nOk: nincs feladat, idle`);
}

export async function watchIdle(): Promise<{ processed: number; shutdown: number }> {
  const now = Math.floor(Date.now() / 1000);
  let processed = 0;
  let shutdown = 0;

  const sessions = await listSessions();

  for (const sessionName of sessions) {
    const terminal = SESSIONS[sessionName];
    if (!terminal) continue;

    processed++;

    // Skip priority sessions
    if (isPrioritySession(sessionName)) {
      continue;
    }

    // Has UNREAD inbox?
    if (await hasUnreadInbox(terminal)) {
      // Has work to do, don't shut down
      await deleteState(`${sessionName}_shutdown_pending`);
      continue;
    }

    // Get idle time
    const activity = await getSessionActivity(sessionName);
    const idleTime = activity > 0 ? now - activity : 0;

    if (idleTime <= IDLE_TIMEOUT) {
      // Not idle enough
      await deleteState(`${sessionName}_shutdown_pending`);
      continue;
    }

    // Idle + no UNREAD → check shutdown pending state
    const shutdownKey = `${sessionName}_shutdown_pending`;
    const lastShutdown = await getState(shutdownKey);

    if (!lastShutdown || lastShutdown === '0') {
      // First detection: mark and wait one more cycle
      await setState(shutdownKey, String(now));
      await log(`Idle session jelölve: ${sessionName} (vár még egy ciklust)`);
    } else {
      const elapsed = now - parseInt(lastShutdown, 10);
      if (elapsed > 120) {
        // Marked for 2+ minutes → shutdown
        await shutdownSession(sessionName, terminal);
        await deleteState(shutdownKey);
        shutdown++;
      }
    }
  }

  return { processed, shutdown };
}

// Run standalone
if (require.main === module) {
  watchIdle().then(result => {
    console.log(`[watchIdle] Processed: ${result.processed}, Shutdown: ${result.shutdown}`);
  });
}
