using AetherVault.Models;

namespace AetherVault.Data;

public interface ITokenRepository
{
    Task<TokenEntity?> GetTokenByUuidAsync(string uuid);
    Task<TokenIdentifierEntity?> GetTokenIdentifierAsync(string uuid);
    Task<IEnumerable<TokenEntity>> GetTokensBySetCodeAsync(string setCode);
}
