namespace PdfSignFlow.Api.IntegrationTests;

public sealed class AuthEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoPassword = "TestDemo123!";

    [Fact]
    public async Task Register_creates_an_account_and_returns_a_session()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Jade Roux", "jade.roux@example.com", "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("jade.roux@example.com", session?.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("Jade Roux", me?.DisplayName);
    }

    [Fact]
    public async Task Register_with_errors_returns_a_validation_problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(" ", "anna.smith@example.com", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "displayName", "email", "password"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Login_with_a_demo_account_returns_a_token()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("Anna.Smith@Example.com", DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));
        Assert.Equal("anna.smith@example.com", body!.User.Email);
    }

    [Theory]
    [InlineData("anna.smith@example.com", "WrongPass123")]
    [InlineData("nobody@example.com", DemoPassword)]
    [InlineData("", "")]
    public async Task Login_with_bad_credentials_returns_401(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email or password is not correct.", problem?.Title);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user()
    {
        var client = await LoggedInClientAsync("lucas.bernard@example.com", DemoPassword);

        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.Equal("lucas.bernard@example.com", user?.Email);
    }

    [Fact]
    public async Task Protected_endpoints_need_a_token()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task A_token_without_a_user_id_is_refused()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject());

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_refused()
    {
        const string email = "temporary@example.com";
        var client = factory.CreateClient();
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Temporary", email, DemoPassword, DemoPassword));
        var session = await registered.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PdfSignFlowDbContext>();
            db.Users.Remove(db.Users.Single(u => u.Email == email));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task Change_password_with_errors_returns_a_validation_problem()
    {
        var client = await LoggedInClientAsync("emma.dubois@example.com", DemoPassword);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest("WrongPass789", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Changed_password_replaces_the_old_one()
    {
        var client = await LoggedInClientAsync("chloe.moreau@example.com", DemoPassword);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("chloe.moreau@example.com", DemoPassword))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("chloe.moreau@example.com", "NewPass456"))).StatusCode);
    }

    private async Task<HttpClient> LoggedInClientAsync(string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private string TokenWithoutSubject()
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }
}
