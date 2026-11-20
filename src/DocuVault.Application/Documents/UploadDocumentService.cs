using System.Security.Cryptography;
using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Application.Abstractions.Storage;
using DocuVault.Domain.Entities;

namespace DocuVault.Application.Documents;

public sealed class UploadDocumentService(IDocumentWriter writer, IDocumentStorage storage, DocumentUploadOptions options)
{
    public async Task<Document> UploadAsync(
        string fileName,
        string contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize);

        if (fileSize > options.MaxFileSize)
        {
            throw new ArgumentException("File size exceeds the maximum allowed size.", nameof(fileSize));
        }

        if (!content.CanRead || !content.CanSeek)
        {
            throw new ArgumentException("Content stream must be readable and seekable.", nameof(content));
        }

        cancellationToken.ThrowIfCancellationRequested();

        content.Position = 0;

        var hashBytes = await SHA256.HashDataAsync(content, cancellationToken);

        var hash = Convert.ToHexString(hashBytes);

        content.Position = 0;

        var document = Document.Create(fileName, contentType, fileSize, hash);

        await storage.SaveAsync(document.Id, content, cancellationToken);

        try
        {
            await writer.AddAsync(document, cancellationToken);
        }
        catch (Exception persistenceException)
        {
            try
            {
                await storage.DeleteAsync(document.Id, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Document persistence failed and the stored file could not be removed", persistenceException, cleanupException);
            }
            throw;
        }

        return document;
    }
}