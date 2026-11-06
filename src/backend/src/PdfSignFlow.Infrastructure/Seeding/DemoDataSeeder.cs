using Microsoft.EntityFrameworkCore;
using PdfSignFlow.Application.Security;
using PdfSignFlow.Domain;
using PdfSignFlow.Infrastructure.Persistence;

namespace PdfSignFlow.Infrastructure.Seeding;

public sealed class DemoDataSeeder(PdfSignFlowDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "TestDemo123!";

    /// <summary>Fictional accounts: two senders who prepare requests, three signers who receive them.</summary>
    public static IReadOnlyList<(string Email, string DisplayName)> Accounts { get; } =
    [
        ("anna.smith@example.com", "Anna Smith"),
        ("lucas.bernard@example.com", "Lucas Bernard"),
        ("emma.dubois@example.com", "Emma Dubois"),
        ("noah.petit@example.com", "Noah Petit"),
        ("chloe.moreau@example.com", "Chloe Moreau"),
    ];

    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.Users.AddRange(Accounts.Select(
            account => new User(account.Email, account.DisplayName, passwordHasher.Hash(DemoPassword), now)));

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
