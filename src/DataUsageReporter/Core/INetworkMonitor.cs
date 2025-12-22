namespace DataUsageReporter.Core;

/// <summary>
/// Responsible for reading network statistics from the operating system.
/// </summary>
public interface INetworkMonitor
{
    /// <summary>
    /// Gets current network statistics for all physical adapters.
    /// This updates internal state and shifts previous/current readings.
    /// </summary>
    /// <returns>Aggregate statistics across all adapters</returns>
    NetworkStats GetCurrentStats();

    /// <summary>
    /// Gets the current download and upload speeds.
    /// This internally calls GetCurrentStats().
    /// </summary>
    /// <returns>Speed in bytes per second</returns>
    SpeedReading GetCurrentSpeed();

    /// <summary>
    /// Gets the last captured stats without fetching new data.
    /// Use after GetCurrentSpeed() to avoid double-fetching.
    /// </summary>
    NetworkStats? LastStats { get; }
}
