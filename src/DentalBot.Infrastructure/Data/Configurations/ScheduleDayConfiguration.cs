using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class ScheduleDayConfiguration : IEntityTypeConfiguration<ScheduleDay>
{
    public void Configure(EntityTypeBuilder<ScheduleDay> builder)
    {
        builder.ToTable("ScheduleDays");

        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.BusinessSchedule)
            .WithMany(s => s.Days)
            .HasForeignKey(d => d.BusinessScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.BusinessScheduleId, d.DayOfWeek }).IsUnique();

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
