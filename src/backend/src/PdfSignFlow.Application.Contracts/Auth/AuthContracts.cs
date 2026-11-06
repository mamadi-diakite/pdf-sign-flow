namespace PdfSignFlow.Application.Contracts.Auth;

public sealed record RegisterRequest(string DisplayName, string Email, string Password, string ConfirmPassword);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserResponse User);

public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
