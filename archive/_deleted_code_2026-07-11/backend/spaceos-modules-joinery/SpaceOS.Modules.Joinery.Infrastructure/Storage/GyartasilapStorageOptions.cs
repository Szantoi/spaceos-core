namespace SpaceOS.Modules.Joinery.Infrastructure.Storage;

public sealed class GyartasilapStorageOptions
{
    public bool Enabled { get; init; } = true;
    public string BucketName { get; init; } = "gyartasilap";
    public string Endpoint { get; init; } = "localhost:9000";
    public string? PublicEndpoint { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool UseSSL { get; init; } = false;
}
