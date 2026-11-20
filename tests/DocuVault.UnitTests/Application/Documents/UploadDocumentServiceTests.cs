
using System.Security.Cryptography;
using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Application.Abstractions.Storage;
using DocuVault.Application.Documents;
using DocuVault.Domain.Entities;

namespace DocuVault.UnitTests.Application.Documents;

public sealed class UploadDocumentServiceTests
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private sealed class FakeDocumentWriter : IDocumentWriter
    {
        public List<Document> Documents { get; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Document document,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Documents.Add(document);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        public List<Guid> SavedDocumentIds { get; } = [];

        public List<Guid> DeletedDocumentIds { get; } = [];

        public byte[]? SavedContent { get; private set; }

        public Exception? SaveException { get; set; }

        public Exception? DeleteException { get; set; }

        public CancellationToken SaveCancellationToken { get; private set; }

        public CancellationToken DeleteCancellationToken { get; private set; }

        public async Task SaveAsync(
            Guid documentId,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            SaveCancellationToken = cancellationToken;

            if (SaveException is not null)
            {
                throw SaveException;
            }

            using var memoryStream = new MemoryStream();

            await content.CopyToAsync(
                memoryStream,
                cancellationToken);

            SavedContent = memoryStream.ToArray();
            SavedDocumentIds.Add(documentId);
        }

        public Task DeleteAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            DeleteCancellationToken = cancellationToken;

            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            DeletedDocumentIds.Add(documentId);

            return Task.CompletedTask;
        }
    }

    private readonly FakeDocumentWriter _writer = new();

    private readonly FakeDocumentStorage _storage = new();

    private UploadDocumentService CreateService()
    {
        return new UploadDocumentService(
            _writer,
            _storage,
            new DocumentUploadOptions
            {
                MaxFileSize = MaxFileSize
            });
    }

    private static MemoryStream CreateContentStream()
    {
        return new MemoryStream(
            "DocuVault test document content"u8.ToArray());
    }

    [Fact]
    public async Task UploadAsyncShouldCreateAndPersistDocument()
    {
        using var content = CreateContentStream();

        var service = CreateService();

        var result = await service.UploadAsync(
            "contract.pdf",
            "application/pdf",
            content.Length,
            content);

        Assert.NotNull(result);

        Assert.Equal("contract.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(content.Length, result.FileSize);

        _ = Assert.Single(_writer.Documents);
        Assert.Same(result, _writer.Documents[0]);

        _ = Assert.Single(_storage.SavedDocumentIds);
        Assert.Equal(result.Id, _storage.SavedDocumentIds[0]);

        Assert.Empty(_storage.DeletedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldCalculateCorrectSha256Hash()
    {
        using var content = CreateContentStream();

        var expectedHash = Convert.ToHexString(
            SHA256.HashData(content.ToArray()));

        var service = CreateService();

        var result = await service.UploadAsync(
            "contract.pdf",
            "application/pdf",
            content.Length,
            content);

        Assert.Equal(expectedHash, result.Hash);
    }

    [Fact]
    public async Task UploadAsyncShouldSaveOriginalStreamContent()
    {
        using var content = CreateContentStream();

        var expectedContent = content.ToArray();

        var service = CreateService();

        _ = await service.UploadAsync(
            "contract.pdf",
            "application/pdf",
            content.Length,
            content);

        Assert.NotNull(_storage.SavedContent);
        Assert.Equal(expectedContent, _storage.SavedContent);
    }

    [Fact]
    public async Task UploadAsyncShouldGenerateUniqueDocumentIds()
    {
        using var firstContent = CreateContentStream();
        using var secondContent = CreateContentStream();

        var service = CreateService();

        var first = await service.UploadAsync(
            "first.pdf",
            "application/pdf",
            firstContent.Length,
            firstContent);

        var second = await service.UploadAsync(
            "second.pdf",
            "application/pdf",
            secondContent.Length,
            secondContent);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _writer.Documents.Count);
        Assert.Equal(2, _storage.SavedDocumentIds.Count);
    }

    [Fact]
    public async Task UploadAsyncShouldRejectEmptyFileName()
    {
        using var content = CreateContentStream();

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "",
                "application/pdf",
                content.Length,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldRejectEmptyContentType()
    {
        using var content = CreateContentStream();

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "contract.pdf",
                "",
                content.Length,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UploadAsyncShouldRejectInvalidFileSize(long fileSize)
    {
        using var content = CreateContentStream();

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                fileSize,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldRejectFileLargerThanMaximumSize()
    {
        using var content = CreateContentStream();

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "large.pdf",
                "application/pdf",
                MaxFileSize + 1,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldRejectUnreadableStream()
    {
        using var content = CreateContentStream();
        content.Dispose();

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                100,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldRejectNonSeekableStream()
    {
        using var innerStream = CreateContentStream();

        await using var content = new NonSeekableStream(innerStream);

        var service = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                innerStream.Length,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.SavedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldNotPersistWhenStorageFails()
    {
        using var content = CreateContentStream();

        _storage.SaveException = new IOException("Storage failed.");

        var service = CreateService();

        _ = await Assert.ThrowsAsync<IOException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                content.Length,
                content));

        Assert.Empty(_writer.Documents);
        Assert.Empty(_storage.DeletedDocumentIds);
    }

    [Fact]
    public async Task UploadAsyncShouldDeleteFileWhenPersistenceFails()
    {
        using var content = CreateContentStream();

        _writer.ExceptionToThrow = new InvalidOperationException(
            "Database failed.");

        var service = CreateService();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                content.Length,
                content));

        _ = Assert.Single(_storage.SavedDocumentIds);
        _ = Assert.Single(_storage.DeletedDocumentIds);

        Assert.Equal(
            _storage.SavedDocumentIds[0],
            _storage.DeletedDocumentIds[0]);
    }

    [Fact]
    public async Task UploadAsyncShouldThrowAggregateExceptionWhenCleanupFails()
    {
        using var content = CreateContentStream();

        _writer.ExceptionToThrow = new InvalidOperationException(
            "Database failed.");

        _storage.DeleteException = new IOException(
            "File cleanup failed.");

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                content.Length,
                content));

        Assert.Equal(2, exception.InnerExceptions.Count);

        _ = Assert.IsType<InvalidOperationException>(
            exception.InnerExceptions[0]);

        _ = Assert.IsType<IOException>(
            exception.InnerExceptions[1]);
    }

    [Fact]
    public async Task UploadAsyncShouldForwardCancellationToken()
    {
        using var content = CreateContentStream();
        using var cts = new CancellationTokenSource();

        var service = CreateService();

        _ = await service.UploadAsync(
            "contract.pdf",
            "application/pdf",
            content.Length,
            content,
            cts.Token);

        Assert.Equal(
            cts.Token,
            _storage.SaveCancellationToken);

        Assert.Equal(
            cts.Token,
            _writer.ReceivedCancellationToken);
    }

    [Fact]
    public async Task UploadAsyncShouldUseNonCancelableTokenForCleanup()
    {
        using var content = CreateContentStream();
        using var cts = new CancellationTokenSource();

        _writer.ExceptionToThrow = new InvalidOperationException(
            "Database failed.");

        var service = CreateService();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(
                "contract.pdf",
                "application/pdf",
                content.Length,
                content,
                cts.Token));

        Assert.Equal(
            CancellationToken.None,
            _storage.DeleteCancellationToken);
    }

    private sealed class NonSeekableStream(Stream innerStream) : Stream
    {
        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => innerStream.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
