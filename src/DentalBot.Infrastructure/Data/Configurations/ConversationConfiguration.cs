using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalBot.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(c => c.Company)
            .WithMany(co => co.Conversations)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany(p => p.Conversations)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Branch)
            .WithMany(b => b.Conversations)
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.WhatsAppInstance)
            .WithMany(w => w.Conversations)
            .HasForeignKey(c => c.WhatsAppInstanceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.CompanyId);
        builder.HasIndex(c => c.Phone);
    }
}
