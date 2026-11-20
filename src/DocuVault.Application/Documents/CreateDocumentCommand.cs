namespace DocuVault.Application.Documents;

public sealed record CreateDocumentCommand(
    string FileName,
    string ContentType,
    long FileSize,
    string Hash
);
