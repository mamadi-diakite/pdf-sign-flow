using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PdfSignFlow.Api.Auth;

namespace PdfSignFlow.Api.IntegrationTests;

public sealed class JwtSettingsTests
{
    [Fact]
    public void Reads_all_values_from_configuration()
    {
        var settings = JwtSettings.FromConfiguration(
            Configuration(("Issuer", "issuer"), ("Audience", "audience"), ("AccessTokenMinutes", "15"),
                ("SigningKey", PdfSignFlowApiFactory.TestSigningKey)),
            new TestEnvironment(Environments.Production));

        Assert.Equal("issuer", settings.Issuer);
        Assert.Equal("audience", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
        Assert.Equal(Convert.FromBase64String(PdfSignFlowApiFactory.TestSigningKey), settings.SigningKey);
    }

    [Fact]
    public void Uses_defaults_and_a_random_key_in_development()
    {
        var first = JwtSettings.FromConfiguration(Configuration(), new TestEnvironment(Environments.Development));
        var second = JwtSettings.FromConfiguration(Configuration(), new TestEnvironment(Environments.Development));

        Assert.Equal("pdf-sign-flow", first.Issuer);
        Assert.Equal("pdf-sign-flow", first.Audience);
        Assert.Equal(TimeSpan.FromMinutes(60), first.AccessTokenLifetime);
        Assert.Equal(JwtSettings.MinimumKeyBytes, first.SigningKey.Length);
        Assert.NotEqual(first.SigningKey, second.SigningKey);
    }

    [Fact]
    public void Missing_key_outside_development_stops_startup()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => JwtSettings.FromConfiguration(Configuration(), new TestEnvironment(Environments.Production)));

        Assert.Contains("Jwt:SigningKey is missing", error.Message);
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("c2hvcnQ=")]
    public void Rejects_an_invalid_key(string key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(
            Configuration(("SigningKey", key)), new TestEnvironment(Environments.Development)));
    }

    [Fact]
    public void Validation_parameters_match_the_settings()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(), new TestEnvironment(Environments.Development));

        var parameters = settings.ToValidationParameters();

        Assert.Equal(settings.Issuer, parameters.ValidIssuer);
        Assert.Equal(settings.Audience, parameters.ValidAudience);
        Assert.Equal(JwtClaims.Name, parameters.NameClaimType);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>($"Jwt:{v.Key}", v.Value)))
            .Build();

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
