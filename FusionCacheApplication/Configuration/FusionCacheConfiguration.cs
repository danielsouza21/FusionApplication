using Microsoft.Extensions.Caching.Memory;

namespace FusionCacheApplication.Configuration
{
    public class FusionCacheConfiguration
    {
        // Backplane Configuration
        public bool UseBackplaneDistributed { get; set; }
        
        // Default Duration Settings
        public int DefaultDurationMinutes { get; set; } = 2;
        public int DefaultDurationSeconds { get; set; } = 0;
        public int DefaultDurationMilliseconds { get; set; } = 0;
        
        // Jitter Settings
        public int DefaultJitterMaxDurationSeconds { get; set; } = 20;
        public int DefaultJitterMaxDurationMilliseconds { get; set; } = 0;
        
        // Fail-Safe Settings
        public bool DefaultFailSafeEnabled { get; set; } = true;
        public int DefaultFailSafeMaxDurationHours { get; set; } = 1;
        public int DefaultFailSafeMaxDurationMinutes { get; set; } = 0;
        
        // Factory Timeout Settings
        public int DefaultFactorySoftTimeoutMilliseconds { get; set; } = 150;
        public int DefaultFactoryHardTimeoutSeconds { get; set; } = 2;
        public int DefaultFactoryHardTimeoutMilliseconds { get; set; } = 0;
        
        // Eager Refresh Settings
        public float DefaultEagerRefreshThreshold { get; set; } = 0.15f;
        
        // Priority Settings
        public string DefaultPriority { get; set; } = "Normal"; // Low, Normal, High, NeverRemove
        
        // Serialization Settings
        public bool SetSystemTextJsonSerializer { get; set; } = true;
        
        // Computed Properties for TimeSpan conversion
        public TimeSpan DefaultDuration => TimeSpan.FromMinutes(DefaultDurationMinutes) 
            + TimeSpan.FromSeconds(DefaultDurationSeconds) 
            + TimeSpan.FromMilliseconds(DefaultDurationMilliseconds);
            
        public TimeSpan DefaultJitterMaxDuration => TimeSpan.FromSeconds(DefaultJitterMaxDurationSeconds) 
            + TimeSpan.FromMilliseconds(DefaultJitterMaxDurationMilliseconds);
            
        public TimeSpan DefaultFailSafeMaxDuration => TimeSpan.FromHours(DefaultFailSafeMaxDurationHours) 
            + TimeSpan.FromMinutes(DefaultFailSafeMaxDurationMinutes);
            
        public TimeSpan DefaultFactorySoftTimeout => TimeSpan.FromMilliseconds(DefaultFactorySoftTimeoutMilliseconds);
        
        public TimeSpan DefaultFactoryHardTimeout => TimeSpan.FromSeconds(DefaultFactoryHardTimeoutSeconds) 
            + TimeSpan.FromMilliseconds(DefaultFactoryHardTimeoutMilliseconds);
            
        public CacheItemPriority DefaultPriorityEnum => DefaultPriority.ToLower() switch
        {
            "low" => CacheItemPriority.Low,
            "normal" => CacheItemPriority.Normal,
            "high" => CacheItemPriority.High,
            "neverremove" => CacheItemPriority.NeverRemove,
            _ => CacheItemPriority.Normal
        };
    }
}
