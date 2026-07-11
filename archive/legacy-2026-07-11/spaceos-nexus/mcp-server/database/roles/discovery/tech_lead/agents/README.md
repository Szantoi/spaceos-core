# Tech Lead Sub-Agents (Specializációk)

Ez a mappa **specialized sub-agent** template-eket tartalmaz, amelyek a Tech Lead role (`tech_lead.role.md`) **tovább specializált változatai** bizonyos feladattípusokhoz.

---

## ?? Agents Listája

### [`se-security-reviewer.agent.md`](se-security-reviewer.agent.md)

**Specializáció**: Security code review, OWASP Top 10, vulnerability detection

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Task acceptance elõtti security review
- Epic-szintû biztonsági audit kérelem
- OWASP Top 10 megfelelõség ellenõrzése
- Sensitive data handling (auth, secrets, PII) revíziója
- Security-critical feat implementációjának review-ja

**Relation to Main Role**:

- **Complementary**: Tech Lead általános code review-ját kiegészíti security fókusszal
- **Használat**: Ha a task biztonsági vonatkozásokat érint (auth, API exposure, data handling)

---

### [`adr-generator.agent.md`](adr-generator.agent.md)

**Specializáció**: Architecture Decision Record (ADR) generálás, dokumentálás

**Forrás**: awesome-copilot repository (Microsoft)

**Mikor használd**:

- Technikai döntések dokumentálása task planning során
- Breaking change vagy architectural trade-off rögzítése
- Implementation pattern kiválasztásának indoklása
- Tech debt tudatos felvállalásának dokumentálása

**Relation to Main Role**:

- **Complementary**: Task planning outcome dokumentálása ADR formátumban
- **Használat**: Ha egy task végrehajtása során fontos architektúrális döntés születik

---

## ?? Döntési Táblázat: Mikor melyiket?

| Szituáció | Ajánlott |
|-----------|----------|
| General task planning & breakdown | Tech Lead (fõ role) |
| Security-critical implementation review | `se-security-reviewer` |
| ADR dokumentálás szükséges | `adr-generator` |
| Task critique (devil's advocate szempontok) | Tech Lead (fõ role) |

---

## ?? Kapcsolódó

- [Tech Lead Role](../tech_lead.role.md)
- [Tech Lead Workflow](../workflows/tech_lead.workflow.md)
- [Instructions (best practices)](../instructions/README.md)
