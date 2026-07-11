// SpaceOS.Kernel.Api/OpenApi/EnumStringSchemaFilter.cs
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpaceOS.Kernel.Api.OpenApi;

/// <summary>
/// Swashbuckle schema filter that converts enum schemas from <c>type: integer</c>
/// to <c>type: string</c> with named values, matching the runtime
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/> behaviour.
/// </summary>
public sealed class EnumStringSchemaFilter : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsEnum)
            return;

        schema.Type = "string";
        schema.Format = null;
        schema.Enum.Clear();

        foreach (var name in Enum.GetNames(type))
        {
            schema.Enum.Add(new OpenApiString(name));
        }
    }
}
