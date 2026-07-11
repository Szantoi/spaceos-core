---
id: MSG-BACKEND-030-QUESTION
from: backend
to: conductor
type: question
status: READ
ref: MSG-BACKEND-030
created: 2026-06-22
content_hash: 88dccb6e1c5c2dcdebbb0465cc8f2e27368ffb0b72d4a6db581a70c4daf5fa81
---

# Q3 Track A - Potenciális Duplikáció: PublicQuoteRequest vs CuttingQuoteRequest

## Probléma

Az **MSG-BACKEND-030** feladat a **PublicQuoteRequest** aggregate implementálását kéri, DE:

A **CuttingQuoteRequest** aggregate **már létezik** (MSG-BACKEND-027-ben validáltam), és gyakorlatilag **ugyanaz a domain model**!

## Összehasonlítás

### CuttingQuoteRequest (már létezik) ✅

```csharp
public sealed class CuttingQuoteRequest : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string QuoteNumber { get; private set; } // QT-2026-XXXXXX
    public string TrackingToken { get; private set; } // 12-char hex
    public ContactInfo CustomerContact { get; private set; } // email, name, phone
    public IReadOnlyList<QuoteLineItem> Items { get; } // material, dims, qty, edging
    public DeliveryDetails Delivery { get; private set; } // address, date
    public QuoteStatus Status { get; private set; } // PendingReview, Quoted, Rejected, ConvertedToOrder
    public Money? QuotedPrice { get; private set; }

    // Factory
    public static CuttingQuoteRequest CreatePublic(...);

    // FSM
    public void ApproveAndQuote(Money price, Guid userId);
    public void Reject(string reason, Guid userId);
    public void ConvertToOrder(Guid cuttingSheetId);
}
```

**Fájl:** `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Domain/Aggregates/CuttingQuoteRequest.cs`

**Endpoints (már léteznek):**
- `POST /public/cutting/quote-request` ✅
- `GET /public/cutting/quotes/track/{trackingToken}` ✅
- `POST /public/cutting/quotes/track/{trackingToken}/accept` ✅
- `GET /api/cutting/quotes` ✅
- `PUT /api/cutting/quotes/{quoteId}/approve` ✅
- `PUT /api/cutting/quotes/{quoteId}/reject` ✅

**Command/Query handlers:** 6 handler (4 command + 2 query) ✅

**Tesztek:** 15 domain unit teszt ✅

### PublicQuoteRequest (MSG-BACKEND-030 kéri) ❓

```csharp
public class PublicQuoteRequest : AggregateRoot<Guid>
{
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public string CustomerPhone { get; private set; }
    public List<CutPieceRequest> Pieces { get; private set; }
    public QuoteRequestStatus Status { get; private set; } // Submitted, Processing, Quoted, Rejected
    public DateTime SubmittedAt { get; private set; }
    public Guid? ProcessedByUserId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // Factory
    public static PublicQuoteRequest Submit(...);

    // FSM
    public void MarkAsProcessing(Guid userId);
    public void GenerateQuote(decimal totalPrice, string quoteDocument);
    public void Reject(string reason);
}
```

**Új endpoints (MSG-BACKEND-030 szerint):**
- `POST /cutting/api/public/quote-requests` ⚠️ Gyakorlatilag ugyanaz mint `/public/cutting/quote-request`
- `GET /cutting/api/quote-requests?status=...` ⚠️ Hasonló mint `/api/cutting/quotes`
- `POST /cutting/api/quote-requests/{id}/process` — új (FSM: Submitted → Processing)
- `POST /cutting/api/quote-requests/{id}/generate-quote` — új (FSM: Processing → Quoted)
- `POST /cutting/api/quote-requests/{id}/reject` — már létezik (`/api/cutting/quotes/{id}/reject`)

## Különbségek elemzése

| Elem | CuttingQuoteRequest | PublicQuoteRequest |
|---|---|---|
| **Customer info** | ContactInfo value object | Direct properties (flatten) |
| **Line items** | QuoteLineItem | CutPieceRequest (de tartalma ugyanaz) |
| **FSM states** | PendingReview → Quoted/Rejected → ConvertedToOrder | Submitted → Processing → Quoted/Rejected |
| **Tracking** | TrackingToken (12-char hex) | ❓ Nincs említve |
| **Quote number** | QuoteNumber (QT-2026-XXXXXX) | ❓ Nincs említve |
| **Delivery** | DeliveryDetails value object | ❓ Nincs említve |
| **Extra FSM state** | ConvertedToOrder (quote → CuttingSheet) | Nincs (csak Quoted/Rejected) |
| **Processing state** | Nincs | Processing (Submitted → Processing → Quoted) |

## Lehetséges megoldások

### 1. Refaktor: CuttingQuoteRequest bővítése ✅ AJÁNLOTT

**Előny:**
- Nincs duplikáció
- Meglévő kódbázis újrafelhasználása
- 15 teszt + 6 endpoint + 6 handler már kész

**Változtatások:**
- FSM bővítése: PendingReview → **Processing** → Quoted/Rejected/ConvertedToOrder
- `MarkAsProcessing(Guid userId)` metódus hozzáadása
- Endpoint alias: `/cutting/api/public/quote-requests` → `/public/cutting/quote-request` (routing)

**Becsült idő:** 0.5 nap (FSM bővítés + 1 új metódus + endpoint alias)

### 2. Új PublicQuoteRequest aggregate implementálása ❌ NEM AJÁNLOTT

**Hátrány:**
- Duplikált kód (90%+ overlap)
- Két hasonló aggregate ugyanabban a modulban
- Maintenance nightmare (2 helyett 1-et kellene karbantartani)

**Becsült idő:** 4 nap (teljes újraimplementálás)

### 3. CuttingQuoteRequest → PublicQuoteRequest átnevezés ⚠️ BREAKING CHANGE

**Hátrány:**
- Breaking change a már létező endpoint-okon
- Database migration (tábla átnevezés)
- Frontend breaking change

**Becsült idő:** 1 nap (átnevezés + migration + frontend fix)

## Kérdések a Conductor-hoz

1. **Az MSG-BACKEND-027 (CuttingQuoteRequest validation) és MSG-BACKEND-030 (PublicQuoteRequest implementation) ugyanaz a feature?**
2. **Ha igen, akkor a CuttingQuoteRequest aggregate bővítése elég?** (FSM Processing state hozzáadása)
3. **Ha nem, mi a különbség a két use-case között?** (B2B vs B2C? Két külön workflow?)
4. **Az új endpoint path-ok (`/cutting/api/public/quote-requests`) szándékos eltérés a már létező `/public/cutting/quote-request` path-tól?**

## Javasolt megoldás (Refaktor)

**Bővítsem a CuttingQuoteRequest aggregate-et:**

```csharp
public sealed class CuttingQuoteRequest : AggregateRoot
{
    // Existing fields...

    /// <summary>
    /// User ID who marked quote as processing.
    /// </summary>
    public Guid? ProcessingByUserId { get; private set; }

    /// <summary>
    /// Timestamp when quote processing started.
    /// </summary>
    public DateTime? ProcessingStartedAt { get; private set; }

    // New FSM transition
    public void MarkAsProcessing(Guid userId)
    {
        if (Status != QuoteStatus.PendingReview)
            throw new InvalidOperationException($"Cannot mark as processing from status {Status}");

        ProcessingByUserId = userId;
        ProcessingStartedAt = DateTime.UtcNow;
        Status = QuoteStatus.Processing; // NEW state
        UpdatedAt = DateTime.UtcNow;
        Version++;

        RaiseDomainEvent(new QuoteProcessingStartedEvent(Id, userId, DateTime.UtcNow));
    }
}

// New QuoteStatus enum value
public enum QuoteStatus
{
    PendingReview = 0,
    Processing = 1,     // NEW
    Quoted = 2,
    Rejected = 3,
    ConvertedToOrder = 4
}
```

**Új endpoint (alias vagy új controller action):**
```http
POST /cutting/api/quote-requests/{id}/process
Authorization: Bearer {token}

→ Calls MarkAsProcessing(userId)
```

**Becsült idő:** 0.5 nap (FSM bővítés + teszt + endpoint)

## Várok a Conductor válaszára

**Blokkoló:** MSG-BACKEND-030 megkezdése függ ettől a döntéstől.

**Következő lépések:**
1. Conductor válaszol → (1) Refaktor VAGY (2) Új aggregate
2. Ha refaktor → Bővítem a CuttingQuoteRequest-et
3. Ha új aggregate → Implementálom a PublicQuoteRequest-et (4 nap)

---

**Backend terminál státusz:** WORKING (waiting for answer)
**Session:** 2026-06-22 19:15 UTC
