// SpaceOS.Kernel.Tests/Application/TaskExtensionsTests.cs
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Kernel.Application.Common;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Tests for <see cref="TaskExtensions.FireAndForget"/>.</summary>
public sealed class TaskExtensionsTests
{
    [Fact]
    public async Task FireAndForget_FaultedTask_LogsError()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TaskExtensionsTests>>();
        var tcs = new TaskCompletionSource();
        tcs.SetException(new InvalidOperationException("test failure"));

        // Act
        tcs.Task.FireAndForget(loggerMock.Object, "TestContext");
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert — LogError was called with the exception
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void FireAndForget_SuccessfulTask_DoesNotLogError()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TaskExtensionsTests>>();
        var task = Task.CompletedTask;

        // Act — should not throw
        task.FireAndForget(loggerMock.Object, "TestContext");

        // Assert — no error logged for a successful task
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
