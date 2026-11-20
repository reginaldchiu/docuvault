namespace DocuVault.Application.Abstractions.Storage;

public interface IDocumentStorage
{
    Task SaveAsync(
        Guid documentId,
        Stream content,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default
    );
}
