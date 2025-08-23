using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FusionCacheApplication.Infrastructure.Database;

public interface IDatabaseMigrationService
{
    Task MigrateAsync();
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly string _connectionString;

    public DatabaseMigrationService(AppDbContext dbContext, ILogger<DatabaseMigrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _connectionString = _dbContext.Database.GetConnectionString() ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task MigrateAsync()
    {
        try
        {
            _logger.LogInformation("Starting database migration...");

            // Check if database exists, if not create it
            await EnsureDatabaseExistsAsync();

            // Create users table if it doesn't exist
            await CreateUsersTableIfNotExistsAsync();

            _logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database migration");
            throw;
        }
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        // Extract database name from connection string
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.Database;
        
        // Create connection string without database name (to connect to postgres database)
        builder.Database = "postgres";
        var masterConnectionString = builder.ToString();

        using var connection = new NpgsqlConnection(masterConnectionString);
        await connection.OpenAsync();

        // Check if database exists
        var databaseExists = await CheckDatabaseExistsAsync(connection, databaseName);
        
        if (!databaseExists)
        {
            _logger.LogInformation("Creating database: {DatabaseName}", databaseName);
            await CreateDatabaseAsync(connection, databaseName);
        }
    }

    private async Task<bool> CheckDatabaseExistsAsync(NpgsqlConnection connection, string databaseName)
    {
        var sql = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@databaseName", databaseName);
        
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task CreateDatabaseAsync(NpgsqlConnection connection, string databaseName)
    {
        var sql = $"CREATE DATABASE \"{databaseName}\"";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateUsersTableIfNotExistsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS users (
                ""Id"" UUID PRIMARY KEY,
                ""Username"" VARCHAR(64) NOT NULL,
                ""Email"" VARCHAR(256) NOT NULL UNIQUE,
                ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ""IX_users_Email"" ON users (""Email"");";

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Users table created/verified successfully.");
    }
}
