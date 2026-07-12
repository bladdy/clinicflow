using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Specialty)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(d => d.Bio)
            .HasMaxLength(2000);

        builder.Property(d => d.PhotoUrl)
            .HasMaxLength(1000);

        builder.Property(d => d.Color)
            .HasMaxLength(20);

        builder.HasOne(d => d.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Company)
            .WithMany(c => c.Doctors)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasIndex(d => d.UserId)
            .IsUnique();

        builder.HasIndex(d => d.LicenseNumber)
            .IsUnique()
            .HasFilter("[LicenseNumber] IS NOT NULL");

        builder.HasIndex(d => d.CompanyId);
    }
}
