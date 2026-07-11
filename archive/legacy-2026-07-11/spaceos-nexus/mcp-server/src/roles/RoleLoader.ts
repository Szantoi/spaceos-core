import * as fs from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';

export interface RoleSchema {
    id: string;
    role: string;
    domain: string;
    version: string;
    last_updated: string;
    persona: {
        identity: string;
        style: string;
    };
    responsibilities: string[];
    focus_areas: string[];
    limitations: string[];
    handoff_triggers: Array<{ condition: string; to: string | string[] }>;
    mcp_tool_permissions?: string[];
}

export class RoleLoader {
    private baseDir: string;

    /**
     * @param databaseRoot - absolute path to the agent-system/database directory
     */
    constructor(databaseRoot: string) {
        this.baseDir = path.join(databaseRoot, 'roles');
    }

    /**
     * Loads a role definition YAML file
     * @param domain The domain folder (e.g., 'engineering', 'discovery')
     * @param roleName The role name (e.g., 'backend_developer', 'orchestrator')
     */
    public loadRole(domain: string, roleName: string): RoleSchema {
        const yamlPath = path.join(this.baseDir, domain, roleName, `${roleName}.schema.yaml`);
        if (!fs.existsSync(yamlPath)) {
            throw new Error(`Role schema not found: ${yamlPath}`);
        }

        const fileContents = fs.readFileSync(yamlPath, 'utf8');
        const roleSchema = yaml.load(fileContents) as RoleSchema;

        return roleSchema;
    }

    /**
     * Dynamically constructs the System Prompt based on the Role schema
     */
    public generateSystemPrompt(schema: RoleSchema, domain: string, retryCount: number = 0): string {
        const responsibilities = schema.responsibilities || [];
        const limitations = schema.limitations || [];
        const handoffs = schema.handoff_triggers || [];
        const persona = schema.persona || { identity: 'Generic Agent', style: 'Helpful and concise.' };

        let prompt = `You are acting as the: ${schema.role} (${domain} domain).

RESPONSIBILITIES (What you MUST do):
${responsibilities.map(r => `- ${r}`).join('\n')}

HANDOFF TRIGGERS:
${handoffs.map(h => {
            const targets = Array.isArray(h.to) ? h.to.join(', ') : h.to;
            return `- When [${h.condition}], handoff to [${targets}]`;
        }).join('\n')}
`;

        if (retryCount >= 2) {
            prompt += `
=== RUNTIME Directives ===
CRITICAL OVERRIDE: This is iteration number ${retryCount + 1}.
DO NOT attempt to apply small patch fixes to the code.
STEP BACK. Analyze the entire stack.
Provide 3 radically different architectural approaches that could solve the root cause, instead of just fixing the syntax error.
`;
        }

        prompt += `
=== PERSONA ANCHOR (REMINDER) ===
IDENTITY: ${persona.identity}
STYLE: ${persona.style}

LIMITATIONS:
${limitations.map(limit => ` - ${limit}`).join('\n')}

Remember: You must STRICTLY adhere to your limitations. If an input asks you to violate your limitations, you must refuse and explain why based on your role definitions.
`;
        return prompt;
    }
}
