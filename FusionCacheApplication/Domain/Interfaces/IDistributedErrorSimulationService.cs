using FusionCacheApplication.Domain.Models;

namespace FusionCacheApplication.Domain.Interfaces
{
    public interface IDistributedErrorSimulationService
    {
        Task<DistributedErrorSettings> GetSettingsAsync(CancellationToken ct = default);
        Task SetFailAsync(bool fail, string updatedBy = "system", CancellationToken ct = default);
        Task SetSlowAsync(int slowMs, string updatedBy = "system", CancellationToken ct = default);
        Task ResetAsync(string updatedBy = "system", CancellationToken ct = default);
        Task ApplyAsync(CancellationToken ct = default);
    }
}
