using System.Diagnostics.CodeAnalysis;

namespace PdfSignFlow.Domain;

public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;
    public const int PasswordHashMaxLength = 500;

    private User()
    {
    }

    public User(string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Email = Guard.Email(email, nameof(email), EmailMaxLength);
        DisplayName = Guard.Required(displayName, nameof(displayName), DisplayNameMaxLength);
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), PasswordHashMaxLength);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static bool IsValidEmail([NotNullWhen(true)] string? email) => Guard.IsEmail(email, EmailMaxLength);

    public void ChangePassword(string passwordHash) =>
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), PasswordHashMaxLength);
}
