using DocAttestation.Models;
using System.Security.Claims;

namespace DocAttestation.Services;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<RefreshToken> SaveRefreshTokenAsync(string userId, string token, DateTime expiresAt);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress = null);
    Task RevokeAllRefreshTokensForUserAsync(string userId);
}

