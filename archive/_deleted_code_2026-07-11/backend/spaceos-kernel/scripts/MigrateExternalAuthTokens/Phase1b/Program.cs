// scripts/MigrateExternalAuthTokens/Phase1b/Program.cs
// Phase 1b: Replace DB token values with KV references → delete tokens.json
// Usage: dotnet run --project scripts/MigrateExternalAuthTokens/Phase1b
//          -- --db-write "Host=localhost;Database=spaceos;Username=...;Password=..."

using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

var config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables("SPACEOS_MIGRATE_")
    .Build();

var dbConnection = config["db-write"]
    ?? throw new InvalidOperationException("--db-write connection string is required.");
var inputPath = config["input"] ?? "tokens.json";

if (!File.Exists(inputPath))
    throw new InvalidOperationException(
        $"tokens.json not found at '{inputPath}'. Run Phase 1a first and verify the file before running Phase 1b.");

var json    = await File.ReadAllTextAsync(inputPath).ConfigureAwait(false);
var entries = JsonSerializer.Deserialize<JsonElement[]>(json)
    ?? throw new InvalidOperationException("Failed to parse tokens.json — file may be corrupted.");

Console.WriteLine($"[Phase 1b] Loaded {entries.Length} token reference(s) from {inputPath}.");
Console.WriteLine("[Phase 1b] Replacing ExternalAuthTokenRef values in DB with KV references...");

await using var conn = new NpgsqlConnection(dbConnection);
await conn.OpenAsync().ConfigureAwait(false);

var updated = 0;
foreach (var entry in entries)
{
    var spaceLayerId = entry.GetProperty("spaceLayerId").GetGuid();
    var kvKey        = entry.GetProperty("kvKey").GetString()!;

    // Store the KV reference URI instead of the actual token value.
    // Format: kv://<kvKey> — resolved at runtime by the KV client.
    var rows = await conn.ExecuteAsync(
        """
        UPDATE "SpaceLayers"
        SET "ExternalAuthTokenRef" = @KvRef
        WHERE "Id" = @SpaceLayerId
          AND "ExternalAuthTokenRef" IS NOT NULL
          AND "ExternalAuthTokenRef" NOT LIKE 'kv://%'
        """,
        new { KvRef = $"kv://{kvKey}", SpaceLayerId = spaceLayerId }).ConfigureAwait(false);

    if (rows > 0) updated++;
}

Console.WriteLine($"[Phase 1b] Updated {updated} of {entries.Length} SpaceLayer(s).");

File.Delete(inputPath);
Console.WriteLine($"[Phase 1b] Deleted {inputPath}. Migration complete.");
Console.WriteLine("[Phase 1b] VERIFY: grep -r \"ExternalAuthToken[^R]\" --include=\"*.cs\" should return 0 results.");
