namespace PdfSignFlow.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Creates_user_with_normalized_values()
    {
        var user = new User("  Anna.Smith@Example.com ", " Anna Smith ", "hash", TestDates.Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("anna.smith@example.com", user.Email);
        Assert.Equal("Anna Smith", user.DisplayName);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(TestDates.Now, user.CreatedAtUtc);
    }

    [Fact]
    public void Rejects_invalid_email()
    {
        var error = Assert.Throws<ArgumentException>(() => new User("anna@", "Anna", "hash", TestDates.Now));

        Assert.Equal("email", error.ParamName);
    }

    [Fact]
    public void Rejects_blank_display_name()
    {
        var error = Assert.Throws<ArgumentException>(() => new User("anna@example.com", " ", "hash", TestDates.Now));

        Assert.Equal("displayName", error.ParamName);
    }

    [Fact]
    public void Rejects_blank_password_hash()
    {
        var error = Assert.Throws<ArgumentException>(() => new User("anna@example.com", "Anna", "", TestDates.Now));

        Assert.Equal("passwordHash", error.ParamName);
    }

    [Theory]
    [InlineData("anna@example.com", true)]
    [InlineData("anna@", false)]
    [InlineData(null, false)]
    public void Is_valid_email_matches_the_constructor_rule(string? email, bool expected)
    {
        Assert.Equal(expected, User.IsValidEmail(email));
    }

    [Fact]
    public void Change_password_replaces_the_hash()
    {
        var user = new User("anna@example.com", "Anna", "old-hash", TestDates.Now);

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void Change_password_rejects_a_blank_hash()
    {
        var user = new User("anna@example.com", "Anna", "old-hash", TestDates.Now);

        var error = Assert.Throws<ArgumentException>(() => user.ChangePassword(" "));

        Assert.Equal("passwordHash", error.ParamName);
        Assert.Equal("old-hash", user.PasswordHash);
    }

    [Fact]
    public void Rejects_non_utc_creation_date()
    {
        var local = DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local);

        var error = Assert.Throws<ArgumentException>(() => new User("anna@example.com", "Anna", "hash", local));

        Assert.Equal("createdAtUtc", error.ParamName);
    }
}
