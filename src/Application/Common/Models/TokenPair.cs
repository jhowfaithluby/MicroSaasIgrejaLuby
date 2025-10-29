namespace MicroSaas.Application.Common.Models;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
