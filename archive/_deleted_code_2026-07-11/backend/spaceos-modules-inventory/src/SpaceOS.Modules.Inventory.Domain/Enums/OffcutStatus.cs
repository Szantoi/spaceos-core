namespace SpaceOS.Modules.Inventory.Domain.Enums;

public enum OffcutStatus
{
    Available = 0,
    Reserved  = 1,
    Used      = 2,
    Waste     = 3,   // legacy — kept for backward compat
    Scrapped  = 4
}
