# SpaceOS — Kockázatelemzések

Devils Advocate vizsgálatok és architekt válaszok nyilvántartása.

---

## Aktív kockázatok összesítője

### 🔴 Kritikus / Magas — nyitott

| ID | Kockázat | Forrás | Akció | Határidő |
|---|---|---|---|---|
| ~~R-13~~ | ~~E2E cross-tenant izoláció teljesen hiányzik~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — 6 cross-tenant teszt + RLS sentinel · 193/193 (2026-04-15) | — |
| ~~R-14~~ | ~~KERNEL DB triggerek SQLite-on nem futnak~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — Testcontainers PG + SEC-01 + SEC-02 trigger tesztek · 1110/1110 · 4cafceb (2026-04-15) | — |
| ~~R-15~~ | ~~Audit chain integritás nem CI-garantált~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — AuditChainIntegrityTest 3 CI gate · tamper detection · ⚠️ OccurredAt sequence gap → KERNEL-075 (2026-04-15) | — |
| ~~R-16~~ | ~~ORCH BFF proxy upstream hibakezelés + requireAuth missing-tid~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — proxy 502 fix + tid claim validáció + SSE abort · 207/207 · b3860ac (2026-04-15) | — |
| ~~R-17~~ | ~~PORTAL PKCE callback hibaágak + refresh race condition~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — 5 OAuth teszt + 3 dedup teszt + 80% branch threshold · 299/299 (2026-04-15) | — |
| ~~R-18~~ | ~~ABSTRACTIONS Graph Engine ciklus detekció csak traversal-time~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — write-time BFS fix + 5 teszt · 81/81 (2026-04-15) | — |
| R-07 | Escrow MNB/PSD2 engedély | Round 2 | Jogi tisztázás: PSP partner vs. soft escrow | Q2 Soft Launch előtt |
| R-12 | Doorstar single-partner dependency | Round 2 | 2 párhuzamos design partner LOI | Q2 Soft Launch előtt |
| R-03 | Single VPS — nincs HA | Round 1 ✅ elfogadva | PostgreSQL replication + pg_dump + RTO/RPO | Q3 2026 előtt |
| R-09 | HU faipari CAC validáció | Round 2 | 10 non-Doorstar WTP interjú | Q3 2026 előtt |
| R-08 | Graph Engine vs. IFC szabvány | Round 2 | xBIM Toolkit értékelés, döntés dokumentálása | 2026 Q4 |

### 🟡 Kockázat — figyelendő

| ID | Kockázat | Forrás | Akció | Határidő |
|---|---|---|---|---|
| ~~R-19~~ | ~~JOINERY PDF gyártásilap golden-file teszt hiánya~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — 9 PDF teszt + 8 dimenzió edge case + 5 RLS + 6 http error · 202/202 (2026-04-15) | — |
| ~~R-20~~ | ~~E2E Ecosystem Actor v4 lefedés hiánya~~ | ~~Test Coverage Audit 2026-04-15~~ | ✅ RESOLVED — 8 ecosystem-actor + 13 error-paths teszt · 193/193 (2026-04-15) | — |
| R-04 | SHA-256 audit chain jogi státusz | Round 1 ✅ elfogadva | OpenTimestamps az Escrow GA sprintben | Escrow GA sprint |
| R-10 | Anthropic cost + EU data residency | Round 2 | GDPR Art. 28 DPA ellenőrzés | Q2 Soft Launch előtt |
| R-11 | Multi-brand prematur scaling | Round 2 | Döntés: asztalostech.hu mikor kap ügyfelet | 2026 Q2 |
| R-02 | Mixed stack kognitív overhead | Round 1 | .NET Anthropic SDK figyelése tech debt-ként | Folyamatos |
| R-05 | Keycloak komplexitás | Round 1 | realm-export.json git commit automatizálás | Következő infra task |

### ✅ Lezárt / Visszautasított

| ID | Kockázat | Verdikt |
|---|---|---|
| R-01 | LLM tool calling CNC kontextusban | ✅ Architekt visszautasította — LLM csak konfig paramétert generál, CNC determinisztikus C#-ban |
| R-06 | B2B Federation YAGNI | ✅ Architekt visszautasította — nem federation, allowlist JOIN tábla, Doorstar use case-hez szükséges |

---

## Dokumentumok

| Dátum | Dokumentum | Tartalom |
|---|---|---|
| 2026-04-15 | [TestCoverageRiskReview_20260415.md](TestCoverageRiskReview_20260415.md) | Test Coverage DA audit — 8 kockázat (R-13–R-20) · 6 terminál · Sprint 5 akciók |
| 2026-04-13 | [ArchitectureRiskReview_20260413.md](ArchitectureRiskReview_20260413.md) | 1. kör — 6 kockázat (R-01–R-06) |
| 2026-04-13 | [ArchitectureRiskReview_Response_20260413.md](ArchitectureRiskReview_Response_20260413.md) | Architekt válasza az 1. körre |
| 2026-04-13 | [ArchitectureRiskReview_Round2_20260413.md](ArchitectureRiskReview_Round2_20260413.md) | 2. kör — 6 új kockázat (R-07–R-12) |

---

## Konvenció

**Fájlnév:** `ArchitectureRiskReview_[Round/Response]_YYYYMMDD.md`  
**Kockázat ID:** `R-NN` — sorszám a keletkezés sorrendjében  
**Státusz frissítés:** csak ezt a README-t kell frissíteni, a forrásdokumentumok változatlanok maradnak
