// eslint-disable-next-line @typescript-eslint/no-var-requires
// @ts-ignore
const Database: any = require('better-sqlite3');
import * as path from 'path';

const DB_PATH = path.resolve(__dirname, '../../metadata.db');

export interface ResourceRecord {
    id: number;
    project: string;
    user: string;
    type: string;
    relative_path: string;
    hash: string;
    status: 'active' | 'archived';
    /** Optional: links this artifact to a registered agent session */
    session_id: string | null;
    created_at: string;
    modified_at: string | null;
    archived_at: string | null;
}

export class ResourceTracker {
    private db: any;

    constructor(dbPath: string = DB_PATH) {
        this.db = new Database(dbPath);
        this.init();
    }

    private init() {
        this.db.exec(`
            CREATE TABLE IF NOT EXISTS resources (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                project TEXT NOT NULL,
                user TEXT NOT NULL,
                type TEXT NOT NULL,
                relative_path TEXT NOT NULL,
                hash TEXT,
                status TEXT NOT NULL DEFAULT 'active',
                session_id TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                modified_at TIMESTAMP,
                archived_at TIMESTAMP
            );
        `);

        // Migration: add session_id column to existing tables that predate this feature
        try {
            const cols = this.db.pragma('table_info(resources)') as Array<{ name: string }>;
            const hasSessionId = cols.some(c => c.name === 'session_id');
            if (!hasSessionId) {
                this.db.exec(`ALTER TABLE resources ADD COLUMN session_id TEXT;`);
            }
        } catch (_e) {
            // If pragma fails for any reason, the column either exists or will in future runs
        }
    }

    addOrUpdate(record: {
        project: string;
        user: string;
        type: string;
        relative_path: string;
        hash?: string;
        /** Optional: links this resource to an agent session (from SessionManager) */
        session_id?: string;
    }): ResourceRecord {
        const existing = this.db.prepare(
            'SELECT * FROM resources WHERE project = ? AND relative_path = ?'
        ).get(record.project, record.relative_path) as ResourceRecord | undefined;

        if (existing) {
            const stmt = this.db.prepare(
                `UPDATE resources SET user = ?, type = ?, hash = ?, session_id = ?, modified_at = CURRENT_TIMESTAMP WHERE id = ?`
            );
            stmt.run(
                record.user,
                record.type,
                record.hash ?? existing.hash,
                record.session_id ?? existing.session_id,
                existing.id
            );
            return this.db.prepare('SELECT * FROM resources WHERE id = ?').get(existing.id) as ResourceRecord;
        }

        const insert = this.db.prepare(
            `INSERT INTO resources (project, user, type, relative_path, hash, session_id) VALUES (?, ?, ?, ?, ?, ?)`
        );
        const info = insert.run(
            record.project,
            record.user,
            record.type,
            record.relative_path,
            record.hash ?? null,
            record.session_id ?? null
        );
        return this.db.prepare('SELECT * FROM resources WHERE id = ?').get(info.lastInsertRowid) as ResourceRecord;
    }

    /**
     * Returns all active resources associated with a given session_id.
     * Used by session/complete to verify that at least 1 artifact was submitted.
     */
    getBySession(sessionId: string): ResourceRecord[] {
        return this.db
            .prepare(`SELECT * FROM resources WHERE session_id = ? AND status = 'active'`)
            .all(sessionId) as ResourceRecord[];
    }

    archive(id: number) {
        const stmt = this.db.prepare(
            `UPDATE resources SET status = 'archived', archived_at = CURRENT_TIMESTAMP WHERE id = ?`);
        stmt.run(id);
    }

    move(id: number, newPath: string) {
        const stmt = this.db.prepare(
            `UPDATE resources SET relative_path = ?, modified_at = CURRENT_TIMESTAMP WHERE id = ?`);
        stmt.run(newPath, id);
    }

    list(project?: string): ResourceRecord[] {
        if (project) {
            return this.db.prepare('SELECT * FROM resources WHERE project = ?').all(project) as ResourceRecord[];
        }
        return this.db.prepare('SELECT * FROM resources').all() as ResourceRecord[];
    }
}
