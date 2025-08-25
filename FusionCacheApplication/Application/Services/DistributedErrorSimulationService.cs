using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheApplication.Application.Services;

public class DistributedErrorSimulationService(IFusionCache cache, ILogger<DistributedErrorSimulationService> logger) : IDistributedErrorSimulationService
{
    private readonly IFusionCache _cache = cache;
    private readonly ILogger<DistributedErrorSimulationService> _logger = logger;

    private readonly string _instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? 
        Environment.MachineName;

    private const string ErrorSettingsKey = "distributed:error:settings";
    private const string ErrorSettingsTag = "error-simulation";
    private const int DURATION_CACHE_MINUTES = 5;

    public async Task<DistributedErrorSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🔍 ERROR SIMULATION: Getting distributed error settings from cache");

        var settings = await _cache.GetOrSetAsync(
            ErrorSettingsKey,
            async _ =>
            {
                _logger.LogInformation("🔄 ERROR SIMULATION: Settings not in cache, creating default");
                return new DistributedErrorSettings();
            },
            opts =>
                opts
                .SetDuration(TimeSpan.FromMinutes(DURATION_CACHE_MINUTES))
                .SetEagerRefresh(null),
            tags: [ErrorSettingsTag],
            token: ct);

        _logger.LogInformation("✅ ERROR SIMULATION: Retrieved settings - Fail: {Fail}, SlowMs: {SlowMs}, LastUpdated: {LastUpdated}",
            settings.Fail, settings.SlowMs, settings.LastUpdated);

        return settings;
    }

    public async Task SetFailAsync(bool fail, string updatedBy = "system", CancellationToken ct = default)
    {
        _logger.LogInformation("⚙️ ERROR SIMULATION: Setting Fail to {Fail} by {UpdatedBy}", fail, updatedBy);

        var settings = await GetSettingsAsync(ct);
        settings.Fail = fail;
        settings.LastUpdated = DateTimeOffset.UtcNow;
        settings.UpdatedBy = updatedBy;
        settings.InstanceId = _instanceId;

        await _cache.SetAsync(
            ErrorSettingsKey,
            settings,
            opts => 
                opts
                .SetDuration(TimeSpan.FromMinutes(DURATION_CACHE_MINUTES))
                .SetEagerRefresh(null),
            tags: [ErrorSettingsTag],
            token: ct);

        _logger.LogInformation("✅ ERROR SIMULATION: Fail setting updated to {Fail} by {UpdatedBy} on instance {InstanceId}",
            fail, updatedBy, _instanceId);
    }

    public async Task SetSlowAsync(int slowMs, string updatedBy = "system", CancellationToken ct = default)
    {
        _logger.LogInformation("⚙️ ERROR SIMULATION: Setting SlowMs to {SlowMs} by {UpdatedBy}", slowMs, updatedBy);

        var settings = await GetSettingsAsync(ct);
        settings.SlowMs = slowMs;
        settings.LastUpdated = DateTimeOffset.UtcNow;
        settings.UpdatedBy = updatedBy;
        settings.InstanceId = _instanceId;

        await _cache.SetAsync(
            ErrorSettingsKey,
            settings,
            opts =>
                opts
                .SetDuration(TimeSpan.FromMinutes(DURATION_CACHE_MINUTES))
                .SetEagerRefresh(null),
            tags: [ErrorSettingsTag],
            token: ct);

        _logger.LogInformation("✅ ERROR SIMULATION: SlowMs setting updated to {SlowMs} by {UpdatedBy} on instance {InstanceId}",
            slowMs, updatedBy, _instanceId);
    }

    public async Task ResetAsync(string updatedBy = "system", CancellationToken ct = default)
    {
        _logger.LogInformation("🔄 ERROR SIMULATION: Resetting all settings by {UpdatedBy}", updatedBy);

        var settings = new DistributedErrorSettings
        {
            LastUpdated = DateTimeOffset.UtcNow,
            UpdatedBy = updatedBy,
            InstanceId = _instanceId
        };

        await _cache.SetAsync(
            ErrorSettingsKey,
            settings,
            opts =>
                opts
                .SetDuration(TimeSpan.FromMinutes(DURATION_CACHE_MINUTES))
                .SetEagerRefresh(null),
            tags: [ErrorSettingsTag],
            token: ct);

        _logger.LogInformation("✅ ERROR SIMULATION: Settings reset by {UpdatedBy} on instance {InstanceId}", updatedBy, _instanceId);
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        var startTimestamp = Stopwatch.GetTimestamp();

        _logger.LogInformation("🎭 ERROR SIMULATION: Applying settings - Fail: {Fail}, SlowMs: {SlowMs} on instance {InstanceId}",
            settings.Fail, settings.SlowMs, _instanceId);

        await settings.ApplyAsync();

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _logger.LogInformation("✅ ERROR SIMULATION: Settings applied successfully on instance {InstanceId} after {elapsedMilliseconds}ms", _instanceId, elapsedMilliseconds);
    }
}