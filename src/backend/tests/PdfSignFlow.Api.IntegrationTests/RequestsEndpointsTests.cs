namespace PdfSignFlow.Api.IntegrationTests;

public sealed class RequestsEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%fake demo content\n%%EOF");

    [Fact]
    public async Task Sender_creates_a_draft_uploads_a_pdf_and_removes_it()
    {
        var client = await LoggedInClientAsync("anna.smith@example.com");

        var created = await client.PostAsJsonAsync("/api/requests", new CreateSignatureRequestRequest("Lease renewal", "Please sign."));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var draft = await created.Content.ReadFromJsonAsync<SignatureRequestDetails>();
        Assert.Equal($"/api/requests/{draft!.Id}", created.Headers.Location?.AbsolutePath);

        var uploaded = await client.PostAsync($"/api/requests/{draft.Id}/documents", PdfForm("lease.pdf", Pdf));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var file = await uploaded.Content.ReadFromJsonAsync<RequestFileResponse>();
        Assert.Equal("lease.pdf", file?.FileName);
        Assert.Single(Directory.GetFiles(factory.StorageRootPath));

        var details = await client.GetFromJsonAsync<SignatureRequestDetails>($"/api/requests/{draft.Id}");
        Assert.Equal(["lease.pdf"], details!.Documents.Select(d => d.FileName));
        var list = await client.GetFromJsonAsync<List<SignatureRequestSummary>>("/api/requests");
        Assert.Contains(list!, r => r.Id == draft.Id && r.DocumentCount == 1);

        var removed = await client.DeleteAsync($"/api/requests/{draft.Id}/files/{file!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(Directory.GetFiles(factory.StorageRootPath));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/requests/{draft.Id}/files/{file.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_draft_returns_a_validation_problem()
    {
        var client = await LoggedInClientAsync("lucas.bernard@example.com");

        var response = await client.PostAsJsonAsync("/api/requests", new CreateSignatureRequestRequest(" ", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["title"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task A_non_pdf_file_is_rejected_even_with_a_pdf_name()
    {
        var client = await LoggedInClientAsync("lucas.bernard@example.com");
        var draft = await CreateDraftAsync(client);

        var response = await client.PostAsync(
            $"/api/requests/{draft.Id}/documents", PdfForm("invoice.pdf", Encoding.ASCII.GetBytes("PK\u0003\u0004 zip content")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["Only PDF files can be signed."], problem!.Errors["file"]);
    }

    [Fact]
    public async Task Requests_of_other_users_are_not_found()
    {
        var owner = await LoggedInClientAsync("emma.dubois@example.com");
        var draft = await CreateDraftAsync(owner);
        var stranger = await LoggedInClientAsync("noah.petit@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/requests/{draft.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync(
            $"/api/requests/{draft.Id}/documents", PdfForm("lease.pdf", Pdf))).StatusCode);
        Assert.DoesNotContain(
            await stranger.GetFromJsonAsync<List<SignatureRequestSummary>>("/api/requests") ?? [], r => r.Id == draft.Id);
    }

    [Fact]
    public async Task Request_endpoints_need_a_token()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/requests", new CreateSignatureRequestRequest("Lease", null))).StatusCode);
    }

    [Fact]
    public async Task A_token_without_a_user_id_gets_nothing()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject());
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/requests", new CreateSignatureRequestRequest("Lease", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/requests/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/requests/{id}/documents", PdfForm("a.pdf", Pdf))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/requests/{id}/files/{id}")).StatusCode);
    }

    private static MultipartFormDataContent PdfForm(string fileName, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static async Task<SignatureRequestDetails> CreateDraftAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/requests", new CreateSignatureRequestRequest("Lease", null));

        return (await response.Content.ReadFromJsonAsync<SignatureRequestDetails>())!;
    }

    private async Task<HttpClient> LoggedInClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TestDemo123!"));
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
