using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Email)
            .HasMaxLength(200);

        builder.Property(p => p.Phone)
            .HasMaxLength(20);

        builder.Property(p => p.Address)
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.Property(p => p.MedicalHistory)
            .HasMaxLength(5000);

        builder.HasOne(p => p.Company)
            .WithMany(c => c.Patients)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Branch)
            .WithMany(b => b.Patients)
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.CompanyId);
    }
}
