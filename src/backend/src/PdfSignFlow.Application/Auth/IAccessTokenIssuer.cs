using PdfSignFlow.Domain;

namespace PdfSignFlow.Application.Auth;

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
