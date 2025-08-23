using FusionCacheApplication.Application.Services;
using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using FusionCacheApplication.Infrastructure.Database;
using FusionCacheApplication.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
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

            var fusionCacheBuilder = services
                .AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(2),
                    JitterMaxDuration = TimeSpan.FromSeconds(20),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FactorySoftTimeout = TimeSpan.FromMilliseconds(150),
                    FactoryHardTimeout = TimeSpan.FromSeconds(2),
                    EagerRefreshThreshold = (float)0.15
                });

            var redisConnectionString = configurationManager.GetConnectionString(ConfigurationConstants.REDIS_FUSION_CONNECTION_NAME);

            if (fusionConfigSection?.UseBackplaneDistributed ?? false && redisConnectionString is not null)
            {
                fusionCacheBuilder
                    .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
                    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                    .WithBackplane(_ => new RedisBackplane(new RedisBackplaneOptions { Configuration = redisConnectionString }));
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
            services.AddSingleton<ChaosSettings>();
            return services;
        }
    }
}
