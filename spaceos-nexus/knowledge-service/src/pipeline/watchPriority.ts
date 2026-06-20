// Watch Priority - TypeScript equivalent of watch-priority.sh
// Ensures priority sessions (Conductor) are always running

import {
  PRIORITY_SESSIONS,
  SESSIONS,
  SESSION_WORKDIR,
  SPACEOS_ROOT,
  hasSession,
  newSession,
  sendKeys,
  sendEnter,
  getInboxModel,
  telegram,
  log,
} from './common';

export async function watchPriority(): Promise<{ checked: number; started: string[] }> {
  const started: string[] = [];

  for (const sessionName of PRIORITY_SESSIONS) {
    const terminal = SESSIONS[sessionName];
    const workdir = SESSION_WORKDIR[sessionName] || SPACEOS_ROOT;

    // Check if session is running
    if (await hasSession(sessionName)) {
      // Session is running - nothing to do
      continue;
    }

    // Session not running → start it
    const wantedModel = await getInboxModel(terminal);
    // Priority sessions default to sonnet
    const model = wantedModel || 'sonnet';

    await newSession(sessionName, workdir);
    await new Promise(r => setTimeout(r, 1000));
    await sendKeys(sessionName, `claude --model ${model}`);
    await new Promise(r => setTimeout(r, 500));
    await sendEnter(sessionName);

    await log(`Priority session indítva: ${sessionName} (model: ${model})`);
    await telegram(`🚀 *${terminal.toUpperCase()} priority session indítva*\nModell: \`${model}\``);

    started.push(sessionName);
  }

  return { checked: PRIORITY_SESSIONS.length, started };
}

// Run standalone
if (require.main === module) {
  watchPriority().then(result => {
    console.log(`[watchPriority] Checked: ${result.checked}, Started: ${result.started.length}`);
    result.started.forEach(s => console.log(`  - ${s}`));
  });
}
