using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SpaceOS.Modules.Abstractions.Application.Common;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);
        return services;
    }
}
