/**
 * seed-agent-db.ts — Load roles, schemas, runbooks, workflows, templates, standards from filesystem
 *
 * This script reads the database/roles/ and database/standards/ directories
 * and populates the agent.db with role definitions, MCP permissions, runbooks,
 * workflows, templates, and organizational standards.
 *
 * **Idempotent:** Uses INSERT OR REPLACE, so re-running is safe (no duplicates).
 *
 * **Usage:**
 * ```
 * npx ts-node scripts/seed-agent-db.ts
 * ```
 *
 * **Output:**
 * ```
 * ✅ Seeding complete:
 *    - roles: 12
 *    - role_schemas: 12
 *    - runbooks: 12
 *    - workflows: 24
 *    - templates: 36
 *    - standards: 8
 * Total: 104 records seeded
 * ```
 */

import * as fs from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';
import { AgentDb } from '../src/mcp/AgentDb';
import { DatabaseConnectionManager } from '../src/metadata/DatabaseConnectionManager';
import { RetryableSeeder } from '../src/rag/RetryableSeeder';
import { SchemaVersionManager } from '../src/mcp/SchemaVersionManager';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES & INTERFACES
// ─────────────────────────────────────────────────────────────────────────────

interface SeederStats {
  roles: number;
  role_schemas: number;
  runbooks: number;
  workflows: number;
  templates: number;
  standards: number;
  dwis: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// SEEDER CLASS
// ─────────────────────────────────────────────────────────────────────────────

export class AgentDbSeeder {
  private agentDb: AgentDb;
  private connectionManager: DatabaseConnectionManager;
  private retryableSeeder: RetryableSeeder;
  private schemaVersionManager: SchemaVersionManager;
  private stats: SeederStats = {
    roles: 0,
    role_schemas: 0,
    runbooks: 0,
    workflows: 0,
    templates: 0,
    standards: 0,
    dwis: 0,
  };

  constructor(
    private rolesDir: string,
    private standardsDir: string,
    private discoveryDir: string,
    dbPath: string
  ) {
    // Initialize dual-pool connection manager
    this.connectionManager = new DatabaseConnectionManager(dbPath);
    this.agentDb = new AgentDb(this.connectionManager);
    this.agentDb.initSchema();

    // Initialize retry seeder (max 3 retries, exponential backoff)
    this.retryableSeeder = new RetryableSeeder({
      maxRetries: 3,
      initialDelayMs: 100,
      maxDelayMs: 8000,
    });

    // Initialize schema version manager (TASK-09-04B)
    this.schemaVersionManager = new SchemaVersionManager(this.connectionManager.getAdminPool());
  }

  /**
   * Run the complete seeding process.
   */
  public async seed(): Promise<void> {
    console.info('[AgentDbSeeder] Starting database seeding...\n');

    try {
      // Seed roles, schemas, runbooks from database/roles/
      await this.seedRoles();

      // Seed standards from database/standards/
      await this.seedStandards();

      // Seed Discovery Work Items from Docs (TASK-18-03)
      await this.seedDiscoveryWorkItems();

      // Seed phase gate history for existing DWIs (TASK-18-03)
      if (typeof (this as any).seedDwiPhaseGates === 'function') {
        await (this as any).seedDwiPhaseGates();
      }

      // ┌─ WAL CHECKPOINT (TASK-09-02B) ─────────────────────────────────────┐
      // │ Force FULL checkpoint after bulk inserts to ensure agents see data  │
      // │ and prevent lock contention during concurrent access              │
      // └────────────────────────────────────────────────────────────────────┘
      console.info('\n[AgentDbSeeder] Forcing WAL checkpoint after bulk inserts...');
      const walOptimizer = this.connectionManager.getWalOptimizer();
      const checkpointResult = walOptimizer.forceCheckpoint();
      console.info(
        `[AgentDbSeeder] ✓ Checkpoint complete: busy=${checkpointResult.busy}, log=${checkpointResult.log}, checkpointed=${checkpointResult.checkpointed}`
      );

      // ┌─ SCHEMA VERSION TRACKING (TASK-09-04B) ─────────────────────────────┐
      // │ Increment read-layer version to signal agents that schema updated  │
      // │ Agents load version at bootstrap + check end-of-session for changes│
      // └────────────────────────────────────────────────────────────────────┘
      console.info('[AgentDbSeeder] Incrementing schema version for agents...');
      this.schemaVersionManager.incrementReadLayerVersion();
      this.schemaVersionManager.logVersions();

      // Log final statistics
      this.logStats();

      console.info('[AgentDbSeeder] ✅ Seeding complete!\n');
    } catch (error) {
      console.error('[AgentDbSeeder] ❌ Seeding failed:', error);
      throw error;
    }
  }

  /**
   * Close internal DB connection pools.
   */
  public close(): void {
    this.agentDb.close();
  }

  /**
   * Seed roles, schemas, runbooks, workflows, and templates from database/roles/
   */
  private async seedRoles(): Promise<void> {
    const domains = fs.readdirSync(this.rolesDir).filter(f => {
      const fullPath = path.join(this.rolesDir, f);
      return fs.statSync(fullPath).isDirectory();
    });

    console.info(`[Seeder] Found ${domains.length} domain(s): ${domains.join(', ')}`);

    for (const domain of domains) {
      const domainDir = path.join(this.rolesDir, domain);
      const roles = fs.readdirSync(domainDir).filter(f => {
        const fullPath = path.join(domainDir, f);
        return fs.statSync(fullPath).isDirectory();
      });

      console.info(`  └─ ${domain}: ${roles.length} role(s)`);

      for (const roleName of roles) {
        const roleDir = path.join(domainDir, roleName);
        this.seedRole(domain, roleName, roleDir);
      }
    }
  }

  /**
   * Seed a single role (and its associated schemas, runbooks, workflows, templates).
   */
  private seedRole(domain: string, roleName: string, roleDir: string): void {
    const db = this.agentDb.getRawDatabase();

    // ─── ROLE DEFINITION ────────────────────────────────────────────────────

    const roleFile = path.join(roleDir, `${roleName}.role.md`);
    if (fs.existsSync(roleFile)) {
      const content = fs.readFileSync(roleFile, 'utf-8');
      const stmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content, created_at, last_updated)
        VALUES (?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
      `);

      // Wrap with retry logic (TASK-09-03B)
      this.retryableSeeder.executeWithRetrSync(
        () => stmt.run(domain, roleName, content),
        `insert role ${domain}/${roleName}`
      );
      this.stats.roles++;
    }

    // ─── ROLE SCHEMA (MCP PERMISSIONS) ──────────────────────────────────────

    const schemaFile = path.join(roleDir, `${roleName}.schema.yaml`);
    if (fs.existsSync(schemaFile)) {
      try {
        const schemaContent = fs.readFileSync(schemaFile, 'utf-8');
        const schemaParsed = yaml.load(schemaContent) as unknown;
        const schemaJson = JSON.stringify(schemaParsed);

        const stmt = db.prepare(`
          INSERT OR REPLACE INTO role_schemas (domain, role_name, mcp_tool_permissions, created_at, last_updated)
          VALUES (?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        `);

        // Wrap with retry logic
        this.retryableSeeder.executeWithRetrSync(
          () => stmt.run(domain, roleName, schemaJson),
          `insert role schema ${domain}/${roleName}`
        );
        this.stats.role_schemas++;
      } catch (error) {
        console.warn(`  ⚠️  Failed to parse schema for ${domain}/${roleName}:`, error);
      }
    }

    // ─── RUNBOOK ────────────────────────────────────────────────────────────

    const runbookFile = path.join(roleDir, `${roleName}.runbook.md`);
    if (fs.existsSync(runbookFile)) {
      const content = fs.readFileSync(runbookFile, 'utf-8');
      const stmt = db.prepare(`
        INSERT OR REPLACE INTO runbooks (domain, role_name, content, created_at, last_updated)
        VALUES (?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
      `);

      // Wrap with retry logic
      this.retryableSeeder.executeWithRetrSync(
        () => stmt.run(domain, roleName, content),
        `insert runbook ${domain}/${roleName}`
      );
      this.stats.runbooks++;
    }

    // ─── WORKFLOWS ──────────────────────────────────────────────────────────

    const workflowsDir = path.join(roleDir, 'workflows');
    if (fs.existsSync(workflowsDir)) {
      const workflowFiles = fs.readdirSync(workflowsDir).filter(f => f.endsWith('.workflow.md'));

      for (const workflowFile of workflowFiles) {
        const workflowPath = path.join(workflowsDir, workflowFile);
        const content = fs.readFileSync(workflowPath, 'utf-8');

        // Extract workflow type from filename (e.g., "feature_delivery.workflow.md" => "feature_delivery")
        const workflowType = workflowFile.replace('.workflow.md', '');

        const stmt = db.prepare(`
          INSERT OR REPLACE INTO workflows (domain, role_name, workflow_type, content, created_at, last_updated)
          VALUES (?, ?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        `);

        // Wrap with retry logic
        this.retryableSeeder.executeWithRetrSync(
          () => stmt.run(domain, roleName, workflowType, content),
          `insert workflow ${domain}/${roleName}/${workflowType}`
        );
        this.stats.workflows++;
      }
    }

    // ─── TEMPLATES ──────────────────────────────────────────────────────────

    const templatesDir = path.join(roleDir, 'templates');
    if (fs.existsSync(templatesDir)) {
      const templateFiles = fs.readdirSync(templatesDir).filter(f => f.endsWith('.template.md'));

      for (const templateFile of templateFiles) {
        const templatePath = path.join(templatesDir, templateFile);
        const content = fs.readFileSync(templatePath, 'utf-8');

        // Extract template name from filename (e.g., "implementation_summary.template.md" => "implementation_summary")
        const templateName = templateFile.replace('.template.md', '');

        const stmt = db.prepare(`
          INSERT OR REPLACE INTO templates (domain, role_name, template_name, content, created_at, last_updated)
          VALUES (?, ?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        `);

        // Wrap with retry logic
        this.retryableSeeder.executeWithRetrSync(
          () => stmt.run(domain, roleName, templateName, content),
          `insert template ${domain}/${roleName}/${templateName}`
        );
        this.stats.templates++;
      }
    }
  }

  /**
   * Seed standards from database/standards/
   */
  private async seedStandards(): Promise<void> {
    const standardsDir = this.standardsDir;

    if (!fs.existsSync(standardsDir)) {
      console.warn('[Seeder] Standards directory not found:', standardsDir);
      return;
    }

    const db = this.agentDb.getRawDatabase();

    // Recursively find all .md files in database/standards/
    const findMarkdownFiles = (dir: string): string[] => {
      let files: string[] = [];
      const entries = fs.readdirSync(dir, { withFileTypes: true });

      for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory() && entry.name !== 'node_modules') {
          files = files.concat(findMarkdownFiles(fullPath));
        } else if (entry.isFile() && entry.name.endsWith('.md')) {
          files.push(fullPath);
        }
      }

      return files;
    };

    const markdownFiles = findMarkdownFiles(standardsDir);
    console.info(`[Seeder] Found ${markdownFiles.length} standard(s)`);

    for (const filePath of markdownFiles) {
      try {
        const content = fs.readFileSync(filePath, 'utf-8');

        // Extract standard ID from filename or frontmatter
        // Convention: filename like "ADR-001-example.md" or extract from frontmatter "id: ADR-001"
        let stdId = this.extractStandardId(filePath, content);

        if (!stdId) {
          console.warn(`  ⚠️  Could not extract standard ID from ${filePath}`);
          continue;
        }

        const stmt = db.prepare(`
          INSERT OR REPLACE INTO standards (id, std_id, content, created_at, last_updated)
          VALUES (?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        `);

        // Wrap with retry logic (TASK-09-03B)
        this.retryableSeeder.executeWithRetrSync(
          () => stmt.run(stdId, stdId, content),
          `insert standard ${stdId}`
        );
        this.stats.standards++;
      } catch (error) {
        console.warn(`  ⚠️  Failed to seed standard ${filePath}:`, error);
      }
    }
  }

  /**
   * Seed Discovery Work Items (DWI) from markdown state files.
   *
   * Uses frontmatter YAML parsing (js-yaml) to extract required fields.
   * Inserts into `discovery_work_items` with INSERT OR REPLACE and idempotent semantics.
   */
  private async seedDiscoveryWorkItems(): Promise<void> {
    const discoveryDir = this.discoveryDir;

    if (!fs.existsSync(discoveryDir)) {
      console.warn('[Seeder] Discovery directory not found:', discoveryDir);
      return;
    }

    const dwiFiles = this.findFilesRecursive(discoveryDir, 'dwi-state.md');
    console.info(`[Seeder] Found ${dwiFiles.length} DWI state file(s)`);

    const db = this.agentDb.getRawDatabase();
    const dwiStmt = db.prepare(`
      INSERT OR REPLACE INTO discovery_work_items (
        id, topic, status, current_phase, next_action, verdict,
        hypothesis_count, validated_count, created_at, updated_at,
        created_by, updated_by
      ) VALUES (
        ?, ?, ?, ?, ?, ?, ?, ?,
        COALESCE(?, datetime('now')),
        COALESCE(?, datetime('now')),
        ?, ?
      )
    `);

    const deletePhaseGatesStmt = db.prepare(`DELETE FROM dwi_phase_gates WHERE dwi_id = ?`);
    const insertPhaseGateStmt = db.prepare(`
      INSERT INTO dwi_phase_gates (dwi_id, phase, gate_crossed, gate_crossed_date, notes)
      VALUES (?, ?, ?, ?, ?)
    `);

    const insertHypothesisStmt = db.prepare(`
      INSERT OR REPLACE INTO dwi_hypotheses (
        id, dwi_id, statement, status, phase, artifact_path, created_at, closed_at, created_by, updated_by
      ) VALUES (?, ?, ?, ?, ?, ?, datetime('now'), NULL, NULL, NULL)
    `);

    for (const filePath of dwiFiles) {
      try {
        const content = fs.readFileSync(filePath, 'utf-8');
        const frontmatter = this.extractFrontmatter(content);
        if (!frontmatter) {
          console.warn(`  ⚠️  No frontmatter found in ${filePath}`);
          continue;
        }

        if (frontmatter.type !== 'discovery_work_item') {
          console.warn(`  ⚠️  Skipping non-DWI state file: ${filePath}`);
          continue;
        }

        const dwiId = String(frontmatter.id ?? '').trim();
        if (!dwiId) {
          console.warn(`  ⚠️  Missing id in DWI frontmatter: ${filePath}`);
          continue;
        }

        const dwiTopic = String(frontmatter.topic ?? '').trim();
        const status = String(frontmatter.status ?? '').trim();
        const currentPhase = Number(frontmatter.current_phase ?? 0);
        const nextAction = String(frontmatter.next_action ?? '').trim();
        const verdict = frontmatter.verdict === null ? null : String(frontmatter.verdict ?? null);
        const hypothesisCount = Number(frontmatter.hypothesis_count ?? 0);
        const validatedCount = Number(frontmatter.validated_count ?? 0);
        const createdAt = frontmatter.created ? String(frontmatter.created) : null;
        const updatedAt = frontmatter.updated ? String(frontmatter.updated) : null;

        this.retryableSeeder.executeWithRetrSync(
          () =>
            dwiStmt.run(
              dwiId,
              dwiTopic,
              status,
              currentPhase,
              nextAction,
              verdict,
              hypothesisCount,
              validatedCount,
              createdAt,
              updatedAt,
              null,
              null
            ),
          `insert DWI ${dwiId}`
        );

        // Phase Gate History
        deletePhaseGatesStmt.run(dwiId);
        const phaseGateTable = this.extractSectionTable(content, 'Phase Gate History');
        for (const row of phaseGateTable) {
          const phase = Number(row['phase'] ?? row['phase '] ?? 0);
          const gate_crossed = /\b(✅|yes|true)\b/i.test(row['gate crossed'] ?? row['gate_crossed'] ?? '');
          const gate_crossed_date = row['date'] ? String(row['date']).trim() : null;
          const notes = row['notes'] ? String(row['notes']).trim() : null;

          if (!Number.isNaN(phase)) {
            insertPhaseGateStmt.run(dwiId, phase, gate_crossed ? 1 : 0, gate_crossed_date, notes);
          }
        }

        // Hypotheses
        const hypothesisTable = this.extractSectionTable(content, 'Hypothesis Summary');
        for (const row of hypothesisTable) {
          const id = String(row['id'] ?? '').trim();
          const statement = String(row['statement (short)'] ?? row['statement'] ?? '').trim();
          const statusRaw = String(row['status'] ?? '').trim().toLowerCase();
          const status = ['open', 'testing', 'validated', 'invalidated'].includes(statusRaw)
            ? statusRaw
            : 'open';
          const phase = Number(frontmatter.current_phase ?? 1);

          if (!id) {
            continue;
          }
          insertHypothesisStmt.run(id, dwiId, statement, status, phase, null);
        }

        this.stats.dwis++;
      } catch (error) {
        console.warn(`  ⚠️  Failed to seed DWI from ${filePath}:`, error);
      }
    }
  }

  /**
   * Seed phase gate history for existing DWIs.
   *
   * NOTE: This is a legacy placeholder (TASK-18-03).
   * The current seeding implementation populates phase gates directly
   * when importing state files in seedDiscoveryWorkItems().
   */
  private async seedDwiPhaseGates(): Promise<void> {
    // No-op for now; retained for backward compatibility with older seeding scripts.
    return;
  }

  /**
   * Extract a markdown table that appears under a given section header.
   */
  private extractSectionTable(content: string, sectionHeading: string): Array<Record<string, string>> {
    const lines = content.split(/\r?\n/);
    const startIndex = lines.findIndex((l) => l.trim().toLowerCase().startsWith(`## ${sectionHeading.toLowerCase()}`));
    if (startIndex === -1) return [];

    const tableLines: string[] = [];
    for (let i = startIndex + 1; i < lines.length; i++) {
      const line = lines[i].trim();
      if (line.startsWith('## ')) break; // next section
      if (line.startsWith('|')) {
        tableLines.push(line);
      }
    }

    return this.parseMarkdownTable(tableLines);
  }

  /**
   * Recursively find files matching a given filename under a directory.
   */
  private findFilesRecursive(dir: string, filename: string): string[] {
    let results: string[] = [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        results = results.concat(this.findFilesRecursive(fullPath, filename));
      } else if (entry.isFile() && entry.name === filename) {
        results.push(fullPath);
      }
    }

    return results;
  }

  /**
   * Parse a markdown table (simple pipe-separated format) into objects.
   */
  private parseMarkdownTable(tableLines: string[]): Array<Record<string, string>> {
    if (tableLines.length < 2) {
      return [];
    }

    // Normalize: trim and remove leading/trailing pipes
    const normalize = (line: string) => line.trim().replace(/^\|/, '').replace(/\|$/, '');

    const headers = normalize(tableLines[0])
      .split('|')
      .map((h) => h.trim().toLowerCase());

    // Skip second line (separator) if present
    const dataLines = tableLines.slice(1).filter((line) => line.trim().startsWith('|'));

    const rows: Array<Record<string, string>> = [];
    for (const line of dataLines) {
      const cols = normalize(line).split('|').map((c) => c.trim());
      if (cols.length !== headers.length) continue;
      const row: Record<string, string> = {};
      headers.forEach((h, idx) => {
        row[h] = cols[idx] ?? '';
      });
      rows.push(row);
    }

    return rows;
  }

  /**
   * Extract standard ID from filename or YAML frontmatter.
   *
   * Examples:
   * - Filename: "ADR-001-example.md" => "ADR-001"
   * - Frontmatter: "id: ADR-001" => "ADR-001"
   */
  private extractFrontmatter(content: string): Record<string, unknown> | null {
    const match = content.match(/^---\n([\s\S]*?)\n---/);
    if (!match) {
      return null;
    }

    try {
      return yaml.load(match[1]) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  /**
   * Extract standard ID from filename or YAML frontmatter.
   *
   * Examples:
   * - Filename: "ADR-001-example.md" => "ADR-001"
   * - Frontmatter: "id: ADR-001" => "ADR-001"
   */
  private extractStandardId(filePath: string, content: string): string | null {
    // Try frontmatter first
    const frontmatter = this.extractFrontmatter(content);
    if (frontmatter && frontmatter.id && typeof frontmatter.id === 'string') {
      return frontmatter.id;
    }

    // Try filename extraction (e.g., "ADR-001-example.md" => "ADR-001")
    const filename = path.basename(filePath, '.md');
    const match = filename.match(/^(ADR-\d+|STD-[A-Z0-9-]+)/);
    if (match) {
      return match[1];
    }

    return null;
  }

  /**
   * Log seeding statistics.
   */
  private logStats(): void {
    const total =
      this.stats.roles +
      this.stats.role_schemas +
      this.stats.runbooks +
      this.stats.workflows +
      this.stats.templates +
      this.stats.standards +
      this.stats.dwis;

    console.info('\n📊 Seeding Statistics:');
    console.info(`  • roles: ${this.stats.roles}`);
    console.info(`  • role_schemas: ${this.stats.role_schemas}`);
    console.info(`  • runbooks: ${this.stats.runbooks}`);
    console.info(`  • workflows: ${this.stats.workflows}`);
    console.info(`  • templates: ${this.stats.templates}`);
    console.info(`  • standards: ${this.stats.standards}`);
    console.info(`  • dwis: ${this.stats.dwis}`);
    console.info(`\n  Total: ${total} records seeded\n`);
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN
// ─────────────────────────────────────────────────────────────────────────────

const main = async () => {
  const workspaceRoot = path.join(__dirname, '..');
  const rolesDir = path.join(workspaceRoot, 'database', 'roles');
  const standardsDir = path.join(workspaceRoot, 'database', 'standards');
  const discoveryDir = path.join(workspaceRoot, 'Docs', 'mcp-context-server', 'discovery');
  const dbPath = path.join(workspaceRoot, 'database', 'metadata.db');

  if (!fs.existsSync(rolesDir)) {
    console.error(`❌ Roles directory not found: ${rolesDir}`);
    process.exit(1);
  }

  const seeder = new AgentDbSeeder(rolesDir, standardsDir, discoveryDir, dbPath);

  try {
    await seeder.seed();
    seeder.close();
    process.exit(0);
  } catch (error) {
    console.error('❌ Seeding failed:', error);
    seeder.close();
    process.exit(1);
  }
};

if (require.main === module) {
  main();
}
