using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Application.Documents;
using DocuVault.Domain.Entities;

namespace DocuVault.UnitTests.Application.Documents;

public sealed class CreateDocumentServiceTests
{
    private sealed class FakeDocumentWriter : IDocumentWriter
    {
        public List<Document> Documents { get; } = [];

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            Documents.Add(document);

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateAsyncShouldCreateAndPersistDocument()
    {
        var writer = new FakeDocumentWriter();
        var service = new CreateDocumentService(writer);

        var command = new CreateDocumentCommand(
            "contract.pdf",
            "application/pdf",
            1024,
            new string('a', 64)
        );

        Document result = await service.CreateAsync(command);

        Assert.NotNull(result);

        Assert.Equal("contract.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(1024, result.FileSize);
        Assert.Equal(new string('a', 64), result.Hash);

        _ = Assert.Single(writer.Documents);
        Assert.Same(result, writer.Documents[0]);
    }

    [Fact]
    public async Task CreateAsyncShouldGenerateUniqueDocumentIds()
    {
        // Arrange
        var writer = new FakeDocumentWriter();
        var service = new CreateDocumentService(writer);

        var command = new CreateDocumentCommand(
            "contract.pdf",
            "application/pdf",
            1024,
            new string('a', 64));

        // Act
        Document first = await service.CreateAsync(command);
        Document second = await service.CreateAsync(command);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, writer.Documents.Count);
    }

    [Fact]
    public async Task CreateAsyncShouldForwardCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var writer = new FakeDocumentWriter();
        var service = new CreateDocumentService(writer);

        var command = new CreateDocumentCommand(
            "contract.pdf",
            "application/pdf",
            1024,
            new string('a', 64));

        // Act
        _ = await service.CreateAsync(command, cts.Token);

        // Assert
        Assert.Equal(
            cts.Token,
            writer.ReceivedCancellationToken);
    }

    [Fact]
    public async Task CreateAsyncShouldNotPersistDocumentWhenDomainValidationFails()
    {
        // Arrange
        var writer = new FakeDocumentWriter();
        var service = new CreateDocumentService(writer);

        var command = new CreateDocumentCommand(
            "",
            "application/pdf",
            1024,
            new string('a', 64));

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(command));

        Assert.Empty(writer.Documents);
    }
}
