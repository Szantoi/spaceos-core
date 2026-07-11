// scripts/MigrateExternalAuthTokens/Phase1a/Program.cs
// Phase 1a: DB readonly → tokens.json (Key Vault write stub)
// Usage: dotnet run --project scripts/MigrateExternalAuthTokens/Phase1a
//          -- --db-readonly "Host=localhost;Database=spaceos;Username=...;Password=..."

using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

var config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables("SPACEOS_MIGRATE_")
    .Build();

var dbConnection = config["db-readonly"]
    ?? throw new InvalidOperationException("--db-readonly connection string is required.");
var outputPath = config["output"] ?? "tokens.json";

Console.WriteLine("[Phase 1a] Reading ExternalAuthTokenRef values from DB...");

await using var conn = new NpgsqlConnection(dbConnection);
await conn.OpenAsync().ConfigureAwait(false);

var rows = (await conn.QueryAsync<(Guid SpaceLayerId, string TokenValue)>(
    """
    SELECT "Id" AS "SpaceLayerId", "ExternalAuthTokenRef" AS "TokenValue"
    FROM "SpaceLayers"
    WHERE "ExternalAuthTokenRef" IS NOT NULL
    """).ConfigureAwait(false)).ToList();

Console.WriteLine($"[Phase 1a] Found {rows.Count} SpaceLayer(s) with ExternalAuthTokenRef.");

var output = rows.Select(r => new
{
    spaceLayerId = r.SpaceLayerId,
    kvKey        = $"spaceos-external-token-{r.SpaceLayerId:N}",
    tokenValue   = r.TokenValue
}).ToList();

var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);

Console.WriteLine($"[Phase 1a] Wrote {rows.Count} token(s) to {outputPath}.");
Console.WriteLine("[Phase 1a] NEXT: verify tokens.json contents, then run Phase 1b to replace DB values with KV references.");
