using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(h => h.Company)
            .WithMany(c => c.Holidays)
            .HasForeignKey(h => h.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Branch)
            .WithMany(b => b.Holidays)
            .HasForeignKey(h => h.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(h => !h.IsDeleted);

        builder.HasIndex(h => h.CompanyId);
    }
}
