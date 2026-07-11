// SpaceOS.Kernel.Tests/Application/UploadFlowEpicProofCommandHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="UploadFlowEpicProofCommandHandler"/>.</summary>
public class UploadFlowEpicProofCommandHandlerTests
{
    private const string ExpectedHash = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1e2";

    private readonly Mock<IImmutableStorage>           _storageMock = new();
    private readonly UploadFlowEpicProofCommandHandler _handler;

    public UploadFlowEpicProofCommandHandlerTests()
    {
        _handler = new UploadFlowEpicProofCommandHandler(_storageMock.Object);
    }

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new UploadFlowEpicProofCommand(
            FlowEpicId: Guid.NewGuid(),
            FileName:   "proof/doc.pdf",
            Content:    [0x25, 0x50, 0x44, 0x46]); // minimal PDF bytes

        _storageMock
            .Setup(s => s.StoreAsync(command.FileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExpectedHash);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProofUploadDtoWithCorrectHash()
    {
        // Arrange
        var command = new UploadFlowEpicProofCommand(
            FlowEpicId: Guid.NewGuid(),
            FileName:   "proof/doc.pdf",
            Content:    [0x25, 0x50, 0x44, 0x46]);

        _storageMock
            .Setup(s => s.StoreAsync(command.FileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExpectedHash);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ExpectedHash, result.Value.ProofHash);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProofUploadDtoWithFileNameAsProofUrl()
    {
        // Arrange
        const string fileName = "proof/doc.pdf";
        var command = new UploadFlowEpicProofCommand(
            FlowEpicId: Guid.NewGuid(),
            FileName:   fileName,
            Content:    [0x25, 0x50, 0x44, 0x46]);

        _storageMock
            .Setup(s => s.StoreAsync(fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExpectedHash);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(fileName, result.Value.ProofUrl);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsStorageStoreAsync_Once()
    {
        // Arrange
        var command = new UploadFlowEpicProofCommand(
            FlowEpicId: Guid.NewGuid(),
            FileName:   "proof/doc.pdf",
            Content:    [0x25, 0x50, 0x44, 0x46]);

        _storageMock
            .Setup(s => s.StoreAsync(command.FileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExpectedHash);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _storageMock.Verify(
            s => s.StoreAsync(command.FileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_PassesContentBytesToStorage()
    {
        // Arrange
        byte[] expectedBytes = [0x01, 0x02, 0x03];
        var command = new UploadFlowEpicProofCommand(
            FlowEpicId: Guid.NewGuid(),
            FileName:   "proof/file.bin",
            Content:    expectedBytes);

        byte[]? capturedBytes = null;
        _storageMock
            .Setup(s => s.StoreAsync(command.FileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<string, Stream, CancellationToken>((_, stream, _) =>
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                capturedBytes = ms.ToArray();
            })
            .ReturnsAsync(ExpectedHash);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedBytes, capturedBytes);
    }
}
