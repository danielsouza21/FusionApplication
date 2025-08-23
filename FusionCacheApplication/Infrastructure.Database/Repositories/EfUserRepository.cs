using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FusionCacheApplication.Infrastructure.Database.Repositories;

public sealed class EfUserRepository(AppDbContext db, ILogger<EfUserRepository> logger) : IUserRepository
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<EfUserRepository> _logger = logger;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("🔍 DATABASE ACCESS: Getting user by ID {UserId}", id);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            stopwatch.Stop();

            if (user != null)
            {
                _logger.LogInformation("✅ DATABASE RESULT: User {UserId} found in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("❌ DATABASE RESULT: User {UserId} not found in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            }

            return user;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to get user {UserId} after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        _logger.LogInformation("🔍 DATABASE ACCESS: Getting user by email {Email}", email);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), ct);
            stopwatch.Stop();

            if (user != null)
            {
                _logger.LogInformation("✅ DATABASE RESULT: User with email {Email} found in {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("❌ DATABASE RESULT: User with email {Email} not found in {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            }

            return user;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to get user by email {Email} after {ElapsedMs}ms", email, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        _logger.LogInformation("💾 DATABASE ACCESS: Upserting user {UserId} with email {Email}", user.Id, user.Email);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var exists = await _db.Users.AnyAsync(x => x.Id == user.Id, ct);
            if (exists)
            {
                _logger.LogInformation("�� DATABASE OPERATION: Updating existing user {UserId}", user.Id);
                _db.Users.Update(user);
            }
            else
            {
                _logger.LogInformation("➕ DATABASE OPERATION: Creating new user {UserId}", user.Id);
                await _db.Users.AddAsync(user, ct);
            }

            await _db.SaveChangesAsync(ct);
            stopwatch.Stop();

            _logger.LogInformation("✅ DATABASE RESULT: User {UserId} upserted successfully in {ElapsedMs}ms", user.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to upsert user {UserId} after {ElapsedMs}ms", user.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ DATABASE ACCESS: Removing user {UserId}", id);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var entity = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
            {
                stopwatch.Stop();
                _logger.LogWarning("❌ DATABASE RESULT: User {UserId} not found for removal in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
                return;
            }

            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct);
            stopwatch.Stop();

            _logger.LogInformation("✅ DATABASE RESULT: User {UserId} removed successfully in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to remove user {UserId} after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}