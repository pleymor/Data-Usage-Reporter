namespace DataUsageReporter.Core;

/// <summary>
/// Responsible for reading network statistics from the operating system.
/// </summary>
public interface INetworkMonitor
{
    /// <summary>
    /// Gets current network statistics for all physical adapters.
    /// </summary>
    /// <returns>Aggregate statistics across all adapters</returns>
    NetworkStats GetCurrentStats();

    /// <summary>
    /// Gets the current download and upload speeds.
    /// </summary>
    /// <returns>Speed in bytes per second</returns>
    SpeedReading GetCurrentSpeed();
}
