using DocuVault.Domain.Entities;
using DocuVault.Domain.Enums;

namespace DocuVault.UnitTests.Domain.Entities;

public class DocumentTests
{
    private const string ValidFileName = "document.pdf";
    private const string ValidContentType = "application/pdf";
    private const long ValidFileSize = 1024;
    private const string ValidHash =
        "a3f5c8d2e1b4a697" +
        "a3f5c8d2e1b4a697" +
        "a3f5c8d2e1b4a697" +
        "a3f5c8d2e1b4a697";

    [Fact]
    public void CreateWithUppercaseHashShouldSucceed()
    {
        string hash = new('A', 64);

        var document = Document.Create(
            ValidFileName,
            ValidContentType,
            ValidFileSize,
            hash
        );

        Assert.Equal(hash, document.Hash);
    }

    [Fact]
    public void CreateWithValidDataShouldCreateDocument()
    {
        var document = Document.Create(
            ValidFileName,
            ValidContentType,
            ValidFileSize,
            ValidHash
        );

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(ValidFileName, document.FileName);
        Assert.Equal(ValidContentType, document.ContentType);
        Assert.Equal(ValidFileSize, document.FileSize);
        Assert.Equal(ValidHash, document.Hash);
        Assert.Equal(DocumentStatus.Active, document.Status);
        Assert.NotEqual(default, document.CreatedAt);
        Assert.Null(document.UpdatedAt);
    }

    [Fact]
    public void CreateShouldGenerateUniqueIds()
    {
        Document first = CreateValidDocument();
        Document second = CreateValidDocument();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateWithInvalidFileNameShouldThrow(string fileName)
    {
        _ = Assert.Throws<ArgumentException>(() =>
            Document.Create(
                fileName,
                ValidContentType,
                ValidFileSize,
                ValidHash
            ));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateWithInvalidContentTypeShouldThrow(string contentType)
    {
        _ = Assert.Throws<ArgumentException>(() =>
            Document.Create(
                ValidFileName,
                contentType,
                ValidFileSize,
                ValidHash
            ));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void CreateWithInvalidFileSizeShouldThrow(long fileSize)
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Document.Create(
                ValidFileName,
                ValidContentType,
                fileSize,
                ValidHash
            ));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("a3f5")]
    public void CreateWithInvalidHashShouldThrow(string hash)
    {
        _ = Assert.Throws<ArgumentException>(() =>
            Document.Create(
                ValidFileName,
                ValidContentType,
                ValidFileSize,
                hash
            ));
    }

    [Fact]
    public void CreateWithNonHexHashShouldThrow()
    {
        string invalidHash = new('z', 64); // 'g' is not a valid hex character

        _ = Assert.Throws<ArgumentException>(() =>
            Document.Create(
                ValidFileName,
                ValidContentType,
                ValidFileSize,
                invalidHash
            ));
    }

    private static Document CreateValidDocument()
    {
        return Document.Create(
            ValidFileName,
            ValidContentType,
            ValidFileSize,
            ValidHash
        );
    }
}
