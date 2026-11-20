using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Domain.Entities;

namespace DocuVault.Infrastructure.Persistence;

public sealed class EfDocumentWriter(DocuVaultDbContext context) : IDocumentWriter
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _ = context.Documents.Add(document);

        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
