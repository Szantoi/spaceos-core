// scripts/MigrateExternalAuthTokens/Shared/TokenEntry.cs
namespace MigrateExternalAuthTokens;

/// <summary>Represents a SpaceLayer row with a non-null ExternalAuthTokenRef value.</summary>
internal sealed record TokenEntry(Guid SpaceLayerId, string TokenValue);
