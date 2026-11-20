using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Domain.Entities;

namespace DocuVault.Application.Documents;

public sealed class CreateDocumentService(IDocumentWriter documentWriter)
{
    public async Task<Document> CreateAsync(
        CreateDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var document = Document.Create(
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Hash
        );

        await documentWriter.AddAsync(document, cancellationToken);

        return document;
    }

}