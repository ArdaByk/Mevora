using BenchmarkApp;
using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        // Benchmark çalıştır
        var summary = BenchmarkRunner.Run<DispatcherBenchmark>();
    }
}