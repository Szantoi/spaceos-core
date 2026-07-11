// SpaceOS.Kernel.Application/Nodes/Commands/RegisterNode/RegisterNodeCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Actors;
using SpaceOS.Modules.Abstractions.Sync;

namespace SpaceOS.Kernel.Application.Nodes.Commands.RegisterNode;

/// <summary>Handles <see cref="RegisterNodeCommand"/> — validates the URL, creates the manifest, issues a node JWT, and persists.</summary>
internal sealed class RegisterNodeCommandHandler
    : IRequestHandler<RegisterNodeCommand, Result<NodeManifestDto>>
{
    private readonly INodeManifestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INodeUrlValidator _urlValidator;
    private readonly INodeAuthService _nodeAuthService;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initialises a new <see cref="RegisterNodeCommandHandler"/>.</summary>
    public RegisterNodeCommandHandler(
        INodeManifestRepository repository,
        IUnitOfWork unitOfWork,
        INodeUrlValidator urlValidator,
        INodeAuthService nodeAuthService,
        IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(urlValidator);
        ArgumentNullException.ThrowIfNull(nodeAuthService);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _urlValidator = urlValidator;
        _nodeAuthService = nodeAuthService;
        _dispatcher = dispatcher;
    }

    /// <summary>Handles the registration of a new node manifest.</summary>
    /// <param name="command">The register-node command.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Result{T}.Success"/> with the new <see cref="NodeManifestDto"/> (including <c>NodeJwt</c>),
    /// <see cref="Result{T}.Invalid"/> when the URL fails SSRF validation,
    /// or <see cref="Result{T}.Conflict"/> when a manifest already exists for the tenant.
    /// </returns>
    public async Task<Result<NodeManifestDto>> Handle(RegisterNodeCommand command, CancellationToken ct)
    {
        var urlError = _urlValidator.Validate(command.ServerUrl);
        if (urlError is not null)
            return Result<NodeManifestDto>.Invalid(
                new Ardalis.Result.ValidationError(nameof(command.ServerUrl), urlError));

        var tenantId = TenantId.From(command.TenantId);

        var existing = await _repository.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);
        if (existing is not null)
            return Result<NodeManifestDto>.Conflict();

        var manifest = NodeManifest.Create(tenantId, command.ServerUrl);

        var jwt = await _nodeAuthService
            .IssueNodeJwtAsync(command.TenantId, command.ServerUrl, ct)
            .ConfigureAwait(false);

        await _repository.AddAsync(manifest, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = manifest.PopDomainEvents();
        await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        var dto = new NodeManifestDto(
            manifest.Id,
            manifest.TenantId.Value,
            manifest.ServerUrl,
            manifest.PublicApiVersion,
            manifest.LastHeartbeatAt,
            manifest.MaxGuestLod,
            NodeJwt: jwt);

        return Result<NodeManifestDto>.Success(dto);
    }
}
