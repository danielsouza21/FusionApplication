using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace FusionCacheApplication.Testing;

public class RedisHelper(string connectionString)
{
    private static readonly ILogger _logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Information))
        .CreateLogger("RedisHelper");

    public async Task<ConnectionMultiplexer> GetConnectionAsync()
    {
        _logger.LogInformation("🔗 REDIS: Connecting to Redis with connection string");
        return await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    public async Task RemoveKeyAsync(string key)
    {
        try
        {
            using var redis = await GetConnectionAsync();
            var db = redis.GetDatabase();

            var removed = await db.KeyDeleteAsync(key);
            _logger.LogInformation("🗑️ REDIS: Key {Key} removed: {Removed}", key, removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 REDIS: Failed to remove key {Key}", key);
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        try
        {
            using var redis = await GetConnectionAsync();
            var db = redis.GetDatabase();

            var exists = await db.KeyExistsAsync(key);
            _logger.LogInformation("🔍 REDIS: Key {Key} exists: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 REDIS: Failed to check if key {Key} exists", key);
            return false;
        }
    }

    public async Task<string?> GetValueAsync(string key)
    {
        try
        {
            using var redis = await GetConnectionAsync();
            var db = redis.GetDatabase();

            var value = await db.StringGetAsync(key);
            _logger.LogInformation("📖 REDIS: Retrieved value for key {Key}: {Value}", key, value.HasValue ? "exists" : "null");
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 REDIS: Failed to get value for key {Key}", key);
            return null;
        }
    }

    public async Task RemoveAllUserKeysAsync()
    {
        try
        {
            using var redis = await GetConnectionAsync();
            var db = redis.GetDatabase();
            var server = redis.GetServer(redis.GetEndPoints().First());

            var keys = server.Keys(pattern: "user:*").ToArray();
            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
                _logger.LogInformation("🗑️ REDIS: Removed {Count} user-related keys", keys.Length);
            }
            else
            {
                _logger.LogInformation("ℹ️ REDIS: No user-related keys found to remove");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 REDIS: Failed to remove all user keys");
        }
    }
}
