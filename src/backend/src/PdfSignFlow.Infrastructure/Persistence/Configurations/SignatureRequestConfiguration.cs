using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Infrastructure.Persistence.Configurations;

internal sealed class SignatureRequestConfiguration : IEntityTypeConfiguration<SignatureRequest>
{
    public void Configure(EntityTypeBuilder<SignatureRequest> builder)
    {
        builder.ToTable("SignatureRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).HasMaxLength(SignatureRequest.TitleMaxLength).IsRequired();
        builder.Property(r => r.Message).HasMaxLength(SignatureRequest.MessageMaxLength);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.OwnerId, r.Status });

        builder.HasMany(r => r.Files)
            .WithOne()
            .HasForeignKey(f => f.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.Documents);
        builder.Ignore(r => r.Attachments);
    }
}
