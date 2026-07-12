using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Amount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Tax).HasColumnType("decimal(18,2)");
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasOne(i => i.Company).WithMany().HasForeignKey(i => i.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Subscription).WithMany().HasForeignKey(i => i.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
