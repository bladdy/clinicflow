using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.MonthlyPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AnnualPrice).HasColumnType("decimal(18,2)");
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
