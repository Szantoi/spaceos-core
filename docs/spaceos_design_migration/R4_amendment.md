# R4 Amendment — Design_Pipeline_Strategy_v1.md

> Apply this in the design session itself (it edits a doc inside `~/spaceos-docs/`,
> which is allowed). Replace the existing R4 row in Section 1 and append the note below.

## Replace R4 (Section 1 — Tervezési szabályok)

**Old:**

| R4 | **A design pipeline a Claude.ai workspace dolga; az implementáció a Claude Code csapaté** | Tiszta felelősségszétválasztás: architektúra itt, kód ott. |

**New:**

| R4 | **A design pipeline a `spaceos-design` VPS session dolga (interaktív conductor); az implementáció az on-demand Claude Code execution-session-öké** | Tiszta felelősségszétválasztás megmarad: design read-only az összes repo-ra, írni csak `~/spaceos-docs/`-ba ír, kódot soha. A szétválasztás immár instrukciós (CLAUDE.md hard constraints), nem környezeti. |

## Append note (under Section 1)

> **2026-06-16 — Topológiaváltás:** A tervezés átkerült Claude.ai-ból a VPS `spaceos-design`
> persistent tmux session-be. A R3/R4 szétválasztás érvényben marad, de a határőr immár a
> `~/spaceos-docs/CLAUDE.md` hard-constraint blokkja (write csak `~/spaceos-docs/`, read-only
> minden más repo, semmilyen mutáció). A ground-truth read access a drift-megszüntetés
> nyeresége. A v1→v4 review pipeline (sub-database-designer / sub-senior-security /
> sub-senior-backend) változatlanul a `spaceos-arch-planner` skillen keresztül fut.
