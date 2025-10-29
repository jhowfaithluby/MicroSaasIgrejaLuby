using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Application.Common.Models;
using MicroSaas.Infrastructure.Persistence;

namespace MicroSaas.Infrastructure.Identity;

public sealed class TokenService : IAuthTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenService(
        AppDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        ILogger<TokenService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _logger = logger;
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<TokenPair> GenerateTokensAsync(Guid userId, string email, IEnumerable<string> roles, CancellationToken cancellationToken)
    {
        var accessToken = CreateAccessToken(userId, email, roles);
        var refreshToken = await CreateRefreshTokenAsync(userId, cancellationToken);

        _logger.LogInformation("Generated access and refresh tokens for user {UserId}", userId);

        return new TokenPair(accessToken.Token, refreshToken.Token, accessToken.ExpiresAtUtc);
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var hashedToken = HashToken(refreshToken);

        var persistedToken = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == hashedToken, cancellationToken);

        if (persistedToken is null || persistedToken.RevokedAtUtc is not null || persistedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(persistedToken.UserId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        persistedToken.RevokedAtUtc = DateTime.UtcNow;

        var newAccessToken = CreateAccessToken(user.Id, user.Email ?? string.Empty, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refreshed tokens for user {UserId}", user.Id);

        return new TokenPair(newAccessToken.Token, newRefreshToken.Token, newAccessToken.ExpiresAtUtc);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hashedToken = HashToken(refreshToken);

        var persistedToken = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == hashedToken, cancellationToken);

        if (persistedToken is null)
        {
            return;
        }

        persistedToken.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked refresh token for user {UserId}", persistedToken.UserId);
    }

    private (string Token, DateTime ExpiresAtUtc) CreateAccessToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: signingCredentials);

        return (_tokenHandler.WriteToken(token), expires);
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)> CreateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashed = HashToken(rawToken);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = hashed,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (rawToken, refreshToken.ExpiresAtUtc);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
