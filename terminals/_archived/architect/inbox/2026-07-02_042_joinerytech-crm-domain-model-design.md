---
id: MSG-ARCHITECT-042
from: conductor
to: architect
type: task
priority: high
status: COMPLETED
injected: 2026-07-02
model: sonnet
epic_id: EPIC-JT-CRM
ref: MSG-BACKEND-105
created: 2026-07-02
content_hash: 1bbe6b568a48a74f1da0adaf31b1f9b24d3808dacd42060c886ece3be3bcefe2
---

# JoineryTech CRM Domain Model Design

## Context

A Backend Architecture Plan (MSG-BACKEND-105) és az OpenAPI spec (Week 0) elkészült és approved. Most a **CRM modul részletes domain model**-jének megtervezése következik.

**Backend Architecture:** `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md`
**OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml`

## Task

Tervezd meg a **CRM modul DDD domain model**-jét a .NET 8 backend implementációhoz.

### Scope

**2 Aggregate Root:**
1. **Lead** (Ajánlatkérés előtti érdeklődő)
2. **Opportunity** (Minősített érdeklődés → ajánlat)

**Kapcsolódó entitások:**
- LeadSource (forrás tracking)
- Activity (hívás, email, találkozó)
- Task (CRM feladat SLA-val)
- Note (belső jegyzetek)

### Elvárások

**1. Aggregate Root Design**
```csharp
// Lead Aggregate példa struktúra
public class Lead : AggregateRoot
{
    public LeadId Id { get; }
    public TenantId TenantId { get; }
    public LeadStatus Status { get; private set; } // FSM
    public ContactInfo Contact { get; }
    public LeadSource Source { get; }
    public Money EstimatedValue { get; }

    // FSM methods
    public void Qualify() { /* Status transition */ }
    public void StartNurturing() { /* Status transition */ }
    public Opportunity ConvertToOpportunity() { /* Factory */ }
}
```

**2. FSM Design**
- Lead FSM: uj → kapcsolat → minosites → nurturing → konvertalva (+ elvetve)
- Opportunity FSM: nyitott → igenyfelmeres → osszeallitas → ajanlat → targyalas → megnyert/elveszett
- Validált átmenetek (Status pattern)
- Domain events (LeadQualified, OpportunityWon, stb.)

**3. Value Objects**
- ContactInfo (name, email, phone - immutable)
- Money (amount + currency)
- Address
- LeadScore (számított érték 0-100)

**4. Domain Services**
- LeadScoringService (forrás, aktivitás, méret alapján)
- OpportunityForecastService (súlyozott forecast számítás)

**5. Repository Contracts**
```csharp
public interface ILeadRepository
{
    Task<Lead> GetByIdAsync(LeadId id, CancellationToken ct);
    Task<IEnumerable<Lead>> GetByStatusAsync(LeadStatus status, CancellationToken ct);
    Task AddAsync(Lead lead, CancellationToken ct);
    Task UpdateAsync(Lead lead, CancellationToken ct);
}
```

**6. Integration Points**
- Sales module: Opportunity → Quote conversion
- Webshop: Auto-lead creation from contact form
- B2B Handshake: Partner lead tracking

### Deliverables

1. **Domain Model Document** (`/opt/spaceos/docs/joinerytech/domain/CRM_DOMAIN_MODEL.md`)
   - Aggregate Root definitions
   - FSM diagrams (Mermaid)
   - Value Object specs
   - Repository contracts
   - Domain Services
   - Integration boundaries

2. **C# Skeleton Code** (pszeudokód szinten, nem teljes implementáció)
   - Lead.cs
   - Opportunity.cs
   - LeadStatus.cs / OpportunityStatus.cs (enum vagy State pattern)
   - ContactInfo.cs, Money.cs (Value Objects)
   - ILeadRepository.cs

3. **FSM Validation Table**
   - Status transitions mátrix
   - Minden átmenet validációs szabályai

## Reference Files

- Backend Plan: `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md` (Section 3: Data Model)
- Prototype Source: `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` (CRM világ leírás)
- OpenAPI: `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml` (CRM endpoints)
- SpaceOS Patterns: `docs/knowledge/patterns/BACKEND_PATTERNS.md`

## Acceptance Criteria

- [ ] 2 Aggregate Root (Lead, Opportunity) részletesen specifikálva
- [ ] FSM transitions validálva és dokumentálva (Mermaid diagram)
- [ ] Value Objects definiálva (ContactInfo, Money, Address, LeadScore)
- [ ] Domain Services (LeadScoring, Forecast) specifikálva
- [ ] Repository contracts C# interface formában
- [ ] Integration boundaries (Sales, Webshop) dokumentálva
- [ ] C# skeleton code (5-6 fájl pszeudokód)
- [ ] DONE outbox üzenet

## Timeline

**Becsült idő:** 3-4 óra (komplex domain, 2 aggregate)

## Notes

Ez a CRM domain model az első modul design - a mintát követheti a többi modul (HR, Maintenance, Controlling). A DDD konzisztencia kritikus a későbbi modulok számára.
