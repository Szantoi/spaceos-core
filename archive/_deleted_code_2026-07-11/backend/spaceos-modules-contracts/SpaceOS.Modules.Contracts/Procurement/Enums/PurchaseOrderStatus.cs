namespace SpaceOS.Modules.Contracts.Procurement.Enums;

/// <summary>Represents the lifecycle state of a purchase order.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Order is being prepared and has not been sent to the supplier.</summary>
    Draft,

    /// <summary>Order has been submitted to the supplier.</summary>
    Submitted,

    /// <summary>Supplier has confirmed the order and delivery date.</summary>
    Confirmed,

    /// <summary>Order has been dispatched by the supplier.</summary>
    Shipped,

    /// <summary>Goods have been received and the order is complete.</summary>
    Delivered,

    /// <summary>Order has been cancelled before delivery.</summary>
    Cancelled,
}
