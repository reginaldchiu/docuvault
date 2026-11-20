using DocuVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id).ValueGeneratedNever();

        builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();

        builder.Property(document => document.ContentType).HasMaxLength(150).IsRequired();

        builder.Property(document => document.FileSize).IsRequired();

        builder.Property(document => document.Hash).HasMaxLength(64).IsRequired();

        builder.Property(document => document.CreatedAt).IsRequired();

        builder.Property(document => document.UpdatedAt);

        builder.Property(document => document.Status).HasConversion<int>().IsRequired();
    }
}
