using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class BreakPeriodConfiguration : IEntityTypeConfiguration<BreakPeriod>
{
    public void Configure(EntityTypeBuilder<BreakPeriod> builder)
    {
        builder.ToTable("BreakPeriods");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.BusinessSchedule)
            .WithMany(s => s.Breaks)
            .HasForeignKey(b => b.BusinessScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.BusinessScheduleId, b.SortOrder });

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
