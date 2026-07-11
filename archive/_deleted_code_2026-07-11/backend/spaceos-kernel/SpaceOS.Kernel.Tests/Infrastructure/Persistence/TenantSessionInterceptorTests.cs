// SpaceOS.Kernel.Tests/Infrastructure/Persistence/TenantSessionInterceptorTests.cs
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Persistence;

/// <summary>
/// Unit tests for <see cref="TenantSessionInterceptor"/>.
/// All tests use a <see cref="CapturingDbConnection"/> that records the last
/// executed <c>set_config</c> key and value so assertions can verify correct
/// behaviour without a real database.
/// </summary>
public sealed class TenantSessionInterceptorTests
{
    // -------------------------------------------------------------------------
    // Constructor guard
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullAccessor_ThrowsArgumentNullException()
    {
        var logger = new Mock<ILogger<TenantSessionInterceptor>>();
        Assert.Throws<ArgumentNullException>(() =>
            new TenantSessionInterceptor(null!, logger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        Assert.Throws<ArgumentNullException>(() =>
            new TenantSessionInterceptor(accessor.Object, null!));
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — valid tid claim sets session variable
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_ValidTidClaim_SetsSessionConfig()
    {
        // Arrange
        var tenantId   = Guid.NewGuid();
        var connection = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: tenantId.ToString());

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — set_config called with the correct tenant GUID
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal(tenantId.ToString(), connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — no tid claim → sentinel fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_NoTidClaim_DoesNotSetSessionConfig()
    {
        // Arrange
        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: null);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — interceptor falls back to sentinel so that PostgreSQL RLS does not throw.
        // The sentinel grants full read access and is only relevant in a PostgreSQL context;
        // in unit tests it is harmless because CapturingDbConnection never enforces RLS.
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal("00000000-0000-0000-0000-000000000001", connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — malformed tid claim → sentinel fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_MalformedTidClaim_DoesNotSetSessionConfig()
    {
        // Arrange
        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: "not-a-guid");

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — malformed claim is rejected; interceptor falls back to sentinel.
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal("00000000-0000-0000-0000-000000000001", connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — Guid.Empty tid → sentinel fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_EmptyGuidTidClaim_DoesNotSetSessionConfig()
    {
        // Arrange
        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: Guid.Empty.ToString());

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — Guid.Empty is rejected; interceptor falls back to sentinel.
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal("00000000-0000-0000-0000-000000000001", connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — no HttpContext (background job) → sentinel fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_NoHttpContext_DoesNotSetSessionConfig()
    {
        // Arrange — accessor returns null HttpContext (background job scenario)
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var logger = new Mock<ILogger<TenantSessionInterceptor>>();
        var interceptor = new TenantSessionInterceptor(accessor.Object, logger.Object);
        var connection  = new CapturingDbConnection();

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — background jobs have no HttpContext; interceptor falls back to sentinel
        // so that RLS USING clauses using current_setting('app.current_tenant_id') do not
        // throw "unrecognized configuration parameter" on PostgreSQL.
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal("00000000-0000-0000-0000-000000000001", connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionClosingAsync — always resets to empty string (pool leak prevention)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionClosingAsync_Always_ResetsSessionConfigToEmpty()
    {
        // Arrange — even without a tenant claim the reset must fire
        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: null);

        // Act
        await interceptor.ConnectionClosingAsync(
            connection,
            CreateClosingEventData(connection),
            InterceptionResult.Suppress());

        // Assert — reset to empty string regardless of claim presence
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal(string.Empty, connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionClosingAsync_WithValidTid_StillResetsToEmpty()
    {
        // Arrange — even when a tenant claim is present, closing always resets
        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptor(tidClaim: Guid.NewGuid().ToString());

        // Act
        await interceptor.ConnectionClosingAsync(
            connection,
            CreateClosingEventData(connection),
            InterceptionResult.Suppress());

        // Assert — closing always resets, not sets
        Assert.Equal(string.Empty, connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // ConnectionOpenedAsync — tid takes priority over spaceos_tenants
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConnectionOpenedAsync_TidAndSpaceosTenants_UsesTidNotSpaceosTenants()
    {
        // Arrange — JWT has both tid (DB tenant UUID) and spaceos_tenants (Keycloak UUID).
        // TenantSessionInterceptor must use tid so that app.current_tenant_id matches
        // ClaimsTenantResolver (which also reads tid), preventing RLS violations on
        // AggregateSnapshots and OutboxMessages (FORCE RLS on TenantId column).
        var tidGuid        = Guid.NewGuid();
        var keycloakGuid   = Guid.NewGuid(); // different UUID — must NOT be used
        var spaceosTenants = "[{\"tenant_id\":\"" + keycloakGuid + "\",\"tenant_type\":\"Producer\",\"enabled_modules\":[],\"brand_skin\":\"default\"}]";

        var claims = new List<Claim>
        {
            new("tid",             tidGuid.ToString()),
            new("spaceos_tenants", spaceosTenants),
        };

        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithClaims(claims);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — tid UUID wins; Keycloak UUID is ignored
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal(tidGuid.ToString(), connection.LastSetValue);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_NoTidButSpaceosTenants_UsesSpaceosTenants()
    {
        // Arrange — pure Keycloak token without tid flat claim.
        var keycloakGuid   = Guid.NewGuid();
        var spaceosTenants = "[{\"tenant_id\":\"" + keycloakGuid + "\",\"tenant_type\":\"Producer\",\"enabled_modules\":[],\"brand_skin\":\"default\"}]";

        var claims = new List<Claim>
        {
            new("spaceos_tenants", spaceosTenants),
        };

        var connection  = new CapturingDbConnection();
        var interceptor = BuildInterceptorWithClaims(claims);

        // Act
        await interceptor.ConnectionOpenedAsync(
            connection,
            CreateOpenedEventData(connection),
            CancellationToken.None);

        // Assert — falls back to spaceos_tenants UUID
        Assert.Equal("app.current_tenant_id", connection.LastSetKey);
        Assert.Equal(keycloakGuid.ToString(), connection.LastSetValue);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static TenantSessionInterceptor BuildInterceptor(string? tidClaim)
    {
        var claims = new List<Claim>();
        if (tidClaim is not null)
            claims.Add(new Claim("tid", tidClaim));

        return BuildInterceptorWithClaims(claims);
    }

    private static TenantSessionInterceptor BuildInterceptorWithClaims(IEnumerable<Claim> claims)
    {
        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context   = new DefaultHttpContext { User = principal };

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);

        var logger = new Mock<ILogger<TenantSessionInterceptor>>();

        return new TenantSessionInterceptor(accessor.Object, logger.Object);
    }

    // The interceptor reads only from DbConnection — event data is never used.
    // The base DbConnectionInterceptor.ConnectionOpenedAsync / ConnectionClosingAsync
    // are confirmed no-ops when event data is null (verified against EF Core 8.0.11).
#pragma warning disable CS8603 // Possible null reference return — intentional for test stubs
    private static ConnectionEndEventData CreateOpenedEventData(DbConnection connection) => null!;
    private static ConnectionEventData    CreateClosingEventData(DbConnection connection) => null!;
#pragma warning restore CS8603
}

// ---------------------------------------------------------------------------
// CapturingDbConnection — records the last set_config parameters
// ---------------------------------------------------------------------------

/// <summary>
/// Minimal <see cref="DbConnection"/> that captures the most recent
/// <c>set_config</c> key and value issued through it.
/// Used only in unit tests — never in production code.
/// </summary>
internal sealed class CapturingDbConnection : DbConnection
{
    /// <summary>The last <c>app.*</c> key passed to <c>set_config</c>, or <c>null</c> if never called.</summary>
    public string? LastSetKey   { get; private set; }

    /// <summary>The last value passed to <c>set_config</c>, or <c>null</c> if never called.</summary>
    public string? LastSetValue { get; private set; }

    [AllowNull] public override string ConnectionString { get; set; } = string.Empty;
    public override string Database         => string.Empty;
    public override string DataSource       => string.Empty;
    public override string ServerVersion    => string.Empty;
    public override ConnectionState State   => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() => new CapturingDbCommand(this);

    internal void RecordSetConfig(string key, string value)
    {
        LastSetKey   = key;
        LastSetValue = value;
    }
}

/// <summary>
/// Minimal <see cref="DbCommand"/> that intercepts <c>set_config</c> calls
/// and records the key/value pair on the owning <see cref="CapturingDbConnection"/>.
/// </summary>
internal sealed class CapturingDbCommand : DbCommand
{
    private readonly CapturingDbConnection     _owner;
    private readonly CapturingParameterCollection _params = new();

    public CapturingDbCommand(CapturingDbConnection owner) => _owner = owner;

    [AllowNull] public override string CommandText { get; set; } = string.Empty;
    public override int      CommandTimeout   { get; set; }
    public override System.Data.CommandType CommandType { get; set; }
    public override bool     DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection?  DbConnection  { get; set; }
    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => _params;

    public override void Cancel()  { }
    public override void Prepare() { }

    public override int ExecuteNonQuery()
    {
        CaptureSetConfig();
        return 0;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        CaptureSetConfig();
        return Task.FromResult(0);
    }

    public override object? ExecuteScalar() => null;

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => throw new NotSupportedException();

    protected override DbParameter CreateDbParameter() => new CapturingDbParameter();

    private void CaptureSetConfig()
    {
        if (!CommandText.Contains("set_config", StringComparison.OrdinalIgnoreCase))
            return;

        if (_params.Count >= 2)
        {
            var key   = _params[0].Value?.ToString() ?? string.Empty;
            var value = _params[1].Value?.ToString() ?? string.Empty;
            _owner.RecordSetConfig(key, value);
        }
    }
}

/// <summary>Minimal <see cref="DbParameter"/> for use with <see cref="CapturingDbCommand"/>.</summary>
internal sealed class CapturingDbParameter : DbParameter
{
    public override DbType DbType         { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool   IsNullable     { get; set; }
    [AllowNull] public override string ParameterName  { get; set; } = string.Empty;
    public override int    Size           { get; set; }
    [AllowNull] public override string SourceColumn   { get; set; } = string.Empty;
    public override bool   SourceColumnNullMapping { get; set; }
    public override object? Value         { get; set; }
    public override void ResetDbType()    { }
}

/// <summary>
/// Minimal <see cref="DbParameterCollection"/> for <see cref="CapturingDbCommand"/>.
/// Stores parameters in insertion order for positional <c>$1</c>/<c>$2</c> access.
/// </summary>
internal sealed class CapturingParameterCollection : DbParameterCollection
{
    private readonly List<CapturingDbParameter> _items = [];

    public override int      Count    => _items.Count;
    public override object   SyncRoot => _items;

    public new CapturingDbParameter this[int index] => _items[index];

    public override int  Add(object value)
    {
        _items.Add((CapturingDbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var v in values) Add(v);
    }

    public override void   Clear()                              => _items.Clear();
    public override bool   Contains(object value)              => _items.Contains((CapturingDbParameter)value);
    public override bool   Contains(string value)              => _items.Any(p => p.ParameterName == value);
    public override void   CopyTo(Array array, int index)      => ((ICollection)_items).CopyTo(array, index);
    public override IEnumerator GetEnumerator()                => _items.GetEnumerator();
    public override int    IndexOf(object value)               => _items.IndexOf((CapturingDbParameter)value);
    public override int    IndexOf(string parameterName)       => _items.FindIndex(p => p.ParameterName == parameterName);
    public override void   Insert(int index, object value)     => _items.Insert(index, (CapturingDbParameter)value);
    public override void   Remove(object value)                => _items.Remove((CapturingDbParameter)value);
    public override void   RemoveAt(int index)                 => _items.RemoveAt(index);
    public override void   RemoveAt(string parameterName)      => _items.RemoveAll(p => p.ParameterName == parameterName);

    protected override DbParameter GetParameter(int index)            => _items[index];
    protected override DbParameter GetParameter(string parameterName) =>
        _items.First(p => p.ParameterName == parameterName);

    protected override void SetParameter(int index, DbParameter value)
        => _items[index] = (CapturingDbParameter)value;

    protected override void SetParameter(string parameterName, DbParameter value)
        => _items[_items.FindIndex(p => p.ParameterName == parameterName)] =
            (CapturingDbParameter)value;
}
