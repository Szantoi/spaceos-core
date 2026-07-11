# Orchestrator Sub-Agents (Specializációk)

Ez a mappa **specialized sub-agent** template-eket tartalmaz, amelyek az Orchestrator role (`orchestrator.role.md`) **tovább specializált változatai** agent governance és prompt engineering feladatokhoz.

---

## ?? Agents Listája

### [`agent-governance-reviewer.agent.md`](agent-governance-reviewer.agent.md)

**Specializáció**: Agent system governance, role consistency review, prompt safety audit

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Agent role fájlok konzisztenciájának ellenõrzése
- Új agent bevezetése elõtti governance review
- Role boundary-k és felelõsségek ütközésének detektálása
- Agent system health check (system prompt quality)
- Multi-agent workflow safety audit

**Relation to Orchestrator**:

- **Complementary**: Orchestrator koordinációs szerepét kiegészíti strukturális ellenõrzéssel
- **Használat**: Mielõtt egy új role-t vagy sub-agent-et élesítenek

---

### [`prompt-builder.agent.md`](prompt-builder.agent.md)

**Specializáció**: Szisztematikus prompt tervezés, prompt engineering patterns

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Új agent prompt / role.md megírásánál
- Meglévõ prompts/ fájlok minõség-ellenõrzésénél
- Complex task decomposition prompt tervezésekor
- Orchestration prompt finomhangolásánál

**Relation to Orchestrator**:

- **Complementary**: Orchestrator prompt-jainak és más agent delegációs üzeneteinek optimalizálása
- **Használat**: Ha az agent kommunikáció nem elég precíz vagy félreértést okoz

---

## ?? Döntési Táblázat

| Szituáció | Ajánlott |
|-----------|----------|
| Epic delegálás, state tracking | Orchestrator (fõ role) |
| Új agent/role bevezetése | `agent-governance-reviewer` |
| Prompts/ fájlok írása, javítása | `prompt-builder` |
| Agent rendszer audit | `agent-governance-reviewer` |

---

## ?? Kapcsolódó

- [Orchestrator Role](../orchestrator.role.md)
- [Instructions (best practices)](../instructions/README.md)
