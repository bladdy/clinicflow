using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class LunchConfigConfiguration : IEntityTypeConfiguration<LunchConfig>
{
    public void Configure(EntityTypeBuilder<LunchConfig> builder)
    {
        builder.ToTable("LunchConfigs");

        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.BusinessSchedule)
            .WithMany()
            .HasForeignKey(l => l.BusinessScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.BusinessScheduleId).IsUnique();

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
