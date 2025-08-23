using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheApplication.Application.Services;

public class UserService(IFusionCache cache, IUserRepository inner, ILogger<UserService> logger) : IUserService
{
    private readonly IFusionCache _cache = cache;
    private readonly IUserRepository _userRepository = inner;
    private readonly ILogger<UserService> _logger = logger;

    private const string UsersTag = "users";
    private static string KeyById(Guid id) => $"user:id:{id}";
    private static string KeyByEmail(string email) => $"user:email:{email.ToLower()}";

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = KeyById(id);
        _logger.LogInformation("🔍 CACHE LOOKUP: Checking cache for user {UserId} with key {CacheKey}", id, cacheKey);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _cache
                .GetOrSetAsync(
                    cacheKey,
                    async _ =>
                    {
                        _logger.LogInformation("�� CACHE MISS: User {UserId} not in cache, fetching from database", id);
                        return await _userRepository.GetByIdAsync(id, ct);
                    },
                    tags: [UsersTag],
                    token: ct);

            stopwatch.Stop();

            if (user != null)
            {
                _logger.LogInformation("✅ CACHE RESULT: User {UserId} retrieved in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("❌ CACHE RESULT: User {UserId} not found in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            }

            return user;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to get user {UserId} after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var cacheKey = KeyByEmail(email);
        _logger.LogInformation("🔍 CACHE LOOKUP: Checking cache for user with email {Email} with key {CacheKey}", email, cacheKey);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _cache
                .GetOrSetAsync(
                    cacheKey,
                    async _ =>
                    {
                        _logger.LogInformation("🔄 CACHE MISS: User with email {Email} not in cache, fetching from database", email);
                        return await _userRepository.GetByEmailAsync(email, ct);
                    },
                    tags: [UsersTag],
                    token: ct);

            stopwatch.Stop();

            if (user != null)
            {
                _logger.LogInformation("✅ CACHE RESULT: User with email {Email} retrieved in {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("❌ CACHE RESULT: User with email {Email} not found in {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            }

            return user;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to get user by email {Email} after {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        _logger.LogInformation("💾 CACHE OPERATION: Upserting user {UserId}, will invalidate cache entries", user.Id);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _userRepository.UpsertAsync(user, ct);

            // Invalidate cache entries
            var idKey = KeyById(user.Id);
            var emailKey = KeyByEmail(user.Email);

            _logger.LogInformation("🗑️ CACHE INVALIDATION: Removing cache entries for user {UserId}", user.Id);
            await _cache.RemoveAsync(idKey, token: ct);
            await _cache.RemoveAsync(emailKey, token: ct);

            stopwatch.Stop();
            _logger.LogInformation("✅ CACHE OPERATION: User {UserId} upserted and cache invalidated in {ElapsedMs}ms", user.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to upsert user {UserId} after {ElapsedMs}ms", user.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ CACHE OPERATION: Removing user {UserId}, will invalidate cache entries", id);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _userRepository.RemoveAsync(id, ct);

            // Invalidate cache entry
            var idKey = KeyById(id);
            _logger.LogInformation("🗑️ CACHE INVALIDATION: Removing cache entry for user {UserId}", id);
            await _cache.RemoveAsync(idKey, token: ct);

            stopwatch.Stop();
            _logger.LogInformation("✅ CACHE OPERATION: User {UserId} removed and cache invalidated in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to remove user {UserId} after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task InvalidateCacheForUsersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ CACHE OPERATION: Invalidating all users cache for tag {Tag}", UsersTag);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _cache.RemoveByTagAsync(UsersTag, token: ct);

            stopwatch.Stop();
            _logger.LogInformation("✅ CACHE OPERATION: Invalidated all users cache for tag {Tag} in {ElapsedMs}ms", UsersTag, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 CACHE ERROR: Error invalidating cache for users for tag {Tag} after {ElapsedMs}ms", UsersTag, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}