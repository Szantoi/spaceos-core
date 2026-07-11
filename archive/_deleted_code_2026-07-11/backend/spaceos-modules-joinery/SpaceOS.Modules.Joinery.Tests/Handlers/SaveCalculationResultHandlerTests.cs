namespace SpaceOS.Modules.Joinery.Tests.Handlers;

/// <summary>
/// Tests for SaveCalculationResultCommandHandler.
///
/// SKIPPED: SaveCalculationResultCommandHandler does not yet exist in the Application layer.
/// The command record (SaveCalculationResultCommand) is defined but has no handler.
/// These tests must be written once the handler is implemented.
///
/// Required tests (tracked here for DoD visibility):
///   - Handle_OrderNotFound_ReturnsNotFound
///   - Handle_ValidResult_CreatesSnapshotAndSaves
///   - Handle_NewResult_MarksOldSnapshotNotLatest (DB-03)
///   - Handle_AllItemsDone_CallsMarkCalculated
///   - Handle_ConcurrencyConflict_ReturnsConcurrencyError
/// </summary>
public class SaveCalculationResultHandlerTests
{
    // No tests yet — handler not implemented.
    // When the handler is added, mock IDoorOrderRepository, ICuttingListSnapshotRepository,
    // IOutboxWriter, and IClock. Follow the pattern in SubmitDoorOrderHandlerTests.cs.
}
