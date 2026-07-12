using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class BusinessHourConfiguration : IEntityTypeConfiguration<BusinessHour>
{
    public void Configure(EntityTypeBuilder<BusinessHour> builder)
    {
        builder.ToTable("BusinessHours");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Branch)
            .WithMany(br => br.BusinessHours)
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Doctor)
            .WithMany(d => d.BusinessHours)
            .HasForeignKey(b => b.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => b.BranchId);
        builder.HasIndex(b => new { b.BranchId, b.DayOfWeek });
    }
}
