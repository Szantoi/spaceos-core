# Devil's Advocate Instructions

Ez a mappa **instruction** fájlokat tartalmaz, amelyek a Devil's Advocate kritikai elemzési munkáját támogatják AI safety és prompt engineering perspektívából.

> **Agent templates** (`.agent.md`) külön mappában: [`../agents/`](../agents/)

---

## ?? Tartalom

### [`agent-safety.instructions.md`](agent-safety.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: AI agent safety principles — mit szabad, mit nem, hol vannak a határok
**Használat**: Agent-rendszer vagy workflow kritikai review-jánál safety szempontból.

- Agent capability scope és principle of least privilege
- Unsafe action detection (irreversible ops, data deletion, external calls)
- Human oversight pontok meghatározása
- Escalation és refusal patterns
- Trust boundary-k multi-agent rendszerben

---

### [`ai-prompt-engineering-safety-best-practices.instructions.md`](ai-prompt-engineering-safety-best-practices.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: AI prompt safety és felelõs AI-használat irányelvei
**Használat**: Prompt-ok, role.md-k és workflow-ok kritikai elemzésekor.

- Prompt injection védelme és tesztelése
- Adversarial input esetek felsorolása
- AI output bias és hallucination kockázatai
- "Mi sülhet el rosszul?" keretrendszer
- Responsible AI checklist

---

## ?? Mikor töltsd be?

- **Agent system safety review**: `agent-safety`
- **Prompt quality critique**: `ai-prompt-engineering-safety`
- **Bias / assumption challenge**: mindkettõ

---

## ?? Kapcsolódó

- [Devil's Advocate Role](../devils_advocate.role.md)
- [Sub-Agents](../agents/README.md)
