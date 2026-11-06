namespace PdfSignFlow.Infrastructure.Persistence.Configurations;

internal sealed class RequestFileConfiguration : IEntityTypeConfiguration<RequestFile>
{
    public void Configure(EntityTypeBuilder<RequestFile> builder)
    {
        builder.ToTable("RequestFiles");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(RequestFile.FileNameMaxLength).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(RequestFile.ContentTypeMaxLength).IsRequired();
        builder.Property(f => f.StorageKey).HasMaxLength(RequestFile.StorageKeyMaxLength).IsRequired();

        builder.HasIndex(f => f.StorageKey).IsUnique();
        builder.HasIndex(f => new { f.RequestId, f.Kind, f.Position }).IsUnique();
    }
}
