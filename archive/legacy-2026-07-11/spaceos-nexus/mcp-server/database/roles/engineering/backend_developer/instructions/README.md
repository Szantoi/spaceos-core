# Guides & Research

Ez a mappa a JoineryTech.Flow projekt **tudásbázisát** tartalmazza: platform referenciákat, kutatási dokumentumokat és az integrációs szintéziseket.

A struktúra **alulról felfelé** épül — az alapozó tényektõl a projekt-specifikus alkalmazásig:

```
01 Reference/   ‹ tényszerû, platform-specifikus leírások
02 Research/    ‹ elemzések, trendek, metodológia
03 Integration/ ‹ szintézis, hogyan alkalmazzuk a projektben
```

---

## ?? 01 Reference — Platformleírások

Tényszerû, frissen tartott referencia dokumentumok az általunk használt AI tooling ökoszisztémáról.

| Dokumentum | Tartalom |
| :--- | :--- |
| [Claude Code Agent Guide](./01%20Reference/Claude%20Code%20Agent%20Guide.md) | CLAUDE.md rendszer, Sub-Agents, Skills, Hooks teljes referencia |
| [GitHub Copilot Agent Guide](./01%20Reference/GitHub%20Copilot%20Agent%20Guide.md) | Copilot agent instrukciók, `.github/agents/`, workspace szabályok |
| [Google Antigravity Agent Guide](./01%20Reference/Google%20Antigravity%20Agent%20Guide.md) | Antigravity platform, agentskills.io integráció |
| [OpenClaw Agent Guide](./01%20Reference/OpenClaw%20Agent%20Guide.md) | Self-hosted gateway, ACP protokoll, ClawHavoc biztonsági incidens |
| [Agile Fogalomtár és Példák](./01%20Reference/Agile%20Fogalomt%C3%A1r%20%C3%A9s%20P%C3%A9ld%C3%A1k.md) | Agile/Scrum/Kanban terminológia és példák |

---

## ?? 02 Research — Elemzések és Kutatás

Mélyebb elemzések, trendvizsgálatok és metodológiai döntések háttéranyaga.

| Dokumentum | Tartalom |
| :--- | :--- |
| [Agile Fejlesztési Workflow](./02%20Research/Agile%20Fejleszt%C3%A9si%20Workflow.md) | Agile workflow alkalmazás a JoineryTech.Flow kontextusában |
| [File-Based rendszerbõl MCP szerver evolúció](./02%20Research/File-Based%20rendszerb%C5%91l%20MCP%20szerver%20evol%C3%BAci%C3%B3.md) | Hogyan érdemes az MCP rétegre migrálni |

---

## ?? 03 Integration — Szintézis és Alkalmazás

Projekt-specifikus dokumentumok: hogyan illesztjük az eszközöket és koncepciókat a JoineryTech.Flow valóságába.

| Dokumentum | Tartalom |
| :--- | :--- |
| [Auto-Dispatch Integration Terv](./03%20Integration/Auto-Dispatch%20Integration%20Terv.md) | OpenClaw bindings minta › GitHub Actions label-alapú role auto-routing |
| [Agent Skills Multi-Platform Management](./03%20Integration/Agent%20Skills%20Multi-Platform%20Management.md) | Egységes skills stratégia Copilot + Claude Code + Antigravity felett |
| [Awesome-Copilot Integráció](./03%20Integration/Awesome-Copilot%20Integr%C3%A1ci%C3%B3.md) | Awesome-Copilot gyûjtemény integrálása az agent rendszerbe |

---

## ?? Kapcsolódó

- [Product Requirements Document](../Product%20Requirements%20Document.md) — a projekt tényleges terve
