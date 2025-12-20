using DataUsageReporter.Core;
using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;

namespace DataUsageReporter;

/// <summary>
/// Quick console test for NetworkMonitor - run with: dotnet run --project src/DataUsageReporter -- --test
/// </summary>
public static class TestNetworkMonitor
{
    public static void Run()
    {
        Console.WriteLine("=== Network Monitor Test ===\n");

        // First, list all adapters to see what's detected
        Console.WriteLine("Detected network adapters:");
        Console.WriteLine(new string('-', 80));

        var result = GetIfTable2(out var table);
        if (result.Succeeded)
        {
            using (table)
            {
                int count = 0;
                foreach (var row in table)
                {
                    count++;
                    var status = row.OperStatus == IF_OPER_STATUS.IfOperStatusUp ? "UP" : row.OperStatus.ToString();
                    Console.WriteLine($"{count}. {row.Description}");
                    Console.WriteLine($"   Type: {row.Type} | Status: {status}");
                    Console.WriteLine($"   In: {FormatBytes(row.InOctets)} | Out: {FormatBytes(row.OutOctets)}");
                    Console.WriteLine();
                }
            }
        }
        else
        {
            Console.WriteLine($"Failed to get adapter table: {result}");
            return;
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("\nTesting NetworkMonitor speed calculation...\n");

        var monitor = new NetworkMonitor();
        var formatter = new SpeedFormatter();

        // Get initial stats
        var stats1 = monitor.GetCurrentStats();
        Console.WriteLine($"Initial stats: Down={FormatBytes(stats1.TotalBytesReceived)}, Up={FormatBytes(stats1.TotalBytesSent)}");

        // Wait and measure
        Console.WriteLine("\nWaiting 2 seconds to measure speed...");
        Thread.Sleep(2000);

        var speed = monitor.GetCurrentSpeed();
        Console.WriteLine($"\nSpeed reading:");
        Console.WriteLine($"  Download: {formatter.FormatSpeed(speed.DownloadBytesPerSecond)}");
        Console.WriteLine($"  Upload: {formatter.FormatSpeed(speed.UploadBytesPerSecond)}");

        // Get final stats
        var stats2 = monitor.GetCurrentStats();
        Console.WriteLine($"\nFinal stats: Down={FormatBytes(stats2.TotalBytesReceived)}, Up={FormatBytes(stats2.TotalBytesSent)}");

        var deltaDown = stats2.TotalBytesReceived - stats1.TotalBytesReceived;
        var deltaUp = stats2.TotalBytesSent - stats1.TotalBytesSent;
        Console.WriteLine($"Delta: Down={FormatBytes(deltaDown)}, Up={FormatBytes(deltaUp)}");

        Console.WriteLine("\n=== Test Complete ===");
    }

    private static string FormatBytes(ulong bytes)
    {
        return FormatBytes((long)bytes);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:F2} {units[unit]}";
    }
}
