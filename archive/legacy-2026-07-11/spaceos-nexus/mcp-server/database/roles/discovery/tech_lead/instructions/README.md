# Tech Lead Instructions

Ez a mappa **instruction** fájlokat tartalmaz, amelyek **referenciák és best practice-ek** a Tech Lead számára task planning és technical review során.

> **Agent templates** (`.agent.md`) külön mappában: [`../agents/`](../agents/)

---

## ?? Tartalom

### [`dotnet-architecture-good-practices.instructions.md`](dotnet-architecture-good-practices.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: .NET Clean Architecture, DDD, SOLID elvek és architectural patterns
**Használat**: Task tervezésnél ellenõrizze, hogy a javasolt implementáció követi-e a projekt architektúráját.

- Clean Architecture layering (Core / Infra / API)
- Domain-Driven Design aggregates, value objects, repositories
- SOLID principles alkalmazása .NET környezetben
- Dependency Injection helyes használata

---

### [`security-and-owasp.instructions.md`](security-and-owasp.instructions.md)

**Forrás**: awesome-copilot repository
**Leírás**: OWASP Top 10 security guidelines .NET/C# alkalmazásokhoz
**Használat**: Task acceptance criteria-ban security szempontok ellenõrzésekor.

- OWASP Top 10 védelmi technikák
- Input validation és sanitization
- Authentication és Authorization patterns
- Secrets management, environment variables
- SQL injection, XSS, CSRF védelme

---

## ?? Mikor töltsd be?

- **Task technikai review**: `dotnet-architecture-good-practices`
- **Security-érintett feladat**: `security-and-owasp`
- **Epic acceptance criteria írása**: mindkettõ

---

## ?? Kapcsolódó

- [Tech Lead Role](../tech_lead.role.md)
- [Sub-Agents](../agents/README.md)
