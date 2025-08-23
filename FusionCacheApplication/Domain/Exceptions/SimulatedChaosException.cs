using System.Runtime.Serialization;

namespace FusionCacheApplication.Domain.Exceptions
{
    public class SimulatedChaosException : Exception
    {
        public SimulatedChaosException()
        {
        }

        public SimulatedChaosException(string? message) : base(message)
        {
        }

        public SimulatedChaosException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
