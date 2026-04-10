using AetherVault.Models;
using Dapper;

namespace AetherVault.Data;

public class TokenRepository : ITokenRepository
{
    private readonly DatabaseManager _db;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public TokenRepository(DatabaseManager databaseManager)
    {
        _db = databaseManager;
    }

    public async Task<TokenEntity?> GetTokenByUuidAsync(string uuid)
    {
        await _lock.WaitAsync();
        try
        {
            return await _db.MtgConnection.QueryFirstOrDefaultAsync<TokenEntity>(
                SqlQueries.SelectTokenByUuid, new { uuid });
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TokenIdentifierEntity?> GetTokenIdentifierAsync(string uuid)
    {
        await _lock.WaitAsync();
        try
        {
            return await _db.MtgConnection.QueryFirstOrDefaultAsync<TokenIdentifierEntity>(
                SqlQueries.SelectTokenIdentifierByUuid, new { uuid });
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<TokenEntity>> GetTokensBySetCodeAsync(string setCode)
    {
        await _lock.WaitAsync();
        try
        {
            return await _db.MtgConnection.QueryAsync<TokenEntity>(
                SqlQueries.SelectTokensBySetCode, new { setCode });
        }
        finally
        {
            _lock.Release();
        }
    }
}
