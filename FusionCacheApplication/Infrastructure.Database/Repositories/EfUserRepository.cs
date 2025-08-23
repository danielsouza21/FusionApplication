using FusionCacheApplication.Domain;
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

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

            if (user != null)
            {
                _logger.LogInformation("✅ DATABASE RESULT: User {UserId} found in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            }
            else
            {
                _logger.LogWarning("❌ DATABASE RESULT: User {UserId} not found in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to get user {UserId} after {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        _logger.LogInformation("🔍 DATABASE ACCESS: Getting user by email {Email}", email);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), ct);


            if (user != null)
            {
                _logger.LogInformation("✅ DATABASE RESULT: User with email {Email} found in {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            }
            else
            {
                _logger.LogWarning("❌ DATABASE RESULT: User with email {Email} not found in {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to get user by email {Email} after {ElapsedMs}ms", email, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        _logger.LogInformation("💾 DATABASE ACCESS: Upserting user {UserId} with email {Email}", user.Id, user.Email);

        var startTimestamp = Stopwatch.GetTimestamp();
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

            _logger.LogInformation("✅ DATABASE RESULT: User {UserId} upserted successfully in {ElapsedMs}ms", user.Id, startTimestamp.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to upsert user {UserId} after {ElapsedMs}ms", user.Id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("🗑️ DATABASE ACCESS: Removing user {UserId}", id);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var entity = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
            {
                _logger.LogWarning("❌ DATABASE RESULT: User {UserId} not found for removal in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
                return;
            }

            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("✅ DATABASE RESULT: User {UserId} removed successfully in {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 DATABASE ERROR: Failed to remove user {UserId} after {ElapsedMs}ms", id, startTimestamp.GetElapsedMilliseconds());
            throw;
        }
    }
}