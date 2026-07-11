import * as fs from 'fs';
import * as path from 'path';
import * as dotenv from 'dotenv';
import { ChatGoogleGenerativeAI } from '@langchain/google-genai';
import { ChatPromptTemplate, SystemMessagePromptTemplate, HumanMessagePromptTemplate } from '@langchain/core/prompts';
import { StringOutputParser } from '@langchain/core/output_parsers';

import { RoleLoader } from '../../roles/RoleLoader';
import { backendDeveloperDataset } from '../datasets/backend_developer_dataset';
import { evaluateAgentResponse } from './evaluator';
import { TestCase } from '../datasets/DatasetTypes';

dotenv.config({ path: path.resolve(__dirname, '../../../../../../.env') }); // This assumes root .env but wait I fixed this in evaluator!
// Let's use the same path resolution as evaluator:
dotenv.config({ path: path.resolve(__dirname, '../../../.env') });

const databaseRoot = path.resolve(__dirname, '../../../../database');
const roleLoader = new RoleLoader(databaseRoot);

const reportsDir = path.resolve(__dirname, '../../../../../../docs/joinerytech-flow/agent-system-v2/role-enforcement/test-reports');
if (!fs.existsSync(reportsDir)) {
    fs.mkdirSync(reportsDir, { recursive: true });
}

async function runGenerativeAgent(systemPromptText: string, task: string): Promise<string> {
    const model = new ChatGoogleGenerativeAI({
        model: "gemini-2.5-flash",
        temperature: 0.2,
        maxOutputTokens: 2048
    });

    const prompt = ChatPromptTemplate.fromMessages([
        SystemMessagePromptTemplate.fromTemplate(systemPromptText),
        HumanMessagePromptTemplate.fromTemplate("{task}")
    ]);

    const chain = prompt.pipe(model).pipe(new StringOutputParser());

    let attempt = 0;
    const maxRetries = 3;

    while (attempt < maxRetries) {
        try {
            const response = await chain.invoke({ task: task });
            return response;
        } catch (error: any) {
            console.warn(`[Agent] Attempt ${attempt + 1} failed: ${error.message}`);
            attempt++;
            if (attempt >= maxRetries) throw new Error("Agent Generation Failed after max retries.");
            await new Promise(res => setTimeout(res, 2000 * attempt)); // exponential backoff
        }
    }
    return "";
}

interface TestResult {
    testCase: TestCase;
    agentResponse: string;
    verdict: string;
    reasoning: string;
    durationMs: number;
}

async function generateEvalReport(results: TestResult[], totalDuration: number, roleSchemaRaw: string): Promise<void> {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const reportPath = path.join(reportsDir, `eval-report-${timestamp}.md`);

    let passCount = 0;
    let failCount = 0;

    const details = results.map(r => {
        if (r.verdict === 'PASS') passCount++;
        else failCount++;

        return `### Test Case: ${r.testCase.id} (${r.testCase.category})
**Description:** ${r.testCase.description}

**📝 Human Prompt:**
> ${r.testCase.prompt}

**🎯 Expected Behavior:**
> ${r.testCase.expectedBehavior}

**🤖 Agent Response:**
\`\`\`markdown
${r.agentResponse}
\`\`\`

**⚖️ Judge Evaluation:**
- **Verdict:** \`${r.verdict}\`
- **Reasoning:** ${r.reasoning}
- **Duration:** ${r.durationMs}ms
---
`;
    }).join('\n');

    const markdown = `# LLMOps - Role Enforcement Evaluation Report

- **Date:** ${new Date().toUTCString()}
- **Role Validated:** ${backendDeveloperDataset.domain} / ${backendDeveloperDataset.roleName}
- **Total Duration:** ${(totalDuration / 1000).toFixed(2)} seconds
- **Total Cases:** ${results.length}
- **Passed:** ${passCount}
- **Failed:** ${failCount}
- **Success Rate:** ${((passCount / results.length) * 100).toFixed(2)}%

## 📊 Summary
| Fallback / Category | TestCase ID | Verdict | Duration |
|---------------------|-------------|---------|----------|
${results.map(r => `| ${r.testCase.category} | ${r.testCase.id} | ${r.verdict === 'PASS' ? '✅ PASS' : '❌ FAIL'} | ${r.durationMs}ms |`).join('\n')}

<details>
<summary><b>View System Prompt (Role Schema Raw)</b></summary>
\`\`\`yaml
${roleSchemaRaw}
\`\`\`
</details>

<br>

## 🔬 Test Case Details

${details}
`;

    fs.writeFileSync(reportPath, markdown, 'utf-8');
    console.log(`\n🎉 Report generated and saved to: ${reportPath}`);
}

async function runPipeline() {
    console.log("=== 🚀 Starting LLMOps Role Enforcement Pipeline ===");
    console.log(`Target Dataset: ${backendDeveloperDataset.domain}/${backendDeveloperDataset.roleName} (${backendDeveloperDataset.testCases.length} cases)`);

    const roleSchema = roleLoader.loadRole(backendDeveloperDataset.domain, backendDeveloperDataset.roleName);
    const systemPromptText = roleLoader.generateSystemPrompt(roleSchema, backendDeveloperDataset.domain);

    // Convert the schema back to raw yaml-like representation for the Judge to use
    // Using simple JSON.stringify with spacing or just fetching the file content directly
    const schemaRawPath = path.join(databaseRoot, 'roles', backendDeveloperDataset.domain, backendDeveloperDataset.roleName, `${backendDeveloperDataset.roleName}.schema.yaml`);
    const roleSchemaRaw = fs.readFileSync(schemaRawPath, 'utf8');

    const results: TestResult[] = [];
    const pipelineStartTime = Date.now();

    for (const testCase of backendDeveloperDataset.testCases) {
        console.log(`\n▶️ Executing Test Case: ${testCase.id} (${testCase.category})...`);
        const caseStartTime = Date.now();

        try {
            // 1. Generation Phase (Agent)
            console.log(`   [1/2] Simulating Agent response...`);
            const agentResponse = await runGenerativeAgent(systemPromptText, testCase.prompt);

            // 2. Evaluation Phase (Judge)
            console.log(`   [2/2] Running Judge Evaluator...`);
            let evalAttempt = 0;
            let evalResult = { verdict: 'FAIL' as 'PASS' | 'FAIL', reasoning: 'Evaluation Failed' };

            while (evalAttempt < 3) {
                try {
                    evalResult = await evaluateAgentResponse(roleSchemaRaw, testCase.prompt, testCase.expectedBehavior, agentResponse);
                    break; // success
                } catch (e: any) {
                    console.warn(`   [Judge] Attempt ${evalAttempt + 1} failed: ${e.message}`);
                    evalAttempt++;
                    if (evalAttempt >= 3) throw new Error("Judge Eval Failed after retries");
                    await new Promise(r => setTimeout(r, 2000));
                }
            }

            const caseDuration = Date.now() - caseStartTime;
            console.log(`   └─ Verdict: ${evalResult.verdict} (${caseDuration}ms)`);

            results.push({
                testCase,
                agentResponse,
                verdict: evalResult.verdict,
                reasoning: evalResult.reasoning,
                durationMs: caseDuration
            });

            // Delay to avoid Gemini Rate Limits (RPM/TPM limits)
            await new Promise(resolve => setTimeout(resolve, 3000));

        } catch (error: any) {
            console.error(`💥 Failed entirely on test case ${testCase.id}:`, error.message);
        }
    }

    const totalDuration = Date.now() - pipelineStartTime;
    console.log(`\n=== 🏁 Pipeline Completed in ${(totalDuration / 1000).toFixed(1)}s ===`);

    if (results.length > 0) {
        await generateEvalReport(results, totalDuration, roleSchemaRaw);
    } else {
        console.warn("No results generated. Check error logs.");
    }
}

runPipeline().catch(console.error);
