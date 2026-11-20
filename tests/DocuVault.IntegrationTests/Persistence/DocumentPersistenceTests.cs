using DocuVault.Domain.Entities;
using DocuVault.Infrastructure.Persistence;
using DocuVault.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace DocuVault.IntegrationTests.Persistence;

public sealed class DocumentPersistenceTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private DbContextOptions<DocuVaultDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<DocuVaultDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
    }

    private static async Task ResetDatabaseAsync(DbContextOptions<DocuVaultDbContext> options)
    {
        await using var context = new DocuVaultDbContext(options);

        _ = await context.Database.EnsureDeletedAsync();
        _ = await context.Database.EnsureCreatedAsync();
    }

    private static DateTimeOffset NormalizeToPostgreSqlPrecision(DateTimeOffset value)
    {
        long utcTicks = value.UtcDateTime.Ticks;
        long normalizedTicks = utcTicks - (utcTicks % 10);

        return new DateTimeOffset(normalizedTicks, TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveAsyncShouldPersistAndRetrieveDocument()
    {
        DbContextOptions<DocuVaultDbContext> options = CreateOptions();
        await ResetDatabaseAsync(options);

        var document = Document.Create(
            "contract.pdf",
            "application/pdf",
            1024,
            new string('a', 64)
        );

        await using (var context = new DocuVaultDbContext(options))
        {

            _ = await context.Database.EnsureCreatedAsync();

            _ = context.Documents.Add(document);

            _ = await context.SaveChangesAsync();
        }

        await using (var context = new DocuVaultDbContext(options))
        {
            Document? savedDocument = await context.Documents
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == document.Id);

            Assert.NotNull(savedDocument);

            Assert.Equal(document.Id, savedDocument.Id);
            Assert.Equal("contract.pdf", savedDocument.FileName);
            Assert.Equal("application/pdf", savedDocument.ContentType);
            Assert.Equal(1024, savedDocument.FileSize);
            Assert.Equal(new string('a', 64), savedDocument.Hash);
            Assert.Equal(
                NormalizeToPostgreSqlPrecision(document.CreatedAt),
                savedDocument.CreatedAt);
            Assert.Equal(document.UpdatedAt, savedDocument.UpdatedAt);
            Assert.Equal(document.Status, savedDocument.Status);
        }
    }

    [Fact]
    public async Task SaveAsyncShouldPersistMultipleDocuments()
    {
        DbContextOptions<DocuVaultDbContext> options = CreateOptions();
        await ResetDatabaseAsync(options);

        Document[] documents =
        [
            Document.Create(
                "contract.pdf",
                "application/pdf",
                1024,
                new string('a', 64)
            ),
            Document.Create(
                "report.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                2048,
                new string('b', 64)
            )
        ];

        await using (var context = new DocuVaultDbContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();

            context.Documents.AddRange(documents);

            _ = await context.SaveChangesAsync();
        }

        await using (var context = new DocuVaultDbContext(options))
        {
            List<Document> savedDocuments = await context.Documents
                .AsNoTracking()
                .OrderBy(x => x.FileName)
                .ToListAsync();

            Assert.Equal(2, savedDocuments.Count);

            Assert.Contains(
                savedDocuments,
                x => x.FileName == "contract.pdf"
            );

            Assert.Contains(
                savedDocuments,
                x => x.FileName == "report.docx"
            );
        }
    }

    [Fact]
    public async Task SaveAsyncShouldAllowDocumentsWithSameHash()
    {
        DbContextOptions<DocuVaultDbContext> options = CreateOptions();
        await ResetDatabaseAsync(options);

        const string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        var firstDocument = Document.Create(
            "first.pdf",
            "application/pdf",
            1024,
            hash);

        var secondDocument = Document.Create(
            "second.pdf",
            "application/pdf",
            2048,
            hash);

        // Act
        await using (var context = new DocuVaultDbContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();

            await context.Documents.AddRangeAsync(
                firstDocument,
                secondDocument);

            _ = await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new DocuVaultDbContext(options))
        {
            int count = await context.Documents
                .AsNoTracking()
                .CountAsync(x => x.Hash == hash);

            Assert.Equal(2, count);
        }
    }
}
