// SpaceOS.Kernel.IntegrationTests/Infrastructure/TestJsonOptions.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>Shared <see cref="JsonSerializerOptions"/> for integration test deserialization.</summary>
internal static class TestJsonOptions
{
    /// <summary>Options that match the server's JSON configuration (camelCase + string enums).</summary>
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
