using System.Diagnostics;

namespace FusionCacheApplication.Domain
{
    public static class StopWatchUtils
    {
        public static double GetElapsedMilliseconds(this long startTimestamp)
        {
            return Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        }
    }
}
