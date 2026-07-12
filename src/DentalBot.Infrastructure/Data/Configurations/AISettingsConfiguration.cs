using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class AISettingsConfiguration : IEntityTypeConfiguration<AISettings>
{
    public void Configure(EntityTypeBuilder<AISettings> builder)
    {
        builder.ToTable("AISettings");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OllamaUrl)
            .HasMaxLength(500);

        builder.Property(a => a.ModelName)
            .HasMaxLength(100);

        builder.Property(a => a.SystemPrompt)
            .HasMaxLength(10000);

        builder.Property(a => a.WelcomeMessage)
            .HasMaxLength(1000);

        builder.Property(a => a.TransferMessage)
            .HasMaxLength(1000);

        builder.Property(a => a.Temperature)
            .HasColumnType("decimal(3,2)");

        builder.HasOne(a => a.Company)
            .WithOne(c => c.AISettings)
            .HasForeignKey<AISettings>(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasIndex(a => a.CompanyId)
            .IsUnique();
    }
}
