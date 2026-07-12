using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(k => k.Content)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(k => k.Category)
            .HasMaxLength(100);

        builder.Property(k => k.Keywords)
            .HasMaxLength(1000);

        builder.HasOne(k => k.Company)
            .WithMany(c => c.KnowledgeArticles)
            .HasForeignKey(k => k.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(k => !k.IsDeleted);

        builder.HasIndex(k => k.CompanyId);
    }
}
