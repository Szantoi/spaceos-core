/**
 * Test Database Fixture
 *
 * Creates an in-memory SQLite database with test data
 */

import Database from 'better-sqlite3';

/**
 * Create a test database with sample data
 * @returns {Database} In-memory SQLite database
 */
export function createTestDb() {
  const db = new Database(':memory:');

  // Create tables
  db.exec(`
    CREATE TABLE daemons (
      id TEXT PRIMARY KEY,
      description TEXT,
      last_heartbeat TEXT
    );

    CREATE TABLE messages (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      from_daemon TEXT NOT NULL,
      to_daemon TEXT NOT NULL,
      msg_type TEXT DEFAULT 'task',
      subject TEXT,
      payload TEXT,
      priority TEXT DEFAULT 'medium',
      status TEXT DEFAULT 'pending',
      created_at TEXT DEFAULT CURRENT_TIMESTAMP,
      correlation_id TEXT
    );
  `);

  // Insert test daemons
  const insertDaemon = db.prepare('INSERT INTO daemons (id, description, last_heartbeat) VALUES (?, ?, ?)');
  const now = new Date().toISOString();
  const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000).toISOString();
  const tenMinutesAgo = new Date(Date.now() - 10 * 60 * 1000).toISOString();

  insertDaemon.run('kernel', 'Backend kernel daemon', now);
  insertDaemon.run('conductor', 'Orchestration daemon', fiveMinutesAgo);
  insertDaemon.run('telegram-bot', 'Telegram bot gateway', tenMinutesAgo);

  // Insert test messages
  const insertMessage = db.prepare(`
    INSERT INTO messages (from_daemon, to_daemon, msg_type, subject, payload, priority, status, created_at)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `);

  insertMessage.run('conductor', 'kernel', 'task', 'Build project', '{"action":"build"}', 'high', 'pending', now);
  insertMessage.run('kernel', 'conductor', 'done', 'Build completed', '{"result":"success"}', 'medium', 'acked', fiveMinutesAgo);
  insertMessage.run('telegram-bot', 'conductor', 'question', 'Need approval', '{}', 'critical', 'pending', now);
  insertMessage.run('conductor', 'telegram-bot', 'task', 'Send notification', '{"msg":"Hello"}', 'low', 'pending', now);
  insertMessage.run('kernel', 'telegram-bot', 'task', 'Report status', '{}', 'medium', 'acked', tenMinutesAgo);

  return db;
}

/**
 * Create an empty test database (tables only, no data)
 * @returns {Database} Empty in-memory SQLite database
 */
export function createEmptyTestDb() {
  const db = new Database(':memory:');

  db.exec(`
    CREATE TABLE daemons (
      id TEXT PRIMARY KEY,
      description TEXT,
      last_heartbeat TEXT
    );

    CREATE TABLE messages (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      from_daemon TEXT NOT NULL,
      to_daemon TEXT NOT NULL,
      msg_type TEXT DEFAULT 'task',
      subject TEXT,
      payload TEXT,
      priority TEXT DEFAULT 'medium',
      status TEXT DEFAULT 'pending',
      created_at TEXT DEFAULT CURRENT_TIMESTAMP,
      correlation_id TEXT
    );
  `);

  return db;
}

export default { createTestDb, createEmptyTestDb };
