using DocuVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        _ = builder.ToTable("Documents");

        _ = builder.HasKey(document => document.Id);

        _ = builder.Property(document => document.Id).ValueGeneratedNever();

        _ = builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();

        _ = builder.Property(document => document.ContentType).HasMaxLength(150).IsRequired();

        _ = builder.Property(document => document.FileSize).IsRequired();

        _ = builder.Property(document => document.Hash).HasMaxLength(64).IsRequired();

        _ = builder.Property(document => document.CreatedAt).IsRequired();

        _ = builder.Property(document => document.UpdatedAt);

        _ = builder.Property(document => document.Status).HasConversion<int>().IsRequired();
    }
}
