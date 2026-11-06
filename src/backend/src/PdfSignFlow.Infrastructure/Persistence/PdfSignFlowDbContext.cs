using Microsoft.EntityFrameworkCore;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Infrastructure.Persistence;

public sealed class PdfSignFlowDbContext(DbContextOptions<PdfSignFlowDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();

    public DbSet<RequestFile> RequestFiles => Set<RequestFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdfSignFlowDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
