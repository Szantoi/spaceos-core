using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.FinalizeGyartasilap;

public sealed class FinalizeGyartasilapCommandHandler
    : IRequestHandler<FinalizeGyartasilapCommand, Result>
{
    private readonly IGyartasilapRepository _repository;

    public FinalizeGyartasilapCommandHandler(IGyartasilapRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(FinalizeGyartasilapCommand request, CancellationToken ct)
    {
        var gyartasilap = await _repository
            .GetByIdAsync(request.GyartasilapId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (gyartasilap is null)
            return Result.NotFound($"Gyártásilap {request.GyartasilapId} not found.");

        var finalizeResult = gyartasilap.Finalize();
        if (!finalizeResult.IsSuccess)
            return Result.Invalid(finalizeResult.ValidationErrors);

        await _repository.UpdateAsync(gyartasilap, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
