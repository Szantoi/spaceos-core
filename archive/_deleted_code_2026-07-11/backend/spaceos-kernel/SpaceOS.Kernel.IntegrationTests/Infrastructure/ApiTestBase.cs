// SpaceOS.Kernel.IntegrationTests/Infrastructure/ApiTestBase.cs
using System.Net.Http.Headers;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for API integration tests. Provides a pre-configured <see cref="HttpClient"/>
/// and access to the test DI container for seed helpers.
/// The client is pre-authorised with a Designer JWT scoped to <see cref="SpaceOsApiFactory.TestTenantId"/>.
/// Designer can read and write, making it suitable as the default role across all pipeline tests.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    /// <summary>The shared <see cref="SpaceOsApiFactory"/> for this test class.</summary>
    protected readonly SpaceOsApiFactory Factory;

    /// <summary>Pre-configured <see cref="HttpClient"/> targeting the in-memory test host.</summary>
    protected readonly HttpClient Client;

    /// <summary>The test host's <see cref="IServiceProvider"/> for seed helpers and scope creation.</summary>
    protected IServiceProvider Services => Factory.Services;

    /// <summary>Initialises the factory and creates the default HTTP client with an Admin JWT.</summary>
    protected ApiTestBase()
    {
        Factory = new SpaceOsApiFactory();
        Client  = Factory.CreateClient();
        // Admin satisfies ReadPolicy, WritePolicy, and AdminPolicy — suitable default
        // for all pipeline tests which exercise the full range of endpoints.
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.ForRole("Admin"));
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await Factory.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync().ConfigureAwait(false);
    }
}
