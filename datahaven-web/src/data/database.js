/**
 * Data Access Layer - Database Connection
 *
 * Manages SQLite connection to Datahaven messages.db
 */

import Database from 'better-sqlite3';

let db = null;

/**
 * Initialize database connection
 * @param {string} dbPath - Path to SQLite database
 * @returns {Database|null} Database instance or null if failed
 */
export function initDatabase(dbPath) {
  try {
    db = new Database(dbPath, { readonly: true });
    console.log(`[DATA] Connected to database: ${dbPath}`);
    return db;
  } catch (err) {
    console.error(`[DATA] Failed to connect to database: ${err.message}`);
    console.log('[DATA] Running in demo mode without database');
    return null;
  }
}

/**
 * Get database instance
 * @returns {Database|null}
 */
export function getDb() {
  return db;
}

/**
 * Close database connection
 */
export function closeDatabase() {
  if (db) {
    db.close();
    db = null;
    console.log('[DATA] Database connection closed');
  }
}

export default { initDatabase, getDb, closeDatabase };
