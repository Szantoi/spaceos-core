#!/usr/bin/env node
/**
 * SpaceOS Knowledge Service Ingestion Script
 *
 * - Scans /opt/spaceos/docs/knowledge/ and terminal memories
 * - Upserts to spaceos_knowledge.knowledge.documents table
 * - Parameterized pg queries (SEC-P1 SQL injection protection)
 * - SHA-256 content hash tracking for incremental updates
 * - Designed for 5-hourly cron execution
 *
 * Usage:
 *   node /opt/spaceos/scripts/02-rag-ingest.js
 *
 * Cron (INFRA):
 *   0 */5 * * * /opt/spaceos/scripts/02-rag-ingest.js >> /var/log/spaceos-rag-ingest.log 2>&1
 *
 * Deploy:
 *   chmod +x /opt/spaceos/scripts/02-rag-ingest.js
 *   sudo -u root /opt/spaceos/scripts/02-rag-ingest.js  # First run
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { Pool } = require('pg');

// Database configuration
const DB_CONFIG = {
  host: 'localhost',
  port: 5433,
  database: 'spaceos_knowledge',
  user: process.env.DB_USER || 'postgres',
  password: process.env.DB_PASSWORD || undefined,
  max: 5,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000,
};

const pool = new Pool(DB_CONFIG);

/**
 * Compute SHA-256 hash of file content
 */
async function hashFile(filePath) {
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
async function extractMetadata(filePath) {
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
      // e.g., /home/gabor/.claude/projects/-opt-spaceos-infra/memory/MEMORY.md -> 'infra'
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
      // e.g., /opt/spaceos/docs/knowledge/deployment/KNOWN_GOTCHAS.md -> 'deployment'
      const match = filePath.match(/knowledge\/([^/]+)\//);
      if (match) category = match[1];
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
  const hash = await hashFile(filePath);
  if (!hash) return false;

  const metadata = await extractMetadata(filePath);
  if (!metadata) return false;

  try {
    const { title, sourceType, category, terminal, wordCount, content } = metadata;

    // Relative path for storage (e.g., "docs/knowledge/deployment/KNOWN_GOTCHAS.md")
    const relativePath = filePath.replace('/opt/spaceos/', '');

    // UPSERT: parameterized query (prevents SQL injection)
    const result = await client.query(
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
 * Delete stale documents not in current filesystem
 */
async function deleteStaleFiles(client, currentPaths) {
  try {
    const result = await client.query(
      `DELETE FROM knowledge.documents
       WHERE file_path NOT IN (SELECT unnest($1::text[]))`,
      [currentPaths]
    );

    if (result.rowCount > 0) {
      console.log(`🗑️  Deleted ${result.rowCount} stale documents`);
    }
    return true;
  } catch (err) {
    console.error(`[ERROR] Failed to delete stale files: ${err.message}`);
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

    // Scan /opt/spaceos/docs/knowledge/
    console.log('[INFO] Scanning docs/knowledge/...');
    const knowledgePath = '/opt/spaceos/docs/knowledge';
    if (fs.existsSync(knowledgePath)) {
      scanDir(knowledgePath, files);
    }

    // Scan terminal memories
    console.log('[INFO] Scanning terminal memories...');
    const projectsDir = '/home/gabor/.claude/projects';
    if (fs.existsSync(projectsDir)) {
      fs.readdirSync(projectsDir, { withFileTypes: true }).forEach(proj => {
        const memoryPath = path.join(projectsDir, proj.name, 'memory');
        if (fs.existsSync(memoryPath)) {
          const memFiles = scanDir(memoryPath);
          // Exclude MEMORY.md (reserved for state)
          memFiles.forEach(f => {
            if (!f.endsWith('MEMORY.md')) {
              files.push(f);
            }
          });
        }
      });
    }

    console.log(`[INFO] Found ${files.length} files to ingest`);

    // Ingest each file
    let successCount = 0;
    for (const file of files) {
      const success = await ingestFile(client, file);
      if (success) successCount++;
    }

    // Delete stale documents
    const relativePaths = files
      .map(f => f.replace('/opt/spaceos/', ''))
      .filter(f => !f.endsWith('/MEMORY.md'));
    await deleteStaleFiles(client, relativePaths);

    console.log(`✅ Ingestion complete: ${successCount}/${files.length} files successfully indexed`);
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

module.exports = { ingestFile, deleteStaleFiles };
