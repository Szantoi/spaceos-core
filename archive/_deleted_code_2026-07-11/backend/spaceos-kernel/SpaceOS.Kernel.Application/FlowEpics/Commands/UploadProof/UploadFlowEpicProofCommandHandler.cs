// SpaceOS.Kernel.Application/FlowEpics/Commands/UploadProof/UploadFlowEpicProofCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;

/// <summary>
/// Handles <see cref="UploadFlowEpicProofCommand"/>: stores the proof document in immutable storage
/// and returns the URL and SHA-256 hash. Does not modify the FlowEpic aggregate.
/// </summary>
internal sealed class UploadFlowEpicProofCommandHandler
    : IRequestHandler<UploadFlowEpicProofCommand, Result<ProofUploadDto>>
{
    private readonly IImmutableStorage _immutableStorage;

    /// <summary>
    /// Initialises a new <see cref="UploadFlowEpicProofCommandHandler"/>.
    /// </summary>
    /// <param name="immutableStorage">The immutable file storage service.</param>
    public UploadFlowEpicProofCommandHandler(IImmutableStorage immutableStorage)
    {
        ArgumentNullException.ThrowIfNull(immutableStorage);
        _immutableStorage = immutableStorage;
    }

    /// <inheritdoc/>
    public async Task<Result<ProofUploadDto>> Handle(
        UploadFlowEpicProofCommand request, CancellationToken ct)
    {
        using var contentStream = new MemoryStream(request.Content);

        var proofHash = await _immutableStorage
            .StoreAsync(request.FileName, contentStream, ct)
            .ConfigureAwait(false);

        // The proof URL is the logical file name — in production this resolves to a blob URI.
        var proofUrl = request.FileName;

        return Result<ProofUploadDto>.Success(new ProofUploadDto(proofUrl, proofHash));
    }
}
