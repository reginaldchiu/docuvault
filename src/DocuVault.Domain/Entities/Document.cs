using DocuVault.Domain.Enums;

namespace DocuVault.Domain.Entities;

public class Document
{
    private Document(string fileName, string contentType, string hash)
    {
        FileName = fileName;
        ContentType = contentType;
        Hash = hash;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long FileSize { get; private set; }

    public string Hash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DocumentStatus Status { get; private set; } = DocumentStatus.Active;

    private static bool IsValidSha256Hash(string hash)
    {
        if (hash is null || hash.Length != 64)
        {
            return false;
        }

        try
        {
            return Convert.FromHexString(hash).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static Document Create(string fileName, string contentType, long fileSize, string hash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName cannot empty.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType cannot empty.", nameof(contentType));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize);

        return !IsValidSha256Hash(hash)
            ? throw new ArgumentException("Hash must be a valid SHA-256 hexadecimal string.", nameof(hash))
            : new Document(fileName, contentType, hash)
            {
                FileSize = fileSize,
                CreatedAt = DateTimeOffset.UtcNow,
            };
    }
}
