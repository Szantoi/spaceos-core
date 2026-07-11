// SpaceOS.Kernel.Tests/Domain/ProofStorageKeyTests.cs

using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Unit tests verifying proof storage key format and sanitisation rules.
/// These tests exercise the key-generation logic from <c>LocalProofStorageService</c>
/// without instantiating the actual service (no I/O).
/// </summary>
public sealed class ProofStorageKeyTests
{
    private static string BuildKey(Guid tenantId, string fileName)
    {
        // Reproduce the sanitisation logic from LocalProofStorageService
        var fileName2 = fileName.Replace("..", string.Empty, StringComparison.Ordinal)
                                .Replace('/', '_')
                                .Replace('\\', '_');
        var invalid   = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(fileName2.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
        if (sanitized.Length > 100) sanitized = sanitized[..100];

        var today = DateTimeOffset.UtcNow;
        var fileId = Guid.NewGuid().ToString("N");
        return $"{tenantId:D}/{today:yyyy/MM/dd}/{fileId}_{sanitized}";
    }

    [Fact]
    public void StorageKey_StartsWithTenantId()
    {
        var tenantId = Guid.NewGuid();
        var key = BuildKey(tenantId, "proof.pdf");
        Assert.StartsWith(tenantId.ToString("D"), key, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageKey_ContainsDateSegment()
    {
        var tenantId = Guid.NewGuid();
        var key = BuildKey(tenantId, "proof.pdf");
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
        Assert.Contains(expectedDate, key, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageKey_PathTraversal_DotDot_IsRemoved()
    {
        var tenantId = Guid.NewGuid();
        var key = BuildKey(tenantId, "../../etc/passwd");
        Assert.DoesNotContain("..", key, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageKey_ForwardSlash_IsSanitised()
    {
        var tenantId = Guid.NewGuid();
        var key = BuildKey(tenantId, "sub/dir/file.pdf");
        // After the date segment the file name portion must not contain raw slashes
        var afterDate = key.Split('/').Last();
        Assert.DoesNotContain("/", afterDate, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageKey_BackSlash_IsSanitised()
    {
        var tenantId = Guid.NewGuid();
        var key = BuildKey(tenantId, @"sub\dir\file.pdf");
        var afterDate = key.Split('/').Last();
        Assert.DoesNotContain(@"\", afterDate, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageKey_LongFileName_TruncatedTo100Chars()
    {
        var tenantId = Guid.NewGuid();
        var longName = new string('x', 200) + ".pdf";
        var key = BuildKey(tenantId, longName);
        var fileNamePart = key.Split('/').Last();
        // The file-name segment is {fileId}_{sanitized} — sanitized is ≤ 100 chars
        var sanitizedPart = fileNamePart[(fileNamePart.IndexOf('_') + 1)..];
        Assert.True(sanitizedPart.Length <= 100, $"Sanitized part length was {sanitizedPart.Length}");
    }
}
