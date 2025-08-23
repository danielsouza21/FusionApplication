using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FusionCacheApplication.Infrastructure.Database.Repositories;

public sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    private readonly AppDbContext _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), ct);

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        var exists = await _db.Users.AnyAsync(x => x.Id == user.Id, ct);
        if (exists) _db.Users.Update(user);
        else await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return;
        _db.Users.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
