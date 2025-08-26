using FusionCacheApplication.Domain;
using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;
using Microsoft.Extensions.DependencyInjection;

namespace FusionCacheApplication.Application.Services;

public class UserService : IUserService
{
    private readonly IFusionCache _cache;
    private readonly IUserRepository _userRepository;
    private readonly IDistributedErrorSimulationService _errorService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IFusionCache cache,
        IUserRepository inner,
        IDistributedErrorSimulationService errorService,
        ILogger<UserService> logger)
    {
        _cache = cache;
        _userRepository = inner;
        _errorService = errorService;
        _logger = logger;
    }

    private const string UsersTag = "users";
    private static string KeyById(Guid id) => $"user:id:{id}";
    private static string KeyByEmail(string email) => $"user:email:{email.ToLower()}";

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = KeyById(id);
        _logger.LogInformation("🔍 CACHE LOOKUP: Checking cache for user {UserId} with key {CacheKey}", id, cacheKey);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var user = await _cache
                .GetOrSetAsync(
                    cacheKey,
                    async ct =>
                    {
                        _logger.LogInformation("�� CACHE MISS: User {UserId} not in cache, fetching from database", id);
                        await _errorService.ApplyAsync(ct);
                        return await _userRepository.GetByIdAsync(id, ct);
                    },
                    tags: [UsersTag],
                    token: ct);

            if (user != null)
            {
                _logger.LogInformation("✅ CACHE RESULT: User {UserId} retrieved in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            }
            else
            {
                _logger.LogWarning("❌ CACHE RESULT: User {UserId} not found in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to get user {UserId} after {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var cacheKey = KeyByEmail(email);
        _logger.LogInformation("🔍 CACHE LOOKUP: Checking cache for user with email {Email} with key {CacheKey}", email, cacheKey);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var user = await _cache
                .GetOrSetAsync(
                    cacheKey,
                    async ct =>
                    {
                        _logger.LogInformation("🔄 CACHE MISS: User with email {Email} not in cache, fetching from database", email);
                        await _errorService.ApplyAsync(ct);
                        return await _userRepository.GetByEmailAsync(email, ct);
                    },
                    tags: [UsersTag],
                    token: ct);

            if (user != null)
            {
                _logger.LogInformation("✅ CACHE RESULT: User with email {Email} retrieved in {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            }
            else
            {
                _logger.LogWarning("❌ CACHE RESULT: User with email {Email} not found in {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to get user by email {Email} after {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        _logger.LogInformation("💾 CACHE OPERATION: Upserting user {UserId}, will invalidate cache entries", user.Id);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await _userRepository.UpsertAsync(user, ct);

            // Invalidate cache entries
            var idKey = KeyById(user.Id);
            var emailKey = KeyByEmail(user.Email);

            _logger.LogInformation("🗑️ CACHE INVALIDATION: Removing cache entries for user {UserId}", user.Id);
            await _cache.RemoveAsync(idKey, token: ct);
            await _cache.RemoveAsync(emailKey, token: ct);

            _logger.LogInformation("✅ CACHE OPERATION: User {UserId} upserted and cache invalidated in {ElapsedMs}ms", user.Id, startTimestamp.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to upsert user {UserId} after {ElapsedMs}ms", user.Id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ CACHE OPERATION: Removing user {UserId}, will invalidate cache entries", id);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await _userRepository.RemoveAsync(id, ct);

            // Invalidate cache entry
            var idKey = KeyById(id);
            _logger.LogInformation("🗑️ CACHE INVALIDATION: Removing cache entry for user {UserId}", id);
            await _cache.RemoveAsync(idKey, token: ct);
            _logger.LogInformation("✅ CACHE OPERATION: User {UserId} removed and cache invalidated in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            _logger.LogError(ex, "💥 CACHE ERROR: Failed to remove user {UserId} after {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task InvalidateCacheForUsersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ CACHE OPERATION: Invalidating all users cache for tag {Tag}", UsersTag);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await _cache.RemoveByTagAsync(UsersTag, token: ct);
            _logger.LogInformation("✅ CACHE OPERATION: Invalidated all users cache for tag {Tag} in {ElapsedMs}ms", UsersTag, startTimestamp.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 CACHE ERROR: Error invalidating cache for users for tag {Tag} after {ElapsedMs}ms", UsersTag, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }
}