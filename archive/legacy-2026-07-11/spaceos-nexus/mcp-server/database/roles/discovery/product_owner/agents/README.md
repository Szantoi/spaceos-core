# Product Owner Sub-Agents (Specializációk)

Ez a mappa **specialized sub-agent** template-eket tartalmaz, amelyek a Product Owner role (`product_owner.role.md`) **tovább specializált változatai**.

---

## ?? Agents Listája

### [`atlassian-requirements-to-jira.agent.md`](atlassian-requirements-to-jira.agent.md)

**Specializáció**: Requirements decomposition, structured issue/story writing, acceptance criteria

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Üzleti igény › strukturált user story / epic ticket
- Acceptance criteria írása BDD (Given/When/Then) formátumban
- Requirement gap-ek azonosítása és kitöltése
- Epic-szintû breakdown (epic › stories › tasks)
- Backlog item szövegezése, priorizálási javaslatok

> **Megjegyzés**: Bár Jira-fókuszú az eredeti forrása, a requirements engineering módszertana platform-független — JoineryTech.Flow GitHub Issues workflow-jában is alkalmazható.

**Relation to Main Role**:

- **Complementary**: PO általános stratégiai szerepét kiegészíti strukturált követelménytervezéssel
- **Használat**: Mielõtt egy új epic-et az Architect-nek átadnak

---

## ?? Döntési Táblázat

| Szituáció | Ajánlott |
|-----------|----------|
| Strategic directive, Epic javaslat | Product Owner (fõ role) |
| User story / acceptance criteria írás | `atlassian-requirements-to-jira` |
| Domain quality mapping | Product Owner (fõ role + DQM skill) |

---

## ?? Kapcsolódó

- [Product Owner Role](../product_owner.role.md)
- [Instructions (best practices)](../instructions/README.md)
