using DocuVault.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace DocuVault.UnitTests.Infrastructure.Storage;

public sealed class LocalDocumentStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "DocuVaultTests",
        Guid.NewGuid().ToString("N"));

    private LocalDocumentStorage CreateStorage()
    {
        var options = Options.Create(
            new LocalDocumentStorageOptions
            {
                RootPath = _rootPath
            });

        return new LocalDocumentStorage(options);
    }

    [Fact]
    public async Task SaveAsyncShouldCreateDirectoryAndSaveFile()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        var expectedContent = "DocuVault storage test"u8.ToArray();

        using var content = new MemoryStream(expectedContent);

        await storage.SaveAsync(documentId, content);

        var filePath = GetFilePath(documentId);

        Assert.True(Directory.Exists(_rootPath));
        Assert.True(File.Exists(filePath));

        var actualContent = await File.ReadAllBytesAsync(filePath);

        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task SaveAsyncShouldUseDocumentIdAsFileName()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        using var content = new MemoryStream(
            "test"u8.ToArray());

        await storage.SaveAsync(documentId, content);

        var expectedFileName = documentId.ToString("N");

        var files = Directory.GetFiles(_rootPath);

        var file = Assert.Single(files);

        Assert.Equal(expectedFileName, Path.GetFileName(file));
    }

    [Fact]
    public async Task SaveAsyncShouldNotOverwriteExistingFile()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        var originalContent = "original content"u8.ToArray();
        var newContent = "new content"u8.ToArray();

        using var firstStream = new MemoryStream(originalContent);

        await storage.SaveAsync(documentId, firstStream);

        using var secondStream = new MemoryStream(newContent);

        _ = await Assert.ThrowsAsync<IOException>(
            () => storage.SaveAsync(documentId, secondStream));

        var actualContent = await File.ReadAllBytesAsync(
            GetFilePath(documentId));

        // The original file must remain untouched.
        Assert.Equal(originalContent, actualContent);
    }

    [Fact]
    public async Task SaveAsyncShouldRejectUnreadableStream()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        using var content = new MemoryStream();
        content.Dispose();

        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => storage.SaveAsync(documentId, content));

        Assert.False(File.Exists(GetFilePath(documentId)));
    }

    [Fact]
    public async Task SaveAsyncShouldDeleteIncompleteFileWhenCanceled()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        using var content = new MemoryStream(
            "test content"u8.ToArray());

        using var cts = new CancellationTokenSource();

        cts.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.SaveAsync(
                documentId,
                content,
                cts.Token));

        Assert.False(File.Exists(GetFilePath(documentId)));
    }

    [Fact]
    public async Task SaveAsyncShouldDeleteIncompleteFileWhenCopyFails()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        await using var content = new FailingReadStream();

        _ = await Assert.ThrowsAsync<IOException>(
            () => storage.SaveAsync(documentId, content));

        Assert.False(File.Exists(GetFilePath(documentId)));
    }

    [Fact]
    public async Task DeleteAsyncShouldRemoveExistingFile()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        using var content = new MemoryStream(
            "test content"u8.ToArray());

        await storage.SaveAsync(documentId, content);

        Assert.True(File.Exists(GetFilePath(documentId)));

        await storage.DeleteAsync(documentId);

        Assert.False(File.Exists(GetFilePath(documentId)));
    }

    [Fact]
    public async Task DeleteAsyncShouldSucceedWhenFileDoesNotExist()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        var exception = await Record.ExceptionAsync(
            () => storage.DeleteAsync(documentId));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteAsyncShouldRespectCancellationToken()
    {
        var storage = CreateStorage();

        var documentId = Guid.NewGuid();

        using var content = new MemoryStream(
            "test content"u8.ToArray());

        await storage.SaveAsync(documentId, content);

        using var cts = new CancellationTokenSource();

        cts.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.DeleteAsync(documentId, cts.Token));

        // The file should remain because deletion was canceled.
        Assert.True(File.Exists(GetFilePath(documentId)));
    }

    [Fact]
    public void ConstructorShouldRejectEmptyRootPath()
    {
        var options = Options.Create(
            new LocalDocumentStorageOptions
            {
                RootPath = ""
            });

        _ = Assert.Throws<InvalidOperationException>(
            () => new LocalDocumentStorage(options));
    }

    private string GetFilePath(Guid documentId)
    {
        return Path.Combine(
            _rootPath,
            documentId.ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private sealed class FailingReadStream : Stream
    {
        private bool _hasThrown;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

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
            throw new IOException("Simulated read failure.");
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (!_hasThrown)
            {
                _hasThrown = true;

                return ValueTask.FromException<int>(
                    new IOException("Simulated read failure."));
            }

            return ValueTask.FromResult(0);
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
        }
    }
}
