using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class BusinessScheduleConfiguration : IEntityTypeConfiguration<BusinessSchedule>
{
    public void Configure(EntityTypeBuilder<BusinessSchedule> builder)
    {
        builder.ToTable("BusinessSchedules");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Branch)
            .WithOne(br => br.BusinessSchedule)
            .HasForeignKey<BusinessSchedule>(b => b.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.BranchId).IsUnique();

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
