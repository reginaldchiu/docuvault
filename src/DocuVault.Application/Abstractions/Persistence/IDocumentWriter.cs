using DocuVault.Domain.Entities;

namespace DocuVault.Application.Abstractions.Persistence;

public interface IDocumentWriter
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
}
