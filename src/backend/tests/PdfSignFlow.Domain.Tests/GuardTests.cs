namespace PdfSignFlow.Domain.Tests;

public sealed class GuardTests
{
    [Fact]
    public void Required_trims_the_value()
    {
        Assert.Equal("value", Guard.Required("  value ", "name", 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_rejects_blank_values(string? value)
    {
        var error = Assert.Throws<ArgumentException>(() => Guard.Required(value, "name", 10));

        Assert.Equal("name", error.ParamName);
    }

    [Fact]
    public void Required_rejects_values_over_the_limit()
    {
        Assert.Throws<ArgumentException>(() => Guard.Required("too long", "name", 3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Optional_turns_blank_values_into_null(string? value)
    {
        Assert.Null(Guard.Optional(value, "name", 10));
    }

    [Fact]
    public void Optional_trims_and_checks_the_limit()
    {
        Assert.Equal("ok", Guard.Optional(" ok ", "name", 10));
        Assert.Throws<ArgumentException>(() => Guard.Optional("too long", "name", 3));
    }

    [Theory]
    [InlineData(" Anna.Smith@Example.com ", 100, true)]
    [InlineData(null, 100, false)]
    [InlineData("  ", 100, false)]
    [InlineData("anna@example.com", 10, false)]
    [InlineData("anna@example", 100, false)]
    public void Is_email_checks_without_throwing(string? value, int maxLength, bool expected)
    {
        Assert.Equal(expected, Guard.IsEmail(value, maxLength));
    }

    [Fact]
    public void Email_is_trimmed_and_lowercased()
    {
        Assert.Equal("anna.smith@example.com", Guard.Email("  Anna.Smith@Example.com ", "email", 100));
    }

    [Theory]
    [InlineData("no-at-sign.example.com")]
    [InlineData("@example.com")]
    [InlineData("anna@")]
    [InlineData("anna@example")]
    [InlineData("anna@.example.com")]
    [InlineData("anna@example.com.")]
    [InlineData("anna@@example.com")]
    [InlineData("an na@example.com")]
    public void Email_rejects_invalid_addresses(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => Guard.Email(value, "email", 100));

        Assert.Equal("email", error.ParamName);
    }

    [Fact]
    public void NotEmpty_rejects_an_empty_id()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, Guard.NotEmpty(id, "id"));
        Assert.Throws<ArgumentException>(() => Guard.NotEmpty(Guid.Empty, "id"));
    }

    [Fact]
    public void NotNegative_accepts_zero_and_rejects_negative_values()
    {
        Assert.Equal(0, Guard.NotNegative(0, "value"));
        Assert.Equal(0m, Guard.NotNegative(0m, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(-1, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(-0.01m, "value"));
    }

    [Fact]
    public void Positive_rejects_zero_and_negative_values()
    {
        Assert.Equal(1, Guard.Positive(1, "value"));
        Assert.Equal(1L, Guard.Positive(1L, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(0, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(0L, "value"));
    }

    [Fact]
    public void Utc_rejects_local_and_unspecified_dates()
    {
        Assert.Equal(TestDates.Now, Guard.Utc(TestDates.Now, "date"));
        Assert.Throws<ArgumentException>(() => Guard.Utc(DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local), "date"));
        Assert.Throws<ArgumentException>(() => Guard.Utc(DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Unspecified), "date"));
    }

    [Fact]
    public void Defined_rejects_unknown_enum_values()
    {
        Assert.Equal(DayOfWeek.Monday, Guard.Defined(DayOfWeek.Monday, "day"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Defined((DayOfWeek)42, "day"));
    }
}
