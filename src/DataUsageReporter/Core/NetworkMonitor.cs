using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;

namespace DataUsageReporter.Core;

/// <summary>
/// Monitors network usage using Windows IP Helper API (GetIfTable2).
/// Aggregates statistics across all physical network adapters.
/// </summary>
public class NetworkMonitor : INetworkMonitor
{
    private NetworkStats? _previousStats;
    private NetworkStats? _currentStats;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the last captured stats without fetching new data.
    /// Use this after calling GetCurrentSpeed() to avoid double-fetching.
    /// </summary>
    public NetworkStats? LastStats => _currentStats;

    public NetworkStats GetCurrentStats()
    {
        lock (_lock)
        {
            long totalReceived = 0;
            long totalSent = 0;

            try
            {
                var result = GetIfTable2(out var table);
                if (result.Succeeded)
                {
                    using (table)
                    {
                        foreach (var row in table)
                        {
                            // Only include physical adapters (Ethernet, Wi-Fi)
                            // Skip loopback, tunnel, and software interfaces
                            if (IsPhysicalAdapter(row))
                            {
                                totalReceived += (long)row.InOctets;
                                totalSent += (long)row.OutOctets;
                            }
                        }
                    }
                }
            }
            catch
            {
                // On error, return last known values or zeros
                if (_currentStats != null)
                {
                    return _currentStats with { Timestamp = DateTime.Now };
                }
            }

            // Shift current to previous, store new as current
            _previousStats = _currentStats;
            _currentStats = new NetworkStats(totalReceived, totalSent, DateTime.Now);

            return _currentStats;
        }
    }

    public SpeedReading GetCurrentSpeed()
    {
        lock (_lock)
        {
            // Get fresh stats (this updates _previousStats and _currentStats)
            var currentStats = GetCurrentStats();

            // Need previous stats to calculate speed
            if (_previousStats == null)
            {
                return new SpeedReading(0, 0, currentStats.Timestamp);
            }

            var timeDelta = (currentStats.Timestamp - _previousStats.Timestamp).TotalSeconds;
            if (timeDelta <= 0)
            {
                return new SpeedReading(0, 0, currentStats.Timestamp);
            }

            var downloadDelta = currentStats.TotalBytesReceived - _previousStats.TotalBytesReceived;
            var uploadDelta = currentStats.TotalBytesSent - _previousStats.TotalBytesSent;

            // Handle counter reset (e.g., adapter reconnect)
            if (downloadDelta < 0) downloadDelta = 0;
            if (uploadDelta < 0) uploadDelta = 0;

            var downloadSpeed = (long)(downloadDelta / timeDelta);
            var uploadSpeed = (long)(uploadDelta / timeDelta);

            return new SpeedReading(downloadSpeed, uploadSpeed, currentStats.Timestamp);
        }
    }

    private static bool IsPhysicalAdapter(MIB_IF_ROW2 row)
    {
        // Exclude loopback and tunnel interfaces
        if (row.Type == IFTYPE.IF_TYPE_SOFTWARE_LOOPBACK ||
            row.Type == IFTYPE.IF_TYPE_TUNNEL)
        {
            return false;
        }

        // Only include adapters that are operational (connected)
        if (row.OperStatus != IF_OPER_STATUS.IfOperStatusUp)
        {
            return false;
        }

        // Exclude filter/lightweight filter layers (they duplicate traffic counts)
        // These contain "-" in their description indicating they're filter layers
        var desc = row.Description ?? "";
        if (desc.Contains("-WFP") || desc.Contains("-QoS") || desc.Contains("Filter"))
        {
            return false;
        }

        // Include adapters with actual traffic
        return row.InOctets > 0 || row.OutOctets > 0;
    }
}
