using FusionCacheApplication.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace FusionCacheApplication.Domain.Models;

public sealed class DistributedErrorSettings
{
    [JsonPropertyName("fail")]
    public bool Fail { get; set; } = false;

    [JsonPropertyName("slowMs")]
    public int SlowMs { get; set; } = 0;

    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updatedBy")]
    public string UpdatedBy { get; set; } = "system";

    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "unknown";

    public async Task ApplyAsync()
    {
        if (SlowMs > 0)
        {
            await Task.Delay(SlowMs);
        }

        if (Fail)
        {
            throw new SimulatedChaosException(
                $"Chaos: DB failure simulated by {UpdatedBy} at {LastUpdated:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public DistributedErrorSettings Clone()
    {
        return new DistributedErrorSettings
        {
            Fail = this.Fail,
            SlowMs = this.SlowMs,
            LastUpdated = this.LastUpdated,
            UpdatedBy = this.UpdatedBy,
            InstanceId = this.InstanceId
        };
    }
}