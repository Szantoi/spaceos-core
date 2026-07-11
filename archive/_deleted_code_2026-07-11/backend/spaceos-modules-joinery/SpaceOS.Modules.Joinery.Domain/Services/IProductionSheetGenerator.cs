using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Domain service contract for generating production document PDFs.
/// Implemented in Infrastructure to keep PDF rendering dependencies out of Domain.
/// </summary>
public interface IProductionSheetGenerator
{
    /// <summary>
    /// Generates a PDF production sheet (vágólista + CNC + folyamat) from calculation snapshots.
    /// </summary>
    Stream Generate(DoorOrder order, IReadOnlyList<CuttingListSnapshot> snapshots);

    /// <summary>
    /// Generates a manufacturing sheet PDF from the order's DoorItems.
    /// Does not require calculation snapshots — works for any order status.
    /// </summary>
    Stream GenerateManufacturingSheet(DoorOrder order);

    /// <summary>
    /// Generates a hardware list PDF from the resolved hardware items.
    /// </summary>
    Stream GenerateHardwareListPdf(DoorOrder order, IReadOnlyList<HardwareListItem> items);

    /// <summary>
    /// Generates a material requirements PDF from the calculated material needs.
    /// </summary>
    Stream GenerateMaterialReqPdf(DoorOrder order, IReadOnlyList<MaterialRequirement> requirements);
}
