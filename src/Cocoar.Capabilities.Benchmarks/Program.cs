using BenchmarkDotNet.Running;

namespace Cocoar.Capabilities.Benchmarks;

/// <summary>
/// Entry point for running performance benchmarks.
/// Usage: dotnet run --configuration Release
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromTypes([typeof(CapabilityBenchmarks)]);

        if (args.Length == 0)
        {
            Console.WriteLine("🎯 Cocoar.Capabilities Performance Benchmarks");
            Console.WriteLine("==============================================");
            Console.WriteLine();
            Console.WriteLine("📊 Quick examples:");
            Console.WriteLine("   dotnet run -c Release -- --anyCategories Summary,Env");
            Console.WriteLine("   dotnet run -c Release -- --filter \"*LookupSmallBag*\"");
            Console.WriteLine("   dotnet run -c Release -- --filter \"*CreateBag_1000Capabilities*\"");
            Console.WriteLine();
            Console.WriteLine("📈 Running all benchmarks (no filter)...");
        }

        switcher.Run(args);
    }
}
