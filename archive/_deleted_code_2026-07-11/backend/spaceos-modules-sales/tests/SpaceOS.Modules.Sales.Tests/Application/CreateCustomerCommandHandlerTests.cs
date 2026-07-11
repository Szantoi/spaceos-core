using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Application;

public class CreateCustomerCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IQuotaGuard> _quota = new();
    private readonly Mock<ITenantContext> _tenant = new();

    public CreateCustomerCommandHandlerTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantId);
        _tenant.Setup(t => t.ActorSub).Returns("sub:user1");
        _quota.Setup(q => q.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success());
        _repo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(1);
    }

    private CreateCustomerCommandHandler BuildHandler() =>
        new(_repo.Object, _quota.Object, _tenant.Object, _clock);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCustomerResponse()
    {
        var cmd = new CreateCustomerCommand(
            CustomerType.Company, "Ajtó Kft.", "Főnök Úr",
            "fonok@ajto.hu", null, null);

        var result = await BuildHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Ajtó Kft.");
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task Handle_QuotaExceeded_ReturnsForbidden()
    {
        _quota.Setup(q => q.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Forbidden());

        var cmd = new CreateCustomerCommand(
            CustomerType.Individual, "Test", "Contact", null, null, null);

        var result = await BuildHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsInvalid()
    {
        var cmd = new CreateCustomerCommand(
            CustomerType.Individual, "Test", "Contact",
            "not-an-email", null, null);

        var result = await BuildHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var cmd = new CreateCustomerCommand(
            CustomerType.Individual, "Teszt", "Kontakt", null, null, null);

        await BuildHandler().Handle(cmd, default);

        _repo.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidEmailPhone_MapsToResponse()
    {
        var cmd = new CreateCustomerCommand(
            CustomerType.Individual, "Test Person", "Contact Name",
            "test@example.com", "+36301234567", "Note here");

        var result = await BuildHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContactEmail.Should().Be("test@example.com");
    }
}
