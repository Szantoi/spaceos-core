using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record ProcessingSpec
{
    public string? CncProcessing { get; init; }
    public string? PanelProcessing { get; init; }
    public string? FrameProcessing { get; init; }
    public string? Note { get; init; }

    private ProcessingSpec() { }

    public static Result<ProcessingSpec> Create(
        string? cncProcessing,
        string? panelProcessing,
        string? frameProcessing,
        string? note)
    {
        return Result<ProcessingSpec>.Success(new ProcessingSpec
        {
            CncProcessing = cncProcessing,
            PanelProcessing = panelProcessing,
            FrameProcessing = frameProcessing,
            Note = note
        });
    }
}
