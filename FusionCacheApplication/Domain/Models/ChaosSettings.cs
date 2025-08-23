namespace FusionCacheApplication.Domain.Models;

public sealed class ChaosSettings
{
    public bool Fail { get; set; }
    public int SlowMs { get; set; }
    public void Apply()
    {
        if (SlowMs > 0) Thread.Sleep(SlowMs);
        if (Fail) throw new InvalidOperationException("Chaos: DB failure simulated");
    }
}
