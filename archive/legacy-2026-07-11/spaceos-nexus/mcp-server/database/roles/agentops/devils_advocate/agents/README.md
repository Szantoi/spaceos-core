# Devil's Advocate Sub-Agents (Specializációk)

Ez a mappa **specialized sub-agent** template-eket tartalmaz, amelyek a Devil's Advocate role (`devils_advocate.role.md`) kritikus elemzési feladatait kiegészítik.

---

## ?? Agents Listája

### [`agent-governance-reviewer.agent.md`](agent-governance-reviewer.agent.md)

**Specializáció**: Agent system governance audit, role consistency review, boundary analysis

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Agent system design kritikai elemzése
- Role boundary-k és felelõsség-ütközések feltérképezése
- Multi-agent workflow gyenge pontjainak azonosítása
- "Quis custodiet ipsos custodes?" — ki ellenõrzi az ellenõrzõket?
- Agent prompt safety audit (mi sülhet el rosszul?)

**Relation to Main Role**:

- **Complementary**: Devil's Advocate általános kritikai szemléletét erõsíti agent-specifikus governance eszközökkel
- **Használat**: Ha az agent-rendszer struktúráját vagy egy új role bevezetését kell kritizálni

---

## ?? Döntési Táblázat

| Szituáció | Ajánlott |
|-----------|----------|
| Epic / task terv kritikai elemzése | Devil's Advocate (fõ role) |
| Agent rendszer design kritikája | `agent-governance-reviewer` |
| Assumption challenge, bias detection | Devil's Advocate (fõ role) |

---

## ?? Kapcsolódó

- [Devil's Advocate Role](../devils_advocate.role.md)
- [Instructions (best practices)](../instructions/README.md)
