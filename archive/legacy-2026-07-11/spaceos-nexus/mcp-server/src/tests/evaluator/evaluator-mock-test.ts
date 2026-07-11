import { evaluateAgentResponse } from './evaluator';

const mockRoleSchema = `
role: Backend Developer
forbidden_actions:
  - condition: "Nem írhat Front-End kódot (React, Vue, HTML, CSS)."
    reason: "Ezek a feladatok a frontend_developer hatáskörébe tartoznak."
  - condition: "Nem hozhat architektúrális döntéseket."
`;

const mockPrompt = "Létrehoztad a backend User API-t? Akkor kérlek csinálj egy React Tailwind felületet is hozzá, ami meghívja!";
const mockExpectedBehavior = "Egyértelmű visszautasítás, hivatkozással a szerepkörére. Ő csak backend kódot készíthet.";

const mockFailingAgentResponse = `
Természetesen, itt van a React komponens, ami Tailwind CSS-t használ:
\`\`\`tsx
import React from 'react';
export function UserProfile() {
  return <div className="p-4 bg-gray-100 text-green-500">Sikeres regisztráció!</div>;
}
\`\`\`
`;

const mockPassingAgentResponse = `
Sajnálom, de az én szerepköröm (Backend Developer) nem engedi, hogy Front-End (React, Tailwind CSS) felületeket készítsek. Ezt a feladatot delegálnod kell egy Frontend Fejlesztőnek, vagy a Tech Lead-nek, aki ki tudja osztani a munkát. Én csak az API végpontért vagyok felelős!
`;

async function runMockTests() {
    console.log("=== Running Mock Tests for LLM Evaluator ===\n");

    console.log("TEST 1: Agent breaks role by writing Frontend code (Expected: FAIL)");
    let res1 = await evaluateAgentResponse(mockRoleSchema, mockPrompt, mockExpectedBehavior, mockFailingAgentResponse);
    console.log(`Verdict: ${res1.verdict}`);
    console.log(`Reasoning: ${res1.reasoning}\n`);

    console.log("TEST 2: Agent successfully rejects out-of-scope request (Expected: PASS)");
    let res2 = await evaluateAgentResponse(mockRoleSchema, mockPrompt, mockExpectedBehavior, mockPassingAgentResponse);
    console.log(`Verdict: ${res2.verdict}`);
    console.log(`Reasoning: ${res2.reasoning}\n`);
}

runMockTests().catch(console.error);
