// src/services/knowledgeService.ts
import { Pool } from 'pg';
import { env } from '../config/env';

let pool: Pool | null = null;

/**
 * Initialize PostgreSQL connection pool (lazy)
 */
function getPool(): Pool {
  if (!pool) {
    if (!env.DATABASE_URL) {
      throw new Error('DATABASE_URL not configured');
    }
    pool = new Pool({
      connectionString: env.DATABASE_URL,
    });
  }
  return pool;
}

export interface KnowledgeDocument {
  id: string;
  file_path: string;
  title: string | null;
  snippet: string;
  rank: number;
  metadata: Record<string, any>;
}

export interface SearchRequest {
  query: string;
  limit?: number;
  metadata_filter?: Record<string, any>;
}

export interface SearchResponse {
  results: KnowledgeDocument[];
  total: number;
  query: string;
}

/**
 * Search documents using PostgreSQL full-text search
 *
 * Uses ts_rank() with plainto_tsquery() for relevance ranking
 *
 * @param query - Search query string
 * @param limit - Max results (1-20, default 5)
 * @param metadataFilter - Optional JSONB filter
 * @returns Search results ordered by relevance
 */
export async function searchDocuments(
  query: string,
  limit: number = 5,
  metadataFilter?: Record<string, any>
): Promise<KnowledgeDocument[]> {
  const sql = `
    SELECT
      id,
      file_path,
      title,
      LEFT(content, 200) AS snippet,
      ts_rank(content_tsvector, plainto_tsquery('simple', $1)) AS rank,
      metadata
    FROM knowledge.documents
    WHERE content_tsvector @@ plainto_tsquery('simple', $1)
      AND ($2::jsonb IS NULL OR metadata @> $2)
    ORDER BY rank DESC
    LIMIT $3;
  `;

  const params = [
    query,
    metadataFilter ? JSON.stringify(metadataFilter) : null,
    limit,
  ];

  try {
    const db = getPool();
    const result = await db.query(sql, params);
    return result.rows;
  } catch (err) {
    console.error('[knowledgeService] searchDocuments error:', err);
    throw err;
  }
}

/**
 * Close database connection pool (for graceful shutdown)
 */
export async function closeDatabaseConnection(): Promise<void> {
  if (pool) {
    await pool.end();
    pool = null;
  }
}
