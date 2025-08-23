using FusionCacheApplication.Domain.Models;

namespace FusionCacheApplication.Domain.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task UpsertAsync(User user, CancellationToken ct = default);
        Task RemoveAsync(Guid id, CancellationToken ct = default);
    }
}
