// SpaceOS.Kernel.Application/Nodes/NodeManifestDto.cs
namespace SpaceOS.Kernel.Application.Nodes;

/// <summary>
/// Read model returned from node manifest queries and the register-node command.
/// <para><see cref="NodeJwt"/> is only populated immediately after registration; subsequent queries return <see langword="null"/>.</para>
/// </summary>
public sealed record NodeManifestDto(
    Guid Id,
    Guid TenantId,
    string ServerUrl,
    string PublicApiVersion,
    DateTimeOffset? LastHeartbeatAt,
    int MaxGuestLod,
    string? NodeJwt = null);
