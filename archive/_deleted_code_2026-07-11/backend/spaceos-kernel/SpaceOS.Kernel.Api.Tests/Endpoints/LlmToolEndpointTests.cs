// SpaceOS.Kernel.Api.Tests/Endpoints/LlmToolEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Endpoints;
using SpaceOS.Kernel.Api.Tests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.Api.Tests.Endpoints;

/// <summary>Integration tests for <c>GET /api/llm-tools</c> — the LLM Tool Registry endpoint.</summary>
public sealed class LlmToolEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initialises factory and an anonymous HTTP client.</summary>
    public LlmToolEndpointTests()
    {
        _factory = new ApiFactory();
        _client  = _factory.CreateClient(); // no auth — endpoint is AllowAnonymous
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _factory.SeedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task GetLlmTools_NoAuth_Returns200()
    {
        var response = await _client.GetAsync("/api/llm-tools").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLlmTools_ReturnsList_NotEmpty()
    {
        var tools = await _client
            .GetFromJsonAsync<List<LlmToolDescriptor>>("/api/llm-tools")
            .ConfigureAwait(false);

        Assert.NotNull(tools);
        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task GetLlmTools_AllDescriptors_HaveRequiredFields()
    {
        var tools = await _client
            .GetFromJsonAsync<List<LlmToolDescriptor>>("/api/llm-tools")
            .ConfigureAwait(false);

        Assert.NotNull(tools);
        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Name),
                $"Tool name must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' description must not be empty");
            Assert.NotNull(tool.Parameters);
            Assert.Equal("object", tool.Parameters.Type);
        }
    }

    [Fact]
    public async Task GetLlmTools_ContainsDoorstarCoreTools()
    {
        var tools = await _client
            .GetFromJsonAsync<List<LlmToolDescriptor>>("/api/llm-tools")
            .ConfigureAwait(false);

        Assert.NotNull(tools);
        var names = tools.Select(t => t.Name).ToHashSet();

        Assert.Contains("create_facility",    names);
        Assert.Contains("get_facilities",     names);
        Assert.Contains("submit_door_order",  names);
        Assert.Contains("create_flow_epic",   names);
    }

    [Fact]
    public async Task GetLlmTools_CreateFacility_HasNameParameter()
    {
        var tools = await _client
            .GetFromJsonAsync<List<LlmToolDescriptor>>("/api/llm-tools")
            .ConfigureAwait(false);

        Assert.NotNull(tools);
        var createFacility = tools.SingleOrDefault(t => t.Name == "create_facility");
        Assert.NotNull(createFacility);
        Assert.Contains("name", createFacility.Parameters.Required);
        Assert.True(createFacility.Parameters.Properties.ContainsKey("name"));
    }
}
