namespace DocuVault.Application.Documents;

public sealed class DocumentUploadOptions
{
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
}
