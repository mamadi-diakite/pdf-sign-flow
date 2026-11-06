using PdfSignFlow.Application.Auth;
using PdfSignFlow.Application.Contracts.Auth;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Application.Tests;

public sealed class AuthServiceTests
{
    private const string Password = "TestDemo123!";
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;
    private readonly User _anna;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _hasher, new FakeTokenIssuer(), new FixedTimeProvider(Now));
        _anna = new User("anna.smith@example.com", "Anna Smith", "hashed:" + Password, Now.UtcDateTime);
        _users.Users.Add(_anna);
    }

    [Fact]
    public async Task Register_creates_the_account_and_opens_a_session()
    {
        var result = await _service.RegisterAsync(
            new RegisterRequest(" Lucas Bernard ", " Lucas.Bernard@Example.com ", "NewPass456", "NewPass456"));

        Assert.Empty(result.Errors);
        var user = Assert.Single(_users.Users, u => u.Email == "lucas.bernard@example.com");
        Assert.Equal("Lucas Bernard", user.DisplayName);
        Assert.Equal("hashed:NewPass456", user.PasswordHash);
        Assert.Equal(Now.UtcDateTime, user.CreatedAtUtc);
        Assert.Equal(1, _users.SaveCount);
        Assert.Equal($"token-for-{user.Id}", result.Response?.AccessToken);
        Assert.Equal(new CurrentUserResponse(user.Id, user.Email, "Lucas Bernard"), result.Response?.User);
    }

    [Theory]
    [InlineData("", "displayName")]
    [InlineData("  ", "displayName")]
    public async Task Register_requires_a_name(string displayName, string field)
    {
        var result = await _service.RegisterAsync(new RegisterRequest(displayName, "new@example.com", "NewPass456", "NewPass456"));

        AssertRegisterInvalid(result, field);
        Assert.Equal(["Name is required."], result.Errors[field]);
    }

    [Fact]
    public async Task Register_limits_the_name_length()
    {
        var result = await _service.RegisterAsync(new RegisterRequest(
            new string('a', User.DisplayNameMaxLength + 1), "new@example.com", "NewPass456", "NewPass456"));

        AssertRegisterInvalid(result, "displayName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Register_requires_a_valid_email(string email)
    {
        var result = await _service.RegisterAsync(new RegisterRequest("New", email, "NewPass456", "NewPass456"));

        AssertRegisterInvalid(result, "email");
        Assert.Equal(["Enter a valid email address."], result.Errors["email"]);
    }

    [Fact]
    public async Task Register_refuses_an_email_already_used()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Anna", "ANNA.SMITH@example.com", "NewPass456", "NewPass456"));

        AssertRegisterInvalid(result, "email");
        Assert.Equal(["An account already uses this email."], result.Errors["email"]);
    }

    [Fact]
    public async Task Register_applies_the_password_policy_and_confirmation()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("New", "new@example.com", "short", "other"));

        AssertRegisterInvalid(result, "password");
        Assert.Equal(["confirmPassword", "password"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Register_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RegisterAsync(null!));
    }

    [Fact]
    public async Task Login_returns_a_token_and_the_user()
    {
        var response = await _service.LoginAsync(new LoginRequest(" Anna.Smith@Example.com ", Password));

        Assert.NotNull(response);
        Assert.Equal($"token-for-{_anna.Id}", response.AccessToken);
        Assert.Equal(FakeTokenIssuer.ExpiresAt, response.ExpiresAtUtc);
        Assert.Equal(new CurrentUserResponse(_anna.Id, "anna.smith@example.com", "Anna Smith"), response.User);
    }

    [Fact]
    public async Task Login_fails_with_a_wrong_password()
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest("anna.smith@example.com", "wrong-password1")));
    }

    [Fact]
    public async Task Login_fails_with_an_unknown_email_and_still_hashes()
    {
        var response = await _service.LoginAsync(new LoginRequest("nobody@example.com", Password));

        Assert.Null(response);
        Assert.Equal(1, _hasher.HashCount);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("  ", Password)]
    [InlineData("anna.smith@example.com", "")]
    public async Task Login_fails_with_blank_values(string email, string password)
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest(email, password)));
        Assert.Equal(0, _hasher.HashCount);
    }

    [Fact]
    public async Task Login_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.LoginAsync(null!));
    }

    [Fact]
    public async Task Current_user_is_found_by_id()
    {
        var user = await _service.GetCurrentUserAsync(_anna.Id);

        Assert.Equal("anna.smith@example.com", user?.Email);
        Assert.Null(await _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Change_password_stores_the_new_hash()
    {
        var result = await _service.ChangePasswordAsync(_anna.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.Changed, result.Status);
        Assert.Empty(result.Errors);
        Assert.Equal("hashed:NewPass456", _anna.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Change_password_for_an_unknown_user()
    {
        var result = await _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.UserNotFound, result.Status);
        Assert.Equal(0, _users.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("WrongPass789")]
    public async Task Change_password_checks_the_current_password(string currentPassword)
    {
        var result = await _service.ChangePasswordAsync(
            _anna.Id, new ChangePasswordRequest(currentPassword, "NewPass456", "NewPass456"));

        AssertChangeInvalid(result, "currentPassword");
    }

    [Fact]
    public async Task Change_password_applies_the_password_policy()
    {
        var result = await _service.ChangePasswordAsync(_anna.Id, new ChangePasswordRequest(Password, "short", "short"));

        AssertChangeInvalid(result, "newPassword");
        Assert.Contains("Use at least 8 characters.", result.Errors["newPassword"]);
    }

    [Fact]
    public async Task New_password_must_differ_from_the_current_one()
    {
        var result = await _service.ChangePasswordAsync(_anna.Id, new ChangePasswordRequest(Password, Password, Password));

        AssertChangeInvalid(result, "newPassword");
        Assert.Equal(["New password must be different from the current one."], result.Errors["newPassword"]);
    }

    [Fact]
    public async Task Confirmation_must_match()
    {
        var result = await _service.ChangePasswordAsync(_anna.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass457"));

        AssertChangeInvalid(result, "confirmPassword");
    }

    [Fact]
    public async Task All_errors_are_reported_together()
    {
        var result = await _service.ChangePasswordAsync(_anna.Id, new ChangePasswordRequest("bad", "short", "other"));

        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Change_password_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ChangePasswordAsync(_anna.Id, null!));
    }

    private void AssertRegisterInvalid(RegisterResult result, string field)
    {
        Assert.Null(result.Response);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Single(_users.Users);
        Assert.Equal(0, _users.SaveCount);
    }

    private void AssertChangeInvalid(ChangePasswordResult result, string field)
    {
        Assert.Equal(ChangePasswordStatus.Invalid, result.Status);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Equal("hashed:" + Password, _anna.PasswordHash);
        Assert.Equal(0, _users.SaveCount);
    }
}
