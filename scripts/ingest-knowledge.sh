#!/usr/bin/env node
/**
 * SpaceOS Knowledge Service Ingestion Script
 *
 * Modified from INFRA 02-rag-ingest.js for LIBRARIAN task MSG-LIBRARIAN-001
 *
 * Changes:
 * - Database: spaceos (not spaceos_knowledge)
 * - Excluded paths: mailbox/, planning/, tasks/ per inbox spec
 * - Scans docs/ directory per inbox requirements
 *
 * Usage:
 *   node /opt/spaceos/scripts/ingest-knowledge.sh
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { Pool } = require('pg');

// Database configuration (spaceos database, port 5433)
// Using Unix socket with peer authentication (postgres user)
const DB_CONFIG = {
  host: '/var/run/postgresql',
  port: 5433,
  database: 'spaceos',
  user: 'postgres',
  max: 5,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000,
};

const pool = new Pool(DB_CONFIG);

// Excluded paths (per inbox spec)
const EXCLUDED_PATTERNS = [
  '/mailbox/',
  '/planning/',
  '/tasks/',
];

/**
 * Compute SHA-256 hash of file content
 */
function hashFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    return crypto.createHash('sha256').update(content).digest('hex');
  } catch (err) {
    console.error(`[ERROR] Failed to hash ${filePath}: ${err.message}`);
    return null;
  }
}

/**
 * Extract metadata from markdown file
 */
function extractMetadata(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');

    // Extract title from first # heading or filename
    const titleLine = lines.find(l => l.match(/^#+\s+/));
    const title = titleLine
      ? titleLine.replace(/^#+\s+/, '').trim()
      : path.basename(filePath, '.md');

    // Determine source_type and category from path
    let sourceType = 'knowledge';
    let category = null;
    let terminal = null;

    if (filePath.includes('/memory/')) {
      sourceType = 'memory';
      // Extract terminal from project directory
      const match = filePath.match(/-opt-spaceos-([^/]+)/);
      if (match) {
        terminal = match[1]
          .toLowerCase()
          .replace('spaceos-', '')
          .replace('backend-', '')
          .replace('frontend-', '');
      }
    } else {
      // Extract category from knowledge/ subdirectory
      const match = filePath.match(/knowledge\/([^/]+)\//);
      if (match) {
        category = match[1];
      } else if (filePath.includes('/architecture/')) {
        category = 'architecture';
      } else if (filePath.includes('/vision/')) {
        category = 'vision';
      } else if (filePath.includes('/docs/')) {
        category = 'system';
      }
    }

    const wordCount = content.split(/\s+/).filter(w => w.length > 0).length;

    return { title, sourceType, category, terminal, wordCount, content };
  } catch (err) {
    console.error(`[ERROR] Failed to extract metadata from ${filePath}: ${err.message}`);
    return null;
  }
}

/**
 * Ingest a single file into the database
 */
async function ingestFile(client, filePath) {
  const hash = hashFile(filePath);
  if (!hash) return false;

  const metadata = extractMetadata(filePath);
  if (!metadata) return false;

  try {
    const { title, sourceType, category, terminal, wordCount, content } = metadata;

    // Relative path for storage
    const relativePath = filePath.replace('/opt/spaceos/', '');

    // UPSERT: parameterized query (prevents SQL injection)
    await client.query(
      `INSERT INTO knowledge.documents
       (file_path, source_type, category, terminal, title, content, content_hash, word_count)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
       ON CONFLICT (file_path) DO UPDATE SET
         title = $5,
         content = $6,
         content_hash = $7,
         word_count = $8,
         updated_at = now()`,
      [relativePath, sourceType, category, terminal, title, content, hash, wordCount]
    );

    console.log(`✓ ${relativePath} (${wordCount} words)`);
    return true;
  } catch (err) {
    console.error(`✗ ${filePath}: ${err.message}`);
    return false;
  }
}

/**
 * Recursively scan directory for markdown files
 */
function scanDir(dir, files = []) {
  try {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    entries.forEach(ent => {
      const fullPath = path.join(dir, ent.name);

      // Skip excluded patterns
      if (EXCLUDED_PATTERNS.some(pattern => fullPath.includes(pattern))) {
        return;
      }

      if (ent.isDirectory()) {
        scanDir(fullPath, files);
      } else if (ent.name.endsWith('.md')) {
        files.push(fullPath);
      }
    });
  } catch (err) {
    console.error(`[WARN] Failed to scan ${dir}: ${err.message}`);
  }
  return files;
}

/**
 * Main ingestion loop
 */
async function run() {
  const client = await pool.connect();

  try {
    console.log(`[INFO] Starting knowledge base ingestion at ${new Date().toISOString()}`);

    const files = [];

    // Scan /opt/spaceos/docs/ (excluding mailbox, planning, tasks)
    console.log('[INFO] Scanning docs/ directory...');
    const docsPath = '/opt/spaceos/docs';
    if (fs.existsSync(docsPath)) {
      scanDir(docsPath, files);
    }

    console.log(`[INFO] Found ${files.length} files to ingest`);

    // Ingest each file
    let successCount = 0;
    for (const file of files) {
      const success = await ingestFile(client, file);
      if (success) successCount++;
    }

    console.log(`✅ Ingestion complete: ${successCount}/${files.length} files successfully indexed`);

    // Display count by category
    const stats = await client.query(`
      SELECT source_type, category, COUNT(*) as count
      FROM knowledge.documents
      GROUP BY source_type, category
      ORDER BY source_type, category
    `);

    console.log('\n📊 Documents by category:');
    stats.rows.forEach(row => {
      console.log(`   ${row.source_type}/${row.category || 'root'}: ${row.count} documents`);
    });

    const total = await client.query('SELECT COUNT(*) FROM knowledge.documents');
    console.log(`\n📚 Total documents: ${total.rows[0].count}`);

  } catch (err) {
    console.error(`[FATAL] Ingestion failed: ${err.message}`);
    process.exit(1);
  } finally {
    await client.release();
    await pool.end();
  }
}

// Error handling
process.on('unhandledRejection', (reason, promise) => {
  console.error('[FATAL] Unhandled rejection:', reason);
  process.exit(1);
});

// Run
if (require.main === module) {
  run().catch(err => {
    console.error('[FATAL]', err);
    process.exit(1);
  });
}

module.exports = { ingestFile };
