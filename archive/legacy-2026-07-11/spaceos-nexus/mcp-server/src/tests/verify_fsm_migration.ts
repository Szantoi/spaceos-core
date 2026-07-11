import Database from 'better-sqlite3';
import * as fs from 'fs';
import * as path from 'path';

const MIGRATIONS_DIR = 'c:/Users/szant/Documents/Development/JoineryTech.McpServer/src/metadata/migrations';
const NEW_MIGRATION = path.join(MIGRATIONS_DIR, '004_epic11_fsm_schema.sql');
const PREV_MIGRATION = path.join(MIGRATIONS_DIR, '003_epic09_context_schema.sql');

async function verify() {
    const db = new Database(':memory:');

    console.log('--- Phase 1: Applying Dependencies ---');
    const prevSql = fs.readFileSync(PREV_MIGRATION, 'utf8');
    db.exec(prevSql);
    console.log('Applied 003_epic09_context_schema.sql');

    console.log('\n--- Phase 2: Applying New Migration (First Pass) ---');
    const newSql = fs.readFileSync(NEW_MIGRATION, 'utf8');
    db.exec(newSql);
    console.log('Applied 004_epic11_fsm_schema.sql successfully');

    console.log('\n--- Phase 3: Verifying Idempotency (Second Pass) ---');
    try {
        db.exec(newSql);
        console.log('Applied 004_epic11_fsm_schema.sql again successfully (Idempotency confirmed)');
    } catch (err: any) {
        console.error('Idempotency check failed:', err.message);
        process.exit(1);
    }

    console.log('\n--- Phase 4: Verifying Table Existence ---');
    const tables = db.prepare("SELECT name FROM sqlite_master WHERE type='table'").all() as { name: string }[];
    const tableNames = tables.map(t => t.name);
    const expected = ['workflow_definitions', 'fsm_state_transitions', 'agent_sessions', 'session_history'];

    for (const table of expected) {
        if (tableNames.includes(table)) {
            console.log(`Table ${table}: FOUND`);
        } else {
            console.error(`Table ${table}: NOT FOUND!`);
            process.exit(1);
        }
    }

    console.log('\n--- Phase 5: Verifying Schema Metadata Version ---');
    const versionInfo = db.prepare("SELECT version FROM schema_metadata WHERE layer = 'read-layer'").get() as { version: number };
    console.log(`Read-layer version: ${versionInfo.version}`);
    if (versionInfo.version === 2) {
        console.log('Version upgrade: SUCCESS');
    } else {
        console.error(`Version upgrade: FAILED (Expected 2, got ${versionInfo.version})`);
        process.exit(1);
    }

    console.log('\n--- ALL VERIFICATIONS PASSED ---');
}

verify().catch(err => {
    console.error('Verification script failed:', err);
    process.exit(1);
});
