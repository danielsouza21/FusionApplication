using FusionCacheApplication.Application.Services;
using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using FusionCacheApplication.Infrastructure.Database;
using FusionCacheApplication.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace FusionCacheApplication.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddFusionCacheService(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            var fusionConfigSection = configurationManager
                .GetSection(ConfigurationConstants.CONFIG_SECTION_FUSION)
                .Get<FusionCacheConfiguration>();

            fusionConfigSection ??= new FusionCacheConfiguration();

            var logger = services.BuildServiceProvider().GetService<ILogger<FusionCacheConfiguration>>();

            var fusionCacheBuilder = services
                .AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = fusionConfigSection.DefaultDuration,
                    JitterMaxDuration = fusionConfigSection.DefaultJitterMaxDuration,
                    IsFailSafeEnabled = fusionConfigSection.DefaultFailSafeEnabled,
                    FailSafeMaxDuration = fusionConfigSection.DefaultFailSafeMaxDuration,
                    FactorySoftTimeout = fusionConfigSection.DefaultFactorySoftTimeout,
                    FactoryHardTimeout = fusionConfigSection.DefaultFactoryHardTimeout,
                    EagerRefreshThreshold = fusionConfigSection.DefaultEagerRefreshThreshold,
                    Priority = fusionConfigSection.DefaultPriorityEnum,
                });

            var redisConnectionString = configurationManager.GetConnectionString(ConfigurationConstants.REDIS_FUSION_CONNECTION_NAME);

            if (fusionConfigSection.UseBackplaneDistributed && redisConnectionString is not null)
            {
                logger?.LogInformation("🔗 FUSION CACHE: Enabling Redis backplane with connection string");
                
                fusionCacheBuilder
                    .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
                    .WithBackplane(_ => new RedisBackplane(new RedisBackplaneOptions { Configuration = redisConnectionString }));

                if (fusionConfigSection.SetSystemTextJsonSerializer)
                    fusionCacheBuilder.WithSerializer(new FusionCacheSystemTextJsonSerializer());
            }
            else
            {
                logger?.LogInformation("🔗 FUSION CACHE: Using local cache only (no backplane)");
            }

            return services;
        }

        public static WebApplicationBuilder AddRedisWithAspire(this WebApplicationBuilder builder)
        {
            builder.AddRedisClient(connectionName: ConfigurationConstants.REDIS_FUSION_CONNECTION_NAME);
            return builder;
        }

        public static IFusionCacheBuilder AddBackplane(this IFusionCacheBuilder fusionCacheBuilder, string redisConnectionString)
        {
            fusionCacheBuilder
                .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .WithBackplane(_ => new RedisBackplane(new RedisBackplaneOptions { Configuration = redisConnectionString }));

            return fusionCacheBuilder;
        }

        public static WebApplicationBuilder AddDatabaseWithAspire(this WebApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<AppDbContext>(connectionName: ConfigurationConstants.DATABASE_FUSION_CONNECTION_NAME);
            return builder;
        }

        public static IServiceCollection AddDatabaseRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IDistributedErrorSimulationService, DistributedErrorSimulationService>();
            return services;
        }

        public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

            try
            {
                await migrationService.MigrateAsync();
                Console.WriteLine("Database migration completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying database migration: {ex.Message}");
            }
        }
    }
}
