import Database from 'better-sqlite3';
import { Episode } from './types';

/**
 * Executes a keyword search against the FTS5 virtual table for episodes.
 *
 * ⚠️ SYNCHRONOUS API! better-sqlite3 is synchronous — DO NOT use async/await.
 * ⚠️ SQL injection protection: ALWAYS use parameterized queries (? placeholder).
 *
 * @param db The initialized Database connection.
 * @param query The user's text query (e.g., "ideation AND engineering").
 * @param domainFilter Optional domain to restrict the search.
 * @returns Array of Episode objects matching the search, ordered by relevance rank.
 */
export function searchExperience(
    db: Database.Database,
    query: string,
    domainFilter?: string
): Episode[] {
    // ⚠️ FTS5 MATCH syntax: double quote escape + filter  // Basic sanitization for FTS5 MATCH syntax
    // We want to avoid syntax errors if a user passes unbalanced quotes or hazardous characters.
    const sanitizedQuery = query
        .replace(/["]/g, '""')       // escape double quotes for FTS5
        .replace(/[;'\\]/g, '')      // remove strictly dangerous SQL/FTS characters
        .replace(/[-*+]/g, ' ')      // strip FTS operators that cause syntax errors if misused
        .trim();

    if (!sanitizedQuery) return [];

    // Because content='episodes' is used, we must JOIN with the base table
    let sql = `
    SELECT e.* FROM episodes e
    INNER JOIN episodes_fts ON episodes_fts.rowid = e.rowid
    WHERE episodes_fts MATCH ?
  `;
    const params: unknown[] = [sanitizedQuery];

    if (domainFilter) {
        sql += ` AND e.domain = ?`;
        params.push(domainFilter);
    }

    // rank = BM25 relevance score (built-in FTS5 ranking function)
    sql += ` ORDER BY rank LIMIT 50`;

    const results = db.prepare(sql).all(...params) as Episode[];

    // Note: toolCalls, artifacts, and outcome_summary are expected to be JSON or text formats exactly as defined.
    // We parse the string JSON fields manually if needed, but since the raw db format matches types closely
    // (and better-sqlite3 returns raw text for JSON fields), we would typically iterate and parse.
    // The interface defines them as any/arrays from JSON but the DB returns strings unless transformed.
    // To match the EpisodeStore behavior:
    return results.map(row => {
        const raw: any = row;
        return {
            id: raw.id,
            sessionId: raw.session_id,
            domain: raw.domain,
            track: raw.track,
            phase: raw.phase,
            toolCalls: typeof raw.tool_calls_json === 'string' ? JSON.parse(raw.tool_calls_json) : [],
            artifacts: typeof raw.artifacts_json === 'string' ? JSON.parse(raw.artifacts_json) : [],
            outcomeSummary: raw.outcome_summary,
            createdAt: new Date(raw.created_at)
        } as Episode;
    });
}
