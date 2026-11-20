using DocuVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuVault.Infrastructure.Persistence;

public class DocuVaultDbContext(DbContextOptions<DocuVaultDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuVaultDbContext).Assembly);
    }
}
