using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheApplication.Application.Services;

public class UserService(IFusionCache cache, IUserRepository inner) : IUserService
{
    private readonly IFusionCache _cache = cache;
    private readonly IUserRepository _userRepository = inner;

    private const string UsersTag = "users";
    private static string KeyById(Guid id) => $"user:id:{id}";
    private static string KeyByEmail(string email) => $"user:email:{email.ToLower()}";

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _cache
            .GetOrSetAsync(
                KeyById(id),
                async _ => await _userRepository.GetByIdAsync(id, ct),
                opts => opts
                    .SetFailSafe(true) //TODO: set it as defauls
                    .SetDuration(TimeSpan.FromMinutes(2)),
                tags: [UsersTag], token: ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _cache
            .GetOrSetAsync(
                KeyByEmail(email),
                async _ => await _userRepository.GetByEmailAsync(email, ct),
                opts => opts
                    .SetFailSafe(true)
                    .SetDuration(TimeSpan.FromMinutes(2)),
                tags: [UsersTag], token: ct);
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default) //TODO: trigger tasks in parallel
    {
        await _userRepository.UpsertAsync(user, ct);

        await _cache.RemoveAsync(KeyById(user.Id), token: ct);
        await _cache.RemoveAsync(KeyByEmail(user.Email), token: ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        await _userRepository.RemoveAsync(id, ct);

        await _cache.RemoveAsync(KeyById(id), token: ct);
    }
}
