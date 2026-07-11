// SpaceOS.Kernel.Api/OpenApi/PagedListSchemaFilter.cs
using Microsoft.OpenApi.Models;
using SpaceOS.Kernel.Application.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpaceOS.Kernel.Api.OpenApi;

/// <summary>
/// Ensures <see cref="PagedList{T}"/> schemas are rendered with their full property set in Swagger UI.
/// Without this filter Swashbuckle may collapse the generic type to <c>object</c>.
/// </summary>
public sealed class PagedListSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType ||
            context.Type.GetGenericTypeDefinition() != typeof(PagedList<>))
        {
            return;
        }

        schema.Properties ??= new Dictionary<string, OpenApiSchema>();

        if (!schema.Properties.ContainsKey("items"))
            schema.Properties["items"] = new OpenApiSchema { Type = "array" };

        if (!schema.Properties.ContainsKey("page"))
            schema.Properties["page"] = new OpenApiSchema { Type = "integer", Format = "int32" };

        if (!schema.Properties.ContainsKey("pageSize"))
            schema.Properties["pageSize"] = new OpenApiSchema { Type = "integer", Format = "int32" };

        if (!schema.Properties.ContainsKey("totalCount"))
            schema.Properties["totalCount"] = new OpenApiSchema { Type = "integer", Format = "int32" };

        if (!schema.Properties.ContainsKey("totalPages"))
            schema.Properties["totalPages"] = new OpenApiSchema { Type = "integer", Format = "int32" };
    }
}
