import { z } from 'zod';
import { McpContext } from '../middleware/contextMiddleware';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';
import * as fs from 'fs/promises';
import * as path from 'path';

/**
 * Files Plugin — Knowledge Base File Management for MCP
 *
 * Enables agents to upload files to the knowledge base and trigger reindexing.
 * Provides both local file operations and HTTP API integration.
 */

interface FileInfo {
    name: string;
    path: string;
    size: number;
    modified: string;
    category: string;
}

interface UploadResult {
    success: boolean;
    file?: FileInfo;
    reindex?: { success: boolean; indexed?: number };
    error?: string;
}

const UPLOAD_BASE = process.env.UPLOAD_PATH || '/opt/spaceos/docs/uploads';
const KNOWLEDGE_BASE = process.env.KNOWLEDGE_PATH || '/opt/spaceos/docs/knowledge';
const DATAHAVEN_URL = process.env.DATAHAVEN_URL || 'http://localhost:3457';
const KNOWLEDGE_URL = process.env.KNOWLEDGE_URL || 'http://localhost:3456';

const CATEGORIES: Record<string, string> = {
    upload: UPLOAD_BASE,
    knowledge: KNOWLEDGE_BASE,
    architecture: path.join(KNOWLEDGE_BASE, 'architecture'),
    deployment: path.join(KNOWLEDGE_BASE, 'deployment'),
    patterns: path.join(KNOWLEDGE_BASE, 'patterns'),
    security: path.join(KNOWLEDGE_BASE, 'security'),
    context: path.join(KNOWLEDGE_BASE, 'context'),
};

const ALLOWED_EXTENSIONS = ['.md', '.txt', '.pdf', '.json', '.yaml', '.yml', '.csv'];
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

@Plugin({
    name: "files",
    version: "1.0.0",
    dependencies: []
})
export class FilesPlugin extends BasePlugin {
    constructor(context: any) {
        super(context);
    }

    /**
     * Trigger knowledge reindex via HTTP API
     */
    private async triggerReindex(): Promise<{ success: boolean; indexed?: number }> {
        try {
            const response = await fetch(`${KNOWLEDGE_URL}/api/knowledge/index`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            if (response.ok) {
                const data = await response.json();
                return { success: true, indexed: data.indexed || 0 };
            }
            return { success: false };
        } catch (err) {
            return { success: false };
        }
    }

    /**
     * Upload a text file to the knowledge base
     */
    @Tool({
        name: "knowledge_upload",
        description: "Upload text content as a file to the SpaceOS knowledge base. Automatically triggers reindexing for knowledge categories. Use for sharing documentation, notes, or reports with other agents.",
        inputSchema: z.object({
            filename: z.string().describe("The filename to save as (e.g., 'my-doc.md')"),
            content: z.string().describe("The text content of the file"),
            category: z.enum(['upload', 'knowledge', 'architecture', 'deployment', 'patterns', 'security', 'context'])
                .default('upload')
                .describe("Target category: 'upload' for general files, others for RAG-indexed knowledge"),
        })
    })
    async uploadFile(
        _ctx: McpContext,
        input: { filename: string; content: string; category: string }
    ): Promise<UploadResult> {
        const { filename, content, category } = input;

        // Validate extension
        const ext = path.extname(filename).toLowerCase();
        if (!ALLOWED_EXTENSIONS.includes(ext)) {
            return {
                success: false,
                error: `Unsupported file type: ${ext}. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}`
            };
        }

        // Check content size
        const size = Buffer.byteLength(content, 'utf8');
        if (size > MAX_FILE_SIZE) {
            return {
                success: false,
                error: `Content too large. Max size: ${MAX_FILE_SIZE / 1024 / 1024} MB`
            };
        }

        const targetDir = CATEGORIES[category] || UPLOAD_BASE;

        try {
            // Ensure directory exists
            await fs.mkdir(targetDir, { recursive: true });

            // Generate unique filename if exists
            let targetPath = path.join(targetDir, filename);
            try {
                await fs.access(targetPath);
                // File exists, add timestamp
                const stem = path.basename(filename, ext);
                const timestamp = Date.now();
                targetPath = path.join(targetDir, `${stem}_${timestamp}${ext}`);
            } catch {
                // File doesn't exist, use original name
            }

            // Write file
            await fs.writeFile(targetPath, content, 'utf8');
            await fs.chmod(targetPath, 0o644);

            const stats = await fs.stat(targetPath);
            const result: UploadResult = {
                success: true,
                file: {
                    name: path.basename(targetPath),
                    path: targetPath.replace('/opt/spaceos/', ''),
                    size: stats.size,
                    modified: stats.mtime.toISOString(),
                    category
                }
            };

            // Trigger reindex if knowledge file
            if (category !== 'upload') {
                result.reindex = await this.triggerReindex();
            }

            return result;

        } catch (err: any) {
            return {
                success: false,
                error: `Upload failed: ${err.message}`
            };
        }
    }

    /**
     * List files in a category
     */
    @Tool({
        name: "knowledge_list",
        description: "List files in a knowledge base category. Returns recent files sorted by modification time.",
        inputSchema: z.object({
            category: z.enum(['upload', 'knowledge', 'architecture', 'deployment', 'patterns', 'security', 'context'])
                .default('upload')
                .describe("Category to list files from"),
            limit: z.number().min(1).max(100).default(20).describe("Maximum files to return"),
        })
    })
    async listFiles(
        _ctx: McpContext,
        input: { category: string; limit: number }
    ): Promise<{ files: FileInfo[]; total: number; category: string }> {
        const { category, limit } = input;
        const targetDir = CATEGORIES[category] || UPLOAD_BASE;

        try {
            await fs.mkdir(targetDir, { recursive: true });

            const entries = await fs.readdir(targetDir, { withFileTypes: true });
            const files: FileInfo[] = [];

            for (const entry of entries) {
                if (entry.isFile()) {
                    const filePath = path.join(targetDir, entry.name);
                    const stats = await fs.stat(filePath);
                    files.push({
                        name: entry.name,
                        path: filePath.replace('/opt/spaceos/', ''),
                        size: stats.size,
                        modified: stats.mtime.toISOString(),
                        category
                    });
                }
            }

            // Sort by modification time, newest first
            files.sort((a, b) => new Date(b.modified).getTime() - new Date(a.modified).getTime());

            return {
                files: files.slice(0, limit),
                total: files.length,
                category
            };
        } catch (err: any) {
            return {
                files: [],
                total: 0,
                category
            };
        }
    }

    /**
     * Read a file from the knowledge base
     */
    @Tool({
        name: "knowledge_read",
        description: "Read the content of a file from the knowledge base. Returns the full text content.",
        inputSchema: z.object({
            category: z.enum(['upload', 'knowledge', 'architecture', 'deployment', 'patterns', 'security', 'context'])
                .describe("Category the file is in"),
            filename: z.string().describe("Name of the file to read"),
        })
    })
    async readFile(
        _ctx: McpContext,
        input: { category: string; filename: string }
    ): Promise<{ success: boolean; content?: string; error?: string }> {
        const { category, filename } = input;
        const targetDir = CATEGORIES[category] || UPLOAD_BASE;
        const filePath = path.join(targetDir, filename);

        // Security check: ensure path is within allowed directory
        if (!filePath.startsWith(targetDir)) {
            return { success: false, error: 'Access denied' };
        }

        try {
            const content = await fs.readFile(filePath, 'utf8');
            return { success: true, content };
        } catch (err: any) {
            if (err.code === 'ENOENT') {
                return { success: false, error: 'File not found' };
            }
            return { success: false, error: err.message };
        }
    }

    /**
     * Delete a file from the knowledge base
     */
    @Tool({
        name: "knowledge_delete",
        description: "Delete a file from the knowledge base. Triggers reindex if deleting from a knowledge category.",
        inputSchema: z.object({
            category: z.enum(['upload', 'knowledge', 'architecture', 'deployment', 'patterns', 'security', 'context'])
                .describe("Category the file is in"),
            filename: z.string().describe("Name of the file to delete"),
        })
    })
    async deleteFile(
        _ctx: McpContext,
        input: { category: string; filename: string }
    ): Promise<{ success: boolean; deleted?: string; reindex?: { success: boolean; indexed?: number }; error?: string }> {
        const { category, filename } = input;
        const targetDir = CATEGORIES[category] || UPLOAD_BASE;
        const filePath = path.join(targetDir, filename);

        // Security check
        if (!filePath.startsWith(targetDir)) {
            return { success: false, error: 'Access denied' };
        }

        try {
            await fs.unlink(filePath);

            const result: { success: boolean; deleted?: string; reindex?: { success: boolean; indexed?: number } } = {
                success: true,
                deleted: filename
            };

            // Trigger reindex if knowledge file
            if (category !== 'upload') {
                result.reindex = await this.triggerReindex();
            }

            return result;
        } catch (err: any) {
            if (err.code === 'ENOENT') {
                return { success: false, error: 'File not found' };
            }
            return { success: false, error: err.message };
        }
    }

    /**
     * Trigger manual reindex of knowledge base
     */
    @Tool({
        name: "knowledge_reindex",
        description: "Trigger a manual reindex of the knowledge base. Use after batch operations or to ensure index is up to date.",
        inputSchema: z.object({})
    })
    async reindex(_ctx: McpContext): Promise<{ success: boolean; indexed?: number; error?: string }> {
        const result = await this.triggerReindex();
        return result.success
            ? { success: true, indexed: result.indexed }
            : { success: false, error: 'Reindex failed or service unavailable' };
    }

    /**
     * Get available categories
     */
    @Tool({
        name: "knowledge_categories",
        description: "List available knowledge base categories and their paths.",
        inputSchema: z.object({})
    })
    async getCategories(_ctx: McpContext): Promise<{ categories: Array<{ id: string; path: string; isKnowledge: boolean }> }> {
        return {
            categories: Object.entries(CATEGORIES).map(([id, fullPath]) => ({
                id,
                path: fullPath.replace('/opt/spaceos/', ''),
                isKnowledge: id !== 'upload'
            }))
        };
    }
}
