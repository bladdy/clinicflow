using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class WhatsAppInstanceConfiguration : IEntityTypeConfiguration<WhatsAppInstance>
{
    public void Configure(EntityTypeBuilder<WhatsAppInstance> builder)
    {
        builder.ToTable("WhatsAppInstances");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.InstanceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.ApiUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.ApiKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(w => w.WebhookUrl)
            .HasMaxLength(500);

        builder.HasOne(w => w.Company)
            .WithMany(c => c.WhatsAppInstances)
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Branch)
            .WithMany(b => b.WhatsAppInstances)
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.HasIndex(w => w.CompanyId);
    }
}
