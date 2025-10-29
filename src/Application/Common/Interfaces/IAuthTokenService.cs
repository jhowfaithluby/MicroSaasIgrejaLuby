using MicroSaas.Application.Common.Models;

namespace MicroSaas.Application.Common.Interfaces;

public interface IAuthTokenService
{
    Task<TokenPair> GenerateTokensAsync(Guid userId, string email, IEnumerable<string> roles, CancellationToken cancellationToken);

    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
