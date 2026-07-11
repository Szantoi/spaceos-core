# Product Owner Instructions

Ez a mappa **instruction** fájlokat tartalmaz, amelyek a Product Owner számára releváns **AI-assisted requirements engineering** best practice-eket tartalmaznak.

> **Agent templates** (`.agent.md`) külön mappában: [`../agents/`](../agents/)

---

## ?? Tartalom

### [`ai-prompt-engineering-safety-best-practices.instructions.md`](ai-prompt-engineering-safety-best-practices.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: AI prompt safety irányelvek, felelõs AI-használat, prompt injection védelem
**Használat**: Amikor a PO AI-val kommunikál (strategic directive írás, epic javaslat generálás).

- Prompt injection és adversarial input védelme
- Felelõs AI-használat alapelvei (fairness, transparency)
- Sensitive data kezelése AI promptokban
- Output validáció: hogyan ellenõrizd az AI-generált tartalmat?
- Bias detection: business döntések AI-támogatásánál

---

## ?? Mikor töltsd be?

- **AI-val generált stratégiai tartalom review-ja**: `ai-prompt-engineering-safety`
- **Epic / story AI-asszisztált írása**: `ai-prompt-engineering-safety`

---

## ?? Kapcsolódó

- [Product Owner Role](../product_owner.role.md)
- [Sub-Agents](../agents/README.md)
