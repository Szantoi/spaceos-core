using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Common.Behaviors;
using SpaceOS.Kernel.Application.Events;
using SpaceOS.Kernel.Application.Snapshots;
using SpaceOS.Kernel.Application.Spaces.Services;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.Services;
using System.Reflection;

namespace SpaceOS.Kernel.Application;

/// <summary>
/// Extension methods for registering Application layer services with the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and the validation pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAuditEventDispatcher, AuditEventDispatcher>();

        // IHashProvider is stateless — singleton is safe and avoids per-request allocation.
        services.AddSingleton<IHashProvider, Sha256HashProvider>();

        // IPseudonymizer is scoped — it depends on IUserProfileRepository which is scoped.
        services.AddScoped<IPseudonymizer, Pseudonymizer>();

        // Phase 3A: Spatial BIM services
        services.AddScoped<BvhQueryService>();
        services.AddScoped<IBvhTreeService, BvhTreeService>();

        // Phase 3B: Snapshot service — internal to Application layer
        services.AddScoped<ISnapshotService, SnapshotService>();

        return services;
    }
}
