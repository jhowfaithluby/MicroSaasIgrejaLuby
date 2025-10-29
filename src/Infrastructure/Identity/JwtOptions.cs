namespace MicroSaas.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MicroSaasIgreja";

    public string Audience { get; set; } = "MicroSaasIgrejaFront";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
