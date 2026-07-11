# Portál Területek — Még Nem Auditált (2026-07-11)

**Generálva:** Designer Terminal proaktív exploráció
**Kontext:** MSG-DESIGNER-002 után (Doorstar Production UI audit befejezve)
**Teljes portál:** 67 komponens + 36 oldal
**Auditálva:** 3 komponens + 1 oldal (Doorstar Production UI)
**Auditálatlan:** ~64 komponens + ~35 oldal

---

## 📊 DOMAIN BONTÁS

### 1. JoineryTech (27 Világ) — LEGNAGYOBB TERÜLET

**Scope:** Multi-domain ERP rendszer faipari KKV-knak

**Domain komponensek:**
- **DMS (Document Management):** 3 komponens
- **EHS (Environment, Health & Safety):** 3 komponens
- **HR (Human Resources):** 4 komponens
- **Kontrolling:** 0 komponens (TypeScript fájlok?)
- **Maintenance (Karbantartás):** 3 komponens
- **QA (Quality Assurance):** 3 komponens

**Domain oldalak:**
- DMSDashboardPage
- EhsDashboardPage
- HRDashboardPage
- KontrollingDashboardPage
- MaintenanceDashboardPage
- QADashboardPage
- IncidentDetailPage, IncidentListPage, IncidentReportPage (EHS)
- RiskAssessmentFormPage, RiskMatrixPage (EHS)
- TrainingCompliancePage, TrainingRecordFormPage (HR)

**UX Kockázatok:**
- 6 dashboard oldal → Konzisztencia audit szükséges (layout, KPI widgetek, szín séma)
- EHS incident reporting → Mobile-first? Touch targets? (műhelyi használat)
- HR training compliance → Accessibility? Tablet használat?

**Prioritás:** 🔶 **HIGH** (27 világ stratégiai termék, production ready kell legyen)

---

### 2. TradeWorld (Supplier Portal) — 7 komponens

**Scope:** Beszállítói portál, RFQ, Quote comparison

**Komponensek:**
- 7 komponens a `/components/TradeWorld/` mappában

**Oldalak:**
- PublicQuoteRequestPage
- PublicQuoteStatusPage
- QuoteComparisonPage
- QuoteRequestFormPage
- SupplierCatalogPage
- SupplierDetailPage

**UX Kockázatok:**
- Public facing UI → Branding konzisztencia? Professzionális megjelenés?
- Quote form → Mobile használat? Több lépéses wizard?
- Supplier catalog → Filter/search UX? Képek lazy loading?

**Prioritás:** 🔵 **MEDIUM** (külső partner UI, minőség fontos de nem CRITICAL)

---

### 3. Industrial Flow/Planning (Datahaven Agent Infra UI) — 6 komponens

**Scope:** SpaceOS agent infrastruktúra monitoring és workflow UI

**Komponensek:**
- 6 komponens a `/components/Industrial/` mappában

**Oldalak:**
- IndustrialFlowEditorPage
- IndustrialKanbanPage
- IndustrialPlanningPage
- IndustrialProjectsPage
- IndustrialDashboardPage
- IndustrialAutonomousPage

**UX Kockázatok:**
- Flow editor → Drag-and-drop UX? Mermaid diagram interaktivitás?
- Kanban board → Real-time SSE frissítés? Smooth drag?
- Planning pipeline → 5-stage visualization? (Idea → Selected → Debate → Consensus → Queue)

**Prioritás:** 🔶 **HIGH** (agent infra core UI, Root/Conductor használja napi szinten)

---

### 4. Kanban Board (Generic) — 5 komponens

**Komponensek:**
- 5 komponens a `/components/Kanban/` mappában

**Oldalak:**
- KanbanPage (generic)

**UX Kockázatok:**
- Dual-track board (Discovery + Delivery)?
- 7 terminal swimlane layout?
- Drag-and-drop performance (50+ kártyák)?

**Prioritás:** 🔵 **MEDIUM** (internal tool, de napi használat)

---

### 5. Dashboard/Kontrolling Widgets — 3-10 komponens

**Komponensek (root level):**
- CostBreakdownChart.tsx
- CostBreakdownModal.tsx
- CostBudgetWidget.tsx
- EACCalculationWidget.tsx
- KPICard.tsx
- KPIStrip.tsx
- PortfolioSummaryCard.tsx
- VarianceAnalysisPanel.tsx
- DarkCard.tsx
- DataDenseTable.tsx

**Komponensek (Dashboard mappa):**
- 3 komponens a `/components/Dashboard/` mappában

**Oldalak:**
- DashboardPage (main)
- PlanningPage
- ProjectsPage

**UX Kockázatok:**
- Bento grid dark theme → Konzisztencia? Spacing?
- KPI widgetek → Readable metrics? Chart accessibility?
- Cost/budget widgets → Számok formázása? Currency display?
- Data dense table → Horizontal scroll? Mobile view?

**Prioritás:** 🔶 **HIGH** (Dashboard első benyomás, stratégiai döntéshozáshoz használt)

---

### 6. Graph/Epic Workflow — 2 komponens

**Komponensek:**
- 2 komponens a `/components/Graph/` mappában

**UX Kockázatok:**
- Epic dependency gráf vizualizáció?
- Critical path highlighting?
- Mermaid diagram interaktivitás?

**Prioritás:** 🔵 **MEDIUM** (conductor használja, de nem public facing)

---

### 7. CRM (Customer Relationship Management) — oldalak

**Oldalak:**
- CRMLeadsPage
- CRMOpportunitiesPage

**UX Kockázatok:**
- Lead → Opportunity konverziós flow?
- Mobile használat (értékesítők terepen)?
- Touch targets, form UX?

**Prioritás:** 🔵 **MEDIUM** (JoineryTech része, de nem core feature)

---

### 8. Auth/Layout (Infrastructure) — komponensek

**Komponensek:**
- Auth/ mappa komponensek
- Layout/ mappa komponensek

**UX Kockázatok:**
- Login/logout flow?
- Auth overlay UX?
- Layout konzisztencia minden domain-en?

**Prioritás:** 🟢 **LOW** (infra, ritkán változik)

---

### 9. Production (Doorstar) — ✅ AUDITÁLVA (MSG-DESIGNER-002)

**Komponensek (auditálva):**
- ProductionJobCard ✅
- WorkflowStepStepper ✅
- KioskMobileLayout ✅

**Oldalak (auditálva):**
- ProductionQueuePage ✅ (részben)
- ProductionJobDetailPage (említve, de nem teljes audit)
- ProductionOverviewPage (nem auditálva)

**Oldalak (NEM auditálva):**
- ProductionOverviewPage

**UX Kockázatok:**
- ProductionOverviewPage → Dashboard-szerű? KPI-k? Metrikák?

**Prioritás:** 🔵 **MEDIUM** (MSG-DESIGNER-002-ben 10 issue azonosítva, fixes folyamatban)

---

### 10. Egyéb Komponensek (root level)

**Komponensek:**
- PublicQuoteForm.tsx
- QuoteStatusTimeline.tsx

**Prioritás:** 🟢 **LOW** (már egy másik domain része - TradeWorld)

---

## 🎯 AJÁNLOTT AUDIT PRIORITÁS (Top 5)

### 1. JoineryTech 6 Dashboard Konzisztencia Audit 🔥 **CRITICAL**

**Scope:**
- DMSDashboardPage
- EhsDashboardPage
- HRDashboardPage
- KontrollingDashboardPage
- MaintenanceDashboardPage
- QADashboardPage

**Kérdések:**
- Ugyanaz a layout (Bento grid)?
- Ugyanazok a KPI widgetek (KPICard, KPIStrip)?
- Ugyanaz a színséma (dark theme)?
- Responsive mobil nézet?
- Accessibility (WCAG 2.1 AA)?

**Estimated Effort:** 4-6 hours (6 dashboard)

**Üzleti Érték:** 🔥 CRITICAL — 27 világ stratégiai termék

---

### 2. Dashboard/Kontrolling Widgets Audit 🔶 **HIGH**

**Scope:**
- KPICard, KPIStrip
- CostBreakdownChart, CostBudgetWidget, EACCalculationWidget
- PortfolioSummaryCard, VarianceAnalysisPanel
- DataDenseTable, DarkCard

**Kérdések:**
- Chart accessibility (color blind safe?)?
- Számok formázása (currency, decimals, separators)?
- Data density vs readability balance?
- Responsive design (mobile, tablet)?
- Dark theme konzisztencia?

**Estimated Effort:** 3-4 hours (10 komponens)

**Üzleti Érték:** 🔶 HIGH — Dashboard első benyomás

---

### 3. Industrial Flow/Planning UI Audit 🔶 **HIGH**

**Scope:**
- IndustrialFlowEditorPage (Mermaid flow editor)
- IndustrialKanbanPage (Dual-track board)
- IndustrialPlanningPage (5-stage pipeline viz)
- IndustrialProjectsPage (Gantt timeline)

**Kérdések:**
- Drag-and-drop UX (smooth, responsive)?
- Real-time SSE updates (visual feedback)?
- Touch vs mouse interaction?
- Complex UI performance (50+ elemek)?

**Estimated Effort:** 4-5 hours (6 komponens + 4 oldal)

**Üzleti Érték:** 🔶 HIGH — Agent infra core UI, napi használat

---

### 4. TradeWorld Supplier Portal Audit 🔵 **MEDIUM**

**Scope:**
- PublicQuoteRequestPage, PublicQuoteStatusPage
- QuoteComparisonPage, QuoteRequestFormPage
- SupplierCatalogPage, SupplierDetailPage
- 7 TradeWorld komponens

**Kérdések:**
- Public facing branding (professzionális?)?
- Quote form wizard UX (multi-step?)?
- Supplier catalog filter/search UX?
- Mobile használat (beszállítók terepen)?

**Estimated Effort:** 3-4 hours (7 komponens + 6 oldal)

**Üzleti Érték:** 🔵 MEDIUM — Külső partner UI

---

### 5. EHS Mobile Incident Reporting Audit 🔵 **MEDIUM**

**Scope:**
- IncidentReportPage, IncidentListPage, IncidentDetailPage
- RiskAssessmentFormPage, RiskMatrixPage
- 3 EHS komponens

**Kérdések:**
- Mobile-first design (műhelyi használat)?
- Touch targets (WCAG 2.1 AA)?
- Photo upload UX (incident dokumentálás)?
- Form validation (multi-step wizard?)?

**Estimated Effort:** 2-3 hours (3 komponens + 5 oldal)

**Üzleti Érték:** 🔵 MEDIUM — Safety compliance, production floor use

---

## 📅 JAVASOLT ROADMAP (4 hét)

### Week 1: JoineryTech Dashboard Konzisztencia (6 dashboard)
- **Effort:** 4-6 hours
- **Deliverable:** Dashboard konzisztencia audit report
- **Output:** Unified design patterns dokumentáció

### Week 2: Dashboard/Kontrolling Widgets (10 komponens)
- **Effort:** 3-4 hours
- **Deliverable:** Widget accessibility & data viz audit
- **Output:** Chart color palette, number formatting guide

### Week 3: Industrial Flow/Planning UI (6 komponens + 4 oldal)
- **Effort:** 4-5 hours
- **Deliverable:** Complex interaction UX audit (drag-drop, real-time)
- **Output:** Interaction design patterns

### Week 4: TradeWorld + EHS (összesen 13 komponens + 11 oldal)
- **Effort:** 5-7 hours
- **Deliverable:** Public facing UI + mobile safety audit
- **Output:** Branding guide, mobile form patterns

**Total Effort:** 16-22 hours (~3-4 weeks, 1-2 hours/day)

---

## 🚀 KÖVETKEZŐ LÉPÉSEK

1. **Root/Conductor döntés:** Melyik audit legyen a következő?
2. **Designer inbox task:** Conductor kiossza a következő audit scope-ot
3. **Parallel track:** Audits futhatnak párhuzamosan Backend/Frontend fejlesztéssel

---

## 🔗 REFERENCIÁK

| Terület | Fájlok | Státusz |
|---------|--------|---------|
| Doorstar Production UI | 3 komponens + 1 oldal | ✅ DONE (MSG-DESIGNER-002) |
| JoineryTech Dashboards | 6 oldal | ⏳ PENDING |
| Dashboard Widgets | 10 komponens | ⏳ PENDING |
| Industrial Flow/Planning | 6 komponens + 4 oldal | ⏳ PENDING |
| TradeWorld | 7 komponens + 6 oldal | ⏳ PENDING |
| EHS Mobile | 3 komponens + 5 oldal | ⏳ PENDING |
| Kanban Board | 5 komponens + 1 oldal | ⏳ PENDING |
| CRM | 2 oldal | ⏳ PENDING |
| Graph/Epic | 2 komponens | ⏳ PENDING |

**Total Auditálatlan:** ~64 komponens + ~35 oldal

---

📋 Designer Terminal — Proaktív Exploráció (2026-07-11)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
