// Identity.Tests/Api/UsersControllerTests.cs

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ardalis.Result;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Application.Users.Queries;
using Identity.Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Identity.Tests.Api;

/// <summary>
/// API layer tests using WebApplicationFactory with mocked MediatR.
/// No live DB/Redis/KC connections — tests only HTTP routing, auth enforcement, and serialization.
/// </summary>
public sealed class UsersControllerTests : IClassFixture<IdentityWebFactory>
{
    private readonly IdentityWebFactory _factory;

    public UsersControllerTests(IdentityWebFactory factory) => _factory = factory;

    // ── GET /identity/users ───────────────────────────────────────────────────

    [Fact]
    public async Task ListUsers_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_WithValidToken_Returns200()
    {
        var uid = Guid.NewGuid();
        var tid = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new(uid, tid, "alice@example.com", "Alice", "Test", "Active", "Synced")
        };

        _factory.MediatorMock
            .Setup(m => m.Send(It.IsAny<ListTenantUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        var client = _factory.CreateClientWithFakeJwt();

        var response = await client.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsersByRole_ValidRole_ReturnsUsersList()
    {
        var uid = Guid.NewGuid();
        var users = new List<UserWithRoleDto>
        {
            new(uid, "János Kovács", "janos.kovacs@doorstar.hu", "machine_operator")
        };

        _factory.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersByRoleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserWithRoleDto>>.Success(users));

        var client = _factory.CreateClientWithFakeJwt();

        var response = await client.GetAsync("/identity/users?role=machine_operator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("machine_operator", json);
    }

    [Fact]
    public async Task GetUsersByRole_EmptyRole_Returns400()
    {
        _factory.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersByRoleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserWithRoleDto>>.Invalid(
                new List<ValidationError>
                {
                    new() { Identifier = "Role", ErrorMessage = "Invalid role. Allowed roles: machine_operator, production_manager, admin" }
                }));

        var client = _factory.CreateClientWithFakeJwt();

        var response = await client.GetAsync("/identity/users?role=invalid_role");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /identity/users/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetUser_NotFound_Returns404()
    {
        _factory.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.NotFound());

        var client = _factory.CreateClientWithFakeJwt();
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/identity/users/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /identity/users ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_NoToken_Returns401()
    {
        var client = _factory.CreateClient();
        var body = new StringContent(
            """{"email":"x@y.com","firstName":"X","lastName":"Y"}""",
            Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/identity/users", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ValidationError_Returns400()
    {
        _factory.MediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Invalid(
                new List<ValidationError>
                {
                    new() { ErrorMessage = "Email is required." }
                }));

        var client = _factory.CreateClientWithFakeJwt(role: "TenantAdmin");
        var body = new StringContent(
            """{"email":"","firstName":"","lastName":""}""",
            Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/identity/users", body);

        // Ardalis.Result v9 maps Result.Invalid → 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
