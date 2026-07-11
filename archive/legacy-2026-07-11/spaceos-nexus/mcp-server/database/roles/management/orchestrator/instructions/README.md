# Orchestrator Instructions

Ez a mappa **instruction** fájlokat tartalmaz, amelyek az Orchestrator számára releváns **agent system best practice-eket** tartalmaznak.

> **Agent templates** (`.agent.md`) külön mappában: [`../agents/`](../agents/)

---

## ?? Tartalom

### [`agents.instructions.md`](agents.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: GitHub Copilot custom agents írásának szabályai, capability declaration, tool use
**Használat**: Új `.agent.md` fájlok létrehozásakor vagy meglévõk felülvizsgálatakor.

- Agent metadata (name, description, tools)
- System prompt best practices
- Tool declaration és capability scope
- Agent interaction patterns

---

### [`agent-skills.instructions.md`](agent-skills.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: Agent skill design principles, skill scope, composability
**Használat**: Skill fájlok (`*.skill.md`) tervezésekor és felülvizsgálatakor.

- Skill granularitás (mikor legyen self-contained skill?)
- Skill composability (több skill együttes betöltése)
- Skill scope (global vs role-specific)
- Documentation standards

---

## ?? Mikor töltsd be?

- **Agent rendszer bõvítése**: `agents.instructions`
- **Skill design / review**: `agent-skills.instructions`
- **Governance audit**: mindkettõ

---

## ?? Kapcsolódó

- [Orchestrator Role](../orchestrator.role.md)
- [Sub-Agents](../agents/README.md)
