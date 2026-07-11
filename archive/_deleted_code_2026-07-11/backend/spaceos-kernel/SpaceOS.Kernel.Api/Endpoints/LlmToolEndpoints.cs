// SpaceOS.Kernel.Api/Endpoints/LlmToolEndpoints.cs

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>
/// Minimal API endpoint that exposes the static LLM Tool Registry.
/// Returns OpenAI-compatible function-calling descriptors for the Orchestrator's
/// agentic loop (5 Golden Rule #1: Data → Rules → Geometry — LLM only supplies parameters).
/// </summary>
public static class LlmToolEndpoints
{
    /// <summary>Maps <c>GET /api/llm-tools</c> to the application.</summary>
    public static IEndpointRouteBuilder MapLlmToolEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/llm-tools", () => Results.Ok(ToolRegistry.All))
            .AllowAnonymous()
            .WithName("GetLlmTools")
            .WithTags("LlmTools")
            .WithSummary("Returns all available LLM tool descriptors (OpenAI function-calling format).")
            .Produces<IReadOnlyList<LlmToolDescriptor>>();

        return app;
    }
}

/// <summary>
/// OpenAI-compatible function-calling tool descriptor.
/// </summary>
/// <param name="Name">Snake_case tool name used in tool_call responses.</param>
/// <param name="Description">Human-readable description for the LLM to understand when to invoke this tool.</param>
/// <param name="Parameters">JSON Schema object describing the tool's input parameters.</param>
public sealed record LlmToolDescriptor(
    string Name,
    string Description,
    LlmToolParameters Parameters);

/// <summary>JSON Schema <c>object</c> wrapper for tool parameters.</summary>
/// <param name="Properties">Map of parameter name → schema definition.</param>
/// <param name="Required">Names of required parameters.</param>
public sealed record LlmToolParameters(
    Dictionary<string, LlmToolProperty> Properties,
    IReadOnlyList<string> Required)
{
    /// <summary>Always <c>"object"</c> per the OpenAI function-calling spec.</summary>
    public string Type => "object";
}

/// <summary>JSON Schema property definition for a single tool parameter.</summary>
/// <param name="Type">JSON Schema type: <c>"string"</c>, <c>"number"</c>, <c>"array"</c>, etc.</param>
/// <param name="Description">Description shown to the LLM for this parameter.</param>
/// <param name="Format">Optional JSON Schema format hint (e.g. <c>"uuid"</c>, <c>"date-time"</c>).</param>
/// <param name="Items">For <c>type: "array"</c> — schema of individual items.</param>
public sealed record LlmToolProperty(
    string Type,
    string Description,
    string? Format = null,
    LlmToolProperty? Items = null);

/// <summary>
/// Static registry of all available LLM tools for the Doorstar tenant type.
/// Extend this list as new Kernel capabilities are exposed to the agentic loop.
/// Dynamic registry is Q4 scope.
/// </summary>
internal static class ToolRegistry
{
    internal static readonly IReadOnlyList<LlmToolDescriptor> All =
    [
        new LlmToolDescriptor(
            Name: "get_facilities",
            Description: "Lists all production facilities for the current tenant.",
            Parameters: new LlmToolParameters(
                Properties: [],
                Required: [])),

        new LlmToolDescriptor(
            Name: "create_facility",
            Description: "Creates a new production facility for the current tenant.",
            Parameters: new LlmToolParameters(
                Properties: new Dictionary<string, LlmToolProperty>
                {
                    ["name"] = new("string", "Display name of the facility (max 100 characters).")
                },
                Required: ["name"])),

        new LlmToolDescriptor(
            Name: "get_work_stations",
            Description: "Lists all work stations in a facility.",
            Parameters: new LlmToolParameters(
                Properties: new Dictionary<string, LlmToolProperty>
                {
                    ["facility_id"] = new("string", "UUID of the facility.", Format: "uuid")
                },
                Required: ["facility_id"])),

        new LlmToolDescriptor(
            Name: "create_work_station",
            Description: "Creates a new work station inside a facility.",
            Parameters: new LlmToolParameters(
                Properties: new Dictionary<string, LlmToolProperty>
                {
                    ["facility_id"] = new("string", "UUID of the parent facility.", Format: "uuid"),
                    ["name"]        = new("string", "Display name of the work station (max 100 characters)."),
                    ["type"]        = new("string", "Work station type: 'CNC', 'Manual', 'Assembly', or 'QualityControl'.")
                },
                Required: ["facility_id", "name", "type"])),

        new LlmToolDescriptor(
            Name: "get_flow_epics",
            Description: "Lists manufacturing flow epics (orders) for the current tenant.",
            Parameters: new LlmToolParameters(
                Properties: [],
                Required: [])),

        new LlmToolDescriptor(
            Name: "create_flow_epic",
            Description: "Opens a new manufacturing flow epic (production order) for the tenant.",
            Parameters: new LlmToolParameters(
                Properties: new Dictionary<string, LlmToolProperty>
                {
                    ["title"]       = new("string", "Short descriptive title for the flow epic."),
                    ["facility_id"] = new("string", "UUID of the facility where production takes place.", Format: "uuid")
                },
                Required: ["title", "facility_id"])),

        new LlmToolDescriptor(
            Name: "submit_door_order",
            Description: "Submits a door manufacturing order to the Joinery module.",
            Parameters: new LlmToolParameters(
                Properties: new Dictionary<string, LlmToolProperty>
                {
                    ["facility_id"] = new("string", "UUID of the facility producing the order.", Format: "uuid"),
                    ["items"]       = new("array", "List of door line items to manufacture.",
                                        Items: new("object", "A single door line item with model, dimensions, and quantity."))
                },
                Required: ["facility_id", "items"])),
    ];
}
