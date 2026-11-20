using DocuVault.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace DocuVault.Infrastructure.Storage;

public sealed class LocalDocumentStorage(IOptions<LocalDocumentStorageOptions> options) : IDocumentStorage
{
    private const int BufferSize = 81920;

    private readonly string _rootPath = GetRootPath(options);

    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string filePath = GetFilePath(documentId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task SaveAsync(Guid documentId, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException("Content stream must be readable.", nameof(content));
        }

        string filePath = GetFilePath(documentId);

        _ = Directory.CreateDirectory(_rootPath);

        bool fileCreated = false;

        try
        {
            await using var destination = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );

            fileCreated = true;

            await content.CopyToAsync(destination, BufferSize, cancellationToken);
        }
        catch (Exception saveException)
        {
            if (!fileCreated)
            {
                throw;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("File saving failed and the incomplete file could not be removed.", saveException, cleanupException);
            }

            throw;
        }
    }

    private string GetFilePath(Guid documentId)
    {
        return Path.Combine(_rootPath, documentId.ToString("N"));
    }

    private static string GetRootPath(IOptions<LocalDocumentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string rootPath = options.Value.RootPath;

        return string.IsNullOrWhiteSpace(rootPath)
            ? throw new InvalidOperationException("Document storage root path is not configured.")
            : Path.GetFullPath(rootPath);
    }
}
